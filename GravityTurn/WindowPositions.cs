using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using UnityEngine;
#if false
namespace GravityTurn
{
    static internal class WindowPositions
    {
        public enum WindowType { StatsWindow, StageSettings, MainWindow, HelpWindow, FlightMapWindow };

        const string FILENAME = "WindowPositions.cfg";

        static public Rect statsWinPos = new Rect();
        static public Rect stageWinPos = new Rect();
        static public Rect mainWinPos = new Rect();
        static public Rect helpWinPos = new Rect();
        static public Rect flightMapWinPos = new Rect();

        static bool loaded = false;

        const string FLIGHTMAPWINDOW = "FlightMapWindow";
        const string HELPWINDOW = "HelpWindow";
        const string MAINWINDOW = "MainWindow";
        const string STAGESETTINGS = "StageSettings";
        const string STATSWINDOW = "StatsWindow";

        const string X = "x";
        const string Y = "y";
        const string WIDTH = "width";
        const string HEIGHT = "height";

        const string GRAVITYTURN = "GravityTurn";

        static public void Save(string dir, WindowType type, Rect winPos)
        {
            switch (type)
            {
                case WindowType.FlightMapWindow:    flightMapWinPos = winPos; break;
                case WindowType.HelpWindow:         helpWinPos = winPos; break;
                case WindowType.MainWindow:         mainWinPos = winPos; break;
                case WindowType.StageSettings:      stageWinPos = winPos; break;
                case WindowType.StatsWindow:        statsWinPos = winPos; break;
            }
            ConfigNode configFile = new ConfigNode();
            ConfigNode root = new ConfigNode(dir +"/" + GRAVITYTURN);

            root.AddNode(FLIGHTMAPWINDOW, MakeNode(flightMapWinPos));
            root.AddNode(HELPWINDOW, MakeNode(helpWinPos));
            root.AddNode(MAINWINDOW, MakeNode(mainWinPos));
            root.AddNode(STAGESETTINGS, MakeNode(stageWinPos));
            root.AddNode(STATSWINDOW, MakeNode(statsWinPos));

            configFile.AddNode(root);

            var filename = dir + "/" +  FILENAME;

            Directory.CreateDirectory(dir);
            root.Save(FILENAME);
        }

        static public void Load(string dir, WindowType type, out Rect winPos)
        {
            if (!loaded)
               if (! LoadFromFile())
                {
                    winPos = new Rect();
                    return;
                }
            switch (type)
            {
                case WindowType.FlightMapWindow: 
                    winPos = new Rect(flightMapWinPos); return;
                case WindowType.HelpWindow:
                    winPos = new Rect(helpWinPos); return;
                case WindowType.MainWindow:
                    winPos = new Rect(mainWinPos); return;
                case WindowType.StageSettings:
                    winPos = new Rect(stageWinPos); return;
                case WindowType.StatsWindow: winPos = new Rect(statsWinPos); return;
            }
            winPos = new Rect();
        }

        static bool LoadFromFile()
        {
            if (!File.Exists(FILENAME))
                return false;

            ConfigNode root = ConfigNode.Load(FILENAME);

            ConfigNode node = root.GetNode(GRAVITYTURN);


            flightMapWinPos= GetRect(FLIGHTMAPWINDOW, node);
            helpWinPos = GetRect(HELPWINDOW, node);
            mainWinPos = GetRect(MAINWINDOW, node);
            stageWinPos = GetRect(STAGESETTINGS, node);
            statsWinPos = GetRect(STATSWINDOW, node);
            return true;
        }

        static ConfigNode MakeNode(Rect pos)
        {
            ConfigNode node = new ConfigNode();

            node.AddValue(X, pos.x);
            node.AddValue(Y, pos.y);
            node.AddValue(WIDTH, pos.width);
            node.AddValue(HEIGHT, pos.height);

            return node;
        }

        static float SafeGetFloat(string str, float def = 0f)
        {
            float f = def;
            float.TryParse(str, out f);
            return f;
        }
        static Rect GetRect(string name, ConfigNode node)
        {
            Rect r = new Rect();

            r.x = SafeGetFloat(node.GetValue(X));
            r.y = SafeGetFloat(node.GetValue(Y));
            r.width = SafeGetFloat(node.GetValue(WIDTH));
            r.height = SafeGetFloat(node.GetValue(HEIGHT));

            return r;
        }
    }
}
#endif