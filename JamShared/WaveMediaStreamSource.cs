using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using NAudio.Wave;

namespace JamShared
{
    public interface IWaveSamplesAndSettings
    {
        double GetVolume();

        double GetDuration();

        IEnumerable<ISampleStream> GetSamples();
    }

    public class WaveMediaStreamSource : MediaStreamSource
    {
        IWaveSamplesAndSettings waveSamplesAndSettings;
        WaveFormat format;
        MemoryStream samples;
        MediaStreamDescription description;
        double min = double.MaxValue;
        double max = double.MinValue;

        public WaveMediaStreamSource(IWaveSamplesAndSettings waveSamplesAndSettings)
        {
            Debug.WriteLine("WaveMediaStreamSource");

            this.waveSamplesAndSettings = waveSamplesAndSettings;
        }

        protected override void CloseMedia()
        {
            Debug.WriteLine("CloseMedia");
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            Debug.WriteLine("GetDiagnosticAsync: " + diagnosticKind);

            this.ReportGetDiagnosticCompleted(diagnosticKind, 0);
        }

        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            Dictionary<MediaSampleAttributeKeys, string> emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>();

            bool done = false;
            if (done)
            {
                this.ReportGetSampleCompleted(new MediaStreamSample(this.description, null, 0, 0, 0, emptySampleDict));
                return;
            }

            bool available = true;
            ISampleStream[] streams = null;
            if (available)
            {
                streams = this.waveSamplesAndSettings.GetSamples().ToArray();
                if (streams.Length == 0)
                {
                    available = false;
                }
            }
            if (!available)
            {
                this.ReportGetSampleProgress(0);
                return;
            }

            // TODO: replace with a stream that only updates position on read
            // save off position, so we can seek back after writing samples
            long position = this.samples.Position;

            // computed in 100 nano seconds
            long timestamp = position * 10 * 1000 * 1000 / this.format.AverageBytesPerSecond;

            // feed 100ms worth of samples at a time (make this configurable?)
            int sampleCount = this.format.AverageBytesPerSecond / 10;

            BinaryWriter bw = new BinaryWriter(this.samples);
            for (int j = 0; j < sampleCount; j++)
            {
                double average = 0.0;
                foreach (ISampleStream ns in streams)
                {
                    double[] streamSamples = ns.ReadOneSample(position + j);
                    for (int i = 0; i < streamSamples.Length; i++)
                    {
                        average += streamSamples[i];
                    }
                }

                average = average * this.waveSamplesAndSettings.GetVolume();

                if (average > 1.0)
                {
                    if (average > this.max)
                    {
                        System.Diagnostics.Debug.WriteLine(average + "\tCLIPPING");
                        this.max = average;
                    }
                    average = 1.0;
                }
                else if (average < -1.0)
                {
                    if (average < this.min)
                    {
                        System.Diagnostics.Debug.WriteLine(average + "\tCLIPPING");
                        this.min = average;
                    }
                    average = -1.0;
                }

                short sample = (short)(average * short.MaxValue);
                bw.Write(sample);
            }

            this.samples.Position = position;

            MediaStreamSample msSamp = new MediaStreamSample(this.description, this.samples, this.samples.Position, sampleCount, timestamp, emptySampleDict);
            System.Diagnostics.Debug.WriteLine("reporting " + sampleCount + " samples, position was: " + position);
            this.ReportGetSampleCompleted(msSamp);

            // now loop by resetting once we get past the duration
            double duration = this.waveSamplesAndSettings.GetDuration();
            if (duration > 0 && this.samples.Position >= this.format.AverageBytesPerSecond * duration)
            {
                this.samples.Seek(0, SeekOrigin.Begin);
                foreach (ISampleStream ns in streams)
                {
                    ns.Reset();
                }
            }
        }

        protected override void OpenMediaAsync()
        {
            Dictionary<MediaSourceAttributesKeys, string> sourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
            sourceAttributes[MediaSourceAttributesKeys.Duration] = 0.ToString();
            sourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();

            // TODO: need to allow for configurable output format.
            this.format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, 44100, 1, 2 * 44100, 2, 2 * 8);
            this.samples = new MemoryStream();

            Dictionary<MediaStreamAttributeKeys, string> streamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            streamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = this.format.ToHexString();

            this.description = new MediaStreamDescription(MediaStreamType.Audio, streamAttributes);

            this.AudioBufferLength = 30;
            
            this.ReportOpenMediaCompleted(sourceAttributes, new MediaStreamDescription[] { this.description });
        }

        protected override void SeekAsync(long seekToTime)
        {
            Debug.WriteLine("SeekAsync: " + seekToTime);

            this.ReportSeekCompleted(seekToTime);
        }

        protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
        {
            Debug.WriteLine("SwitchMediaStreamAsync: " + mediaStreamDescription.StreamId);

            this.ReportSwitchMediaStreamCompleted(mediaStreamDescription);
        }
    }

    public abstract class ReadOnlyStream : Stream
    {
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotSupportedException();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
