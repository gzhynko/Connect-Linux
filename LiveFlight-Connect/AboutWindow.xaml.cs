using System;
using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LiveFlight
{
    /// <summary>
    ///     Interaction logic for AboutWindow.xaml
    /// </summary>
    public class AboutWindow : Window
    {
        private TextBlock _version;
        
        public AboutWindow()
        {
            InitializeComponent();
            InitializeUiElements();
        }

        private void InitializeUiElements()
        {
            _version = this.FindControl<TextBlock>("version");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void cameronButton_Click(object sender, RoutedEventArgs e)
        {
            var URL = "http://www.carmichaelalonso.co.uk";
            Process.Start(URL);
        }

        private void arButton_Click(object sender, RoutedEventArgs e)
        {
            var URL = "https://community.infinite-flight.com/users/ar_ar";
            Process.Start(URL);
        }

        private void licenseButton_Click(object sender, RoutedEventArgs e)
        {
            var URL = "https://github.com/LiveFlightApp/Connect-Windows/blob/master/LICENSE";
            Process.Start(URL);
        }

        private void sourceCode_Click(object sender, RoutedEventArgs e)
        {
            var URL = "https://github.com/LiveFlightApp/Connect-Windows/";
            Process.Start(URL);
        }

        private void liveFlightLink_Click(object sender, RoutedEventArgs e)
        {
            var URL = "http://www.liveflightapp.com";
            Process.Start(URL);
        }

        private void Window_Loaded(object sender, EventArgs eventArgs)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var assemblyVersion = fvi.FileVersion;
            Console.WriteLine(assemblyVersion);

            _version.Text = string.Format("Version {0}", assemblyVersion);
        }
    }
}
