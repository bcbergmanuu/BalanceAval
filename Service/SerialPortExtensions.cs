using System.IO.Ports;
using System;
using System.Threading.Tasks;

namespace BalanceAval.Service
{
    public static class SerialPortExtensions
    {
        /// https://stackoverflow.com/questions/24041378/c-sharp-async-serial-port-read
        public async static Task ReadAsync(this SerialPort serialPort, byte[] buffer, int offset, int count)
        {
            var bytesToRead = count;
            var temp = new byte[count];

            while (bytesToRead > 0)
            {
                var readBytes = await serialPort.BaseStream.ReadAsync(temp, 0, bytesToRead);
                Array.Copy(temp, 0, buffer, offset + count - bytesToRead, readBytes);
                bytesToRead -= readBytes;
            }
        }

        public async static Task<byte[]> ReadAsync(this SerialPort serialPort, int count)
        {
            var buffer = new byte[count];
            await serialPort.ReadAsync(buffer, 0, count);
            return buffer;
        }

        /// <summary>
        /// Read a line from the SerialPort asynchronously
        /// </summary>
        /// <param name="serialPort">The port to read data from</param>
        /// <returns>A line read from the input</returns>
        public static async Task<string> ReadLineAsync(
            this SerialPort serialPort)
        {
            byte[] buffer = new byte[1];
            string ret = string.Empty;

            // Read the input one byte at a time, convert the
            // byte into a char, add that char to the overall
            // response string, once the response string ends
            // with the line ending then stop reading
            while (true)
            {
                await serialPort.BaseStream.ReadAsync(buffer, 0, 1);
                ret += serialPort.Encoding.GetString(buffer);

                if (ret.EndsWith(serialPort.NewLine))
                    // Truncate the line ending
                    return ret.Substring(0, ret.Length - serialPort.NewLine.Length);
            }
        }

        /// <summary>
        /// Write a line to the SerialPort asynchronously
        /// </summary>
        /// <param name="serialPort">The port to send text to</param>
        /// <param name="str">The text to send</param>
        /// <returns></returns>
        public static async Task WriteLineAsync(
            this SerialPort serialPort, string str)
        {
            byte[] encodedStr =
                serialPort.Encoding.GetBytes(str + serialPort.NewLine);

            await serialPort.BaseStream.WriteAsync(encodedStr, 0, encodedStr.Length);
            await serialPort.BaseStream.FlushAsync();
        }

        /// <summary>
        /// Write a line to the ICommunicationPortAdaptor asynchronously followed
        /// immediately by attempting to read a line from the same port. Useful
        /// for COMMAND --> RESPONSE type communication.
        /// </summary>
        /// <param name="serialPort">The port to process commands through</param>
        /// <param name="command">The command to send through the port</param>
        /// <returns>The response from the port</returns>
        public static async Task<string> SendCommandAsync(
            this SerialPort serialPort, string command)
        {
            await serialPort.WriteLineAsync(command);
            return await serialPort.ReadLineAsync();
        }
    }
}