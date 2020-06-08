using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        [NonSerialized]
        private bool ProjectInitialised = false;

        public ProjectSettings LaunchSettings = new ProjectSettings();

        public Project()
        {

        }

        public Project(string FullProjectPath)
        {
            ProjectFullPath = FullProjectPath;

            Init();
        }

        public void Init()
        {
            ProjectName = Path.GetFileNameWithoutExtension(ProjectFullPath);
            ProjectDirectory = Path.GetDirectoryName(ProjectFullPath);

            GetEngineDir();

            if(string.IsNullOrWhiteSpace(ProjectNiceName))
            {
                ProjectNiceName = ProjectName;
            }

            ProjectInitialised = true;
        }

        private void GetEngineDir()
        {
            string UProjectData = File.ReadAllText(ProjectFullPath);
            dynamic data = JObject.Parse(UProjectData);

            EngineAssociation = data.EngineAssociation;

            EnginePath = "";

            // Binary Version of Unreal
            // 64 Bit
            RegistryKey localKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey64 = localKey64.OpenSubKey(@"SOFTWARE\EpicGames\Unreal Engine\" + EngineAssociation);
            if (localKey64 != null)
            {
                object EngineAssociationValue = localKey64.GetValue("InstalledDirectory");
                if (EngineAssociationValue != null)
                {
                    EnginePath = EngineAssociationValue.ToString();
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
                    object EngineAssociationValue = localKey32.GetValue("InstalledDirectory");
                    if (EngineAssociationValue != null)
                    {
                        EnginePath = EngineAssociationValue.ToString();
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
                    object EngineAssociationValue = localKey64.GetValue(EngineAssociation);
                    if (EngineAssociationValue != null)
                    {
                        EnginePath = EngineAssociationValue.ToString();
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
                    object EngineAssociationValue = localKey32.GetValue(EngineAssociation);
                    if (EngineAssociationValue != null)
                    {
                        EnginePath = EngineAssociationValue.ToString();
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

        public Dictionary<string, Object> GetData()
        {
            Dictionary<string, Object> Data = new Dictionary<string, object>
            {
                { "ProjectFullPath", ProjectFullPath },
                { "ProjectDirectory", ProjectDirectory },
                { "EngineAssociation", EngineAssociation },
                { "EnginePath", EnginePath },
                { "ProjectName", ProjectNiceName },
                { "ProjectCompany", ProjectCompany },
                { "OneLineCopyright", Copyright }
            };

            return Data;
        }

        public void SaveProject()
        {
            if (ProjectName != null && ProjectInitialised)

            {
                for (int i = 0; i < Settings.Default.Projects.Count; ++i)
                {
                    string EscapedFiledname = ProjectFullPath.Replace("\\", "\\\\");
                    if (Settings.Default.Projects[i].Contains(EscapedFiledname))
                    {
                        Settings.Default.Projects[i] = JsonConvert.SerializeObject(this);
                        Settings.Default.Save();
                    }
                }
            }
        }

        private string GetMapAsLaunchArgument()
        {
            string Arguments = "";

            if (!string.IsNullOrWhiteSpace(LaunchSettings.LastSelectedMap) && LaunchSettings.LastSelectedMap != @"(Default)")
            {
                Arguments += " " + LaunchSettings.LastSelectedMap;
            }

            return Arguments;
        }

        public string GenerateEditorArguments()
        {
            SaveProject();

            string Arguments = ProjectFullPath;
            Arguments += GetMapAsLaunchArgument();

            Arguments += @" -skipcompile";

            return Arguments;
        }

        // Order is Important!
        public string GenerateGameArguments()
        {
            SaveProject();

            string Arguments = ProjectFullPath;
            Arguments += GetMapAsLaunchArgument();

            Arguments += @" -skipcompile -game";

            if(!LaunchSettings.bFullScreen)
            {
                Arguments += @" -WINDOWED -ResX=960 -ResY=540";
            }

            return Arguments;
        }
    }
}
