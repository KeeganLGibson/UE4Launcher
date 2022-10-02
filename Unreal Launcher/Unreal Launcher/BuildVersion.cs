// Copyright (c) Keegan L Gibson. All rights reserved.

using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Unreal_Launcher
{
	[Serializable]
	public class BuildVersion
	{
		public int MajorVersion { get; set; }
		public int MinorVersion { get; set; }
		public int PatchVersion { get; set; }
		public int Changelist { get; set; }
		public int CompatibleChangelist { get; set; }
		public bool IsLicenseeVersion { get; set; }
		public bool IsPromotedBuild { get; set; }
		public string BranchName { get; set; }

		public bool LoadFromJson(string buildVersionFilePath)
		{
			string json = File.ReadAllText(buildVersionFilePath);

			try
			{
				JObject buildVersionJSON = JObject.Parse(json);
				MajorVersion = buildVersionJSON["MajorVersion"].Value<int>();
				MinorVersion = buildVersionJSON["MinorVersion"].Value<int>();
				PatchVersion = buildVersionJSON["PatchVersion"].Value<int>();
				Changelist = buildVersionJSON["Changelist"].Value<int>();
				CompatibleChangelist = buildVersionJSON["CompatibleChangelist"].Value<int>();
				IsLicenseeVersion = buildVersionJSON["IsLicenseeVersion"].Value<bool>();
				IsPromotedBuild = buildVersionJSON["IsPromotedBuild"].Value<bool>();
				BranchName = buildVersionJSON["BranchName"].Value<string>();
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
		}
	}
}
