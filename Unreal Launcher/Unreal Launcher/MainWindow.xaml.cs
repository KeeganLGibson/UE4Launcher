using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using Unreal_Launcher.Properties;
using Newtonsoft.Json;

namespace Unreal_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ProjectFileFilterType = "Project Files (*.uproject)|*.uproject";
        private const string ProjectFileType = "*.uproject";
        private readonly List<TabItem> TabItems;
        private readonly TabItem TabAdd;

        public MainWindow()
        {
            InitializeComponent();

            TabItems = new List<TabItem>();
            TabAdd = new TabItem
            {
                Header = "+"
            };

            TabItems.Add(TabAdd);
            Tab_Control.DataContext = TabItems;
            Tab_Control.SelectedIndex = -1;

            if (Settings.Default.Projects != null)
            {
                foreach (string projectstr in Settings.Default.Projects)
                {
                    Project project = JsonConvert.DeserializeObject<Project>(projectstr);
                    if (project != null)
                    {
                        project.Init();
                        AddProjectTab(project);
                    }
                }
            }
            else
            {
                Settings.Default.Projects = new System.Collections.Specialized.StringCollection();

                List<string> projectsFound = new List<string>();
                projectsFound.AddRange(Directory.GetFiles(@".\", ProjectFileType, SearchOption.TopDirectoryOnly));

                if(projectsFound.Count == 0)
                {
                    string[] projectDirectories = Directory.GetDirectories(@".\", "*", SearchOption.TopDirectoryOnly);
                    foreach (string projectDirectory in projectDirectories)
                    {
                        projectsFound.AddRange(Directory.GetFiles(projectDirectory, ProjectFileType, SearchOption.TopDirectoryOnly));
                    }
                }

                List<string> SerialisedProject = new List<string>();
                foreach (string projectFilename in projectsFound)
                {
                    Project project = new Project(projectFilename);
                    SerialisedProject.Add(JsonConvert.SerializeObject(project));
                    AddProjectTab(project);
                }

                Settings.Default.Projects.AddRange(SerialisedProject.ToArray());
                Settings.Default.Save();
            }
        }

        private void AddProjectTab(Project project)
        {
            int count = TabItems.Count;
            Tab_Control.DataContext = null;

            string ProjectFileName = project.ProjectName;

            // create new tab item
            TabItem NewTab = new TabItem
            {
                Header = ProjectFileName,
                Name = $"tab_{ProjectFileName}",
                HeaderTemplate = Tab_Control.FindResource("TabHeader") as DataTemplate
            };

            NewTab.Content = new MainTab(project);

            // insert tab item right before the last (+) tab item
            TabItems.Insert(count - 1, NewTab);

            Tab_Control.DataContext = TabItems;
            // select newly added tab item
            Tab_Control.SelectedItem = NewTab;
        }

        private void OpenNewProject()
        {
            OpenFileDialog ProjectDialog = new OpenFileDialog
            {
                Filter = ProjectFileFilterType
            };

            DialogResult DR = ProjectDialog.ShowDialog();

            if (DR == System.Windows.Forms.DialogResult.OK)
            {
                string Filename = ProjectDialog.FileName;
                bool bFoundProject = DoesProjectAlreadyExist(Filename);

                if (!bFoundProject)
                {
                    Project project = new Project(Filename);
                    Settings.Default.Projects.Add(JsonConvert.SerializeObject(project));
                    Settings.Default.Save();
                    AddProjectTab(project);
                }
            }
        }

        private bool DoesProjectAlreadyExist(string Filename)
        {
            bool bFoundProject = false;

            for (int i = 0; i < Settings.Default.Projects.Count; ++i)
            {
                string EscapedFiledname = Filename.Replace("\\", "\\\\");
                if (Settings.Default.Projects[i].Contains(EscapedFiledname))
                {
                    bFoundProject = true;
                    break;
                }
            }

            return bFoundProject;
        }

        private void Tab_Control_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem tab = Tab_Control.SelectedItem as TabItem;

            if (tab != null && tab.Header != null)
            {
                if (tab.Equals(TabAdd))
                {
                    OpenNewProject();
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            string tabName = (sender as System.Windows.Controls.Button).CommandParameter.ToString();

            TabItem item = Tab_Control.Items.Cast<TabItem>().Where(i => i.Name.Equals(tabName)).FirstOrDefault();

            if (item != null)
            {
                if (TabItems.Count > 1 && System.Windows.MessageBox.Show($"Are you sure you want to remove project '{item.Header}'?", "Remove Project", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // get selected tab
                    TabItem selectedTab = Tab_Control.SelectedItem as TabItem;

                    MainTab tab = item.Content as MainTab;

                    for (int i = 0; i < Settings.Default.Projects.Count; ++i)
                    {
                        string EscapedFiledname = tab.Project.ProjectFullPath.Replace("\\", "\\\\");
                        if (Settings.Default.Projects[i].Contains(EscapedFiledname))
                        {
                            Settings.Default.Projects.RemoveAt(i);
                            break;
                        }
                    }

                    Settings.Default.Save();

                    // clear tab control binding
                    Tab_Control.DataContext = null;

                    TabItems.Remove(item);

                    // bind tab control
                    Tab_Control.DataContext = TabItems;

                    // select previously selected tab. if that is removed then select first tab
                    if (selectedTab == null || selectedTab.Equals(item))
                    {
                        if (TabItems[0] != TabAdd)
                        {
                            selectedTab = TabItems[0];
                        }
                        else
                        {
                            selectedTab = null;
                        }
                    }
                    Tab_Control.SelectedItem = selectedTab;
                }
            }

        }
    }
}
