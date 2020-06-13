using System.Windows;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.Win32;
using static System.Environment;
using System;
using Unreal_Launcher.Properties;
using Stubble.Core.Builders;
using Stubble.Core;
using System.Text;

namespace Unreal_Launcher
{
    /// <summary>
    /// Interaction logic for New_Class.xaml
    /// </summary>
    public partial class NewClass : Window
    {
        private Project Project;

        ClassItem SourceRoot;

        public NewClass(Project ClassProject)
        {
            InitializeComponent();

            Project = ClassProject;

            Title = "New Class : " + Project.ProjectNiceName;

            if (!File.Exists(GetClassCache()))
            {
                RescanAllSourceFiles();
            }
            else
            {
                SourceRoot = BinarySerialization.ReadFromBinaryFile<ClassItem>(GetClassCache());
                RescanGameSourceFiles(false);
            }

            UpdateTreeVis();

            ckbAllClasses.IsChecked = Settings.Default.bShowAllClasses;

            UpdateVisibleClasses();
        }

        private string GetClassCache()
        {
            return Project.ProjectDirectory + "/Saved/Tools/ClassCache.data";
        }

        private void UpdateTreeVis()
        {
            tvParentClasses.Items.Clear();
            SourceRoot.PopulateItems(tvParentClasses.Items);
        }

