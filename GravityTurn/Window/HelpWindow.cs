using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GravityTurn.Window
{
    public class HelpWindow : BaseWindow
    {
        public string helpWindowText = "";
        bool initted = false;

        public HelpWindow(GravityTurner inTurner, int inWindowID)
            : base(inTurner,inWindowID)
        {
            WindowTitle = Localizer.Format("#autoLOC_GT_GravityTurnHelpTitle"); // GravityTurn Help
            //windowPos.left = Screen.width - (windowPos.width + 40);
            //windowPos.top = 30;
        }

        public void InitPos()
        {
            if (!initted)
            {
                // Display help window on top or under of main window
                if ((windowPos.top + windowPos.height) > Screen.height)
                    windowPos.top = GravityTurner.mainWindow.windowPos.top - windowPos.height;
                windowPos.top = GravityTurner.mainWindow.windowPos.top + GravityTurner.mainWindow.windowPos.height;
                windowPos.left = GravityTurner.mainWindow.windowPos.left;
                windowPos.width = GravityTurner.mainWindow.windowPos.width;

                initted = true;
            }
        }

        public void Button(string helpMessage)
        {
            if (GUILayout.Button("?", GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false), GUILayout.MaxWidth(18), GUILayout.MinHeight(18)))
            {
                InitPos();
                if (helpWindowText == helpMessage && WindowVisible)
                    WindowVisible = false;
                else
                    WindowVisible = true;

                helpWindowText = helpMessage;
            }
        }

        public override void WindowGUI(int windowID)
        {
            base.WindowGUI(windowID);
            GUILayout.BeginVertical();
            //GUILayout.TextArea(helpWindowText);
            //To display bold text (<b>C</b>), for CompactSkin and RegularSkin :
            GUILayout.TextArea(helpWindowText, new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
