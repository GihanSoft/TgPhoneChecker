using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TeleSharp.TL;
using TLSharp.Core;

namespace TgPhoneChecker.Views
{
    /// <summary>
    /// Interaction logic for WinLogin.xaml
    /// </summary>
    public partial class WinLogin : Window
    {
        public WinLogin()
        {
            InitializeComponent();
        }

        private void CodePage()
        {
            TxtId.IsEnabled = false;
            TxtHash.IsEnabled = false;
            //TxtPhone.IsEnabled = false;
            (Content as Grid).RowDefinitions.Add(new RowDefinition());

            TxtAuthCode.Visibility = Visibility.Visible;
            TbAuthCode.Visibility = Visibility.Visible;
            MaxHeight = MinHeight = MaxHeight * 5 / 4;

            Grid.SetColumn(BtnLogin, 1);
            Grid.SetColumnSpan(BtnLogin, 1);
            BtnLogin.Content = "Login";
            BtnCancel.Visibility = Visibility.Visible;
        }

        private void InputPage()
        {
            TxtId.IsEnabled = true;
            TxtHash.IsEnabled = true;
            //TxtPhone.IsEnabled = true;
            (Content as Grid).RowDefinitions.RemoveAt(0);

            TxtAuthCode.Visibility = Visibility.Collapsed;
            TbAuthCode.Visibility = Visibility.Collapsed;
            MaxHeight = MinHeight = MaxHeight * 4 / 5;

            Grid.SetColumn(BtnLogin, 0);
            Grid.SetColumnSpan(BtnLogin, 2);
            BtnLogin.Content = "Connect";
            BtnCancel.Visibility = Visibility.Collapsed;
        }

        private void PasswordPage()
        {
            TxtAuthCode.IsEnabled = false;
            (Content as Grid).RowDefinitions.Add(new RowDefinition());
            TxtPassword.Visibility = Visibility.Visible;
            TxtPassword.Visibility = Visibility.Visible;
            MaxHeight = MinHeight = MaxHeight * 6 / 5;
        }

        public string LastAuthHash { get; set; }
        public TelegramClient LastClient { get; set; }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (TxtId.IsEnabled)
            {
                var apiIdParsed = int.TryParse(TxtId.Text, out var apiId);
                if (!apiIdParsed)
                {
                    TxtId.Focus();
                    TxtId.SelectAll();
                    return;
                }
                LastClient = new TelegramClient(apiId, TxtHash.Text);
                try
                {
                    await LastClient.ConnectAsync(); // err here
                    LastAuthHash = await LastClient.SendCodeRequestAsync(TxtPhone.Text);
                    CodePage();
                }
                catch
                {
                    Button_Click(sender, e);
                }
            }
            else
            {
                if (TxtPassword.Visibility != Visibility.Visible)
                {
                    try
                    {
                        await LastClient.MakeAuthAsync(TxtPhone.Text, LastAuthHash, TxtAuthCode.Text);
                        var win = new WinMain(LastClient);
                        win.Show();
                        Close();
                    }
                    catch (CloudPasswordNeededException)
                    {
                        PasswordPage();
                    }
                }
                else
                {
                    try
                    {
                        var password = await LastClient.GetPasswordSetting(); // err here
                        var password_str = TxtPassword.Text;

                        await LastClient.MakeAuthWithPasswordAsync(password, password_str);

                        var win = new WinMain(LastClient);
                        win.Show();
                        Close();
                    }
                    catch
                    {
                        Button_Click(sender, e);
                    }
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            InputPage();
        }
    }
}
