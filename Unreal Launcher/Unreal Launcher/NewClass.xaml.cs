// Copyright (c) Keegan L Gibson. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using Stubble.Core;
using Stubble.Core.Builders;
using Unreal_Launcher.Properties;
using static System.Environment;

namespace Unreal_Launcher
{
	/// <summary>
	/// The code generation window and logic.
	/// </summary>
	public partial class NewClass : Window
	{
		private Project Project { get; }

		private ClassItem SourceRoot { get; set; }

		private bool _isScanRunning = false;

		private bool IsScanRunning { get => _isScanRunning; set => _isScanRunning = value; }

		public NewClass(Project project)
		{
			InitializeComponent();

			Project = project;

			Title = "New Class : " + Project.ProjectNiceName;
			CheckBox_AllClasses.IsChecked = Settings.Default.bShowAllClasses;

			LoadClassCache();
		}

		private async void LoadClassCache()
		{
			SetProgressBarVisibility(Visibility.Visible);

			IsScanRunning = true;

			if (!File.Exists(GetClassCache()))
			{
				await RescanAllSourceFiles();
			}
			else
			{
				SourceRoot = BinarySerialization.ReadFromBinaryFile<ClassItem>(GetClassCache());
				await RescanGameSourceFiles(false);
			}

			ScanComplete();
		}

