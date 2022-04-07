using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace GravityTurn.Window
{
    public class MainWindow :  BaseWindow
    {

        HelpWindow helpWindow = null;
        StageSettings stagesettings = null;

        bool initted = false;

        // To calculate space needed by toggle text
        public float mainWindowBiggerLineWidth;
        public float inputTextField = 60;

        public MainWindow(GravityTurner inTurner, int inWindowID)
            : base(inTurner,inWindowID)
        {
            turner = inTurner;
            helpWindow = new HelpWindow(inTurner,inWindowID+1);
            stagesettings = new StageSettings(inTurner, inWindowID + 2, helpWindow);

            windowPos.width = 250;
            windowPos.height = 100;
            //windowPos.left = Screen.width - (windowPos.width + 40);
            windowPos.left = 63;
            windowPos.top = 65;
            Version v = typeof(GravityTurner).Assembly.GetName().Version;
            WindowTitle = String.Format("GravityTurn");
        }

        private void UiStartSpeed()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_Start")); // Start m/s
            turner.StartSpeed.setValue(GUILayout.TextField(string.Format("{0:0.0}", turner.StartSpeed), GUILayout.Width(inputTextField)));
            turner.StartSpeed.locked = GuiUtils.LockToggle(turner.StartSpeed.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_StartHelp")); //At this speed, pitch to Turn Angle to begin the gravity turn.  Stronger rockets and extremely aerodynamically stable rockets should do this earlier.
            GUILayout.EndHorizontal();

        }
        private void UiTurnAngle()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_TurnAngle")); // Turn Angle
            turner.TurnAngle.setValue(GUILayout.TextField(string.Format("{0:0.0}", turner.TurnAngle), GUILayout.Width(inputTextField)));
            turner.TurnAngle.locked = GuiUtils.LockToggle(turner.TurnAngle.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_TurnAngleHelp")); // Angle to start turn at Start Speed.  Higher values may cause aerodynamic stress.
            GUILayout.EndHorizontal();
        }
        private void UiAPTimeStart()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_APTimeStart")); // Hold AP Time Start
            turner.APTimeStart.setValue(GUILayout.TextField(turner.APTimeStart.ToString(), GUILayout.Width(inputTextField)));
            turner.APTimeStart.locked = GuiUtils.LockToggle(turner.APTimeStart.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_APTimeStartHelp")); // Starting value for Time To Prograde.  Higher values will make a steeper climb.  Steeper climbs are usually worse.  Lower values may cause overheating or death.
            GUILayout.EndHorizontal();
        }
        private void UiAPTimeFinish()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_APTimeFinish")); // Hold AP Time Finish
            turner.APTimeFinish.setValue(GUILayout.TextField(turner.APTimeFinish.ToString(), GUILayout.Width(inputTextField)));
            turner.APTimeFinish.locked = GuiUtils.LockToggle(turner.APTimeFinish.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_APTimeFinishHelp")); // AP Time will fade to this value, to vary the steepness of the ascent during the ascent.
            GUILayout.EndHorizontal();
        }
        private void UiSensitivity()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_Sensitivity")); // Sensitivity
            turner.Sensitivity.setValue(GUILayout.TextField(turner.Sensitivity.ToString(), GUILayout.Width(inputTextField)));
            turner.Sensitivity.locked = GuiUtils.LockToggle(turner.Sensitivity.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_SensitivityHelp")); // Will not throttle below this value.  Mostly a factor at the end of ascent.
            GUILayout.EndHorizontal();
        }
        private void UiDestinationHeight()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_DestinationHeight")); // Destination Height (km)
            turner.DestinationHeight.setValue(GUILayout.TextField(turner.DestinationHeight.ToString(), GUILayout.Width(inputTextField)));
            turner.DestinationHeight.locked = GuiUtils.LockToggle(turner.DestinationHeight.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_DestinationHeightHelp")); // Desired Apoapsis.
            GUILayout.EndHorizontal();
        }
        private void UiRoll()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_Roll")); // Roll
            turner.Roll.setValue(GUILayout.TextField(turner.Roll.ToString(), GUILayout.Width(inputTextField)));
            turner.Roll.locked = GuiUtils.LockToggle(turner.Roll.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_RollHelp")); // If you want a particular side of your ship to face downwards.  Shouldn't matter for most ships.  May cause mild nausea.
            GUILayout.EndHorizontal();
        }
        private void UiInclination()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_Inclination")); // Inclination
            turner.Inclination.setValue(GUILayout.TextField(turner.Inclination.ToString(), GUILayout.Width(inputTextField)));
            turner.Inclination.locked = GuiUtils.LockToggle(turner.Inclination.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_InclinationHelp")); // Desired orbit inclination.  Any non-zero value WILL make your launch less efficient. Final inclination will also not be perfect.  Sorry about that, predicting coriolis is hard.
            GUILayout.EndHorizontal();
        }
        private void UiPressureCutoff()
        {
            GUILayout.BeginHorizontal();
            ItemLabel(Localizer.Format("#autoLOC_GT_PressureCutoff")); // Pressure Cutoff
            turner.PressureCutoff.setValue(GUILayout.TextField(turner.PressureCutoff.ToString(), GUILayout.Width(inputTextField)));
            turner.PressureCutoff.locked = GuiUtils.LockToggle(turner.PressureCutoff.locked);
            helpWindow.Button(Localizer.Format("#autoLOC_GT_PressureCutoffHelp")); // Dynamic pressure where we change from Surface to Orbital velocity tracking\nThis will be a balance point between aerodynamic drag in the upper atmosphere vs. thrust vector loss.
            GUILayout.EndHorizontal();
        }

        private string GetAscentPhaseString(GravityTurner.AscentProgram program)
        {
            switch (program)
            {
                case GravityTurner.AscentProgram.Landed:
                    return Localizer.Format("#autoLOC_GT_Landed"); // Landed
                case GravityTurner.AscentProgram.InLaunch:
                    return Localizer.Format("#autoLOC_GT_Launching"); // Launching
                case GravityTurner.AscentProgram.InInitialPitch:
                    return Localizer.Format("#autoLOC_GT_Pitching"); // Pitching
                case GravityTurner.AscentProgram.InTurn:
                    return Localizer.Format("#autoLOC_GT_Turning"); // Turning
                case GravityTurner.AscentProgram.InInsertion:
                    return Localizer.Format("#autoLOC_GT_Insertion"); // Insertion
                case GravityTurner.AscentProgram.InCoasting:
                    return Localizer.Format("#autoLOC_GT_Coasting"); // Coasting
                case GravityTurner.AscentProgram.InCircularisation:
                    return "";
            }
            return "";
        }

        public override void WindowGUI(int windowID)
        {
            base.WindowGUI(windowID);
            if (!WindowVisible && turner.toolbarControl.enabled)
            {
                turner.toolbarControl.SetFalse(false);
                turner.SaveParameters();
            }
            GUILayout.BeginVertical();
            UiStartSpeed();
            UiTurnAngle();
            UiAPTimeStart();
            UiAPTimeFinish();
            UiSensitivity();
            UiDestinationHeight();
            UiRoll();
            UiInclination();
            UiPressureCutoff();

            GUILayout.BeginHorizontal();
            string localizedSetupTxt = Localizer.Format("#autoLOC_GT_Setup"); // Setup
            if (HighLogic.CurrentGame.Parameters.CustomParams<GT>().useStock)
                mainWindowBiggerLineWidth = TxtWidth(localizedSetupTxt);
            if (GUILayout.Button(localizedSetupTxt, GUILayout.ExpandWidth(false)))
            {
                stagesettings.WindowVisible = !stagesettings.WindowVisible;
                stagesettings.InitPos();
            }

            string localizedAutoStageTxt = Localizer.Format("#autoLOC_GT_AutoStage"); // Auto Stage
            if (HighLogic.CurrentGame.Parameters.CustomParams<GT>().useStock)
            {
                /*GUILayout.Label("   "); ;
                GUILayout.FlexibleSpace();*/
                float autoStageLineWidth = TxtWidth(localizedAutoStageTxt) + 24;
                turner.EnableStageManager = GUILayout.Toggle(turner.EnableStageManager, localizedAutoStageTxt, GUILayout.Width(autoStageLineWidth));
                mainWindowBiggerLineWidth += TxtWidth(localizedAutoStageTxt);
            }
            else
                turner.EnableStageManager = GUILayout.Toggle(turner.EnableStageManager, Localizer.Format(localizedAutoStageTxt));

            string localizedTimewarpTxt = Localizer.Format("#autoLOC_GT_UseTimewarp"); // Use Timewarp
            if (HighLogic.CurrentGame.Parameters.CustomParams<GT>().useStock)
            {
                /*GUILayout.Label("            ");
                GUILayout.FlexibleSpace();*/
                float timewarpLineWidth = TxtWidth(localizedTimewarpTxt) + 24;
                turner.EnableSpeedup = GUILayout.Toggle(turner.EnableSpeedup, localizedTimewarpTxt, GUILayout.Width(timewarpLineWidth));
                mainWindowBiggerLineWidth += TxtWidth(localizedTimewarpTxt);
            }
            else
                turner.EnableSpeedup = GUILayout.Toggle(turner.EnableSpeedup, Localizer.Format(localizedTimewarpTxt));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            string localizedLaunchMapTxt = Localizer.Format("#autoLOC_GT_ShowLaunchMap"); // Show Launch Map
            if (HighLogic.CurrentGame.Parameters.CustomParams<GT>().useStock)
            {
                float launchMapLineWidth = TxtWidth(localizedLaunchMapTxt) + 24;
                turner.flightMapWindow.WindowVisible = GUILayout.Toggle(turner.flightMapWindow.WindowVisible, localizedLaunchMapTxt, GUILayout.Width(launchMapLineWidth));
            }
            else
                turner.flightMapWindow.WindowVisible = GUILayout.Toggle(turner.flightMapWindow.WindowVisible, localizedLaunchMapTxt, GUILayout.ExpandWidth(false));
            /*GUILayout.Label("   ");
        if (HighLogic.CurrentGame.Parameters.CustomParams<GT>().useStock)
            GUILayout.FlexibleSpace();*/
            string localizedStatsTxt = Localizer.Format("#autoLOC_GT_ShowStats");
            if (HighLogic.CurrentGame.Parameters.CustomParams<GT>().useStock)
            {
                float statsLineWidth = TxtWidth(localizedStatsTxt) + 24;
                turner.EnableStats = GUILayout.Toggle(turner.EnableStats, localizedStatsTxt, GUILayout.Width(statsLineWidth));
            }
            else
                turner.EnableStats = GUILayout.Toggle(turner.EnableStats, localizedStatsTxt, GUILayout.ExpandWidth(false)); // Show Stats
            //GUILayout.FlexibleSpace();

            if (turner.statsWindow.WindowVisible != turner.EnableStats && initted)
            {
                turner.statsWindow.WindowVisible = turner.EnableStats;
                turner.statsWindow.Save();
                if (!turner.statsWindow.WindowVisible)
                {
                    turner.statsWindow.windowPos.height = 200;
                    GravityTurner.DebugShow = false;
                }
                else
                    turner.statsWindow.InitPos();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            // when not landed and not launching we are in orbit. allow to save.
            if (!GravityTurner.getVessel.Landed && !turner.Launching)
            {
                if (turner.program >= GravityTurner.AscentProgram.InCircularisation)
                    GUILayout.Label(Localizer.Format("#autoLOC_GT_LaunchSuccess"), GUILayout.ExpandWidth(false)); // Launch success! 

                if (GUILayout.Button(GuiUtils.saveIcon, GUILayout.ExpandWidth(false), GUILayout.MinWidth(18), GUILayout.MinHeight(21)))
                    turner.SaveDefaultParameters();
            }
            else
                GUILayout.Label(Localizer.Format("#autoLOC_GT_TimeToMatch", GetAscentPhaseString(turner.program), string.Format("{0:0.0}", turner.HoldAPTime)), GUILayout.ExpandWidth(false)); // {0}, time to match: {1:0.0} s
            //GUILayout.Label(string.Format("{0}, time to match: {1:0.0} s", GetAscentPhaseString(turner.program), turner.HoldAPTime), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            // landed, not launched yet. Allow configuration
            if (GravityTurner.getVessel.Landed && !turner.Launching)
            {
                GUILayout.BeginHorizontal();
                string guess = turner.IsLaunchDBEmpty() ? Localizer.Format("#autoLOC_GT_FirstGuess") : Localizer.Format("#autoLOC_GT_ImproveGuess"); // First Guess  Improve Guess
                if (GUILayout.Button(guess, GUILayout.ExpandWidth(false)))
                    turner.CalculateSettings(GravityTurner.getVessel);

                if (!turner.IsLaunchDBEmpty() && GUILayout.Button(Localizer.Format("#autoLOC_GT_PreviousBest"), GUILayout.ExpandWidth(false))) // Previous Best
                    turner.CalculateSettings(GravityTurner.getVessel, true);

                if (GUILayout.Button("C", GUILayout.ExpandWidth(false)))
                {
                    if (File.Exists(GravityTurner.ConfigFilename(GravityTurner.getVessel)))
                        File.Delete(GravityTurner.ConfigFilename(GravityTurner.getVessel));

                    if (File.Exists(turner.launchdb.GetFilename()))
                        File.Delete(turner.launchdb.GetFilename());
                    turner.ClearLaunchDB();
                }


                helpWindow.Button(Localizer.Format("#autoLOC_GT_GravityTurnHelp")); // Improve Guess will try to extrapolate the best settings based on previous launches.  This may end in fiery death, but it won't happen the same way twice.  Be warned, sometimes launches get worse before they get better.  But they do get better.  To reset, click the <bold>C</bold> button
                if (GUILayout.Button(GuiUtils.saveIcon, GUILayout.ExpandWidth(false), GUILayout.MinWidth(18), GUILayout.MinHeight(21)))
                    turner.SaveDefaultParameters();
                GUILayout.EndHorizontal();
            }
            // while landed, show launch button
            if (GravityTurner.getVessel.Landed && !turner.Launching && GUILayout.Button(Localizer.Format("#autoLOC_GT_LaunchButton"), GUILayout.ExpandWidth(true), GUILayout.MinHeight(30))) // Launch!
            {
                Debug.Log("Launch button pressed again");
                turner.Launch();
            }
            // while launching, show launch button
            if (turner.Launching && GUILayout.Button(Localizer.Format("#autoLOC_GT_AbortButton"), GUILayout.MinHeight(30))) // Abort!
            {
                turner.Kill();
                turner.RecordAbortedLaunch();
            }
#if DEBUG
            // GUILayout.Label(GravityTurner.DebugMessage, GUILayout.ExpandWidth(true), GUILayout.MinHeight(200));
#endif

            GUILayout.EndVertical();
   
            double StopHeight = GravityTurner.getVessel.mainBody.atmosphereDepth;
            if (StopHeight <= 0)
                StopHeight = turner.DestinationHeight * 1000;
            turner.HoldAPTime = turner.APTimeStart + ((float)GravityTurner.getVessel.altitude / (float)StopHeight * (turner.APTimeFinish - turner.APTimeStart));
            if (turner.HoldAPTime > Math.Max(turner.APTimeFinish, turner.APTimeStart))
                turner.HoldAPTime = Math.Max(turner.APTimeFinish, turner.APTimeStart);
            if (turner.HoldAPTime < Math.Min(turner.APTimeFinish, turner.APTimeStart))
                turner.HoldAPTime = Math.Min(turner.APTimeFinish, turner.APTimeStart);
            Rect r = GUILayoutUtility.GetLastRect();
            float minHeight = r.height + r.yMin + 10;
            if (windowPos.height != minHeight && minHeight>20)
            {
                windowPos.height = minHeight;
                Save();
            }
            GUI.DragWindow();
            initted = true;
        }
    }
}
