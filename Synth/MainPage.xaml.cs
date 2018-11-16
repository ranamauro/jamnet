using System.Windows;
using System.Windows.Controls;

namespace SynthNet
{
    public partial class MainPage : UserControl
    {
        MainPageViewModel vm;

        public MainPage()
        {
            InitializeComponent();
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.vm = new MainPageViewModel();

            this.DataContext = this.vm;
        }

        void OnAddWave(object sender, RoutedEventArgs e)
        {
            this.vm.AddWave();
        }

        void OnPlay(object sender, RoutedEventArgs e)
        {
            this.vm.PlayOrStop(this.mediaElement);
        }
    }
}
