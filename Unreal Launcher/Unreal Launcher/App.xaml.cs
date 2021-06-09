// Copyright (c) Keegan L Gibson. All rights reserved.

using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Squirrel;
using Unreal_Launcher.Properties;

namespace Unreal_Launcher
{
	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		private const string RepoUrl = "https://d1ekcf247n7aun.cloudfront.net";

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			CheckForUpdates();

			// Upgrade User Settings across versions.
			if (Settings.Default != null && Settings.Default.UpgradeRequired)
			{
				Settings.Default.Upgrade();
				Settings.Default.UpgradeRequired = false;
				Settings.Default.Save();
			}
		}

		private async void CheckForUpdates()
		{
			// Check for Updates
			using (var mgr = new UpdateManager(RepoUrl))
			{
				await mgr.UpdateApp();
			}
		}
	}
}
