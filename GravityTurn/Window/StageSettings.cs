using KSP.Localization;
using System.IO;
using UnityEngine;

namespace GravityTurn.Window
{
    public class StageSettings : BaseWindow
    {
        HelpWindow helpWindow;
        bool initted = false;
        public StageSettings(GravityTurner turner, int WindowID, HelpWindow inhelpWindow)
            : base(turner, WindowID)
        {
            helpWindow = inhelpWindow;
            WindowTitle = Localizer.Format("#autoLOC_GT_StageSettingsTitle"); // GravityTurn Stage & Cache Settings
            windowPos.width = 300;
        }
        public void InitPos()
        {
            if (!initted)
            {
                windowPos.left = GravityTurner.mainWindow.windowPos.left - windowPos.width;
                if (windowPos.left < 0)
                    windowPos.left = GravityTurner.mainWindow.windowPos.left + GravityTurner.mainWindow.windowPos.width;
                windowPos.top = GravityTurner.mainWindow.windowPos.top + GravityTurner.mainWindow.windowPos.height / 2;
                initted = true;
            }
        }
        public override void WindowGUI(int windowID)
        {
            base.WindowGUI(windowID);
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    ItemLabel(Localizer.Format("#autoLOC_GT_FairingPressure")); // Fairing Pressure
                    turner.FairingPressure.setValue(GUILayout.TextField(string.Format("{0:0}", turner.FairingPressure), GUILayout.Width(60)));
                    turner.FairingPressure.locked = GuiUtils.LockToggle(turner.FairingPressure.locked);
                    helpWindow.Button(Localizer.Format("#autoLOC_GT_FairingPressureHelp")); // Dynamic pressure where we pop the procedural fairings.  Higher values will pop lower in the atmosphere, which saves weight, but can cause overheating.  Fairings are heavy, so it's definitely a good idea to pop them as soon as possible.
                }
                using (new GUILayout.HorizontalScope())
                {
                    ItemLabel(Localizer.Format("#autoLOC_GT_StagePostDelay")); // Stage Post Delay
                    turner.autostagePostDelay.setValue(GUILayout.TextField(string.Format("{0:0}", turner.autostagePostDelay), GUILayout.Width(60)));
                    turner.autostagePostDelay.locked = GuiUtils.LockToggle(turner.autostagePostDelay.locked);
                    helpWindow.Button(Localizer.Format("#autoLOC_GT_StagePostDelayHelp")); // Delay after a stage event before we consider the next stage.
                }
                using (new GUILayout.HorizontalScope())
                {
                    ItemLabel(Localizer.Format("#autoLOC_GT_StagePreDelay")); // Stage Pre Delay
                    turner.autostagePreDelay.setValue(GUILayout.TextField(string.Format("{0:0}", turner.autostagePreDelay), GUILayout.Width(60)));
                    turner.autostagePreDelay.locked = GuiUtils.LockToggle(turner.autostagePreDelay.locked);
                    helpWindow.Button(Localizer.Format("#autoLOC_GT_StagePreDelayHelp")); // Delay after running out of fuel before we activate the next stage.

                }
                using (new GUILayout.HorizontalScope())
                {
                    ItemLabel(Localizer.Format("#autoLOC_GT_StageLimit")); // Stage Limit
                    turner.autostageLimit.setValue(GUILayout.TextField(string.Format("{0:0}", turner.autostageLimit), GUILayout.Width(60)));
                    turner.autostageLimit.locked = GuiUtils.LockToggle(turner.autostageLimit.locked);
                    helpWindow.Button(Localizer.Format("#autoLOC_GT_StageLimitHelp")); // Stop at this stage number
                }
                using (new GUILayout.HorizontalScope())
                {
                   ItemLabel(Localizer.Format("#autoLOC_GT_UseMechjeb")); // use Mechjeb
                    HighLogic.CurrentGame.Parameters.CustomParams<GT>().useMechjebIfAvailable = GUILayout.Toggle((bool)HighLogic.CurrentGame.Parameters.CustomParams<GT>().useMechjebIfAvailable, "", GUILayout.Width(60));
                    turner.useMechjeb.setValue(HighLogic.CurrentGame.Parameters.CustomParams<GT>().useMechjebIfAvailable);
                    GUILayout.Label("(this is a global setting)");
                }
                GUILayout.Space(10);

                using (new GUILayout.HorizontalScope())
                {

                    //if (GUILayout.Button(Localizer.Format("#autoLOC_GT_ClearCacheButton"), GUILayout.Width(90))) // Clear Cache
                    if (GUILayout.Button(Localizer.Format("#autoLOC_GT_ClearCacheButton")))
                    {
                        // Need to clear the cache directory
                        // gt_launchdb*
                        // gt_vessel*

                        foreach (string f in Directory.EnumerateFiles(LaunchDB.GetBaseFilePath(this.GetType(), ""), "gt_vessel_*"))
                        {
                            File.Delete(f);
                        }
                        foreach (string f in Directory.EnumerateFiles(LaunchDB.GetBaseFilePath(this.GetType(), ""), "gt_launchdb"))
                        {
                            File.Delete(f);
                        }
                    }
#if false
            if (!turner.IsLaunchDBEmpty())
            {
                if (GUILayout.Button("Reset Guess", GUILayout.ExpandWidth(false)))
                {
                    if (File.Exists(GravityTurner.ConfigFilename(GravityTurner.getVessel)))
                        File.Delete(GravityTurner.ConfigFilename(GravityTurner.getVessel));

                    if (File.Exists(turner.launchdb.GetFilename()))
                        File.Delete(turner.launchdb.GetFilename());
                    turner.ClearLaunchDB();
                }
            }
#endif
                }
            }
            GUI.DragWindow();
        }
    }
}
