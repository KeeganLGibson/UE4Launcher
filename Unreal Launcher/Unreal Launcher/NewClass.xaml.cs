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
			ckbAllClasses.IsChecked = Settings.Default.bShowAllClasses;

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
			prgbRescanProgress.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				prgbRescanProgress.Visibility = visibility;
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
			return Project.ProjectDirectory + "/Saved/Tools/ClassCache.data";
		}

		private void RepopulateTreeVis()
		{
			tvParentClasses.Items.Clear();
			SourceRoot.PopulateItems(tvParentClasses.Items);
		}

		private void RescanSourceFiles(List<string> headerFiles, bool isGameModule)
		{
			// class GAME_API {Group1} : public {Group2}
			Regex regex = new Regex(@"^(?!\s*\/\/*\s*)(?:\s*class\s*\w*\s+)([UAF]\w+)\s*(?:\s*:\s*public\s+)?([UAF]\w+)?(?:,\s+\w+\s+\w+)?$(?!;)");

			prgbRescanProgress.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
			{
				prgbRescanProgress.Maximum = headerFiles.Count;
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

				prgbRescanProgress.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate()
				{
					prgbRescanProgress.Value = i;
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
			FilterTreeView(tvParentClasses.Items, txtParentSearch.Text, (ckbAllClasses.IsChecked ?? false) ? new string[] { } : new[] { "UObject", "AActor", "AGameMode", "ACharacter", "APlayerController", "AGameState", "APlayerState" });
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

		private void btnRescan_Click(object sender, RoutedEventArgs e)
		{
			if (IsScanRunning == false)
			{
				ForceRescan();
			}
		}

		private void ClearForm()
		{
			txtclass.Text = string.Empty;
			txtParent.Text = string.Empty;
		}

		private void btnclear_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			txtSaveLocation.Text = @".\Source\";
			txtParentSearch.Text = string.Empty;
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void tvParentClasses_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			TreeViewItem treeItem = (TreeViewItem)e.NewValue;

			txtParent.Text = treeItem != null ? treeItem.Header.ToString() : string.Empty;
		}

		private void txtParentSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateVisibleClasses();
		}

		private void btnBrowseSourceLocation_Click(object sender, RoutedEventArgs e)
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
				txtSaveLocation.Text = relPath;
			}
		}

		private void ckbAllClasses_Changed(object sender, RoutedEventArgs e)
		{
			Settings.Default.bShowAllClasses = ckbAllClasses.IsChecked ?? false;
			Settings.Default.Save();

			UpdateVisibleClasses();
		}

		private void btnCreate_Click(object sender, RoutedEventArgs e)
		{
			StubbleVisitorRenderer stubble = new StubbleBuilder().Build();

			Dictionary<string, object> data = Project.GetData();
			data.Add("Year", DateTime.Now.Year.ToString());
			data.Add("Class", txtclass.Text);
			data.Add("ParentClass", txtParent.Text);
			data.Add("ProjectCompany", Project.ProjectCompany);

			if (!string.IsNullOrWhiteSpace(Project.Copyright))
			{
				data.Add("CustomCopyright", Project.Copyright);
			}

			string description = txtDescription.Text;
			data.Add("Description", string.IsNullOrWhiteSpace(description) ? "TODO:" : description);

			Regex regex = new Regex(@"^[AU][A-Z]");
			bool isUClass = regex.IsMatch(txtParent.Text);
			data.Add("bIsUClass", isUClass);

			string fileName = isUClass ? txtclass.Text.Substring(1) : txtclass.Text;
			data.Add("FileName", fileName);

			TreeViewItem treeViewItem = (TreeViewItem)tvParentClasses.SelectedItem;
			ClassItem parentClass = (ClassItem)treeViewItem.Tag;

			data.Add("bIsGameModule", parentClass.IsGameModule);

			string classAbsoluteSaveLocation = Path.Combine(Project.ProjectDirectory, txtSaveLocation.Text);

			string parentSourceFile = parentClass.IsGameModule
				? PathHelpers.EvaluateRelativePath(classAbsoluteSaveLocation, Path.GetDirectoryName(parentClass.SourceFileLocation))
				: PathHelpers.EvaluateRelativePath(Path.Combine(Project.EnginePath, @"Engine\Source"), Path.GetDirectoryName(parentClass.SourceFileLocation));

			parentSourceFile += Path.GetFileName(parentClass.SourceFileLocation);

			data.Add("ParentClassSource", parentSourceFile);

			string svaeFolder = Path.Combine(Project.ProjectDirectory, txtSaveLocation.Text);

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
