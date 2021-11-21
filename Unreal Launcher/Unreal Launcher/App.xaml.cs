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

		public bool UpdateAvailable { get; private set; } = false;
		public string UpdateVersion { get; private set; } = string.Empty;

		public async void CheckForUpdates(System.Action<int> downloadProgressCallback, System.Action<int> installProgressCallback)
		{
			// Check for Updates
			using (var mgr = new UpdateManager(RepoUrl))
			{
				Task<UpdateInfo> updates = mgr.CheckForUpdate();
				await updates;
				if (updates.Result.ReleasesToApply.Count > 0)
				{
					UpdateAvailable = true;
					UpdateVersion = updates.Result.FutureReleaseEntry.EntryAsString;
				}

				if (UpdateAvailable)
				{
					await mgr.DownloadReleases(updates.Result.ReleasesToApply, downloadProgressCallback);
				}

				await mgr.ApplyReleases(updates.Result, installProgressCallback);
			}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Upgrade User Settings across versions.
			if (Settings.Default != null && Settings.Default.UpgradeRequired)
			{
				Settings.Default.Upgrade();
				Settings.Default.UpgradeRequired = false;
				Settings.Default.Save();
			}
		}
	}
}
