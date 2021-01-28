// Copyright (c) Keegan L Gibson. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
			KillList = new List<Process>();

			InitialiseUI();

			InitKillTimer();
		}

		public Project Project { get; }

		private List<Process> KillList { get;  }

		private DispatcherTimer killTimer;

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

		private void InitKillTimer()
		{
			killTimer = new DispatcherTimer();
			killTimer.Tick += new EventHandler(UpdateKillList_Tick);
			killTimer.Interval = new TimeSpan(0, 0, 2);
			killTimer.Start();
		}

		private void UpdateKillList_Tick(object sender, EventArgs e)
		{
			KillList.RemoveAll(proc => proc == null || proc.HasExited);

			if (KillList.Count > 0)
			{
				Button_KillAll.IsEnabled = true;
				Label_KillAll.Text = "Kill All (" + KillList.Count + ")";
			}
			else
			{
				Label_KillAll.Text = "Kill All";
				Button_KillAll.IsEnabled = false;
			}
		}

		private void Button_NewClass_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			NewClass newClassWindow = new NewClass(Project);
			newClassWindow.ShowDialog();
		}

		private void StartProccess(ProcessStartInfo startInfo, bool addToKillList = false)
		{
			Process proc = Process.Start(startInfo);

			if (addToKillList && proc != null)
			{
				KillList.Add(proc);
			}
		}

		private void Button_OpenEditor_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe"),
				Arguments = Project.GenerateEditorArguments(),
			};

			StartProccess(startInfo, false);
		}

		private void Button_PlayGame_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe"),
				Arguments = Project.GenerateGameArguments(),
			};

			StartProccess(startInfo, true);
		}

		private void Button_StartServer_Click(object sender, RoutedEventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe"),
				Arguments = Project.GenerateServerArguments(),
			};

			StartProccess(startInfo, true);
		}

		private void Button_StartClient_Click(object sender, RoutedEventArgs e)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe"),
				Arguments = Project.GenerateClientArguments(),
			};

			StartProccess(startInfo, true);
		}

		private void Button_BrowseFolder_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			// opens explorer)
			Process.Start("explorer.exe", Project.ProjectDirectory);
		}

		private void Button_KillAll_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			MessageBoxResult result = MessageBox.Show("Are you sure you want to close all instances of " + Project.ProjectNiceName + " (excluding the Editor)?", "Kill All", MessageBoxButton.YesNo);

			if (result == MessageBoxResult.Yes)
			{
				foreach (Process proc in KillList)
				{
					proc.Kill();
				}
			}
		}

		private void ComboBox_Maps_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ComboBox_Maps.SelectedItem != null)
			{
				Project.LaunchSettings.LastSelectedMap = ComboBox_Maps.SelectedItem.ToString();
			}
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
