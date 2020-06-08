using System.Diagnostics;
using System.IO;
using System.Windows.Controls;

namespace Unreal_Launcher
{

    /// <summary>
    /// Interaction logic for MainTab.xaml
    /// </summary>
    public partial class MainTab : UserControl
    {
        public Project Project { get; }

        public MainTab(Project project)
        {
            InitializeComponent();

            Project = project;

            txtProjectNiceName.Text = Project.ProjectNiceName;
            lblProjectDir.Content = Project.ProjectDirectory;
            cbFullScreen.IsChecked = Project.LaunchSettings.bFullScreen;

            FindAllMaps();
        }

        private void FindAllMaps()
        {
            string[] Files = Directory.GetFiles(Path.Combine(Project.ProjectDirectory, @".\Content\"), "*umap", SearchOption.AllDirectories);

            // add a black default;
            cmbMaps.Items.Add("(Default)");

            foreach (string file in Files)
            {
                if (!file.Contains("Marketplace") && !file.Contains("StarterContent"))
                {
                    ComboBoxItem CBI = new ComboBoxItem();
                    cmbMaps.Items.Add(Path.GetFileNameWithoutExtension(file));
                }
            }

            // If no map selected use the project defualts.
            if(string.IsNullOrWhiteSpace(Project.LaunchSettings.LastSelectedMap))
            {
                cmbMaps.SelectedItem = "(Default)";
            }
            else
            {
                cmbMaps.SelectedItem = Project.LaunchSettings.LastSelectedMap;
            }
        }

        private void btnNewClass_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NewClass NewClassWindow = new NewClass(Project);
            NewClassWindow.ShowDialog();
        }

        private void btnOpenEditor_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe");
            startInfo.Arguments = Project.GenerateEditorArguments();

            Process.Start(startInfo);
        }

        private void btnPlayGame_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Project.EnginePath, @".\Engine\Binaries\Win64\UE4Editor.exe");
            startInfo.Arguments = Project.GenerateGameArguments();

            Process.Start(startInfo);
        }

        private void btnBrowseFolder_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // opens explorer)
            Process.Start("explorer.exe", Project.ProjectDirectory);
        }

        private void txtProjectNiceName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Project.ProjectNiceName = txtProjectNiceName.Text;
        }

        private void btnKillAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void cmbMaps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Project.LaunchSettings.LastSelectedMap = cmbMaps.SelectedItem.ToString();
        }

        private void cbFullScreen_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            Project.LaunchSettings.bFullScreen = cbFullScreen.IsChecked ?? false;
        }
    }
}
