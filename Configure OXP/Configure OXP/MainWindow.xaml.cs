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
            //await CallScript(ConfigState.PathFolderIIS, TB_PatchedDbs);
            //ConfigState = DB.PatchDBIfFilesFound(ConfigState);
            
            s1.Stop();
        }

        private void BTN_LocationOfIisSite_Click(object sender, RoutedEventArgs e)
        {
            var path = FolderPicker(TB_LocationOfIisSite);
            //ConfigState.PathFolderIIS = path;
        }

        private void BTN_LocationOfGitSite_Click(object sender, RoutedEventArgs e)
        {
            var path = FolderPicker(TB_LocationOfGitSite);
            //ConfigState.PathFolderGIT = path;
        }

        private void BTN_ImportDB_Click(object sender, RoutedEventArgs e)
        {
            API.ImportBacPacs(ConfigState);
            //ConfigState = DB.Import(ConfigState);
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

        private async Task<List<Collection<PSObject>>> CallScript(string pPathFolderIIS, TextBlock pTextBlock)
        {
            var ps = GetPS();

            return await Task.Run(
                () =>
                GetPathBackpacsNotPatched(pPathFolderIIS).
                Select(UpdateControll(pTextBlock)).
                AsParallel().
                Select(Patch(ps)).
                ToList());
        }

        private Func<string, Collection<PSObject>> Patch(string ps) =>
            (pPathToDB) =>
        {
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                PowerShellInstance.AddScript(ps);
                PowerShellInstance.AddParameter("bacpacPath", pPathToDB);
                Collection<PSObject> PSOutput = PowerShellInstance.Invoke();
                //UpdateControll(TB_PatchedDbs, "done")(pPathToDB);
                return PSOutput;
            }
        };

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

        private string GetPS()
        {
            var path = Path.Combine(GetProjectDirectory(), "PS", "RemoveMasterKeyLT4GB.ps1");
            var res = File.ReadAllText(path);
            return res;
        }

        private string GetProjectDirectory()
        {
            string res = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
            return res;
        }

        private IEnumerable<string> GetPathBackpacsNotPatched(string pPathFolderIIS)
        {
            var path = Path.Combine(pPathFolderIIS, "Databases");
            var res = Directory.GetFiles(path).Where(z => z.Contains(".bacpac") && !z.Contains("-patched"));
            return res;
        }

        private IEnumerable<string> GetPathBackpacsPatched(string pPathFolderIIS)
        {
            var path = Path.Combine(pPathFolderIIS, "Databases");
            var res = Directory.GetFiles(path).Where(z => z.Contains(".bacpac") && !z.Contains("-patched"));
            return res;
        }
    }
}
