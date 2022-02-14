using System;
using System.Collections.Generic;
using System.Linq;
using BalanceAval.Models;
using Stateless;
using System.IO.Ports;
using System.Reflection;

namespace BalanceAval.Service
{

    public class ReadSerial : IReadNidaq
    {
        private volatile bool _stop = false;
        private const int buffer = 10;
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

        private static SerialPort _serialPort;

        private SerialPort SerialPortFactory()
        {
            return new SerialPort
            {
                BaudRate = 9600,
                PortName = "COM3",
                ReadTimeout = 60,
                WriteTimeout = 60
            };
        }

        public ReadSerial()
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
                    _serialPort.Close();
                }).Ignore(NidaqTriggers.Stop)
        .Permit(NidaqTriggers.Start, NidaqStates.Running)
        .Permit(NidaqTriggers.Calibrate, NidaqStates.Calibrating);

            _stateMachine.Configure(NidaqStates.Calibrating).OnEntry(() =>
                {
                    _stop = false;
                    StartStream(true);

                })
                .Permit(NidaqTriggers.Calibrated, NidaqStates.Stopped)
                .Permit(NidaqTriggers.Stop, NidaqStates.Stopped);
        }

        private async void StartStream(bool calibrate)
        {
            try
            {
                _serialPort = SerialPortFactory();
                _serialPort.Open();
            }
            catch (Exception e)
            {
                OnError("Cannot open serial port");
                return;
            }

            
            while (true)
            {
                List<CSVFormat> data = new();
                // Create a new task

                for (var i = 0; i < buffer; i++)
                {
                    if (_stop)
                        return;
                    int[] values;
                    string line = string.Empty;
                    for (int retry = 0; retry < 3; retry++)
                    {
                        try
                        {
                            line = await _serialPort.ReadLineAsync();
                        }
                        catch (Exception e)
                        {
                            //suppress;
                        }

                        if (line.Contains('|') || _stop)
                            break;
                    }

                    if (!line.Contains('|'))
                    {
                        OnError("could not interpret data");
                        return;
                    }

                    try
                    {
                        values = line.Split('|').Select(int.Parse).ToArray();
                    }
                    catch
                    {
                        OnError("Could not interpret data");
                        return;
                    }

                    var results = values.Select(s => (25167408 - s))
                        .Select(n => ((float)n) / 281).ToArray();

                    CSVFormat entry = new()
                    {
                        Z1 = results[0],
                        Z2 = results[1],
                        Z3 = results[2],
                        Z4 = results[3],
                        X1 = 0,
                        X2 = 0,
                        Y = 0,
                    };
                    data.Add(entry);
                }

                if (calibrate)
                {
                    CalibrateFinished(SamplesToModel(data));
                    return;
                }

                OnDataReceived(SamplesToModel(data));
            }
        }
        private async void CalibrateFinished(IEnumerable<AnalogChannel> channels)
        {
            IEnumerable<double> averages = channels.Select(analogChannel => analogChannel.Values.Average());
            _calibration = averages.ToArray();
            await _stateMachine.FireAsync(NidaqTriggers.Calibrated);
            OnCalibrationFinished();
        }

        private static IEnumerable<AnalogChannel> SamplesToModel(IEnumerable<CSVFormat> samples)
        {
            PropertyInfo[] props = typeof(CSVFormat).GetProperties();
            string[] channelnames = { "z1", "z2", "z3", "z4", "x1", "x2", "y" };
            var channels = new List<AnalogChannel>();

            foreach (var model in props.Select((value, i) => new { value, i }))
            {

                var channel = new AnalogChannel()
                {
                    Name = model.value.Name,
                    Values = new List<double>()
                };
                foreach (var csvFormat in samples)
                {
                    object value = csvFormat.GetType().GetProperty(model.value.Name).GetValue(csvFormat, null);
                    channel.Values.Add(float.Parse(value.ToString()));
                }
                channels.Add(channel);
            }

            return channels.Select((i, n) => ReadNidaq.AdjuctCalibrate(n, i, _calibration));
        }

        public event EventHandler<IEnumerable<AnalogChannel>>? DataReceived;
        public event EventHandler? CalibrationFinished;

        public async void Start()
        {
            await _stateMachine.FireAsync(NidaqTriggers.Start);
        }

        public async void Stop()
        {
            await _stateMachine.FireAsync(NidaqTriggers.Stop);
        }

        public event EventHandler<string>? Error;

        private static double[] _calibration = new double[7];

        public async void CalibrateAsync()
        {
            ResetCalibration();
            await _stateMachine.FireAsync(NidaqTriggers.Calibrate);
        }

        private static void ResetCalibration()
        {
            _calibration = Enumerable.Range(0, 7).Select(i => 0.0).ToArray();
        }

        protected virtual void OnCalibrationFinished()
        {
            CalibrationFinished?.Invoke(this, EventArgs.Empty);
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

    }
}
