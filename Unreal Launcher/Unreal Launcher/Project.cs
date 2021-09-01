// Copyright (c) Keegan L Gibson. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unreal_Launcher.Properties;

namespace Unreal_Launcher
{
	[Serializable]
	public class Project
	{
		public string ProjectFullPath;

		[NonSerialized]
		public string ProjectName;
		[NonSerialized]
		public string ProjectDirectory;

		public string ProjectNiceName;
		public string ProjectCompany;
		public string Copyright;

		[NonSerialized]
		public string EngineAssociation;
		[NonSerialized]
		public string EnginePath;

		public ProjectLaunchSettings LaunchSettings = new ProjectLaunchSettings();

		[NonSerialized]
		public bool projectInitialised = false;

		public Project()
		{
		}

		public Project(string fullProjectPath)
		{
			ProjectFullPath = fullProjectPath;

			Init();
		}

		public void Init()
		{
			ProjectName = Path.GetFileNameWithoutExtension(ProjectFullPath);
			ProjectDirectory = Path.GetDirectoryName(ProjectFullPath);

			if (File.Exists(ProjectFullPath))
			{
				GetEngineDir();

				if (string.IsNullOrWhiteSpace(ProjectNiceName))
				{
					ProjectNiceName = ProjectName;
				}

				projectInitialised = true;
			}
			else
			{
				MessageBox.Show("Unable to find file: '" + ProjectFullPath + "'!", "Unable to find project file!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		// Command Line Arguments the Order is Important!
		public string GenerateEditorArguments()
		{
			SaveProject();

			string arguments = ProjectFullPath;
			arguments += GetMapAsLaunchArgument();

			arguments += @" -skipcompile";

			return arguments;
		}

		// Command Line Arguments the Order is Important!
		public string GenerateGameArguments()
		{
			SaveProject();

			string arguments = ProjectFullPath;
			arguments += GetMapAsLaunchArgument();

			arguments += @" -skipcompile -game";

			if (LaunchSettings.Log)
			{
				arguments += @" -log";
			}

			if (!LaunchSettings.FullScreen)
			{
				arguments += @" -WINDOWED -ResX=960 -ResY=540";
			}

			return arguments;
		}

		// Command Line Arguments the Order is Important!
		public string GenerateServerArguments()
		{
			SaveProject();

			string arguments = ProjectFullPath;
			arguments += GetMapAsLaunchArgument();

			arguments += @" -server";

			if (LaunchSettings.Log)
			{
				arguments += @" -log";
			}

			return arguments;
		}

		public string GenerateClientArguments()
		{
			SaveProject();

			string arguments = ProjectFullPath;
			arguments += @" 127.0.0.1";

			arguments += @" -skipcompile -game";

			if (LaunchSettings.Log)
			{
				arguments += @" -log";
			}

			if (!LaunchSettings.FullScreen)
			{
				arguments += @" -WINDOWED -ResX=960 -ResY=540";
			}

			return arguments;
		}

		public Dictionary<string, object> GetData()
		{
			Dictionary<string, object> data = new Dictionary<string, object>
			{
				{ "ProjectFullPath", ProjectFullPath },
				{ "ProjectDirectory", ProjectDirectory },
				{ "EngineAssociation", EngineAssociation },
				{ "EnginePath", EnginePath },
				{ "ProjectName", ProjectNiceName },
				{ "ProjectCompany", ProjectCompany },
			};

			if (!string.IsNullOrWhiteSpace(Copyright))
			{
				data.Add("CustomCopyright", Copyright);
			}

			return data;
		}

		public void SaveProject()
		{
			if (ProjectName != null && projectInitialised)
			{
				for (int i = 0; i < Settings.Default.Projects.Count; ++i)
				{
					string escapedFiledname = ProjectFullPath.Replace("\\", "\\\\");
					if (Settings.Default.Projects[i].Contains(escapedFiledname))
					{
						Settings.Default.Projects[i] = JsonConvert.SerializeObject(this);
						Settings.Default.Save();
					}
				}
			}
		}

		private void GetEngineDir()
		{
			string unrealProjectFile = File.ReadAllText(ProjectFullPath);
			dynamic data = JObject.Parse(unrealProjectFile);

			EngineAssociation = data.EngineAssociation;

			EnginePath = string.Empty;

			// Binary Version of Unreal
			// 64 Bit
			RegistryKey localKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
			localKey64 = localKey64.OpenSubKey(@"SOFTWARE\EpicGames\Unreal Engine\" + EngineAssociation);
			if (localKey64 != null)
			{
				object engineAssociationValue = localKey64.GetValue("InstalledDirectory");
				if (engineAssociationValue != null)
				{
					EnginePath = engineAssociationValue.ToString();
				}

				localKey64.Close();
			}

			// 32 Bit
			if (string.IsNullOrWhiteSpace(EnginePath))
			{
				RegistryKey localKey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
				localKey32 = localKey32.OpenSubKey(@"SOFTWARE\EpicGames\Unreal Engine\" + EngineAssociation);
				if (localKey32 != null)
				{
					object engineAssociationValue1 = localKey32.GetValue("InstalledDirectory");
					if (engineAssociationValue1 != null)
					{
						EnginePath = engineAssociationValue1.ToString();
					}

					localKey32.Close();
				}
			}

			// Installed from a source code build of unreal.
			// 64 Bit
			if (string.IsNullOrWhiteSpace(EnginePath))
			{
				localKey64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
				localKey64 = localKey64.OpenSubKey(@"Software\Epic Games\Unreal Engine\Builds");
				if (localKey64 != null)
				{
					object engineAssociationValue2 = localKey64.GetValue(EngineAssociation);
					if (engineAssociationValue2 != null)
					{
						EnginePath = engineAssociationValue2.ToString();
					}

					localKey64.Close();
				}
			}

			// 32 Bit
			if (string.IsNullOrWhiteSpace(EnginePath))
			{
				RegistryKey localKey32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
				localKey32 = localKey32.OpenSubKey(@"Software\Epic Games\Unreal Engine\Builds");
				if (localKey32 != null)
				{
					object engineAssociationValue3 = localKey32.GetValue(EngineAssociation);
					if (engineAssociationValue3 != null)
					{
						EnginePath = engineAssociationValue3.ToString();
					}

					localKey32.Close();
				}
			}

			if (string.IsNullOrWhiteSpace(EnginePath))
			{
				string strMsg = "Unable to find an engine for " + ProjectName + ", the engine association may need to be refreshed. Right click on the .uProjectFile, \"Switch Unreal Engine Version...\"";

				MessageBox.Show(strMsg, "Oopsie'd " + ProjectName);
			}
		}

		private string GetMapAsLaunchArgument()
		{
			string arguments = string.Empty;

			if (!string.IsNullOrWhiteSpace(LaunchSettings.LastSelectedMap) && LaunchSettings.LastSelectedMap != @"(Default)")
			{
				arguments += " " + LaunchSettings.LastSelectedMap;
			}

			return arguments;
		}
	}
}
