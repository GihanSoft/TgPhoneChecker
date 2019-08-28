using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for WinMain.xaml
    /// </summary>
    public partial class WinMain : Window
    {
        private TelegramClient Client { get; set; }
        private Logic.PhoneExistChecker Checker { get; set; }
        private List<string> PhoneList { get; set; }

        public WinMain(TelegramClient client/*, TLUser user = null*/)
        {
            InitializeComponent();
            Client = client;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "TextFiles(.txt)|*.txt"
            };
            var result = dialog.ShowDialog();
            if (!result ?? false)
                return;
            TxtFilePath.Text = dialog.FileName;
        }

        //private bool isWorking = false;
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (PhoneList is null)
            {
                var reader = new StreamReader(new FileStream(TxtFilePath.Text, FileMode.Open, FileAccess.Read));
                PhoneList = reader.ReadToEnd().Split('\n').Select(p => p.Trim('\r')).ToList();
                reader.Dispose();
                Checker = new Logic.PhoneExistChecker(Client, PhoneList);
                Checker.OnePhoneChecked += Checker_OnePhoneChecked;
                Checker.Start();
                BtnStart.Visibility = Visibility.Collapsed;
                BtnCancel.Visibility = Visibility.Visible;
                BtnPause.Visibility = Visibility.Visible;
            }
            else
            {
                Checker.Start();
                BtnStart.Visibility = Visibility.Collapsed;
                BtnCancel.Visibility = Visibility.Visible;
                BtnPause.Visibility = Visibility.Visible;
            }
        }

        private void Checker_OnePhoneChecked(object sender, KeyValuePair<string, bool?> e)
        {
            var tb = new TextBlock
            {
                Text = e.Key,
                Background = new SolidColorBrush(
                    (e.Value ?? false) ? Colors.LightGreen : Color.FromRgb(255, 200, 200))
            };
            Sp.Children.Add(tb);
            var pack = new ExcelPackage(new FileInfo(TxtSaveFilePath.Text));
            //pack.Save();
            var sheet = pack.Workbook.Worksheets.Count == 0 ?
                        pack.Workbook.Worksheets.Add("Results")
                        : pack.Workbook.Worksheets["Results"];
            pack.Save();
            var table = sheet.Tables.Count == 0 ?
                sheet.Tables.Add(new ExcelAddress("A1:B2"), "Results")
                : sheet.Tables[0];
            table.Columns[0].Name = "Phone";
            table.Columns[1].Name = "IsExist";
            table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium6;
            sheet.InsertRow(table.Address.End.Row, 1);
            sheet.Cells[table.Address.End.Row - 1, 1].Value = e.Key;
            sheet.Cells[table.Address.End.Row - 1, 2].Value = e.Value;
            pack.Save();
            pack.Dispose();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            Checker.Pause();
            BtnStart.Visibility = Visibility.Visible;
            BtnPause.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(BtnStart, 1);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Checker.Pause();
            BtnStart.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Collapsed;
            BtnPause.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(BtnStart, 2);
            PhoneList = null;
        }

        private void BtnSaveBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "TextFiles(.xlsx)|*.xlsx"
            };
            var result = dialog.ShowDialog();
            if (!result ?? false)
                return;
            TxtSaveFilePath.Text = dialog.FileName;
        }
    }
}
