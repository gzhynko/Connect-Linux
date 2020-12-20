using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Fds.IFAPI;
using FlightPlanDatabase;
using IF_FMS;
using Indicators;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;

namespace LiveFlight
{
    public class MainWindow : Window
    {
        public static IFConnect.IFConnectorClient Client = new IFConnect.IFConnectorClient();
        public static Commands Commands = new Commands();

        /* UI Elements */
        private Grid _airplaneStateGrid;
        private DataGrid _atcMessagesDataGrid;
        private bool _connectionStatus;
        private Expander _expander;
        private bool _expanderExpanded;
        private TextBlock _ipLabel;
        private readonly JoystickHelper _joystickClient = new JoystickHelper();
        private TabControl _mainTabControl;
        private Grid _overlayGrid;
        private TabItem _TabItem_ATC;
        private LandingStats _landingDetails;
        private FMS _FMSControl;
        private AttitudeIndicator _AttitudeIndicator;
        private AircraftStatus _AircraftStateControl;
        private FlightPlanDb _FpdControl;

        private APIAircraftState _pAircraftState = new APIAircraftState();
        private APIAutopilotState _pAutopilotState = new APIAutopilotState();
        private Coordinate _pLandingLocation;
        private LandingStats _pLandingStatDlg;
        private double _pLastLandingRoll;

        private APIAircraftState _pLastState, _pStateJustBeforeTouchdown, _pStateJustAfterTouchdown;
        private readonly BroadcastReceiver _receiver = new BroadcastReceiver();
        private TextBlock _txtLandingRoll;
        private TextBlock _txtLandingRollLabel;

        public MainWindow()
        {
            InitializeComponent();
            InitializeUiElements();

            _airplaneStateGrid.DataContext = null;
            _airplaneStateGrid.DataContext = new APIAircraftState();

            _mainTabControl.IsVisible = true;
            _overlayGrid.IsVisible = false;

            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            // log to file
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pathToFile = string.Format("{0}\\LiveFlight_Connect_Windows.log", path);
            Console.WriteLine(pathToFile);
            File.Delete(pathToFile);
            var filestream = new FileStream(pathToFile, FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            //Console.SetOut(streamwriter);
            //Console.SetError(streamwriter);

            Console.WriteLine(
                "LiveFlight Connect\n\nPlease send this log to contact@liveflightapp.com if you experience issues. Thanks!\n\n\n");
        }
        
        // Not Tested
        private void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeUiElements()
        {
            _airplaneStateGrid = this.FindControl<Grid>("airplaneStateGrid");
            _overlayGrid = this.FindControl<Grid>("overlayGrid");
            _atcMessagesDataGrid = this.FindControl<DataGrid>("atcMessagesDataGrid");
            _ipLabel = this.FindControl<TextBlock>("ipLabel");
            _txtLandingRoll = this.FindControl<TextBlock>("txtLandingRoll");
            _txtLandingRollLabel = this.FindControl<TextBlock>("txtLandingRollLabel");
            _mainTabControl = this.FindControl<TabControl>("mainTabControl");
            _expander = this.FindControl<Expander>("expander");
            _TabItem_ATC = this.FindControl<TabItem>("TabItem_ATC");
            _landingDetails = this.FindControl<LandingStats>("landingDetails");
            _FMSControl = this.FindControl<FMS>("FMSControl");
            _AttitudeIndicator = this.FindControl<AttitudeIndicator>("AttitudeIndicator");
            _AircraftStateControl = this.FindControl<AircraftStatus>("AircraftStateControl");
            _FpdControl = this.FindControl<FlightPlanDb>("FpdControl");
        }

        private void tabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_TabItem_ATC != null && _TabItem_ATC.IsSelected) Commands.atcMenu();
        }

