using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Unreal_Launcher
{
    /// <summary>
    /// Interaction logic for ProjectSettings.xaml
    /// </summary>
    public partial class ProjectSettings : Window
    {
        Project project;
        public ProjectSettings(Project Associatedproject)
        {
            InitializeComponent();

            project = Associatedproject;

            UpdateSettings();
        }

        private void UpdateSettings()
        {
            if (string.IsNullOrWhiteSpace(project.ProjectNiceName))
            {
                tbProjectNiceName.Text = project.ProjectName;
            }
            else
            {
                tbProjectNiceName.Text = project.ProjectNiceName;
            }

            tbCompanyName.Text = project.ProjectCompany;
            tbEnginePath.Text = project.EnginePath;

            tbCustomCopyright.Text = project.Copyright;
        }

        private void ApplyNewSettings()
        {
            if(!string.IsNullOrWhiteSpace(tbProjectNiceName.Text))
            {
                project.ProjectNiceName = tbProjectNiceName.Text;
            }

            project.ProjectCompany = tbCompanyName.Text;
            project.ProjectCompany = tbCustomCopyright.Text;

            project.SaveProject();
        }

        private void CloseWindow()
        {
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            ApplyNewSettings();
            CloseWindow();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            ApplyNewSettings();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }
    }
}
