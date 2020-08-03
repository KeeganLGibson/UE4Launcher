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
		private const string RepoUrl = "https://github.com/KeeganLGibson/UE4Launcher";

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Upgrade User Settings across versions.
			if (Settings.Default.UpgradeRequired)
			{
				Settings.Default.Upgrade();
				Settings.Default.UpgradeRequired = false;
				Settings.Default.Save();
			}

			CheckForUpdates();
		}

		private async void CheckForUpdates()
		{
			// Check for Updates
			using (Task<UpdateManager> mgr = UpdateManager.GitHubUpdateManager(RepoUrl))
			{
				using (UpdateManager result = await mgr)
				{
					await result.UpdateApp();
				}
			}
		}
	}
}
