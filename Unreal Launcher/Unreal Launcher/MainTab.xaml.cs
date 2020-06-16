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

			InitialiseUI();
		}

		public Project Project { get; }

		private void InitialiseUI()
		{
			TextBlock_ProjectNiceName.Text = Project.ProjectNiceName;
			Label_ProjectDir.Content = Project.ProjectDirectory;
			CheckBox_FullScreen.IsChecked = Project.LaunchSettings.FullScreen;

			FindAllMaps();
		}

		private void FindAllMaps()
		{
			ComboBox_Maps.Items.Clear();

			string[] files = Directory.GetFiles(Path.Combine(Project.ProjectDirectory, @".\Content\"), "*umap", SearchOption.AllDirectories);

			// add a black default;
			ComboBox_Maps.Items.Add("(Default)");

			foreach (string file in files)
			{
				if (!file.Contains("Marketplace") && !file.Contains("StarterContent"))
				{
					ComboBox_Maps.Items.Add(Path.GetFileNameWithoutExtension(file));
				}
			}

			// If no map selected use the project defaults.
			ComboBox_Maps.SelectedItem = string.IsNullOrWhiteSpace(Project.LaunchSettings.LastSelectedMap) ? "(Default)" : Project.LaunchSettings.LastSelectedMap;
		}

		private void Button_NewClass_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			NewClass newClassWindow = new NewClass(Project);
			newClassWindow.ShowDialog();
		}

		private void Button_OpenEditor_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe"),
				Arguments = Project.GenerateEditorArguments(),
			};

			Process.Start(startInfo);
		}

		private void Button_PlayGame_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe"),
				Arguments = Project.GenerateGameArguments(),
			};

			Process.Start(startInfo);
		}

		private void Button_BrowseFolder_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			// opens explorer)
			Process.Start("explorer.exe", Project.ProjectDirectory);
		}

		private void Button_KillAll_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			// TODO: Implement
		}

		private void ComboBox_Maps_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Project.LaunchSettings.LastSelectedMap = ComboBox_Maps.SelectedItem.ToString();
		}

		private void CheckBox_FullScreen_Changed(object sender, System.Windows.RoutedEventArgs e)
		{
			Project.LaunchSettings.FullScreen = CheckBox_FullScreen.IsChecked ?? false;
		}

		private void Button_ProjectSettings_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProjectSettings projectSettings = new ProjectSettings(Project);
			projectSettings.ShowDialog();

			InitialiseUI();
		}
	}
}
