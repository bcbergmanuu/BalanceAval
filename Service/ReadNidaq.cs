using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BalanceAval.Models;
using NationalInstruments;
using NationalInstruments.DAQmx;
using Task = System.Threading.Tasks.Task;

namespace BalanceAval.Service
{
    public class ReadNidaq : IReadNidaq
    {
        private AnalogMultiChannelReader analogInReader;

        private NationalInstruments.DAQmx.Task myTask;

        public const int Buffersize = 50;
        public const double Frequency = 100;

        public static readonly string[] ChannelNames = { "Dev1/ai1", "Dev1/ai2", "Dev1/ai3", "Dev1/ai4", };

        public void Start()
        {
            if(myTask != null)
                myTask.Dispose();

            // Create a new task
            myTask = new NationalInstruments.DAQmx.Task();

            // Create a virtual channel

            foreach (var channel in ChannelNames)
            {
                myTask.AIChannels.CreateVoltageChannel(channel, "",
                    (AITerminalConfiguration)(-1), 0.0, 10.0, AIVoltageUnits.Volts);
            }

            // Configure the timing parameters
            myTask.Timing.ConfigureSampleClock("", Frequency,
                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Buffersize);

            //myTask.ConfigureLogging(System.Windows.Application.Current.StartupUri + @"logfiles-wpf.txt", TdmsLoggingOperation.CreateOrReplace, LoggingMode.LogAndRead, "Group Name");

            // Verify the Task
            myTask.Control(TaskAction.Verify);

            analogInReader = new AnalogMultiChannelReader(myTask.Stream);

            // Use SynchronizeCallbacks to specify that the object 
            // marshals callbacks across threads appropriately.
            analogInReader.SynchronizeCallbacks = true;
            analogInReader.BeginReadWaveform(Buffersize, AnalogInCallback, myTask);
        }

        public void Stop()
        {
            myTask.Stop();
  
        }

        public event EventHandler<List<AnalogChannel>> DataReceived;



        private void AnalogInCallback(IAsyncResult ar)
        {
            AnalogWaveform<double>[]? data;
            
            if (myTask.IsDone)
                return;
            try
            {
                data = analogInReader.EndReadWaveform(ar);
            }
            catch (NationalInstruments.DAQmx.DaqException exception)
            {
                //should contain logging
                return;
            }
            // Read the available data from the channels

            var model = SamplesToModel(data);
            OnDataReceived(model);

            analogInReader.BeginMemoryOptimizedReadWaveform(Buffersize,
                AnalogInCallback, myTask, data);
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



        //private static void Printsamples(IEnumerable<AnalogWaveform<double>> samples)
        //{
        //    foreach (var channel in samples)
        //    {
        //        Debug.WriteLine(channel.ChannelName);

        //        foreach (var analogWaveformSample in channel.Samples)
        //        {
        //            Debug.WriteLine(analogWaveformSample.Value.ToString("N"));
        //        }
        //    }
        //}

        protected virtual void OnDataReceived(List<AnalogChannel> e)
        {
            DataReceived?.Invoke(this, e);
        }
    }
}

