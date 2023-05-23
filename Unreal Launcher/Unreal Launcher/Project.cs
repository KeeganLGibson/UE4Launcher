// Copyright (c) Keegan L Gibson. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unreal_Launcher.Properties;

namespace Unreal_Launcher
{
	[Serializable]
	public class Project
	{
		private string _fullPath = string.Empty;
		public string FullPath { get; set; }

		private string _niceName = string.Empty;
		public string NiceName { get; set; }

		private string _company = string.Empty;
		public string Company { get; set; }

		private string _copyright = string.Empty;
		public string Copyright { get; set; }

		private ProjectLaunchSettings _launchSettings = new ProjectLaunchSettings();
		public ProjectLaunchSettings LaunchSettings => _launchSettings;

		public bool IsProjectInitialised { get; private set; } = false;

		public string ProjectName { get; private set; } = string.Empty;

		public string ProjectDirectory { get; private set; } = string.Empty;

		public string EnginePath { get; private set; } = string.Empty;

		public string EngineAssociation { get; private set; } = string.Empty;

		public BuildVersion BuildVersion { get; private set; } = new BuildVersion();

		public Project()
		{
		}

		public Project(string fullProjectPath)
		{
			FullPath = fullProjectPath;

			Init();
		}

		public void Init()
		{
			ProjectName = Path.GetFileNameWithoutExtension(FullPath);
			ProjectDirectory = Path.GetDirectoryName(FullPath);

			if (File.Exists(FullPath))
			{
				GetEngineDir();

				if (string.IsNullOrWhiteSpace(NiceName))
				{
					NiceName = ProjectName;
				}

				IsProjectInitialised = true;
			}
			else
			{
				System.Windows.MessageBox.Show("Unable to find file: '" + FullPath + "'!", "Unable to find project file!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}

		// Command Line Arguments the Order is Important!
		public string GenerateEditorArguments()
		{
			SaveProject();

			string arguments = FullPath;
			arguments += GetMapAsLaunchArgument();

			arguments += @" -skipcompile";

			return arguments;
		}

		// Command Line Arguments the Order is Important!
		public string GenerateGameArguments()
		{
			SaveProject();

			string arguments = FullPath;
			arguments += GetMapAsLaunchArgument();

			arguments += @" -skipcompile -game";
			arguments += GetSaveGameArguments();

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

			string arguments = FullPath;
			arguments += GetMapAsLaunchArgument();

			arguments += @" -server";
			arguments += GetSaveGameArguments();

			if (LaunchSettings.Log)
			{
				arguments += @" -log";
			}

			return arguments;
		}

		public string GenerateClientArguments()
		{
			SaveProject();

			string arguments = FullPath;
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
				{ "ProjectFullPath", FullPath },
				{ "ProjectDirectory", ProjectDirectory },
				{ "EngineAssociation", EngineAssociation },
				{ "EnginePath", EnginePath },
				{ "ProjectName", NiceName },
				{ "ProjectCompany", Company },
			};

			if (!string.IsNullOrWhiteSpace(Copyright))
			{
				data.Add("CustomCopyright", Copyright);
			}

			return data;
		}

		public void SaveProject()
		{
			if (ProjectName != null && IsProjectInitialised)
			{
				for (int i = 0; i < Settings.Default.Projects.Count; ++i)
				{
					string escapedFiledname = FullPath.Replace("\\", "\\\\");
					if (Settings.Default.Projects[i].Contains(escapedFiledname))
					{
						Settings.Default.Projects[i] = JsonConvert.SerializeObject(this);
						Settings.Default.Save();
					}
				}
			}
		}

		public string GetEditorPath()
		{
			string editorPath = Path.Combine(EnginePath, "Engine", "Binaries", "Win64", "UnrealEditor.exe");

			if (BuildVersion.MajorVersion == 4)
			{
				editorPath = Path.Combine(EnginePath, "Engine", "Binaries", "Win64", "UE4Editor.exe");
			}

			return editorPath;
		}

		private void GetEngineDir()
		{
			string unrealProjectFile = File.ReadAllText(FullPath);
			dynamic data = JObject.Parse(unrealProjectFile);

			EngineAssociation = data.EngineAssociation;

			EnginePath = string.Empty;

			// Source Code Version of Unreal.
			// Assume the engine folder is located in the directory about the .uproject file, this is where UGS expects it to be.
			EnginePath = Path.GetFullPath(Path.Combine(ProjectDirectory, "../"));

			if (!Directory.Exists(Path.Combine(EnginePath, "Engine")))
			{
				EnginePath = string.Empty;
			}

			// Binary Version of Unreal
			if (string.IsNullOrWhiteSpace(EnginePath))
			{
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
				RegistryKey localKey64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
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
				string launcherInstalledFile = File.ReadAllText(@"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat");
				dynamic launcherInstalledData = JObject.Parse(launcherInstalledFile);
				if (launcherInstalledData.InstallationList != null)
				{
					foreach (dynamic engineInstallation in launcherInstalledData.InstallationList)
					{
						string appVersion = engineInstallation.AppVersion;
						if (appVersion.StartsWith(EngineAssociation))
						{
							EnginePath = engineInstallation.InstallLocation;
							break;
						}
					}
				}
			}

			if (string.IsNullOrWhiteSpace(EnginePath))
			{
				string strMsg = "Unable to find an engine for " + ProjectName + ", the engine association may need to be refreshed. Right click on the .uProjectFile, \"Switch Unreal Engine Version...\"";

				System.Windows.MessageBox.Show(strMsg, "Oopsie'd " + ProjectName);
				return;
			}

			if (!BuildVersion.LoadFromJson(Path.Combine(EnginePath, "Engine", "Build", "Build.version")))
			{
				string strMsg = "Unable to determine engine engine version for: " + ProjectName + ",using the engine located at: " + EnginePath + ", the Build.version file may not be present.";

				System.Windows.MessageBox.Show(strMsg, "Oopsie'd " + ProjectName);
				return;
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

		private string GetSaveGameArguments()
		{
			string arguments = string.Empty;

			if (!string.IsNullOrWhiteSpace(LaunchSettings.LastSelectedSaveGame))
			{
				arguments = " -SaveGame=" + LaunchSettings.LastSelectedSaveGame;
			}

			return arguments;
		}
	}
}
