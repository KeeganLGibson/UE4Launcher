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
			TextBox_ProjectNiceName.Text = string.IsNullOrWhiteSpace(_project.ProjectNiceName) ? _project.ProjectName : _project.ProjectNiceName;

			TextBox_CompanyName.Text = _project.ProjectCompany;
			TextBox_EnginePath.Text = _project.EnginePath;

			TextBox_CustomCopyright.Text = _project.Copyright;
		}

		private void ApplyNewSettings()
		{
			if (!string.IsNullOrWhiteSpace(TextBox_ProjectNiceName.Text))
			{
				_project.ProjectNiceName = TextBox_ProjectNiceName.Text;
			}

			_project.ProjectCompany = TextBox_CompanyName.Text;
			_project.Copyright = TextBox_CustomCopyright.Text;

			_project.SaveProject();
		}

		private void CloseWindow()
		{
			Close();
		}

		private void Button_OK_Click(object sender, RoutedEventArgs e)
		{
			ApplyNewSettings();
			CloseWindow();
		}

		private void Button_Apply_Click(object sender, RoutedEventArgs e)
		{
			ApplyNewSettings();
		}

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			CloseWindow();
		}
	}
}
