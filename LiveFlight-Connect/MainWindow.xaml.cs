﻿//
//  MainWindow.xaml.cs
//  LiveFlight Connect
//
//  Copyright © 2015 Cameron Carmichael Alonso. All rights reserved.
//
//  Licensed under GPL-V3.
//  https://github.com/LiveFlightApp/Connect-Windows/blob/master/LICENSE
//

using Fds.IFAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace LiveFlight
{

    public partial class MainWindow : Window
    {
        public static IFConnectorClient client = new IFConnectorClient();
        public static Commands commands = new Commands();
        BroadcastReceiver receiver = new BroadcastReceiver();

        public MainWindow()
        {
            InitializeComponent();

            airplaneStateGrid.DataContext = null;
            airplaneStateGrid.DataContext = new APIAircraftState();

            mainTabControl.Visibility = System.Windows.Visibility.Collapsed;

            Console.WriteLine("LiveFlight Connect\nPlease send this log to cameron@liveflightapp.com if you experience issues. Thanks!\n\n");
        }

        bool serverInfoReceived = false;

        void receiver_DataReceived(object sender, EventArgs e)
        {
            byte[] data = (byte[])sender;

            var apiServerInfo = Serializer.DeserializeJson<APIServerInfo>(UTF8Encoding.UTF8.GetString(data));

            if (apiServerInfo != null)
            {
                Console.WriteLine("Received Server Info from: {0}:{1}", apiServerInfo.Address, apiServerInfo.Port);
                serverInfoReceived = true;
                receiver.Stop();
                Dispatcher.BeginInvoke((Action)(() => 
                {
                    Connect(IPAddress.Parse(apiServerInfo.Address), apiServerInfo.Port);
                }));
            }
            else
            {
                Console.WriteLine("Invalid Server Info Received");
            }
        }

        private void Connect(IPAddress iPAddress, int port)
        {
            client.Connect(iPAddress.ToString(), port);

            // set label text
            ipLabel.Content = String.Format("Infinite Flight is at {0}", iPAddress.ToString());
           
            overlayGrid.Visibility = System.Windows.Visibility.Collapsed;
            mainTabControl.Visibility = System.Windows.Visibility.Visible;

            client.CommandReceived += client_CommandReceived;

            client.SendCommand(new APICall { Command = "InfiniteFlight.GetStatus" });
            client.SendCommand(new APICall { Command = "Live.EnableATCMessageListUpdated" });            

            Task.Run(() =>
            {

                while (true)
                {
                    try
                    {
                        client.SendCommand(new APICall { Command = "Airplane.GetState" });

                        Thread.Sleep(2000);

                    }
                    catch (Exception ex)
                    {

                    }
                }
            });

            Task.Run(() =>
            {

                while (true)
                {
                    try
                    {
                        client.SendCommand(new APICall { Command = "Live.GetTraffic" });
                        client.SendCommand(new APICall { Command = "Live.ATCFacilities" });

                        Thread.Sleep(5000);

                    }
                    catch (Exception ex)
                    {

                    }
                }
            });
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            receiver.DataReceived += receiver_DataReceived;
            receiver.StartListening();
        }

        void client_CommandReceived(object sender, CommandReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => 
            {
                var type = Type.GetType(e.Response.Type);

                if (type == typeof(APIAircraftState))
                {
                    var state = Serializer.DeserializeJson<APIAircraftState>(e.CommandString);

                    airplaneStateGrid.DataContext = null;
                    airplaneStateGrid.DataContext = state;
                }
                else if (type == typeof(GetValueResponse))
                {
                    var state = Serializer.DeserializeJson<GetValueResponse>(e.CommandString);

                    Console.WriteLine("{0} -> {1}", state.Parameters[0].Name, state.Parameters[0].Value);
                }
                else if (type == typeof(LiveAirplaneList))
                {
                    var airplaneList = Serializer.DeserializeJson<LiveAirplaneList>(e.CommandString);

                    //airplaneDataGrid.ItemsSource = airplaneList.Airplanes;
                }
                else if (type == typeof(FacilityList))
                {
                    var facilityList = Serializer.DeserializeJson<FacilityList>(e.CommandString);

                    //facilitiesDataGrid.ItemsSource = facilityList.Facilities;
                }
                else if (type == typeof(IFAPIStatus))
                {
                    var status = Serializer.DeserializeJson<IFAPIStatus>(e.CommandString);


                }
                else if (type == typeof(APIATCMessage))
                {
                    var msg = Serializer.DeserializeJson<APIATCMessage>(e.CommandString);
                    // TODO client.ExecuteCommand("Live.GetCurrentCOMFrequencies");
                }
                else if (type == typeof(APIFrequencyInfoList))
                {
                    var msg = Serializer.DeserializeJson<APIFrequencyInfoList>(e.CommandString);
                }
                else if (type == typeof(ATCMessageList))
                {
                    var msg = Serializer.DeserializeJson<ATCMessageList>(e.CommandString);
                    atcMessagesDataGrid.ItemsSource = msg.ATCMessages;
  
                }
                else if (type == typeof(APIFlightPlan))
                {
                    var msg = Serializer.DeserializeJson<APIFlightPlan>(e.CommandString);
                    Console.WriteLine("Flight Plan: {0} items", msg.Waypoints.Length);

                    foreach (var item in msg.Waypoints)
                    {
                        Console.WriteLine(" -> {0} {1} - {2}, {3}", item.Name, item.Code, item.Latitude, item.Longitude);
                    }
                }             
            }));            
        }
        
        private void checkbox_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as System.Windows.Controls.CheckBox;

            if (checkbox.Equals(altitudeStateCheckbox))
                client.ExecuteCommand("Commands.Autopilot.SetAltitudeState", new CallParameter[] { new CallParameter { Value = checkbox.IsChecked.ToString() } });
            if (checkbox.Equals(headingStateCheckbox))
                client.ExecuteCommand("Commands.Autopilot.SetHeadingState", new CallParameter[] { new CallParameter { Value = checkbox.IsChecked.ToString() } });
            if (checkbox.Equals(verticalSpeedStateCheckbox))
                client.ExecuteCommand("Commands.Autopilot.SetVSState", new CallParameter[] { new CallParameter { Value = checkbox.IsChecked.ToString() } });
            if (checkbox.Equals(speedStateCheckbox))
                client.ExecuteCommand("Commands.Autopilot.SetSpeedState", new CallParameter[] { new CallParameter { Value = checkbox.IsChecked.ToString() } });
            if (checkbox.Equals(apprStateCheckbox))
                client.ExecuteCommand("Commands.Autopilot.SetApproachModeState", new CallParameter[] { new CallParameter { Value = checkbox.IsChecked.ToString() } });
        }

        private void speedTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBlock = sender as System.Windows.Controls.TextBox;

            if (textBlock.Equals(speedTextBlock))
                client.ExecuteCommand("Commands.Autopilot.SetSpeed", new CallParameter[] { new CallParameter { Value = textBlock.Text.ToString() } });
            if (textBlock.Equals(altitudeTextBlock))
                client.ExecuteCommand("Commands.Autopilot.SetAltitude", new CallParameter[] { new CallParameter { Value = textBlock.Text.ToString() } });
            if (textBlock.Equals(verticalSpeedTextBlock))
                client.ExecuteCommand("Commands.Autopilot.SetVS", new CallParameter[] { new CallParameter { Value = textBlock.Text.ToString() } });
            if (textBlock.Equals(headingTextBlock))
                client.ExecuteCommand("Commands.Autopilot.SetHeading", new CallParameter[] { new CallParameter { Value = textBlock.Text.ToString() } });

        }

        private void enableATCMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            client.ExecuteCommand("Live.EnableATCMessageNotification");
        }


        private void atcMessagesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var command = string.Format("Commands.ATCEntry{0}", atcMessagesDataGrid.SelectedIndex + 1);

            client.ExecuteCommand(command);            
        }
        
        Point lastMousePosition = new Point();


        private void captureMouseButton_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var position = e.GetPosition(this);

            var command = "NetworkMouse.SetPosition";
            
            client.ExecuteCommand(command, new CallParameter[]
            {
                new CallParameter { Name = "X", Value = ((int)position.X).ToString() }, 
                new CallParameter { Name = "Y", Value = ((int)position.Y).ToString() }
            });

            lastMousePosition = position;
        }

        private void captureMouseButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var position = lastMousePosition;

            var command = "NetworkMouse.MouseUp";

            client.ExecuteCommand(command, new CallParameter[]
            {
                new CallParameter { Name = "X", Value = ((int)position.X).ToString() }, 
                new CallParameter { Name = "Y", Value = ((int)position.Y).ToString() }
            });   
        }

        private void captureMouseButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = lastMousePosition;

            var command = "NetworkMouse.MouseDown";

            client.ExecuteCommand(command, new CallParameter[]
            {
                new CallParameter { Name = "X", Value = ((int)position.X).ToString() }, 
                new CallParameter { Name = "Y", Value = ((int)position.Y).ToString() }
            });   
        }

  

        protected bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Console.WriteLine("Key pressed: {0}", keyData.ToString());


            return true;
        }

        private void joystickSetupGuide(object sender, RoutedEventArgs e)
        {

            // go to community forums
            var forumURL = "https://community.infinite-flight.com/t/joysticks-on-ios-android-over-the-network-liveflight-connect/20017?u=carmalonso";
            System.Diagnostics.Process.Start(forumURL);

        }
    }
}