        private void enableATCMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            Client.ExecuteCommand("Live.EnableATCMessageNotification");
        }
        
        private void atcMessagesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var command = string.Format("Commands.ATCEntry{0}", _atcMessagesDataGrid.SelectedIndex + 1);

            Client.ExecuteCommand(command);
        }

        #region "FlightPlanDatabase"

        /*
            FPD work
            ===========================
        */

        private void FlightPlanDb_FplUpdated(object sender, EventArgs e)
        {
            _FMSControl.FPLState = new IF_FMS.FMS.flightPlanState(); //Clear state of FMS
            _FMSControl.CustomFPL.waypoints.Clear(); //Clear FPL

            foreach (IF_FMS.FMS.fplDetails f in _FpdControl.FmsFpl)
                //Load waypoints to FMS
                _FMSControl.CustomFPL.waypoints.Add(f);
            _FMSControl.FPLState.fpl = _FpdControl.ApiFpl;
            _FMSControl.FPLState.fplDetails = _FMSControl.CustomFPL;

            //Go to FMS tab so user can see flight plan
            _mainTabControl.SelectedIndex = _mainTabControl.SelectedIndex - 1;
        }

        #endregion

        private void expander_Clicked(object sender, KeyEventArgs e)
        {
            if (_expanderExpanded)
            {
                Width = 525;
                _expander.Header = "Expand";
                _expanderExpanded = false;
            }
            else
            {
                Width = 1125;
                _expander.Header = "Collapse";
                _expanderExpanded = true;
            }
        }
        
        private void expander_Pressed(object sender, PointerPressedEventArgs e)
        {
            if (_expanderExpanded)
            {
                Width = 525;
                _expander.Header = "Expand";
                _expanderExpanded = false;
            }
            else
            {
                Width = 1125;
                _expander.Header = "Collapse";
                _expanderExpanded = true;
            }
        }

        #region PageLoaded

        /*
            Start listeners on page load
            ===========================
        */

        private void OnSourceInitialized(object? sender, EventArgs eventArgs)
        {
            // Adds the windows message processing hook and registers USB device add/removal notification.
            /* HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            ((TopLevel) this.GetVisualRoot()).PlatformImpl.
            if (source != null)
            {
                var windowHandle = source.Handle;
                source.AddHook(HwndHandler);
                UsbNotification.RegisterUsbDeviceNotification(windowHandle);
            } */
        }

        /// <summary>
        ///     Method that receives window messages.
        /// </summary>
        private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == UsbNotification.WmDevicechange)
                switch ((int) wparam)
                {
                    case UsbNotification.DbtDeviceremovecomplete:

                        Console.WriteLine("USB device removed");
                        _joystickClient.deviceRemoved();

                        break;
                    case UsbNotification.DbtDevicearrival:

                        Console.WriteLine("USB device connected, poll for joysticks again...");
                        _joystickClient.beginJoystickPoll();

                        break;
                }

            handled = false;
            return IntPtr.Zero;
        }


        private void PageLoaded(object? sender, EventArgs eventArgs)
        {
            // check for an update to app first
            Versioning.checkForUpdate();

            _receiver.DataReceived += receiver_DataReceived;
            _receiver.StartListening();

            // Start JoystickHelper async
            //Task.Run(() => { _joystickClient.beginJoystickPoll(); });
        }

        #endregion

        #region Networking

        /*
            Connections to API, reading in values, etc.
            ===========================
        */


        private void receiver_DataReceived(object sender, EventArgs e)
        {
            var data = (byte[]) sender;

            var apiServerInfo = JsonSerializer.Deserialize<APIServerInfoLegacy>(Encoding.UTF8.GetString(data));

            if (apiServerInfo != null)
            {
                Console.WriteLine("Received Server Info from: {0}:{1}", apiServerInfo.Address, apiServerInfo.Port);
                _receiver.Stop();
                if (apiServerInfo.Address != null)
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Legacy version

                        MessageBoxManager
                            .GetMessageBoxStandardWindow("There was a problem",
                                "The version of Infinite Flight you are trying to connect to is no longer supported. Please update Infinite Flight in the App Store or the Google Play Store to the latest version.",
                                ButtonEnum.Ok, MessageBox.Avalonia.Enums.Icon.Warning)
                            .Show();
                        Close();
                    });
                else
                    // Use new method
                    DataReceivedNewMethod(data);
            }
            else
            {
                Console.WriteLine("Invalid Server Info Received with old method");
                DataReceivedNewMethod(data);
            }
        }

        private void DataReceivedNewMethod(byte[] data)
        {
            Console.WriteLine("Attempting to connect with new method...");
            var apiServerInfo = JsonSerializer.Deserialize<Fds.IFAPI.APIServerInfo>(Encoding.UTF8.GetString(data));

            if (apiServerInfo != null)
            {
                Console.WriteLine("Received Server Info from: {0}:{1}", apiServerInfo.Addresses.ToString(),
                    apiServerInfo.Port);
                _receiver.Stop();
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var ipToConnectTo = apiServerInfo.Addresses[0];
                    for (var i = 0; i < apiServerInfo.Addresses.Length; i++)
                    {
                        // Prefer IPv4 if available
                        var match = Regex.Match(apiServerInfo.Addresses[i], @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                        if (match.Success) ipToConnectTo = match.Value;
                    }

                    Connect(IPAddress.Parse((string) ipToConnectTo), apiServerInfo.Port);
                });
            }
            else
            {
                Console.WriteLine("Invalid Server Info Received");
            }
        }

        private void Connect(IPAddress iPAddress, int port)
        {
            Client.Connect(iPAddress.ToString(), port);
            _FMSControl.Client = Client;

            // set connected bool
            _connectionStatus = true;

            // set label text
            _ipLabel.Text = string.Format("Infinite Flight is at {0}", iPAddress);

            _overlayGrid.IsVisible = false;
            _mainTabControl.IsVisible = true;

            Client.CommandReceived += client_CommandReceived;
            Client.Disconnected += client_Disconnected;

            Client.SendCommand(new APICall {Command = "InfiniteFlight.GetStatus"});
            Client.SendCommand(new APICall {Command = "Live.EnableATCMessageListUpdated"});

            Task.Run(() =>
            {
                while (_connectionStatus)
                    try
                    {
                        Client.SendCommand(new APICall {Command = "Airplane.GetState"});
                        Thread.Sleep(200);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception whilst getting aircraft state: {0}", ex);
                    }
            });

            Task.Run(() =>
            {
                while (_connectionStatus)
                    try
                    {
                        Client.SendCommand(new APICall {Command = "Live.GetTraffic"});
                        Client.SendCommand(new APICall {Command = "Live.ATCFacilities"});

                        Thread.Sleep(5000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception whilst getting Live state: {0}", ex);
                    }
            });
        }

        private void client_Disconnected(object? sender, IFConnect.CommandReceivedEventArgs commandReceivedEventArgs)
        {
            Dispatcher.UIThread.InvokeAsync(delegate { connectionLost(); });
        }

        private void connectionLost()
        {
            if (_connectionStatus)
            {
                _connectionStatus = false;
                _overlayGrid.IsVisible = true;
                _mainTabControl.IsVisible = false;
                Client = new IFConnect.IFConnectorClient();
                _receiver.Stop();
                _receiver.StartListening();
            }
        }

        private void client_CommandReceived(object sender, IFConnect.CommandReceivedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // System.Diagnostics.Debug.WriteLine(e.CommandString);
                    var type = typeof(IFAPIStatus).Assembly.GetType(e.Response.Type);

                    if (type == typeof(APIAircraftState))
                    {
                        var state = JsonSerializer.Deserialize<APIAircraftState>(e.CommandString);

                        // convert to fpm
                        state.VerticalSpeed =
                            float.Parse(
                                Convert.ToString(state.VerticalSpeed * 200, CultureInfo.InvariantCulture.NumberFormat),
                                CultureInfo.InvariantCulture
                                    .NumberFormat); // multiply by 200, this somehow gets it accurate..

                        _airplaneStateGrid.DataContext = null;
                        _airplaneStateGrid.DataContext = state;
                        _pAircraftState = state;
                        if (_FMSControl.autoFplDirectActive) _FMSControl.updateAutoNav(state);
                        if (_FMSControl.HoldingActive) _FMSControl.performHold(state);
                        _AircraftStateControl.AircraftState = state;
                        _AttitudeIndicator.updateAttitude(state.Pitch, state.Bank);
                        updateLandingRoll(state);
                    }
                    else if (type == typeof(GetValueResponse))
                    {
                        var state = JsonSerializer.Deserialize<GetValueResponse>(e.CommandString);

                        Console.WriteLine("{0} -> {1}", state.Parameters[0].Name, state.Parameters[0].Value);
                    }
                    else if (type == typeof(APIATCMessage))
                    {
                        var msg = JsonSerializer.Deserialize<APIATCMessage>(e.CommandString);

                        //Handle the ATC message to control the autopilot if enabled by checkbox
                        _FMSControl.handleAtcMessage(msg, _pAircraftState);

                        // TODO client.ExecuteCommand("Live.GetCurrentCOMFrequencies");
                    }
                    else if (type == typeof(ATCMessageList))
                    {
                        var msg = JsonSerializer.Deserialize<ATCMessageList>(e.CommandString);
                        _atcMessagesDataGrid.Items = msg.ATCMessages;
                    }
                    else if (type == typeof(APIFlightPlan))
                    {
                        var msg = JsonSerializer.Deserialize<APIFlightPlan>(e.CommandString);
                        Console.WriteLine("Flight Plan: {0} items", msg.Waypoints.Length);
                        _FMSControl.FplReceived(msg); //Update FMS with FPL from IF.
                        foreach (var item in msg.Waypoints)
                            Console.WriteLine(" -> {0} {1} - {2}, {3}", item.Name, item.Code, item.Latitude,
                                item.Longitude);
                    }
                    else if (type == typeof(APIAutopilotState))
                    {
                        _FMSControl.APState = Serializer.DeserializeJson<APIAutopilotState>(e.CommandString);
                    }
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Disconnected from server!");
                    //Let the client handle the lost connection.
                    //connectionStatus = false;
                }
            });
        }

        private void updateLandingRoll(APIAircraftState state)
        {
            if (_pLastState == null)
            {
                _pLastState = state;
            }
            else if (!state.IsOnGround)
            {
                _pLastState = state;
                _pLastLandingRoll = 0.0;
                _pLandingLocation = null;
                _txtLandingRoll.IsVisible = false;
                _txtLandingRollLabel.IsVisible = false;
                //btnViewLandingStats.Visibility = Visibility.Hidden;
            }
            else if (!_pLastState.IsLanded && state.IsLanded
            ) //Just transitioned to "landed" state, so start the roll accumulation
            {
                _pLandingLocation = state.Location;
                _pStateJustBeforeTouchdown = _pLastState;
                _pStateJustAfterTouchdown = state;
                _pLastState = state;
            }
            else if (state.IsLanded && _pLandingLocation != null) //We are in landed state. Calc the roll length.
            {
                var currentPosition = state.Location;
                var R = 3959; // Radius of the earth in miles
                var dLat = (currentPosition.Latitude - _pLandingLocation.Latitude) * (Math.PI / 180);
                var dLon = (currentPosition.Longitude - _pLandingLocation.Longitude) * (Math.PI / 180);
                var a =
                        Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(currentPosition.Latitude * (Math.PI / 180)) *
                        Math.Cos(_pLandingLocation.Latitude * (Math.PI / 180)) *
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2)
                    ;
                var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                var d = R * c; // Distance in miles
                d *= 5280; //Distance in ft
                _txtLandingRoll.Text = string.Format("{0:0.00}", d) + " ft";
                _txtLandingRoll.IsVisible = true;
                _txtLandingRollLabel.IsVisible = true;
                _pLastState = state;
                _landingDetails.updateLandingStats(_pLandingLocation, _pAircraftState.Location,
                    _pStateJustBeforeTouchdown, _pStateJustAfterTouchdown, "");
            }

            if (_pLandingLocation != null && state.IsLanded && state.IsOnGround && state.GroundSpeedKts < 10)
            {
                _pLandingStatDlg = new LandingStats();
                _pLandingStatDlg.updateLandingStats(_pLandingLocation, _pAircraftState.Location,
                    _pStateJustBeforeTouchdown, _pStateJustAfterTouchdown, "");
                _landingDetails.updateLandingStats(_pLandingLocation, _pAircraftState.Location,
                    _pStateJustBeforeTouchdown, _pStateJustAfterTouchdown, "");
                //btnViewLandingStats.Visibility = Visibility.Visible;
            }
        }

        private void btnViewLandingStats_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = "Last Landing Stats",
                Content = _pLandingStatDlg,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false
            };

            window.ShowDialog(this);
        }

        #endregion


        #region Keyboard commands

        /*
            Keyboard Commands
            ===========================
        */

        private void keyDownEvent(object? sender, KeyEventArgs keyEventArgs)
        {
            // check if a field is focused
            if (KeyboardCommandHandler.keyboardCommandsDisabled != FlightPlanDatabase.FlightPlanDb.textFieldFocused)
                KeyboardCommandHandler.keyboardCommandsDisabled = FlightPlanDatabase.FlightPlanDb.textFieldFocused;

            if (KeyboardCommandHandler.keyboardCommandsDisabled != IF_FMS.FMS.textFieldFocused)
                KeyboardCommandHandler.keyboardCommandsDisabled = IF_FMS.FMS.textFieldFocused;

            Console.WriteLine("Key pressed: {0}", keyEventArgs.Key);

            KeyboardCommandHandler.keyPressed(keyEventArgs.Key);
        }

        private void keyUpEvent(object? sender, KeyEventArgs keyEventArgs)
        {
            Console.WriteLine("KeyUp: {0}", keyEventArgs.Key);

            KeyboardCommandHandler.keyUp(keyEventArgs.Key);
        }

        #endregion

        #region Menu items

        /*
            Menu Items
            ===========================
        */

        // Camera menu

        private void nextCameraMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.nextCamera();
        }

        private void previousCameraMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.previousCamera();
        }

        private void cockpitCameraMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.cockpitCamera();
        }

        private void virtualCockpitCameraMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.vcCamera();
        }

        private void followCameraMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.followCamera();
        }

        private void onBoardCameraMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.onboardCamera();
        }

        private void fybyCameraMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.flybyCamera();
        }

        private void towerCameraMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.towerCamera();
        }

        //  Controls menu

        private void landingGearMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.landingGear();
        }

        private void spoilersMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.spoilers();
        }

        private void flapsUpMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.flapsUp();
        }

        private void flapsDownMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.flapsDown();
        }

        private void parkingBrakesMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.parkingBrake();
        }

        private void autopilotMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.autopilot();
        }

        private void pushbackMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.pushback();
        }

        private void pauseMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.pause();
        }

        //  Lights menu

        private void landingLightsMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.landing();
        }

        private void strobeLightsMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.strobe();
        }

        private void navLightsMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.nav();
        }

        private void beaconLightsMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.beacon();
        }

        //  Live menu

        private void atcWindowMenu_Click(object sender, RoutedEventArgs e)
        {
            Commands.atcMenu();
        }

        //  Help menu

        private void joystickSetupGuide(object sender, RoutedEventArgs e)
        {
            // go to liveflight help site
            var URL = "http://help.liveflightapp.com";
            Process.Start(URL);
        }

        private void sourceCodeMenu_Click(object sender, RoutedEventArgs e)
        {
            // go to GitHub
            var URL = "https://github.com/LiveFlightApp/Connect-Windows";
            Process.Start(URL);
        }

        private void communityMenu_Click(object sender, RoutedEventArgs e)
        {
            // go to Community
            var URL = "http://community.infinite-flight.com/?u=carmalonso";
            Process.Start(URL);
        }

        private void liveFlightMenu_Click(object sender, RoutedEventArgs e)
        {
            // go to LiveFlight
            var URL = "http://www.liveflightapp.com";
            Process.Start(URL);
        }

        private void lfFacebookMenu_Click(object sender, RoutedEventArgs e)
        {
            // go to LiveFlight Facebook
            var URL = "http://facebook.com/LiveFlightApp/";
            Process.Start(URL);
        }

        private void lfTwitterMenu_Click(object sender, RoutedEventArgs e)
        {
            // go to LiveFlight Twitter
            var URL = "http://twitter.com/LiveFlightApp/";
            Process.Start(URL);
        }

        private void aboutLfMenu_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.Show();
        }

        #endregion
    }
}