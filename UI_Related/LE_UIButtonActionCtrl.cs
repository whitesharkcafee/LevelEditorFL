using FractalSpace;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    
    public class LE_UIButtonActionCtrl : MonoBehaviour
    {
        public void OnClick()
        {
            if (!LE_MenuUIManager.Instance.levelButtonsWasClicked)
            {
                LE_MenuUIManager.Instance.SwitchBetweenMenuAndLEMenu();
            }
        }
    }
}
