using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using JamShared;
using NAudio.Wave;

namespace JamNet
{
    public partial class MainPage : UserControl, IWaveSamplesAndSettings
    {
        const string resourcePrefix = "JamNet.Samples.";

        public static MainPage instance;

        public MainPage()
        {
            // Required to initialize variables
            InitializeComponent();

            instance = this;

            // find embedded samples
            foreach (var resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (resourceName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    this.sampleLibrary.Items.Add(resourceName.Substring(resourcePrefix.Length));
                }
            }
        }

        public IEnumerable<ISampleStream> GetSamples()
        {
            return this.songCanvas.Children
                .Where(ue => ue is FrameworkElement)
                .Cast<FrameworkElement>()
                .Select<FrameworkElement, ISampleStream>(r =>
                {
                    string name = r.Tag as string;
                    WaveFileReader wfr = new WaveFileReader(MainPage.GetResourceStream(name));
                    double offset = 1000.0 * ((TranslateTransform)r.RenderTransform).X * MainPage.instance.duration.Value / MainPage.instance.songCanvas.ActualWidth;
                    return new NamedStreamWithOffset(wfr, name, offset);
                });
        }

        public long GetLength()
        {
            return (long)MainPage.instance.songCanvas.Width;
        }

        public double GetDuration()
        {
            return this.duration != null ? this.duration.Value : 2.0;
        }

        public double GetVolume()
        {
            return this.volume != null ? this.volume.Value : 50.0;
        }

        static Stream GetResourceStream(string resourceName)
        {
            if (!resourceName.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase))
            {
                resourceName = resourcePrefix + resourceName;
            }
            Debug.WriteLine(resourceName);
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        }

        void play_Click(object sender, RoutedEventArgs e)
        {
            if ((string)this.play.Content == "Play")
            {
                this.play.Content = "Stop";
                this.player.SetSource(new WaveMediaStreamSource(instance));
            }
            else
            {
                this.play.Content = "Play";
                this.player.Stop();
            }
        }

        UIElement dragged;

        void songCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // first check if we're dragging an existing item
            IEnumerable<UIElement> hits = VisualTreeHelper.FindElementsInHostCoordinates(e.GetPosition(null), this.songCanvas);
            this.dragged = hits.FirstOrDefault() as Rectangle;
            if (this.dragged != null)
            {
                return;
            }
            // otherwise we might be dropping a new one
            object selected = this.sampleLibrary.SelectedItem;
            if (selected == null)
            {
                // or not
                return;
            }
            Stream s = MainPage.GetResourceStream(selected as string);
            s.Seek(0, SeekOrigin.Begin);
            using (WaveFileReader wfr = new WaveFileReader(s))
            {
                double widthScale = this.songCanvas.ActualWidth / this.duration.Value;
                UIElement shape = samplesAsWaves.IsChecked.HasValue && samplesAsWaves.IsChecked.Value ? GenPolyline(selected, wfr, widthScale, 64, 64) : GenRectangle(selected, wfr, widthScale, 64);
                this.songCanvas.Children.Add(shape);
                this.dragged = shape;
            }
            songCanvas_MouseMove(sender, e);
        }

        UIElement GenRectangle(object selected, WaveFileReader wfr, double widthScale, double height)
        {
            double width = wfr.TotalTime.TotalSeconds * widthScale;
            Rectangle shape = new Rectangle
            {
                Tag = selected,
                Height = height,
                Width = width,
                Fill = new LinearGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop { Color = Colors.LightGray, Offset = 0 },
                        new GradientStop { Color = Colors.Gray, Offset = 0.8 },
                    }
                },
            };
            return shape;
        }

        UIElement GenPolyline(object selected, WaveFileReader wfr, double widthScale, double height, int scale)
        {
            double width = wfr.TotalTime.TotalSeconds * widthScale;
            Polyline shape = new Polyline
            {
                StrokeThickness = 1.0,
                Tag = selected,
                Height = height,
                Width = width,
                Stroke = new LinearGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop { Color = Colors.DarkGray, Offset = 0 },
                        new GradientStop { Color = Colors.Gray, Offset = 0.8 },
                    }
                },
            };
            for (int x = 0; ; x++)
            {
                double[] samples = wfr.ReadOneSample();
                if (samples == null || samples.Length < 1)
                {
                    break;
                }
                if (x % scale != 0) continue;
                double xx = x * shape.Width / wfr.Length;
                double yy = (samples[0] + 1.0) * height / 2.0;
                shape.Points.Add(new Point(xx, yy));
            }
            return shape;
        }

        void songCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.dragged == null)
            {
                return;
            }
            this.dragged.RenderTransform = new TranslateTransform
            {
                X = e.GetPosition(this.songCanvas).X,
                Y = e.GetPosition(this.songCanvas).Y,
            };
        }

        // TODO: allow editing on double click?
        int last = Environment.TickCount;
        void sampleLibrary_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int last = Environment.TickCount;
            if (last - this.last < 250)
            {
                // otherwise we might be dropping a new one
                object selected = this.sampleLibrary.SelectedItem;
                if (selected == null)
                {
                    // or not
                    return;
                }
            }
            this.last = last;
        }

        void songCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.dragged = null;
        }

        void sampleLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selected = this.sampleLibrary.SelectedItem;
            if (selected == null)
            {
                return;
            }
            this.wave.Children.Clear();
            Stream s = MainPage.GetResourceStream(selected as string);
            s.Seek(0, SeekOrigin.Begin);
            using (WaveFileReader wfr = new WaveFileReader(s))
            {
                double widthScale = this.wave.Width / wfr.TotalTime.TotalSeconds;
                UIElement shape = GenPolyline(selected, wfr, widthScale * 2.0, this.wave.Height, 1);
                this.wave.Children.Add(shape);
            }
        }
    }
}