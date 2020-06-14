// Copyright (c) Keegan L Gibson. All rights reserved.

using System.Diagnostics;
using System.IO;
using System.Windows.Controls;

namespace Unreal_Launcher
{
	/// <summary>
	/// Represents a project in a tab.
	/// </summary>
	public partial class MainTab : UserControl
	{
		public MainTab(Project project)
		{
			InitializeComponent();

			Project = project;

			txtProjectNiceName.Text = Project.ProjectNiceName;
			lblProjectDir.Content = Project.ProjectDirectory;
			cbFullScreen.IsChecked = Project.LaunchSettings.FullScreen;

			FindAllMaps();
		}

		public Project Project { get; }

		private void FindAllMaps()
		{
			string[] files = Directory.GetFiles(Path.Combine(Project.ProjectDirectory, @".\Content\"), "*umap", SearchOption.AllDirectories);

			// add a black default;
			cmbMaps.Items.Add("(Default)");

			foreach (string file in files)
			{
				if (!file.Contains("Marketplace") && !file.Contains("StarterContent"))
				{
					cmbMaps.Items.Add(Path.GetFileNameWithoutExtension(file));
				}
			}

			// If no map selected use the project defaults.
			cmbMaps.SelectedItem = string.IsNullOrWhiteSpace(Project.LaunchSettings.LastSelectedMap) ? "(Default)" : Project.LaunchSettings.LastSelectedMap;
		}

		private void btnNewClass_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			NewClass newClassWindow = new NewClass(Project);
			newClassWindow.ShowDialog();
		}

		private void btnOpenEditor_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe"),
				Arguments = Project.GenerateEditorArguments(),
			};

			Process.Start(startInfo);
		}

		private void btnPlayGame_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe"),
				Arguments = Project.GenerateGameArguments(),
			};

			Process.Start(startInfo);
		}

		private void btnBrowseFolder_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			// opens explorer)
			Process.Start("explorer.exe", Project.ProjectDirectory);
		}

		private void btnKillAll_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			// TODO: Implement
		}

		private void cmbMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Project.LaunchSettings.LastSelectedMap = cmbMaps.SelectedItem.ToString();
		}

		private void cbFullScreen_Changed(object sender, System.Windows.RoutedEventArgs e)
		{
			Project.LaunchSettings.FullScreen = cbFullScreen.IsChecked ?? false;
		}

		private void btnProjectSettings_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProjectSettings projectSettings = new ProjectSettings(Project);
			projectSettings.ShowDialog();
		}
	}
}