		private void SetProgressBarVisibility(Visibility visibility)
		{
			ProgressBar_Rescan.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				ProgressBar_Rescan.Visibility = visibility;
			}));
		}

		private async void ForceRescan()
		{
			SetProgressBarVisibility(Visibility.Visible);

			IsScanRunning = true;

			await RescanAllSourceFiles();
			ScanComplete();
		}

		private void ScanComplete()
		{
			SetProgressBarVisibility(Visibility.Hidden);

			RepopulateTreeVis();
			UpdateVisibleClasses();

			IsScanRunning = false;
		}

		private string GetClassCache()
		{
			return Project.ProjectDirectory + "/Saved/Tools/Classes.cache";
		}

		private void RepopulateTreeVis()
		{
			TreeView_ParentClasses.Items.Clear();
			SourceRoot.PopulateItems(TreeView_ParentClasses.Items);
		}

		private void RescanSourceFiles(List<string> headerFiles, bool isGameModule)
		{
			// class GAME_API {Group1} : public {Group2}
			Regex regex = new Regex(@"^(?!\s*\/\/*\s*)(?:\s*class\s*\w*\s+)([UAF]\w+)\s*(?:\s*:\s*public\s+)?([UAF]\w+)?(?:,\s+\w+\s+\w+)?$(?!;)");

			ProgressBar_Rescan.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				ProgressBar_Rescan.Maximum = headerFiles.Count;
			}));

			for (int i = 0; i < headerFiles.Count; ++i)
			{
				string headerFile = headerFiles[i];
				using (StreamReader reader = new StreamReader(headerFile))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						// Try to match each line against the Regex.
						Match match = regex.Match(line);
						if (match.Success)
						{
							// Write original line and the value.
							string currentClass = match.Groups[1].Value;
							string parentClass = match.Groups[2].Value;

							ClassItem parent = null;

							// Find the parent
							if (parentClass != string.Empty)
							{
								parent = SourceRoot.FindClassByName(parentClass);
								if (parent == null)
								{
									// Create a Temporary Parent to Be Updated Later
									parent = new ClassItem(parentClass, string.Empty, isGameModule, SourceRoot);
								}
							}
							else
							{
								parent = SourceRoot;
							}

							// See if the class already exists and Update it.
							ClassItem classItem = SourceRoot.FindClassByName(currentClass);

							if (classItem != null)
							{
								classItem.Parent = parent;
								classItem.SourceFileLocation = headerFile;
							}
							else
							{
								classItem = new ClassItem(currentClass, headerFile, isGameModule, parent);
							}
						}
					}
				}

				ProgressBar_Rescan.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
				{
					ProgressBar_Rescan.Value = i;
				}));
			}
		}

		private void PurgeGameSourceFilesFromTree(ClassItem currentClass)
		{
			currentClass.SubClasses.RemoveAll(subClass => subClass.IsGameModule);

			foreach (ClassItem subClass in currentClass.SubClasses)
			{
				PurgeGameSourceFilesFromTree(subClass);
			}
		}

		private async Task RescanGameSourceFiles(bool performPurge = true)
		{
			if (performPurge)
			{
				PurgeGameSourceFilesFromTree(SourceRoot);
			}

			List<string> headerFiles = new List<string>();
			headerFiles.AddRange(Directory.GetFiles(Project.ProjectDirectory + "/Source/", "*.h", SearchOption.AllDirectories));

			if (Directory.Exists(Project.ProjectDirectory + "/Plugins/"))
			{
				headerFiles.AddRange(Directory.GetFiles(Project.ProjectDirectory + "/Plugins/", "*.h", SearchOption.AllDirectories));
			}

			await Task.Run(() => RescanSourceFiles(headerFiles, true));
		}

		private async Task RescanAllSourceFiles()
		{
			SourceRoot = new ClassItem("root", "Source", false);

			List<string> headerFiles = new List<string>();
			headerFiles.AddRange(Directory.GetFiles(Project.EnginePath + "/Engine/Source/", "*.h", SearchOption.AllDirectories));
			headerFiles.AddRange(Directory.GetFiles(Project.EnginePath + "/Engine/Plugins/", "*.h", SearchOption.AllDirectories));

			await Task.Run(() => RescanSourceFiles(headerFiles, false));
			BinarySerialization.WriteToBinaryFile<ClassItem>(GetClassCache(), SourceRoot);

			await RescanGameSourceFiles(false);
		}

		private void UpdateVisibleClasses()
		{
			FilterTreeView(TreeView_ParentClasses.Items, TextBox_ParentSearch.Text, (CheckBox_AllClasses.IsChecked ?? false) ? new string[] { } : new[] { "UObject", "AActor", "AGameMode", "ACharacter", "APlayerController", "AGameState", "APlayerState" });
		}

		// Return true if the parent should be visible.
		private (bool Visible, bool Expanded) FilterTreeView(ItemCollection items, string filter, string[] baseClasses, bool isOfBaseClass = false)
		{
			bool parentVisible = false;
			bool parentExpanded = false;

			foreach (TreeViewItem treeItem in items)
			{
				bool passedFilter = string.IsNullOrWhiteSpace(filter) || treeItem.Header.ToString().Contains(filter);
				bool isDecendantOfClass = baseClasses.Length == 0;

				foreach (string baseClass in baseClasses)
				{
					if (treeItem.Header.ToString() == baseClass)
					{
						isDecendantOfClass = true;
						break;
					}
				}

				(bool childRequestsVisible, bool childRequestsExpanded) = FilterTreeView(treeItem.Items, filter, baseClasses, isDecendantOfClass || isOfBaseClass);

				parentExpanded |= childRequestsExpanded || (passedFilter && !string.IsNullOrWhiteSpace(filter)) || (isDecendantOfClass && baseClasses.Length != 0);

				if (childRequestsVisible || (passedFilter && (isDecendantOfClass || isOfBaseClass)))
				{
					treeItem.Visibility = Visibility.Visible;
					treeItem.IsExpanded = childRequestsExpanded;

					parentVisible |= true;
				}
				else
				{
					treeItem.Visibility = Visibility.Collapsed;
					treeItem.IsExpanded = false;

					parentVisible |= false;
				}
			}

			return (parentVisible, parentExpanded);
		}

		private void Button_Rescan_Click(object sender, RoutedEventArgs e)
		{
			if (IsScanRunning == false)
			{
				ForceRescan();
			}
		}

		private void ClearForm()
		{
			TextBox_Class.Text = string.Empty;
			TextBox_Parent.Text = string.Empty;
		}

		private void Button_clear_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			TextBox_SaveLocation.Text = @".\Source\";
			TextBox_ParentSearch.Text = string.Empty;
		}

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void TreeView_ParentClasses_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			TreeViewItem treeItem = (TreeViewItem)e.NewValue;

			TextBox_Parent.Text = treeItem != null ? treeItem.Header.ToString() : string.Empty;
		}

		private void TextBox_ParentSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateVisibleClasses();
		}

		private void Button_BrowseSourceLocation_Click(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog folderDialog = new FolderBrowserDialog
			{
				RootFolder = SpecialFolder.MyComputer,
				ShowNewFolderButton = true,
				SelectedPath = Project.ProjectDirectory + @"\Source\",
			};

			DialogResult dialogResult = folderDialog.ShowDialog();

			if (dialogResult == System.Windows.Forms.DialogResult.OK)
			{
				string relPath = PathHelpers.EvaluateRelativePath(Project.ProjectDirectory, folderDialog.SelectedPath);
				TextBox_SaveLocation.Text = relPath;
			}
		}

		private void CheckBox_AllClasses_Changed(object sender, RoutedEventArgs e)
		{
			Settings.Default.bShowAllClasses = CheckBox_AllClasses.IsChecked ?? false;
			Settings.Default.Save();

			UpdateVisibleClasses();
		}

		private void Button_Create_Click(object sender, RoutedEventArgs e)
		{
			StubbleVisitorRenderer stubble = new StubbleBuilder().Build();

			Dictionary<string, object> data = Project.GetData();
			data.Add("Year", DateTime.Now.Year.ToString());
			data.Add("Class", TextBox_Class.Text);
			data.Add("ParentClass", TextBox_Parent.Text);

			string description = TextBox_Description.Text;
			data.Add("Description", string.IsNullOrWhiteSpace(description) ? "TODO:" : description);

			Regex regex = new Regex(@"^[AU][A-Z]");
			bool isUClass = regex.IsMatch(TextBox_Parent.Text);
			data.Add("bIsUClass", isUClass);

			string fileName = isUClass ? TextBox_Class.Text.Substring(1) : TextBox_Class.Text;
			data.Add("FileName", fileName);

			TreeViewItem treeViewItem = (TreeViewItem)TreeView_ParentClasses.SelectedItem;
			ClassItem parentClass = (ClassItem)treeViewItem.Tag;

			data.Add("bIsGameModule", parentClass.IsGameModule);

			string classAbsoluteSaveLocation = Path.Combine(Project.ProjectDirectory, TextBox_SaveLocation.Text);

			string parentSourceFile = parentClass.IsGameModule
				? PathHelpers.EvaluateRelativePath(classAbsoluteSaveLocation, Path.GetDirectoryName(parentClass.SourceFileLocation))
				: PathHelpers.EvaluateRelativePath(Path.Combine(Project.EnginePath, @"Engine\Source"), Path.GetDirectoryName(parentClass.SourceFileLocation));

			parentSourceFile += Path.GetFileName(parentClass.SourceFileLocation);

			data.Add("ParentClassSource", parentSourceFile);

			string svaeFolder = Path.Combine(Project.ProjectDirectory, TextBox_SaveLocation.Text);

			using (StreamReader streamReader = new StreamReader(@".\Header.mustache", Encoding.UTF8))
			{
				string output = stubble.Render(streamReader.ReadToEnd(), data);

				using (StreamWriter streamWriter = new StreamWriter(Path.Combine(svaeFolder, fileName + ".h")))
				{
					streamWriter.Write(output);
				}
			}

			using (StreamReader streamReader = new StreamReader(@".\Cpp.mustache", Encoding.UTF8))
			{
				string output = stubble.Render(streamReader.ReadToEnd(), data);
				using (StreamWriter streamWriter = new StreamWriter(Path.Combine(svaeFolder, fileName + ".cpp")))
				{
					streamWriter.Write(output);
				}
			}

			ClearForm();
		}
	}
}
