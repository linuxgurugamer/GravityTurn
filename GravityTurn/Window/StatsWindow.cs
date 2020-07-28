using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GravityTurn.Window
{
    public class StatsWindow: BaseWindow
    {
        bool initted = false;
        public StatsWindow(GravityTurner turner, int WindowID)
            : base(turner, WindowID)
        {
            WindowTitle = "GravityTurn Statistics Window";
            windowPos.height = 200;
            windowPos.width = 310;
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
            GUILayout.Label(turner.Message, GUILayout.Width(300), GUILayout.Height(250));
            GUILayout.EndVertical();
            if (GameSettings.MODIFIER_KEY.GetKeyDown() && !GravityTurner.DebugShow)
            {
                GravityTurner.DebugShow = true;
            }
            if (GravityTurner.DebugShow)
            {
                GUILayout.BeginVertical();
                GUILayout.Label(GravityTurner.DebugMessage, GUILayout.Width(300), GUILayout.Height(350));
                GUILayout.EndVertical();
            }
            GUI.DragWindow();
        }
    }
}
