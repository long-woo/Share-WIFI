using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Windows.Threading;
using MahApps.Metro.Controls;
using NETCONLib;
using System.Windows.Forms;

namespace ShareWIFI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            IconShow();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string result = StartCmd("netsh.exe", "wlan show hostednetwork");
            if (result.IndexOf("未配置") <= -1)
            {
                var startVal_1 = result.IndexOf("“");
                var endVal_1 = result.LastIndexOf("”");
                string name = result.Substring(startVal_1 + 1, endVal_1 - startVal_1 - 1);
                txtNetworkName.Text = name;
            }
        }

        //关于
        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            gridOpacity.Visibility = Visibility.Visible;
            gridDialog.Visibility = Visibility.Visible;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            gridOpacity.Visibility = Visibility.Hidden;
            gridDialog.Visibility = Visibility.Hidden;
        }

        //退出
        private void menuQuit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void chkIsEnable_IsCheckedChanged(object sender, EventArgs e)
        {
            if (chkIsEnable.IsChecked == true)
            {
                borderSite.IsEnabled = true;
            }
            else
            {
                progressBar.IsActive = true;
                borderSite.IsEnabled = false;
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        string result = StartCmd("netsh.exe", "wlan stop hostednetwork");
                        if (result.IndexOf("已停止") > -1)
                        {
                            JShareWIFI(false);
                        }
                    }));
                }).Start();

            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {

            if (txtNetworkName.Text.Trim() == "")
            {
                lblStatus.Content = lblStatus.ToolTip = "Please enter the network name.";
                txtNetworkName.Focus();
                return;
            }

            if (txtPassword.Password.Trim() == "")
            {
                lblStatus.Content = lblStatus.ToolTip = "Please enter the password.";
                txtPassword.Focus();
                return;
            }

            if (txtPassword.Password.Trim().Length < 8)
            {
                lblStatus.Content = lblStatus.ToolTip = "Please enter a password length of 8 or more.";
                txtPassword.SelectAll();
                return;
            }

            if (txtNetworkName.Text.Trim() == "" && txtPassword.Password.Trim() == "")
            {
                lblStatus.Content = lblStatus.ToolTip = "Please enter complete information.";
                txtNetworkName.Focus();
                return;
            }

            progressBar.IsActive = true;
            borderSite.IsEnabled = false;
            lblStatus.Content = lblStatus.ToolTip = "Network creating...";

            try
            {
                new Thread(() =>
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        string fileName = "netsh.exe";
                        string argument_1 = "wlan set hostednetwork mode=allow ssid=" + txtNetworkName.Text.Trim() + " key=" + txtPassword.Password.Trim();
                        string argument_2 = "wlan start hostednetwork";
                        string result = StartCmd(fileName, argument_1);
                        if (result.IndexOf("已成功") > -1)
                        {
                            string startResult = StartCmd(fileName, argument_2);
                            if (startResult.IndexOf("已启动") > -1)
                            {
                                JShareWIFI(true);

                                borderSite.IsEnabled = true;
                                lblStatus.Content = lblStatus.ToolTip = "Network create successful.";
                            }
                            else
                            {
                                progressBar.IsActive = false;
                                lblStatus.Content = lblStatus.ToolTip = "Network create failure,ensure that this computer provides wireless networking capabilities.";
                                borderSite.IsEnabled = true;
                            }
                        }
                        else
                        {
                            progressBar.IsActive = false;
                            lblStatus.Content = lblStatus.ToolTip = "Network create failure,insufficient permissions, you must run this program as administrator.";
                            borderSite.IsEnabled = true;
                        }
                    }));
                }).Start();
            }
            catch (Exception ex)
            {
                progressBar.IsActive = false;
                borderSite.IsEnabled = true;
                lblStatus.Content = ex.Message;
                lblStatus.ToolTip = ex.Message;
            }
        }

        private void JShareWIFI(bool isShare)
        {
            try
            {
                NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                string currentIP = "";
                if (isShare)
                {
                    currentIP = Dns.GetHostAddresses(Dns.GetHostName()).GetValue(3).ToString();
                }
                else
                {
                    currentIP = Dns.GetHostAddresses(Dns.GetHostName()).GetValue(2).ToString();
                }
                string currentNetwork = "";
                string shareNetwork = adapters[0].Name;
                foreach (NetworkInterface adapter in adapters)
                {
                    IPInterfaceProperties ip = adapter.GetIPProperties();
                    if (ip.UnicastAddresses[1].Address.ToString() == currentIP)
                    {
                        currentNetwork = adapter.Name;
                        break;
                    }
                }
                var manager = new NetSharingManager();
                var conn = manager.EnumEveryConnection;
                foreach (INetConnection netConn in conn)
                {
                    var props = manager.NetConnectionProps[netConn];
                    var sharingCfg = manager.INetSharingConfigurationForINetConnection[netConn];
                    if (isShare)
                    {
                        if (props.Name == currentNetwork)
                        {
                            sharingCfg.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);
                        }
                        else if (props.Name == shareNetwork)
                        {
                            sharingCfg.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
                        }
                    }
                    else
                    {
                        sharingCfg.DisableSharing();
                    }
                }
            }
            catch (Exception e)
            {
                lblStatus.Content = lblStatus.ToolTip = "Operation fails, you must run this program as an administrator.";
                chkIsEnable.IsChecked = true;
            }
            progressBar.IsActive = false;
        }

        private string StartCmd(string fileName, string argument)
        {
            string result = "";
            try
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = fileName;
                cmd.StartInfo.Arguments = argument;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmd.Start();
                result = cmd.StandardOutput.ReadToEnd();
                cmd.WaitForExit();
                cmd.Close();
            }
            catch (Exception e)
            {
                result = e.Message;
            }
            return result;
        }


        private void MetroWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void IconShow()
        {
            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.BalloonTipText = "";
            this.notifyIcon.Text = "";
            this.notifyIcon.Icon = ShareWIFI.Properties.Resources.logo; //new System.Drawing.Icon("logo.ico");//程序图标
            this.notifyIcon.Visible = true;
            this.notifyIcon.Click += notityIcon_Click;
        }

        private void notityIcon_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Topmost = true;
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            this.notifyIcon.Visible = false;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/JasonLong-7/Share-WIFI");
        }

    }
}
