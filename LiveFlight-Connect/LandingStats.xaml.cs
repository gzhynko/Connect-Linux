using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Fds.IFAPI;

namespace LiveFlight
{
    /// <summary>
    ///     Interaction logic for LandingStats.xaml
    /// </summary>
    public class LandingStats : UserControl
    {
        private Grid _gridLandingStats;

        private touchdownInfo pTouchdownInfo;

        public LandingStats()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeUiElements()
        {
            _gridLandingStats = this.FindControl<Grid>("gridLandingStats");
        }

        public void updateLandingStats(Coordinate touchdownPosition, Coordinate positionAtEndOfRoll,
            APIAircraftState stateJustBeforeTouchdown, APIAircraftState stateJustAfterTouchdown, string aircraftType)
        {
            pTouchdownInfo = new touchdownInfo();
            pTouchdownInfo.touchdownGroundSpeed = stateJustBeforeTouchdown.GroundSpeedKts;
            pTouchdownInfo.touchdownIAS = stateJustBeforeTouchdown.IndicatedAirspeedKts;
            pTouchdownInfo.touchdownPitch = stateJustBeforeTouchdown.Pitch;
            pTouchdownInfo.touchdownGforce = stateJustBeforeTouchdown.GForce > stateJustAfterTouchdown.GForce
                ? stateJustBeforeTouchdown.GForce
                : stateJustAfterTouchdown.GForce;
            pTouchdownInfo.touchdownVS = stateJustBeforeTouchdown.VerticalSpeed;
            pTouchdownInfo.wasStalling = stateJustBeforeTouchdown.Stalling;
            pTouchdownInfo.wasNearStall = stateJustBeforeTouchdown.StallWarning;
            pTouchdownInfo.wasOverMLW = stateJustBeforeTouchdown.IsOverLandingWeight;

            pTouchdownInfo.rollDistance = calcRollDistance(touchdownPosition, positionAtEndOfRoll);

            _gridLandingStats.DataContext = pTouchdownInfo;
        }

        private double calcRollDistance(Coordinate touchdownPosition, Coordinate currentPosition)
        {
            var R = 3959; // Radius of the earth in miles
            var dLat = (currentPosition.Latitude - touchdownPosition.Latitude) * (Math.PI / 180);
            var dLon = (currentPosition.Longitude - touchdownPosition.Longitude) * (Math.PI / 180);
            var a =
                    Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(currentPosition.Latitude * (Math.PI / 180)) *
                    Math.Cos(touchdownPosition.Latitude * (Math.PI / 180)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
                ;
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in miles
            d *= 5280; //Distance in ft
            return d;
        }

        public class touchdownInfo
        {
            public string aircraftType { get; set; }
            public double rollDistance { get; set; }
            public double touchdownGroundSpeed { get; set; }
            public double touchdownIAS { get; set; }
            public double touchdownGforce { get; set; }
            public double touchdownVS { get; set; }
            public double touchdownPitch { get; set; }
            public bool wasStalling { get; set; }
            public bool wasNearStall { get; set; }
            public bool wasOverMLW { get; set; }
        }
    }
}