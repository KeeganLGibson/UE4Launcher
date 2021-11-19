// Copyright (c) Keegan L Gibson. All rights reserved.

using System;

namespace Unreal_Launcher
{
	[Serializable]
	public class ProjectLaunchSettings
	{
		private string _lastSelectedMap;
		private string _lastSelectedSaveGame;
		private bool _fullScreen = false;
		private bool _log = false;

		public string LastSelectedMap { get => _lastSelectedMap; set => _lastSelectedMap = value; }

		public string LastSelectedSaveGame { get => _lastSelectedSaveGame; set => _lastSelectedSaveGame = value; }

		public bool FullScreen { get => _fullScreen; set => _fullScreen = value; }

		public bool Log { get => _log; set => _log = value; }
	}
}
