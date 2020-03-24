using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.IO;
using ClickThroughFix;

namespace GravityTurn.Window
{

    public class PersistentWindow
    {
        [Persistent]
        public float left;
        [Persistent]
        public float top;
        [Persistent]
        public float width;
        [Persistent]
        public float height;

        public PersistentWindow(float left,float top,float width,float height)
        {
            this.left=left;
            this.top=top;
            this.width=width;
            this.height=height;
        }
        public PersistentWindow()
        {
            this.left = 0;
            this.top = 0;
            this.width = 0;
            this.height = 0;
        }
        public static implicit operator Rect(PersistentWindow rect)
        {
            return new Rect(rect.left, rect.top, rect.width, rect.height);
        }
        public static implicit operator PersistentWindow(Rect rect)
        {
            return new PersistentWindow(rect.xMin, rect.yMin, rect.width, rect.height);
        }
    }

    public class BaseWindow
    {
        int WindowID;
        protected GravityTurner turner;
        public bool WindowVisible = false; 
        public string WindowTitle = "GravityTurn";
        string filename;
#if false
        WindowPositions.WindowType winType;
#endif
        public static bool ShowGUI = true;

        [Persistent]
        public PersistentWindow windowPos = new PersistentWindow();


        protected void ItemLabel(string labelText)
        {
            GUILayout.Label(labelText, GUILayout.ExpandWidth(false), GUILayout.Width(windowPos.width / 2));
        }

        public BaseWindow(GravityTurner turner, int inWindowID)
        {
            this.turner = turner;
            turner.windowManager.Register(this);
            WindowID = inWindowID;
#if false
            switch (inWindowID)
            {
                case 6378070 + 4:   // StatsWindow
                    winType = WindowPositions.WindowType.StatsWindow;
                    break;
                case 6378070 + 2:   // StageSettings
                    winType = WindowPositions.WindowType.StageSettings;
                    break;
                case 6378070 + 0:   // MainWindoiw
                    winType = WindowPositions.WindowType.MainWindow;
                    break;
                case 6378070 + 1:   // HelpWindow
                    winType = WindowPositions.WindowType.HelpWindow;
                    break;
                case 548302 + 0:    // FlightMapWindow
                    winType = WindowPositions.WindowType.FlightMapWindow;
                    break;
            }
#endif
            filename = LaunchDB.GetBaseFilePath(turner.GetType(), string.Format("gt_window_{0}.cfg", WindowID));
            Load();

            if (windowPos.left + windowPos.width > Screen.width)
            {
                windowPos.left = Screen.width - windowPos.width;
            }
            if (windowPos.top + windowPos.height > Screen.height )
            {
                windowPos.top = Screen.height - windowPos.height;
            }
            if (windowPos.top < 0)
                windowPos.top = 0;

#if false
            WindowPositions.Load(
                LaunchDB.GetBaseFilePath(turner.GetType(), ""),
                winType, out Rect winPos);

            if (winPos.x != windowPos.left)
                Debug.Log("X not equal");
            if (winPos.y != Screen.height - windowPos.top)
                Debug.Log("Y not equal");
            if (winPos.width != windowPos.width)
                Debug.Log("WIDTH not equal");
            if (winPos.height != windowPos.height)
                Debug.Log("HEIGHT not equal");

#endif
        }

        public void Load()
        {
            try
            {
                ConfigNode root = ConfigNode.Load(filename);
                if (root != null)
                {
                    ConfigNode.LoadObjectFromConfig(this, root);
                }
            }
            catch (Exception ex)
            {
                GravityTurner.Log("Window Load error {0}", ex.ToString());
            }
        }

        public virtual void WindowGUI(int windowID)
        {
            if (!ShowGUI)
                return;
            if (GUI.Button(new Rect(windowPos.width - 18, 2, 16, 16), "X"))
            {
                WindowVisible = false;
            }
            GUI.DragWindow();
        }
        public void drawGUI()
        {
            if (WindowVisible && ShowGUI)
            {
                GuiUtils.LoadSkin(GuiUtils.SkinType.Compact);
                GUI.skin = GuiUtils.skin;
                windowPos = ClickThruBlocker.GUILayoutWindow(WindowID, windowPos, WindowGUI, WindowTitle, GUILayout.MinWidth(300));
            }
        }

        public void OnDestroy()
        {
            Save();
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            ConfigNode root = ConfigNode.CreateConfigFromObject(this);
            root.Save(filename);
#if false
            Rect r = new Rect();
            r.x = windowPos.left;
            r.y = Screen.height - windowPos.top;
            r.width = windowPos.width;
            r.height = windowPos. height;
            WindowPositions.Save(Path.GetDirectoryName(filename), winType, r);
#endif
        }
    }
}
