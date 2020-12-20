using System;
using System.Collections.Generic;
using System.Drawing;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace IF_FMS
{
    /// <summary>
    /// Interaction logic for AircraftStatus.xaml
    /// </summary>
    public partial class AircraftStatus : UserControl
    {
        private Avalonia.Controls.Shapes.Rectangle _ApOff;
        private Avalonia.Controls.Shapes.Rectangle _GearDownOff;
        private Avalonia.Controls.Shapes.Rectangle _GearUpOff;
        private Avalonia.Controls.Shapes.Rectangle _StallOff;
        private Avalonia.Controls.Shapes.Rectangle _StallWarnOff;

        public AircraftStatus()
        {
            InitializeComponent();
            InitializeUiElements();
        }

        private void InitializeUiElements()
        {
            _ApOff = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("ApOff");
            _GearDownOff = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("GearDownOff");
            _GearUpOff = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("GearUpOff");
            _StallOff = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("StallOff");
            _StallWarnOff = this.FindControl<Avalonia.Controls.Shapes.Rectangle>("StallWarnOff");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public class AircraftStatusForDisplay
        {
            public float AltitudeAGL { get; set; }
            public float AltitudeMSL { get; set; }
            public float IndicatedAirspeedKts { get; set; }
            public float MachNumber { get; set; }
            public float VerticalSpeed { get; set; }
            public float GroundSpeedKts { get; set; }
            public float Pitch { get; set; }
            public float Bank { get; set; }
            public float HeadingMagnetic { get; set; }
            public float HeadingTrue { get; set; }
            public float CourseTrue { get; set; }
            public float GForce { get; set; }
            public float SideForce { get; set; }
        }

        private AircraftStatusForDisplay pAcStateDisplay;
        private Fds.IFAPI.APIAircraftState pAcState;
        public Fds.IFAPI.APIAircraftState AircraftState
        {
            get
            {
                if (pAcState == null) { pAcState = new Fds.IFAPI.APIAircraftState(); }
                if(pAcStateDisplay == null) { pAcStateDisplay = new AircraftStatusForDisplay(); }
                return pAcState;
            }
            set
            {
                pAcState = value;
                if(pAcStateDisplay == null) { pAcStateDisplay = new AircraftStatusForDisplay(); }
                pAcStateDisplay.AltitudeAGL = pAcState.AltitudeAGL;
                pAcStateDisplay.AltitudeMSL = pAcState.AltitudeMSL;
                pAcStateDisplay.Bank = pAcState.Bank;
                pAcStateDisplay.CourseTrue = pAcState.CourseTrue;
                pAcStateDisplay.GForce = pAcState.GForce;
                pAcStateDisplay.GroundSpeedKts = pAcState.GroundSpeedKts;
                pAcStateDisplay.HeadingMagnetic = pAcState.HeadingMagnetic;
                pAcStateDisplay.HeadingTrue = pAcState.HeadingTrue;
                pAcStateDisplay.IndicatedAirspeedKts = pAcState.IndicatedAirspeedKts;
                pAcStateDisplay.MachNumber = pAcState.MachNumber;
                pAcStateDisplay.Pitch = pAcState.Pitch;
                pAcStateDisplay.SideForce = pAcState.SideForce;
                pAcStateDisplay.VerticalSpeed = pAcState.VerticalSpeed;
                
                updateView();
            }
        }

        private void updateView()
        {
            Dictionary<String, object> acStateDict = new Dictionary<String, object>();
            var props = typeof(AircraftStatusForDisplay).GetProperties();
            foreach (var prop in props)
            {
                object value = prop.GetValue(pAcStateDisplay, null); // against prop.Name
                if (prop.Name != "Type")
                {
                    acStateDict.Add(prop.Name, value);
                }
            }
            //listView.ItemsSource = acStateDict;

            //AutoPilot Light
            if(pAcState.IsAutopilotOn)
            { _ApOff.IsVisible = false;  }
            else { _ApOff.IsVisible = true; }

            //Gear Lights
            if (pAcState.GearState == Fds.IFAPI.GearState.Down)
            {
                _GearDownOff.IsVisible = false;
                _GearUpOff.IsVisible = true;
            }
            else
            {
                _GearDownOff.IsVisible = true;
                _GearUpOff.IsVisible = false;
            }

            if (pAcState.StallWarning)
            {
                _StallWarnOff.IsVisible = false;
            }
            else
            {
                _StallWarnOff.IsVisible = true;
            }

            if (pAcState.Stalling)
            {
                _StallOff.IsVisible = false;
            }
            else
            {
                _StallOff.IsVisible = true;
            }
        }
    }
}
