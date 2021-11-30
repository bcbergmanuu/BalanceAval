using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BalanceAval.Models;
using BalanceAval.ViewModels;
using NationalInstruments;
using NationalInstruments.DAQmx;
using Stateless;
using Task = System.Threading.Tasks.Task;

namespace BalanceAval.Service
{


    public class ReadNidaq : IReadNidaq
    {
        public const double MultiplicationFactor = 25.00;
        public const int Buffersize = 10;
        public const double Frequency = 100;

        public static readonly Dictionary<string, string> Channels;
        private volatile bool _stop = false;

        private readonly StateMachine<NidaqStates, NidaqTriggers> _stateMachine = new(NidaqStates.Stopped);
        private enum NidaqTriggers
        {
            Start,
            Stop,
            Calibrate,
            Calibrated
        }
        public enum NidaqStates
        {
            Running,
            Stopped,
            Calibrating
        }

        public ReadNidaq()
        {

            _stateMachine.Configure(NidaqStates.Running).OnEntry(() =>
                {
                    _stop = false;

                    StartStream(false);
                })
                .Permit(NidaqTriggers.Stop, NidaqStates.Stopped);


            _stateMachine.Configure(NidaqStates.Stopped).OnEntry(() =>
                {
                    _stop = true;
                })
                .Permit(NidaqTriggers.Start, NidaqStates.Running)
                .Permit(NidaqTriggers.Calibrate, NidaqStates.Calibrating);
            _stateMachine.Configure(NidaqStates.Calibrating).OnEntry(() =>
            {
                StartStream(true);
            }).Permit(NidaqTriggers.Calibrated, NidaqStates.Stopped)
                .Permit(NidaqTriggers.Stop, NidaqStates.Stopped);
        }


        static ReadNidaq()
        {
            Channels = new[] { "Z1", "Z2", "Z3", "Z4", "X2", "X1", "Y" }
                .Select((n, i) => new KeyValuePair<string, string>("Dev1/ai" + (i + 1), n))
                .ToDictionary(x => x.Key, x => x.Value); ;

            _calibration = new double[Channels.Count];
        }


        public event EventHandler CalibrationFinished;

        public async void Start()
        {
            await _stateMachine.FireAsync(NidaqTriggers.Start);
        }

        private async void StartStream(bool calibrate)
        {
            // Create a new task
            try
            {
                var running = await CreateNidaqTask();


                AnalogMultiChannelReader analogInReader = new(running.Stream)
                {
                    // marshals callbacks across threads appropriately.
                    // Use SynchronizeCallbacks to specify that the object 
                    SynchronizeCallbacks = true
                };


                running.Timing.ConfigureSampleClock("", Frequency, SampleClockActiveEdge.Rising,
                                  SampleQuantityMode.ContinuousSamples, Buffersize);

                running.Control(TaskAction.Verify);

                var data = await BeginRead(analogInReader, running);
                if (calibrate)
                {
                    CalibrateFinished(SamplesToModel(data));
                    return;
                }
                OnDataReceived(SamplesToModel(data));

                while (true)
                {
                    if (_stop)
                    {
                        running.Stop();
                        break;
                    }

                    data = await ContinueRead(data, analogInReader, running);

                    OnDataReceived(SamplesToModel(data));
                }

                // Verify the Task


            }
            catch (DaqException e)
            {
                OnError(e.Message);
            }
        }

        private static Task<AnalogWaveform<double>[]> ContinueRead(AnalogWaveform<double>[] data, AnalogMultiChannelReader analogInReader, NationalInstruments.DAQmx.Task running)
        {
            var tcs = new TaskCompletionSource<AnalogWaveform<double>[]>();

            analogInReader.BeginMemoryOptimizedReadWaveform(Buffersize,
                ar =>
                {
                    try
                    {

                        tcs.SetResult(analogInReader.EndReadWaveform(ar));
                    }
                    catch (NationalInstruments.DAQmx.DaqException exception)
                    {
                        tcs.SetException(exception);
                    }
                    // Read the available data from the channels

                }, running, data);
            return tcs.Task;
        }

        private static Task<AnalogWaveform<double>[]> BeginRead(AnalogMultiChannelReader analogInReader, NationalInstruments.DAQmx.Task running)
        {
            var tcs = new TaskCompletionSource<AnalogWaveform<double>[]>();
            analogInReader.BeginReadWaveform(Buffersize, ar =>
            {
                try
                {

                    tcs.SetResult(analogInReader.EndReadWaveform(ar));
                }
                catch (NationalInstruments.DAQmx.DaqException exception)
                {
                    tcs.SetException(exception);

                }
                // Read the available data from the channels

            }, running);

            return tcs.Task;
        }


        private static Task<NationalInstruments.DAQmx.Task> CreateNidaqTask()
        {
            var tcs = new TaskCompletionSource<NationalInstruments.DAQmx.Task>();

            try
            {
                NationalInstruments.DAQmx.Task nidaqtask = new NationalInstruments.DAQmx.Task();

                // Create a virtual channels
                foreach (var channel in Channels)
                {
                    nidaqtask.AIChannels.CreateVoltageChannel(channel.Key, "", (AITerminalConfiguration)(-1), 0.0,
                        10.0, AIVoltageUnits.Volts);
                }
                tcs.SetResult(nidaqtask);
            }

            catch (DaqException e)
            {
                tcs.SetException(e);
            }

            return tcs.Task;
        }



        public async void Stop()
        {
            try
            {
                await _stateMachine.FireAsync(NidaqTriggers.Stop);
            }
            catch
            {
                //suppress this
            }
        }

        public event EventHandler<string> Error;

        private static double[] _calibration;
        public async void CalibrateAsync()
        {
            ResetCalibration();
            await _stateMachine.FireAsync(NidaqTriggers.Calibrate);
        }

        private async void CalibrateFinished(IEnumerable<AnalogChannel> channels)
        {
            IEnumerable<double> averages = channels.Select(analogChannel => analogChannel.Values.Average());
            _calibration = averages.ToArray();
            await _stateMachine.FireAsync(NidaqTriggers.Calibrated);
            OnCalibrationFinished();
        }

        private static void ResetCalibration()
        {
            _calibration = Channels.Select(n => 0.0).ToArray();
        }

        public event EventHandler<IEnumerable<AnalogChannel>> DataReceived;


        private static IEnumerable<AnalogChannel> SamplesToModel(IEnumerable<AnalogWaveform<double>> samples)
        {
            var channels = new List<AnalogChannel>();
            foreach (var channelName in samples)
            {
                var channel = new AnalogChannel()
                {
                    NiInput = channelName.ChannelName,
                    Values = new List<double>()
                };

                channels.Add(channel);

                foreach (var analogWaveformSample in channelName.Samples)
                {
                    channel.Values.Add(analogWaveformSample.Value);
                }
            }

            return channels.Select((i,n)=> AdjuctCalibrate(n,i));
        }

        private static AnalogChannel AdjuctCalibrate(int index, AnalogChannel channel)
        {
            return new AnalogChannel
            {
                Values = channel.Values.Select(n => n - _calibration[index]).ToList(),
                NiInput = channel.NiInput
            };
        }


        protected virtual void OnDataReceived(IEnumerable<AnalogChannel> e)
        {
            DataReceived?.Invoke(this, e);
        }

        protected virtual void OnError(string e)
        {
            _stateMachine.Fire(NidaqTriggers.Stop);
            Error?.Invoke(this, e);
        }

        protected virtual void OnCalibrationFinished()
        {
            CalibrationFinished?.Invoke(this, EventArgs.Empty);
        }
    }
}

