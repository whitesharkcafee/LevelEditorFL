using FractalSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace FS_LevelEditor.Playmode.Patches
{
    [HarmonyPatch(typeof(MenuController), "ConfigureMenuForPause")]
    public static class GamePauseCurrentLevelPath
    {
        public static void Prefix()
        {
            if (PlayModeController.Instance)
            {
                PlayModeController.Instance.PatchPauseCurrentLevelNameInResumeButton();
            }
        }
    }
}
