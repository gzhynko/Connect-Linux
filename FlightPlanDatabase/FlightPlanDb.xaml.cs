using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using IF_FMS;

namespace FlightPlanDatabase
{
    /// <summary>
    /// Interaction logic for FlightPlanDb.xaml
    /// </summary>
    public partial class FlightPlanDb : UserControl
    {
        public event EventHandler FplUpdated;

        // for keyboard events
        public static bool textFieldFocused = false;

        private List<FMS.fplDetails> pFmsFpl;
        public List<FMS.fplDetails> FmsFpl {
            get {
                if (pFmsFpl == null) { pFmsFpl = new List<FMS.fplDetails>(); }
                return pFmsFpl;
            }
            set {
                if (pFmsFpl == null) { pFmsFpl = new List<FMS.fplDetails>(); }
                pFmsFpl = value;
                if (this.FplUpdated != null) { this.FplUpdated(this,null); }
            }
        }
        
        /* UI Elements */
        private TextBox _txtFromICAO;
        private TextBox _txtDestICAO;
        private TextBlock _lblSearchMsg;
        private TextBox _txtFplId;
        private ComboBox _cbFpls;

        private Fds.IFAPI.APIFlightPlan pApiFpl;
        public Fds.IFAPI.APIFlightPlan ApiFpl
        {
            get { return pApiFpl; }
            set { pApiFpl = value; }
        }
        
        public FlightPlanDb()
        {
            InitializeComponent();
            InitializeUiElements();
            //fpdLnk.RequestNavigate += (s, e) => { System.Diagnostics.Process.Start(e.Uri.ToString()); };
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeUiElements()
        {
            _txtFromICAO = this.FindControl<TextBox>("txtFromICAO");
            _txtDestICAO = this.FindControl<TextBox>("txtDestICAO");
            _lblSearchMsg = this.FindControl<TextBlock>("lblSearchMsg");
            _txtFplId = this.FindControl<TextBox>("txtFplId");
            _cbFpls = this.FindControl<ComboBox>("cbFpls");
        }

        private List<FlightPlanDatabase.ApiDataTypes.FlightPlanSummary> pFplList;

        private void btnGetFplFromFpd_Click(object sender, RoutedEventArgs e)
        {
            FlightPlanDatabase.FpdApi fd = new FlightPlanDatabase.FpdApi();
            List<FlightPlanDatabase.ApiDataTypes.FlightPlanSummary> fpls = new List<FlightPlanDatabase.ApiDataTypes.FlightPlanSummary>();
            try {
                fpls = fd.searchFlightPlans(_txtFromICAO.Text, _txtDestICAO.Text);
            }catch(Exception ex)
            {
                String exmsg = ex.Message;
                if (ex.Message.Contains(":")) { exmsg = exmsg.Split(':')[1]; }
                if (ex.Message.Contains(")")) { exmsg = exmsg.Split(')')[1]; }
                _lblSearchMsg.Text = "Error: " + exmsg;
                return;
            }
            _cbFpls.Items = null;
            if (fpls == null || fpls.Count < 1)
            {
                _lblSearchMsg.Text = "Suitable FPL could not be found.";
                _cbFpls.IsVisible = false;
            }
            else
            {
                pFplList = new List<FlightPlanDatabase.ApiDataTypes.FlightPlanSummary>();
                pFplList = fpls;
                foreach (FlightPlanDatabase.ApiDataTypes.FlightPlanSummary f in fpls)
                {
                    //_cbFpls.Items.Add(f.id + " (" + String.Format("{0:0.00}", f.distance) + "nm - " + f.waypoints.ToString() + "wpts)");
                }
                _lblSearchMsg.Text = "FPL(s) found. Select below to load.";
                _cbFpls.IsVisible = true;
            }
        }

        private void cbFpls_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
            if (_cbFpls.Items. > 0 && cbFpls.SelectedValue != null)
            {
                string fplID = cbFpls.SelectedValue.ToString().Split(' ').First();
                loadFpdFplFromId(fplID);
                txtFplId.Text = fplID;
            } */
        }

        private void btnLoadFromId_Click(object sender, RoutedEventArgs e)
        {
            if (_txtFplId.Text.Length > 0)
            {
                loadFpdFplFromId(_txtFplId.Text);
            }
        }

        private void loadFpdFplFromId(string id)
        {
            FlightPlanDatabase.FpdApi fd = new FlightPlanDatabase.FpdApi();
            FlightPlanDatabase.ApiDataTypes.FlightPlanDetails planDetail = fd.getPlan(id);
            //FMSControl.CustomFPL.waypoints.Clear();
            List<FMS.fplDetails> fpl = new List<FMS.fplDetails>();
            pApiFpl = new Fds.IFAPI.APIFlightPlan();
            List<Fds.IFAPI.APIWaypoint> apiWpts = new List<Fds.IFAPI.APIWaypoint>();

            foreach (FlightPlanDatabase.ApiDataTypes.Node wpt in planDetail.route.nodes)
            {
                Fds.IFAPI.APIWaypoint apiWpt = new Fds.IFAPI.APIWaypoint();
                apiWpt.Name = wpt.ident;
                apiWpt.Code = wpt.name;
                apiWpt.Latitude = wpt.lat;
                apiWpt.Longitude = wpt.lon;
                apiWpts.Add(apiWpt);

                FMS.fplDetails n = new FMS.fplDetails();
                n.WaypointName = wpt.ident;
                n.Altitude = wpt.alt;

                //FMSControl.CustomFPL.waypoints.Add(n);
                fpl.Add(n);
            }

            pApiFpl.Waypoints = apiWpts.ToArray();

            FmsFpl = fpl;
        }

        private void txtFromICAO_GotFocus(object sender, GotFocusEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.Foreground = Brushes.Black;
            tb.GotFocus -= txtFromICAO_GotFocus;

            textFieldFocused = true;
        }

        private void txtDestICAO_GotFocus(object sender, GotFocusEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.Foreground = Brushes.Black;
            tb.GotFocus -= txtDestICAO_GotFocus;

            textFieldFocused = true;
        }

        private void txtFromICAO_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text == string.Empty)
            {
                tb.Foreground = Brushes.LightGray;
                tb.GotFocus += txtFromICAO_GotFocus;
                tb.Text = "KLAX";
            }

            textFieldFocused = false;
        }

        private void txtDestICAO_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text == string.Empty)
            {
                tb.Foreground = Brushes.LightGray;
                tb.GotFocus += txtDestICAO_GotFocus;
                tb.Text = "KSAN";
            }

            textFieldFocused = false;
        }

        private void txtFplId_GotFocus(object sender, GotFocusEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.Foreground = Brushes.Black;
            tb.GotFocus -= txtDestICAO_GotFocus;

            textFieldFocused = true;
        }

        private void txtFplId_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text == string.Empty)
            {
                tb.Foreground = Brushes.LightGray;
                tb.GotFocus += txtDestICAO_GotFocus;
                tb.Text = "81896";
            }

            textFieldFocused = false;

        }

    }
}
