// Copyright (c) Keegan L Gibson. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Newtonsoft.Json;
using Unreal_Launcher.Properties;

namespace Unreal_Launcher
{
	/// <summary>
	/// The main application window.
	/// </summary>
	public partial class MainWindow : Window
	{
		private const string ProjectFileFilterType = "Project Files (*.uproject)|*.uproject";
		private const string ProjectFileType = "*.uproject";
		private readonly List<TabItem> _tabItems;
		private readonly TabItem _tabAdd;

		public MainWindow()
		{
			InitializeComponent();

			_tabItems = new List<TabItem>();
			_tabAdd = new TabItem
			{
				Header = "+",
			};

			Title = Title + " - Version : " + typeof(MainWindow).Assembly.GetName().Version;

			_tabItems.Add(_tabAdd);
			TabControl_Projects.DataContext = _tabItems;
			TabControl_Projects.SelectedIndex = -1;

			if (Settings.Default.Projects != null)
			{
				StringCollection invalidProjects = new StringCollection();

				foreach (string projectJson in Settings.Default.Projects)
				{
					Project project = JsonConvert.DeserializeObject<Project>(projectJson);
					if (project != null)
					{
						project.Init();

						if (project.IsProjectInitialised)
						{
							AddProjectTab(project);
						}
						else
						{
							invalidProjects.Add(projectJson);
						}
					}
				}

				// Loop through projects that could not be found on disk and remove them.
				foreach (string invalidProjectJson in invalidProjects)
				{
					Settings.Default.Projects.Remove(invalidProjectJson);
				}
			}
			else
			{
				Settings.Default.Projects = new System.Collections.Specialized.StringCollection();

				List<string> projectsFound = new List<string>();
				projectsFound.AddRange(Directory.GetFiles(@".\", ProjectFileType, SearchOption.TopDirectoryOnly));

				if (projectsFound.Count == 0)
				{
					string[] projectDirectories = Directory.GetDirectories(@".\", "*", SearchOption.TopDirectoryOnly);
					foreach (string projectDirectory in projectDirectories)
					{
						projectsFound.AddRange(Directory.GetFiles(projectDirectory, ProjectFileType, SearchOption.TopDirectoryOnly));
					}
				}

				List<string> serialisedProject = new List<string>();
				foreach (string projectFilename in projectsFound)
				{
					Project project = new Project(projectFilename);
					serialisedProject.Add(JsonConvert.SerializeObject(project));
					AddProjectTab(project);
				}

				Settings.Default.Projects.AddRange(serialisedProject.ToArray());
				Settings.Default.Save();
			}
		}

		private void AddProjectTab(Project project)
		{
			int count = _tabItems.Count;
			TabControl_Projects.DataContext = null;

			string projectFileName = project.ProjectName;

			// Create new tab item.
			TabItem newTab = new TabItem
			{
				Header = projectFileName,
				Name = $"tab_{projectFileName}",
				HeaderTemplate = TabControl_Projects.FindResource("TabHeader") as DataTemplate,
			};

			newTab.Content = new MainTab(project);

			// Insert tab item right before the last (+) tab item.
			_tabItems.Insert(count - 1, newTab);

			TabControl_Projects.DataContext = _tabItems;

			// Select newly added tab item.
			TabControl_Projects.SelectedItem = newTab;
		}

		private void OpenNewProject()
		{
			OpenFileDialog projectDialog = new OpenFileDialog
			{
				Filter = ProjectFileFilterType,
			};

			Dispatcher.BeginInvoke(new Action(() =>
			{
				DialogResult result = projectDialog.ShowDialog();

				if (result == System.Windows.Forms.DialogResult.OK)
				{
					string filename = projectDialog.FileName;
					bool foundProject = DoesProjectAlreadyExist(filename);

					if (!foundProject)
					{
						Project project = new Project(filename);
						Settings.Default.Projects.Add(JsonConvert.SerializeObject(project));
						Settings.Default.Save();
						AddProjectTab(project);
					}
				}
			}));
		}

		private bool DoesProjectAlreadyExist(string filename)
		{
			bool foundProject = false;

			for (int i = 0; i < Settings.Default.Projects.Count; ++i)
			{
				string escapedFiledname = filename.Replace("\\", "\\\\");
				if (Settings.Default.Projects[i].Contains(escapedFiledname))
				{
					foundProject = true;
					break;
				}
			}

			return foundProject;
		}

		private void TabControl_Projects_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TabControl_Projects.SelectedItem is TabItem tab && tab.Header != null)
			{
				if (tab.Equals(_tabAdd))
				{
					OpenNewProject();
				}
			}
		}

		private void Button_Delete_Click(object sender, RoutedEventArgs e)
		{
			string tabName = (sender as System.Windows.Controls.Button).CommandParameter.ToString();
			TabItem item = TabControl_Projects.Items.Cast<TabItem>().Where(i => i.Name.Equals(tabName)).FirstOrDefault() ?? throw new InvalidOperationException("Trying to remove a project tab that doest exist.");

			if (_tabItems.Count > 1 && System.Windows.MessageBox.Show($"Are you sure you want to remove project '{item.Header}'?", "Remove Project", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				MainTab tab = item.Content as MainTab;

				for (int i = 0; i < Settings.Default.Projects.Count; ++i)
				{
					string escapedFiledname = tab.Project.FullPath.Replace("\\", "\\\\");
					if (Settings.Default.Projects[i].Contains(escapedFiledname))
					{
						Settings.Default.Projects.RemoveAt(i);
						break;
					}
				}

				Settings.Default.Save();

				// clear tab control binding
				TabControl_Projects.DataContext = null;

				_tabItems.Remove(item);

				// bind tab control
				TabControl_Projects.DataContext = _tabItems;

				// select previously selected tab. if that is removed then select first tab
				if (!(TabControl_Projects.SelectedItem is TabItem selectedTab) || selectedTab.Equals(item))
				{
					selectedTab = _tabItems[0] != _tabAdd ? _tabItems[0] : null;
				}

				TabControl_Projects.SelectedItem = selectedTab;
			}
		}
	}
}
