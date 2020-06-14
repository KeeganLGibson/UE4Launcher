// Copyright (c) Keegan L Gibson. All rights reserved.

using System.Windows;

namespace Unreal_Launcher
{
	/// <summary>
	/// Represents the project settings window.
	/// </summary>
	public partial class ProjectSettings : Window
	{
		private readonly Project _project;

		public ProjectSettings(Project project)
		{
			InitializeComponent();

			_project = project;

			UpdateSettings();
		}

		private void UpdateSettings()
		{
			tbProjectNiceName.Text = string.IsNullOrWhiteSpace(_project.ProjectNiceName) ? _project.ProjectName : _project.ProjectNiceName;

			tbCompanyName.Text = _project.ProjectCompany;
			tbEnginePath.Text = _project.EnginePath;

			tbCustomCopyright.Text = _project.Copyright;
		}

		private void ApplyNewSettings()
		{
			if (!string.IsNullOrWhiteSpace(tbProjectNiceName.Text))
			{
				_project.ProjectNiceName = tbProjectNiceName.Text;
			}

			_project.ProjectCompany = tbCompanyName.Text;
			_project.ProjectCompany = tbCustomCopyright.Text;

			_project.SaveProject();
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
