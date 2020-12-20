using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Indicators
{
    /// <summary>
    ///     Interaction logic for IndicatorControl.xaml
    /// </summary>
    public class AttitudeIndicator : UserControl
    {
        private pitchAndRoll attitude;

        public AttitudeIndicator()
        {
            InitializeComponent();
            attitude = new pitchAndRoll();
        }

        public void updateAttitude(double pitch, double roll)
        {
            attitude.pitch = pitch;
            attitude.roll = -1 * roll;
            applyRotTranslation();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void applyRotTranslation()
        {
            // UIElement container = VisualTreeHelper.GetParent(centerCircle) as UIElement;
            // Point center = centerCircle.TranslatePoint(new Point(0, 0), container);

            var tg = new TransformGroup();

            //The attitude scale is 5.5px/deg until +/-30deg where it changes to ~2.75px/deg
            var translation = 0.0;
            if (attitude.pitch > 30)
                translation = 5.5 * 30 + (attitude.pitch - 30) * 2.75;
            else if (attitude.pitch < -30)
                translation = 5.5 * -30 + (attitude.pitch + 30) * 2.75;
            else //+/-30 deg
                translation = attitude.pitch * 5.5;

            var t = new TranslateTransform(0, translation);
            var r = new RotateTransform(attitude
                .roll); //, center.X, center.Y); // (horizon.X2 - horizon.X1) / 2, (horizon.Y2 - horizon.Y1) / 2);
        }

        private struct pitchAndRoll
        {
            public double pitch;
            public double roll;
        }
    }
}