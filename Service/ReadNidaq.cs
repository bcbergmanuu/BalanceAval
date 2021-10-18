using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using BalanceAval.Models;
using NationalInstruments;
using NationalInstruments.DAQmx;
using Task = System.Threading.Tasks.Task;

namespace BalanceAval.Service
{
    public readonly struct Channel
    {
        public Channel(string niInput, string name)
        {
            NiInput = niInput;
            Name = name;
        }
        public string NiInput { get; }
        public string Name { get; }
    }



    public class ReadNidaq : IReadNidaq
    {
        public const double MultiplicationFactor = 25.00;
        public const int Buffersize = 50;
        public const double Frequency = 100;

        public static readonly IList<Channel> ChannelNames;
        public static readonly string[] ChannelValues = { "Z1", "Z4", "Z2", "Z3", "Y", "X2", "X1" }; //note these refer to object properties also
        private NationalInstruments.DAQmx.Task running;
        private AnalogMultiChannelReader analogInReader;

        static ReadNidaq()
        {
            ChannelNames = new List<Channel>(ChannelValues.OrderBy(n => n).Select((n, i) => new Channel("Dev1/ai" + i, n)));
        }

        public async void Start()
        {
            // Create a new task
            try
            {
                Stop();
                running = await CreateNidaqTask();


                analogInReader = new AnalogMultiChannelReader(running.Stream)
                {
                    // marshals callbacks across threads appropriately.
                    // Use SynchronizeCallbacks to specify that the object 
                    SynchronizeCallbacks = true
                };


                analogInReader.BeginReadWaveform(Buffersize, AnalogInCallback, running);
                running.Timing.ConfigureSampleClock("", Frequency, SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples, Buffersize);

                // Verify the Task


                running.Control(TaskAction.Verify);
            }
            catch (DaqException e)
            {
                OnError(e.Message);
            }
        }

        private static Task<NationalInstruments.DAQmx.Task> CreateNidaqTask()
        {
            var tcs = new TaskCompletionSource<NationalInstruments.DAQmx.Task>();

            NationalInstruments.DAQmx.Task nidaqtask;
            try
            {
                nidaqtask = new NationalInstruments.DAQmx.Task();

                // Create a virtual channels
                foreach (var channel in ChannelNames)
                {
                    nidaqtask.AIChannels.CreateVoltageChannel(channel.NiInput, "", (AITerminalConfiguration)(-1), 0.0,
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



        public void Stop()
        {
            try
            {
                running.Stop();
            }
            catch
            {
                //suppress this
            }
        }

        public event EventHandler<string> Error;

        public event EventHandler<List<AnalogChannel>> DataReceived;

        private void AnalogInCallback(IAsyncResult ar)
        {
            AnalogWaveform<double>[]? data;

            try
            {
                if (running.IsDone)
                    return;
                data = analogInReader.EndReadWaveform(ar);
            }
            catch (NationalInstruments.DAQmx.DaqException exception)
            {
                OnError(exception.Message);
                return;
            }
            // Read the available data from the channels

            var model = SamplesToModel(data);
            OnDataReceived(model);

            analogInReader.BeginMemoryOptimizedReadWaveform(Buffersize,
                AnalogInCallback, running, data);
        }

        private static List<AnalogChannel> SamplesToModel(IEnumerable<AnalogWaveform<double>> samples)
        {
            var channels = new List<AnalogChannel>();
            foreach (var channelName in samples)
            {
                var channel = new AnalogChannel()
                {
                    Name = channelName.ChannelName,
                    Values = new List<double>()
                };

                channels.Add(channel);

                foreach (var analogWaveformSample in channelName.Samples)
                {
                    channel.Values.Add(analogWaveformSample.Value);
                }
            }

            return channels;
        }

        protected virtual void OnDataReceived(List<AnalogChannel> e)
        {
            DataReceived?.Invoke(this, e);
        }

        protected virtual void OnError(string e)
        {
            Error?.Invoke(this, e);
        }
    }
}

