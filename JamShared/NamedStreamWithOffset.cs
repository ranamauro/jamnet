using System.IO;
using NAudio.Wave;

namespace JamShared
{
    public class NamedStreamWithOffset : ISampleStream
    {
        WaveFileReader Wave { get; set; }
        string Name { get; set; }
        double Offset { get; set; } // in milliseconds

        double[] empty;

        public NamedStreamWithOffset(WaveFileReader wave, string name, double offset)
        {
            this.Wave = wave;
            this.Name = name;
            this.Offset = offset;
        }

        public double[] ReadOneSample(double offset)
        {
            if (offset >= this.Offset)
            {
                double[] result = this.Wave.ReadOneSample();
                if (result != null)
                {
                    return result;
                }
            }
            if (this.empty == null)
            {
                this.empty = new double[this.Wave.WaveFormat.Channels];
            }
            return this.empty;
        }

        public void Reset()
        {
            this.Wave.Seek(0, SeekOrigin.Begin);
        }
    }

    public static class WaveFileReaderExtensions
    {
        public static double[] ReadOneSample(this WaveFileReader reader)
        {
            double[] buffer = new double[reader.WaveFormat.Channels];
            for (int channel = 0; channel < reader.WaveFormat.Channels; channel++)
            {
                byte[] bytes = new byte[2];
                int read = reader.Read(bytes, 0, 2);
                if (read != 2)
                {
                    return null;
                }
                short sample = (short)(bytes[0] | (bytes[1] << 8));
                buffer[channel] = (double)sample / 32768.0;
            }
            return buffer;
        }
    }
}
