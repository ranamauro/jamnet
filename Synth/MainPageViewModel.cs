using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using JamShared;
using OxyPlot;

namespace SynthNet
{
    public class MainPageViewModel : INotifyPropertyChanged, IWaveSamplesAndSettings
    {
        public MainPageViewModel()
        {
            this.PlayContent = "Play";
        }

        string playContent;
        public string PlayContent
        {
            get { return this.playContent; }
            set
            {
                this.playContent = value;

                this.OnPropertyChanged("PlayContent");
            }
        }

        public void PlayOrStop(MediaElement mediaElement)
        {
            switch (this.PlayContent)
            {
                case "Play":
                    this.PlayContent = "Stop";
                    mediaElement.SetSource(new WaveMediaStreamSource(this));
                    mediaElement.Play();
                    break;
                default:
                    this.PlayContent = "Play";
                    mediaElement.Source = null;
                    mediaElement.Stop();
                    break;
            }
        }

        ObservableCollection<WaveModel> waves = new ObservableCollection<WaveModel>();
        public ObservableCollection<WaveModel> Waves
        {
            get { return this.waves; }
        }

        public void AddWave()
        {
            WaveModel wm = new WaveModel();
            wm.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                this.OnPropertyChanged("Chart");
            };
            this.waves.Add(wm);

            this.OnPropertyChanged("Chart");
        }

        public PlotModel Chart
        {
            get
            {
                const int Size = 4410; // 100 ms

                PlotModel plotModel = new PlotModel("Wave");
                foreach (WaveModel wm in this.Waves.Where(wm => wm.Enabled))
                {
                    var ws = new WaveStream(wm);
                    var ls = new LineSeries();
                    for (int x = 0; x < Size; x++)
                    {
                        double y = ws.ReadOneSample(x)[0];
                        ls.Points.Add(new DataPoint(x, y));
                    }
                    plotModel.Series.Add(ls);
                }
                var lsSum = new LineSeries { StrokeThickness = 5.0 };
                for (int x = 0; x < Size; x++)
                {
                    double y = 0.0;
                    foreach (DataPointSeries dps in plotModel.Series)
                    {
                        y += dps.Points[x].Y;
                    }
                    lsSum.Points.Add(new DataPoint(x, y));
                }
                plotModel.Series.Add(lsSum);
                return plotModel;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public double GetVolume()
        {
            return 1.0;
        }

        public double GetDuration()
        {
            return -1.0;
        }

        public IEnumerable<ISampleStream> GetSamples()
        {
            foreach (WaveModel ws in this.Waves)
            {
                yield return new WaveStream(ws);
            }
        }
    }

    public class WaveModel : INotifyPropertyChanged
    {
        bool enabled;
        public bool Enabled
        {
            get { return this.enabled; }
            set
            {
                this.enabled = value;
                this.OnPropertyChanged("Enabled");
            }
        }

        double frequency;
        public double Frequency
        {
            get { return this.frequency; }
            set
            {
                this.frequency = value;
                this.OnPropertyChanged("Frequency");
            }
        }

        double amplitude;
        public double Amplitude
        {
            get { return this.amplitude; }
            set
            {
                this.amplitude = value;
                this.OnPropertyChanged("Amplitude");
            }
        }

        double offset;
        public double Offset
        {
            get { return this.offset; }
            set
            {
                this.offset = value;
                this.OnPropertyChanged("Offset");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return base.ToString();
        }
    }

    class WaveStream : ISampleStream
    {
        WaveModel wm;

        public WaveStream(WaveModel wm)
        {
            this.wm = wm;
        }

        public double[] ReadOneSample(double offset)
        {
            double value = this.wm.Amplitude * Math.Sin((this.wm.Offset + offset) * 2 * Math.PI * this.wm.Frequency / 44100.0);
            return new double[] { value };
        }

        public void Reset()
        {
        }
    }
}