        private void RescanSourceFiles(List<string> HeaderFiles, bool bGameModule)
        {
            // class GAME_API {Group1} : public {Group2}
            Regex regex = new Regex(@"^(?!\s*\/\/*\s*)(?:\s*class\s*\w*\s+)([UAF]\w+)\s*(?:\s*:\s*public\s+)?([UAF]\w+)?(?:,\s+\w+\s+\w+)?$(?!;)");

            foreach (string HeaderFile in HeaderFiles)
            {
                using (StreamReader reader = new StreamReader(HeaderFile))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Try to match each line against the Regex.
                        Match match = regex.Match(line);
                        if (match.Success)
                        {
                            // Write original line and the value.
                            string CurrentClass = match.Groups[1].Value;
                            string ParentClass = match.Groups[2].Value;

                            ClassItem Parent = null;
                            // Find the parent
                            if (ParentClass != "")
                            {
                                Parent = SourceRoot.FindClassByName(ParentClass);
                                if (Parent == null)
                                {
                                    // Create a Temporary Parent to Be Updated Later
                                    Parent = new ClassItem(ParentClass, "", bGameModule, SourceRoot);
                                }
                            }
                            else
                            {
                                Parent = SourceRoot;
                            }

                            // See if the class already exists and Update it.
                            ClassItem Class = SourceRoot.FindClassByName(CurrentClass);

                            if (Class != null)
                            {
                                Class.SetParent(Parent);
                                Class.SourceFileLocation = HeaderFile;
                            }
                            else
                            {
                                Class = new ClassItem(CurrentClass, HeaderFile, bGameModule, Parent);
                            }
                        }
                    }
                }
            }
        }

        void PurgeGameSourceFilesFromTree(ClassItem CurrentClass)
        {
            CurrentClass.SubClasses.RemoveAll(SubClass => SubClass.IsGameModule);

            foreach(ClassItem SubClass in CurrentClass.SubClasses)
            {
                PurgeGameSourceFilesFromTree(SubClass);
            }
        }

        private void RescanGameSourceFiles(bool bPurge = true)
        {
            if (bPurge)
            {
                PurgeGameSourceFilesFromTree(SourceRoot);
            }

            List<string> HeaderFiles = new List<string>();
            HeaderFiles.AddRange(Directory.GetFiles(Project.ProjectDirectory + "/Source/", "*.h", SearchOption.AllDirectories));

            if (Directory.Exists(Project.ProjectDirectory + "/Plugins/"))
            {
                HeaderFiles.AddRange(Directory.GetFiles(Project.ProjectDirectory + "/Plugins/", "*.h", SearchOption.AllDirectories));
            }

            RescanSourceFiles(HeaderFiles, true);
        }

        private void RescanAllSourceFiles()
        {
            SourceRoot = new ClassItem("root", "Source", false);

            List<string> HeaderFiles = new List<string>();
            HeaderFiles.AddRange(Directory.GetFiles(Project.EnginePath + "/Engine/Source/", "*.h", SearchOption.AllDirectories));
            HeaderFiles.AddRange(Directory.GetFiles(Project.EnginePath + "/Engine/Plugins/", "*.h", SearchOption.AllDirectories));

            RescanSourceFiles(HeaderFiles, false);
            BinarySerialization.WriteToBinaryFile<ClassItem>(GetClassCache(), SourceRoot);

            RescanGameSourceFiles(false);
        }

        private void UpdateVisibleClasses()
        {
            FilterTreeView(tvParentClasses.Items, txtParentSearch.Text, (ckbAllClasses.IsChecked ?? false) ? new string[] { } : new[] { "UObject", "AActor", "AGameMode", "ACharacter", "APlayerController", "AGameState", "APlayerState"});
        }

        // Return true if the parent should be visible.
        private (bool /*Visible*/, bool /*Expanded*/) FilterTreeView(ItemCollection Items, string Filter, string[] BaseClasses, bool bIsOfBaseClass = false)
        {
            bool bParentVisible = false;
            bool bParentExpanded = false;

            foreach (TreeViewItem TreeItem in Items)
            {
                bool bPassedFilter = string.IsNullOrWhiteSpace(Filter) || TreeItem.Header.ToString().Contains(Filter);
                bool bIsOfClass = BaseClasses.Length == 0;

                foreach (string Class in BaseClasses)
                {
                    if (TreeItem.Header.ToString() == Class)
                    {
                        bIsOfClass = true;
                        break;
                    }
                }

                (bool bChildRequestsVisible, bool bChildRequestsExpanded) = FilterTreeView(TreeItem.Items, Filter, BaseClasses, bIsOfClass || bIsOfBaseClass);

                bParentExpanded |= bChildRequestsExpanded || (bPassedFilter && !string.IsNullOrWhiteSpace(Filter)) || (bIsOfClass && BaseClasses.Length != 0);

                if (bChildRequestsVisible || (bPassedFilter && (bIsOfClass || bIsOfBaseClass)))
                {
                    TreeItem.Visibility = Visibility.Visible;
                    TreeItem.IsExpanded = bChildRequestsExpanded;

                    bParentVisible |= true;
                }
                else
                {
                    TreeItem.Visibility = Visibility.Collapsed;
                    TreeItem.IsExpanded = false;

                    bParentVisible |= false;
                }
            }

            return (bParentVisible, bParentExpanded);
        }

        private void btnRescan_Click(object sender, RoutedEventArgs e)
        {
            RescanAllSourceFiles();
            UpdateTreeVis();
            UpdateVisibleClasses();
        }

        private void ClearForm()
        {
            txtclass.Text = "";
            txtParent.Text = "";
        }

        private void btnclear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            txtSaveLocation.Text = @".\Source\";
            txtParentSearch.Text = "";
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void tvParentClasses_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem TreeItem = (TreeViewItem)e.NewValue;
            txtParent.Text = TreeItem.Header.ToString();
        }

        private void txtParentSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateVisibleClasses();
        }

        private static string EvaluateRelativePath(string mainDirPath, string absoluteFilePath)
        {
            string[] firstPathParts = mainDirPath.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            string[] secondPathParts = absoluteFilePath.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);

            int sameCounter = 0;
            for (int i = 0; i < Math.Min(firstPathParts.Length, secondPathParts.Length); i++)
            {
                if (!firstPathParts[i].ToLower().Equals(secondPathParts[i].ToLower()))
                {
                    break;
                }
                sameCounter++;
            }

            if (sameCounter == 0)
            {
                return absoluteFilePath;
            }

            string newPath = String.Empty;
            for (int i = sameCounter; i < firstPathParts.Length; i++)
            {
                if (i > sameCounter)
                {
                    newPath += Path.DirectorySeparatorChar;
                }
                newPath += "..";
            }

            if (newPath.Length == 0)
            {
                newPath = ".";
            }

            for (int i = sameCounter; i < secondPathParts.Length; i++)
            {
                newPath += Path.DirectorySeparatorChar;
                newPath += secondPathParts[i];
            }

            return newPath;
        }

        private void btnBrowseSourceLocation_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog FolderDialog = new FolderBrowserDialog
            {
                RootFolder = SpecialFolder.MyComputer,
                ShowNewFolderButton = true,
                SelectedPath = Project.ProjectDirectory + @"\Source\"
            };

            DialogResult DR = FolderDialog.ShowDialog();

            if (DR == System.Windows.Forms.DialogResult.OK)
            {
                string RelPath = EvaluateRelativePath(Project.ProjectDirectory, FolderDialog.SelectedPath);
                txtSaveLocation.Text = RelPath;
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

            if(!string.IsNullOrWhiteSpace(Project.Copyright))
            {
                data.Add("CustomCopyright", Project.Copyright);
            }

            string Description = txtDescription.Text;
            data.Add("Description", string.IsNullOrWhiteSpace(Description) ? "TODO:" : Description);

            Regex regex = new Regex(@"^[AU][A-Z]");
            bool bIsUClass = regex.IsMatch(txtParent.Text);
            data.Add("bIsUClass", bIsUClass);

            string FileName = bIsUClass ? txtclass.Text.Substring(1) : txtclass.Text;
            data.Add("FileName", FileName);

            TreeViewItem treeViewItem = (TreeViewItem)tvParentClasses.SelectedItem;
            ClassItem ParentClass = (ClassItem)treeViewItem.Tag;

            data.Add("bIsGameModule", ParentClass.IsGameModule);

            string ClassAbsoluteSaveLocation = Path.Combine(Project.ProjectDirectory, txtSaveLocation.Text);

            string ParentSourceFile = ParentClass.SourceFileLocation;
            if (ParentClass.IsGameModule)
            {
                ParentSourceFile = EvaluateRelativePath(ClassAbsoluteSaveLocation, Path.GetDirectoryName(ParentClass.SourceFileLocation));
            }
            else
            {
                ParentSourceFile = EvaluateRelativePath(Path.Combine(Project.EnginePath, @"Engine\Source"), Path.GetDirectoryName(ParentClass.SourceFileLocation));
            }

            ParentSourceFile += Path.GetFileName(ParentClass.SourceFileLocation);

            data.Add("ParentClassSource", ParentSourceFile);

            string SvaeFolder = Path.Combine(Project.ProjectDirectory, txtSaveLocation.Text);

            using (StreamReader streamReader = new StreamReader(@".\Header.mustache", Encoding.UTF8))
            {
                string output = stubble.Render(streamReader.ReadToEnd(), data);

                using (StreamWriter streamWriter = new StreamWriter(Path.Combine(SvaeFolder, FileName + ".h")))
                {
                    streamWriter.Write(output);
                }
            }

            using (StreamReader streamReader = new StreamReader(@".\Cpp.mustache", Encoding.UTF8))
            {
                string output = stubble.Render(streamReader.ReadToEnd(), data);
                using (StreamWriter streamWriter = new StreamWriter(Path.Combine(SvaeFolder, FileName + ".cpp")))
                {
                    streamWriter.Write(output);
                }
            }

            ClearForm();
        }
    }
}
