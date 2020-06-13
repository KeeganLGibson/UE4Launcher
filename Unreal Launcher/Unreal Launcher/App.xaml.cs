using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Squirrel;
using System.Windows;

namespace Unreal_Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            CheckForUpdates();
        }

        static async Task CheckForUpdates()
        {
            // Check for Updates
            using (var mgr = UpdateManager.GitHubUpdateManager("https://github.com/KeeganLGibson/UE4Launcher"))
            {
                using (var Result = await mgr)
                {
                    await Result.UpdateApp();
                }
            }
        }
    }
}
