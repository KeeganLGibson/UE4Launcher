using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unreal_Launcher
{
    [Serializable]
    public class ProjectSettings
    {
        public string LastSelectedMap;
        public bool bFullScreen = false;
    }
}
