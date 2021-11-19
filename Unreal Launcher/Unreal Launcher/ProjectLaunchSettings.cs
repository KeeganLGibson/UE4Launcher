// Copyright (c) Keegan L Gibson. All rights reserved.

using System;

namespace Unreal_Launcher
{
	[Serializable]
	public class ProjectLaunchSettings
	{
		private string lastSelectedMap;
		private string lastSelectedSaveGame;
		private bool fullScreen = false;
		private bool log = false;

		public string LastSelectedMap { get => lastSelectedMap; set => lastSelectedMap = value; }

		public string LastSelectedSaveGame { get => lastSelectedSaveGame; set => lastSelectedSaveGame = value; }

		public bool FullScreen { get => fullScreen; set => fullScreen = value; }

		public bool Log { get => log; set => log = value; }
	}
}
