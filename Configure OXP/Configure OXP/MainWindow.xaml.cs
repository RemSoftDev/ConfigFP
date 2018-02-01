using ConfigFP;
using ConfigFP.Types;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        Types.ConfigState ConfigState;

        public MainWindow()
        {
            InitializeComponent();
            ConfigState = new Types.ConfigState(
                 @"C:\\inetpub\\wwwroot\\volvo",
                 @"C:\Users\oleksandr.dubyna\Documents\GIT\integration\Volvo.Web",
                 "sa",
                 "1qaz!QAZ",
                 "LVAGPLTP2642",
                 null,
                 "Database"
                   );

            TB_LocationOfIisSite.Text = ConfigState.PathFolderIIS;
            TB_LocationOfGitSite.Text = ConfigState.PathFolderGIT;
            TB_DbUser.Text = ConfigState.DbUser;
            TB_DbPassword.Password = ConfigState.DbPassword;
            TB_DbServerName.Text = ConfigState.DbServerName;

            ConfigState = API.Init(
                ConfigState.DbUser,
                ConfigState.DbPassword,
                ConfigState.DbServerName,
                ConfigState.PathFolderIIS,
                ConfigState.PathFolderIIS,
                UpdateControll(TB_PatchedDbs));
        }

        private async void BTN_PatchDB_Click(object sender, RoutedEventArgs e)
        {
            var s1 = Stopwatch.StartNew();
            API.PatchBacPacs(ConfigState);
            s1.Stop();
        }

        private void BTN_LocationOfIisSite_Click(object sender, RoutedEventArgs e)
        {
            var path = FolderPicker(TB_LocationOfIisSite);
        }

        private void BTN_LocationOfGitSite_Click(object sender, RoutedEventArgs e)
        {
            var path = FolderPicker(TB_LocationOfGitSite);
        }

        private void BTN_ImportDB_Click(object sender, RoutedEventArgs e)
        {
            API.ImportBacPacs(ConfigState);
        }

        private string FolderPicker(TextBlock pTextBlock)
        {
            var fileDialog = new System.Windows.Forms.FolderBrowserDialog();
            var result = fileDialog.ShowDialog();
            string path = null;

            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    path = fileDialog.SelectedPath;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    break;
            }

            pTextBlock.Text = path;

            return path;
        }

        private Func<string, string> UpdateControll(TextBlock pTextBlock) =>
            (pText) =>
            {
                pTextBlock.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                        new Action(delegate ()
                        {
                            pTextBlock.Text += pText + Environment.NewLine;
                        }));

                return pText;
            };
    }
}
