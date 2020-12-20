using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Indicators
{
    /// <summary>
    ///     Interaction logic for AltitudeGauge.xaml
    /// </summary>
    public class AltitudeGauge : UserControl
    {
        public AltitudeGauge()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}