using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GravityTurn.Window
{
    public class StatsWindow: BaseWindow
    {
        bool initted = false;
        string biggestlineTxt;
        string biggestlineValue;
        float biggerlineTxtSize = 0;
        float biggerlineValueSize = 0;

        public StatsWindow(GravityTurner turner, int WindowID)
            : base(turner, WindowID)
        {
            WindowTitle = Localizer.Format("#autoLOC_GT_StatsWindowTitle"); // GravityTurn Statistics Window
            windowPos.height = 200;
            windowPos.width = 100;
        }

        public void InitPos()
        {
            if (!initted)
            {
                windowPos.left = GravityTurner.mainWindow.windowPos.left - windowPos.width;
                if (windowPos.left < 0)
                    windowPos.left = GravityTurner.mainWindow.windowPos.left + GravityTurner.mainWindow.windowPos.width;
                windowPos.top = GravityTurner.mainWindow.windowPos.top;
                initted = true;
            }
        }

        public override void WindowGUI(int windowID)
        {
            base.WindowGUI(windowID);

            GUILayout.BeginVertical();

            if (turner.locStatStrList != null)
            {
                biggestlineTxt = turner.BiggestStr(turner.locStatStrList);
                biggestlineValue = "9999.00 t";
                if (turner.Launching)
                    biggestlineValue = "9999.00 -> 9999.00 m/s @AP";
                biggerlineTxtSize = TxtWidth(biggestlineTxt);
                biggerlineValueSize = TxtWidth(biggestlineValue);

                windowPos.width = biggerlineTxtSize + biggerlineValueSize;

                for (int i = 0; i < turner.locStatStrList.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(turner.locStatStrList[i][0], GUILayout.Width(biggerlineTxtSize));
                    GUILayout.Label(turner.locStatStrList[i][1], GUILayout.Width(biggerlineValueSize));
                    GUILayout.EndHorizontal();
                    GUILayout.ExpandHeight(true);
                }
            }
            GUILayout.EndVertical();

            if (GameSettings.MODIFIER_KEY.GetKeyDown() && !GravityTurner.DebugShow)
            {
                GravityTurner.DebugShow = true;
            }
            if (GravityTurner.DebugShow)
            {
                GUILayout.BeginVertical();
                GUILayout.Label(GravityTurner.DebugMessage, GUILayout.Width(250), GUILayout.Height(350));
                GUILayout.EndVertical();
            }
            GUI.DragWindow();
        }
    }
}
