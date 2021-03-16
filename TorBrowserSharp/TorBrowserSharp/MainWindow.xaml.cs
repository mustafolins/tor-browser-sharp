using CefSharp;
using Knapcode.TorSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

namespace TorBrowserSharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public TorSharpProxy Proxy { get; set; }

        public MainWindow()
        {
            _ = RefreshTorProxy(Proxy);

            InitializeComponent();
        }

        public async Task RefreshTorProxy(TorSharpProxy proxy)
        {
            // configure tor sharp settings
            var settings = new TorSharpSettings
            {
                ZippedToolsDirectory = Path.Combine(Path.GetTempPath(), "TorZipped"),
                ExtractedToolsDirectory = Path.Combine(Path.GetTempPath(), "TorExtracted"),
                PrivoxySettings = { Port = 1337 },
                TorSettings =
                   {
                      SocksPort = 1338,
                      ControlPort = 1339,
                      ControlPassword = "foobar",
                   },
            };

            // download tools
            await new TorSharpToolFetcher(settings, new HttpClient()).FetchAsync();

            // start proxy
            proxy = new TorSharpProxy(settings);
            await proxy.ConfigureAndStartAsync();
            // get new identity
            await proxy.GetNewIdentityAsync();
        }

        public async Task SetCefProxySettings()
        {
            await Task.Delay(10_000);
            await Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var rc = Browser.GetBrowser().GetHost().RequestContext;
                var values = new Dictionary<string, object>
                {
                    ["mode"] = "fixed_servers",
                    ["server"] = "socks://localhost:1338"
                };
                bool success = rc.SetPreference("proxy", values, out string error); // todo: do something with error
            });
        }

        private void Browser_Initialized(object sender, EventArgs e)
        {
            // make chromium use tor proxy
            _ = SetCefProxySettings();
        }

        private void Anonymize_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshTorProxy(Proxy);
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            Browser.Load(UrlTextbox.Text);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Browser.Back();
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            Browser.Forward();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Proxy?.Stop();
        }

        private void UrlTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Browser.Load(UrlTextbox.Text);
            }
        }
    }
}
