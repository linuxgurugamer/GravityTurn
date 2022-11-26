using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using KSP.UI.Screens;
using KramaxReloadExtensions;
using ToolbarControl_NS;
using System.Linq;
using KSP.Localization;

namespace GravityTurn
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(GravityTurner.MODID, GravityTurner.MODNAME);
        }
    }

    public class StringInList
    {
        public int Size { get; set; }
        public int Index { get; set; }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GravityTurner : ReloadableMonoBehaviour
    {
        static GravityTurner instance;
        public enum AscentProgram
        {
            Landed,
            InLaunch,
            InTurn,
            InInitialPitch,
            InInsertion,
            InCoasting,
            InCircularisation
        }

        public AscentProgram program = AscentProgram.Landed;

        public static Vessel getVessel { get { return FlightGlobals.ActiveVessel; } }

        #region GUI Variables

        [Persistent]
        public EditableValue StartSpeed = new EditableValue(100, locked: false);
        [Persistent]
        public EditableValue HoldAPTime = new EditableValue(50, locked: false);
        [Persistent]
        public EditableValue APTimeStart = new EditableValue(50, locked: true);
        [Persistent]
        public EditableValue APTimeFinish = new EditableValue(50, locked: true);
        [Persistent]
        public EditableValue TurnAngle = new EditableValue(10, locked: false);
        [Persistent]
        public EditableValue Sensitivity = new EditableValue(0.3, locked: true);
        [Persistent]
        public EditableValue Roll = new EditableValue(0, locked: true);
        [Persistent]
        public EditableValue DestinationHeight = new EditableValue(80, locked: true);
        [Persistent]
        public EditableValue PressureCutoff = new EditableValue(1200, locked: false);
        [Persistent]
        public EditableValue Inclination = new EditableValue(0, locked: true);
        [Persistent]
        public bool EnableStageManager = true;
        [Persistent]
        public bool EnableSpeedup = false;
        [Persistent]
        public EditableValue FairingPressure = new EditableValue(1000, "{0:0}");
        [Persistent]
        public EditableValue autostagePostDelay = new EditableValue(0.3d, "{0:0.0}");
        [Persistent]
        public EditableValue autostagePreDelay = new EditableValue(0.7d, "{0:0.0}");
        [Persistent]
        public EditableValue autostageLimit = new EditableValue(0, "{0:0}");
        [Persistent]
        public bool EnableStats = false;


        #endregion

        #region Misc. Public Variables
        public double HorizontalDistance = 0;
        public double MaxThrust = 0;
        public MovingAverage Throttle = new MovingAverage(10, 1);
        public float lastTimeMeasured = 0.0f;
        public VesselState vesselState = null;
        public double TimeSpeed = 0;
        public double PrevTime = 0;
        public MovingAverage PitchAdjustment = new MovingAverage(4, 0);
        public float YawAdjustment = 0.0f;
        public bool Launching = false;
        public string LaunchName = "";
        public CelestialBody LaunchBody = null;

        #endregion

        #region Window Stuff

        //public ApplicationLauncherButton button;
        internal ToolbarControl toolbarControl;

        internal static Window.MainWindow mainWindow = null;
        public Window.WindowManager windowManager = new Window.WindowManager();
        public Window.FlightMapWindow flightMapWindow;
        public Window.StatsWindow statsWindow;
        public string Message = "";

        public List<string[]> locStatStrList;
        //public string biggestLocStr;
        public string BiggestStr(List<string[]> list)
        {
            List<StringInList> charList = new List<StringInList>();
            int index = 0;
            foreach (string[] strArray in list)
            {
                charList.Add(new StringInList() { Size = strArray[0].Length, Index = index });
                index++;
            }
            charList.Sort(delegate (StringInList x, StringInList y) { return x.Size.CompareTo(y.Size); });
            charList.Reverse();
            string biggestStr = list[charList[0].Index][0];

            return biggestStr;
        }

        static public string DebugMessage = "";
        static public bool DebugShow = false;

        #endregion

        #region Loss and related variables

        public float NeutralThrottle = 0.5f;
        public double TotalLoss = 0;
        public double MaxHeat = 0;
        double VelocityLost = 0;
        double DragLoss = 0;
        double GravityDragLoss = 0;
        double FlyTimeInterval = 0;
        double VectorLoss = 0;
        double TotalBurn = 0;
        bool PitchSet = false;
        MovingAverage DragRatio = new MovingAverage();

        #endregion

        #region Controllers and such

        AttitudeController attitude = null;
        public StageController stage;
        StageStats stagestats = null;
        MechjebWrapper mucore = new MechjebWrapper();
        internal LaunchDB launchdb = null;
        static int previousTimeWarp = 0;
        static public double inclinationHeadingCorrectionSpeed = 0;

        public bool IsLaunchDBEmpty()
        {
            return launchdb.IsEmpty();
        }
        public void ClearLaunchDB()
        {
            launchdb.ClearDB();
        }

        internal class EnginePlate
        {
            internal bool isEnginePlate;
            internal AttachNode bottomNode;

            internal EnginePlate(bool isEnginePlate, Part p)
            {
                this.isEnginePlate = isEnginePlate;
                if (isEnginePlate)
                {
                    bottomNode = GetNode("bottom", p);
                }
            }
            internal void SetAsEnginePlate(bool isEnginePlate, Part p)
            {
                this.isEnginePlate = isEnginePlate;
                if (isEnginePlate)
                {
                    bottomNode = GetNode("bottom", p);
                }
            }
            AttachNode GetNode(string nodeId, Part p)
            {
                foreach (var attachNode in p.attachNodes.Where(an => an != null))
                {
                    if (p.srfAttachNode != null && attachNode == p.srfAttachNode)
                        continue;
                    if (attachNode.id == nodeId)
                        return attachNode;

                }
                return null;
            }

        }
        static internal Dictionary<uint, EnginePlate> enginePlates;
        #endregion

        private int lineno { get { StackFrame callStack = new StackFrame(1, true); return callStack.GetFileLineNumber(); } }
        public static void Log(
            string format,
            params object[] args
            )
        {

            //string method = "";
#if DEBUGfalse
            StackFrame stackFrame = new StackFrame(1, true);
            method = string.Format(" [{0}]|{1}", stackFrame.GetMethod().ToString(), stackFrame.GetFileLineNumber());
#endif
            string incomingMessage;
            if (args == null)
                incomingMessage = format;
            else
                incomingMessage = string.Format(format, args);
#if false
            UnityEngine.Debug.Log("GravityTurn: " + incomingMessage);
#endif
        }


        string DefaultConfigFilename(Vessel vessel)
        {
            string name = vessel.mainBody.name.Replace('"', '_');
            return LaunchDB.GetBaseFilePath(this.GetType(), string.Format("gt_vessel_default_{0}.cfg", name));
        }
        internal static string ConfigFilename(Vessel vessel)
        {
            string name = vessel.mainBody.name.Replace('"', '_');
            return LaunchDB.GetBaseFilePath(instance.GetType(), string.Format("gt_vessel_{0}_{1}.cfg", vessel.id.ToString(), name));
        }

        private void OnGUI()
        {
            // hide UI if F2 was pressed
            if (!Window.BaseWindow.ShowGUI)
                return;
            if (Event.current.type == EventType.Repaint || Event.current.isMouse)
            {
                //myPreDrawQueue(); // Your current on preDrawQueue code
            }
            windowManager.DrawGuis(); // Your current on postDrawQueue code
        }

        private void ShowGUI()
        {
            Window.BaseWindow.ShowGUI = true;
        }
        private void HideGUI()
        {
            Window.BaseWindow.ShowGUI = false;
        }

        /*
         * Called after the scene is loaded.
         */
        public void Awake()
        {
            Log("GravityTurn: Awake {0}", this.GetInstanceID());
        }

        void Start()
        {
            instance = this;
            Log("Starting");
            try
            {
                mucore.init();
                vesselState = new VesselState();
                attitude = new AttitudeController(this);
                stage = new StageController(this);
                attitude.OnStart();
                stagestats = new StageStats(stage);
                stagestats.editorBody = getVessel.mainBody;
                stagestats.OnModuleEnabled();
                stagestats.OnFixedUpdate();
                stagestats.RequestUpdate(this);
                stagestats.OnFixedUpdate();
                CreateButtonIcon();
                LaunchName = new string(getVessel.vesselName.ToCharArray());
                LaunchBody = getVessel.mainBody;
                launchdb = new LaunchDB(this);
                launchdb.Load();

                mainWindow = new Window.MainWindow(this, 6378070);
                flightMapWindow = new Window.FlightMapWindow(this, 548302);
                statsWindow = new Window.StatsWindow(this, 6378070 + 4);
                double h = 80f;
                if (FlightGlobals.ActiveVessel.mainBody.atmosphere)
                {
                    h = Math.Max(h, FlightGlobals.ActiveVessel.mainBody.atmosphereDepth + 10000f);
                    DestinationHeight = new EditableValue(h, locked: true) / 1000;
                }

                delayUT = double.NaN;

                GameEvents.onShowUI.Add(ShowGUI);
                GameEvents.onHideUI.Add(HideGUI);

                LoadKeybind();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void SetWindowOpen()
        {
            mainWindow.WindowVisible = true;
            if (!Launching)
            {
                LoadParameters();
                InitializeNumbers(getVessel);
            }
        }

        void InitializeNumbers(Vessel vessel)
        {
            NeutralThrottle = 0.5f;
            PrevTime = 0;
            VelocityLost = 0;
            DragLoss = 0;
            GravityDragLoss = 0;
            FlyTimeInterval = Time.time;
            Message = "";
            VectorLoss = 0;
            HorizontalDistance = 0;
            inclinationHeadingCorrectionSpeed = Calculations.CircularOrbitSpeed(getVessel.mainBody, getVessel.mainBody.Radius + DestinationHeight * 1000);
            Log("Orbit velocity {0:0.0}", inclinationHeadingCorrectionSpeed);
            inclinationHeadingCorrectionSpeed /= 1.7;
            Log("inclination heading correction {0:0.0}", inclinationHeadingCorrectionSpeed);
            MaxThrust = GetMaxThrust(vessel);
            bool openFlightmap = false;
            openFlightmap = flightMapWindow.WindowVisible;
            flightMapWindow.flightMap = new FlightMap(this);
            flightMapWindow.WindowVisible = openFlightmap;
        }

        internal const string MODID = "GravityTurn_NS";
        internal const string MODNAME = "GravityTurn";
        private void CreateButtonIcon()
        {
#if false
            button = ApplicationLauncher.Instance.AddModApplication(
                SetWindowOpen,
                () => mainWindow.WindowVisible = false,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.ALWAYS,
                GameDatabase.Instance.GetTexture("GravityTurn/Textures/icon", false)
                );
#endif
            Log("CreateButtonIcon");
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(SetWindowOpen,
                () => mainWindow.WindowVisible = false,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.TRACKSTATION,
                MODID,
                "gravityTurnButton",
                "GravityTurn/PluginData/Textures/icon_38",
                "GravityTurn/PluginData/Textures/icon_24",
                MODNAME
            );
        }


        double TWRWeightedAverage(double MinimumDeltaV, Vessel vessel)
        {
            stagestats.RequestUpdate(this);
            double TWR = 0;
            double deltav = 0;
            for (int i = stagestats.atmoStats.Length - 1; i >= 0; i--)
            {
                double stagetwr = (stagestats.atmoStats[i].StartTWR(vessel.mainBody.GeeASL) + stagestats.atmoStats[i].MaxTWR(vessel.mainBody.GeeASL)) / 2;
                if (stagetwr > 0)
                {
                    TWR += stagetwr * stagestats.atmoStats[i].deltaV;
                    deltav += stagestats.atmoStats[i].deltaV;
                    if (deltav >= MinimumDeltaV)
                        break;
                }
            }
            return TWR / deltav;
        }

        public void CalculateSettings(Vessel vessel, bool UseBest = false)
        {
            float baseFactor = Mathf.Round((float)vessel.mainBody.GeeASL * 100.0f) / 10.0f;
            Log("Base turn speed factor {0:0.00}", baseFactor);

            // reset the settings to defaults
            if (GameSettings.MODIFIER_KEY.GetKey())
            {
                launchdb.Clear();
                TurnAngle = 10;
                StartSpeed = baseFactor * 10.0;
                DestinationHeight = (vessel.StableOrbitHeight() + 10000) / 1000;
                GravityTurner.Log("Reset results");
                return;
            }
            Log("Min orbit height: {0}", vessel.StableOrbitHeight());

            stagestats.ForceSimunlation();
            double TWR = 0;
            for (int i = stagestats.atmoStats.Length - 1; i >= 0; i--)
            {
                double stagetwr = stagestats.atmoStats[i].StartTWR(vessel.mainBody.GeeASL);
                if (stagetwr > 0)
                {
                    if (vessel.StageHasSolidEngine(i))
                        TWR = (stagetwr + stagestats.atmoStats[i].MaxTWR(vessel.mainBody.GeeASL)) / 2.3;
                    else
                        TWR = stagetwr;
                    break;
                }
            }
            if (TWR > 1.2)
            {
                Log("First guess for TWR > 1.2 {0:0.00}", TWR);
                TWR -= 1.2;
                if (!TurnAngle.locked)
                    TurnAngle = Mathf.Clamp((float)(10 + TWR * 5), 10, 80);
                if (!StartSpeed.locked)
                {
                    StartSpeed = Mathf.Clamp((float)(baseFactor * 10 - TWR * baseFactor * 3), baseFactor, baseFactor * 10);
                    if (StartSpeed < 10)
                        StartSpeed = 10;
                }
            }

            double guessTurn, guessSpeed;
            if (UseBest && launchdb.BestSettings(out guessTurn, out guessSpeed))
            {
                Log("UseBest && launchdb.BestSettings");
                if (!StartSpeed.locked)
                    StartSpeed = guessSpeed;
                if (!TurnAngle.locked)
                    TurnAngle = guessTurn;
            }
            else if (launchdb.GuessSettings(out guessTurn, out guessSpeed))
            {
                Log("GuessSettings");
                if (!StartSpeed.locked)
                    StartSpeed = guessSpeed;
                if (!TurnAngle.locked)
                    TurnAngle = guessTurn;
            }

            if (!APTimeStart.locked)
                APTimeStart = 50;
            if (!APTimeFinish.locked)
                APTimeFinish = 50;
            if (!Sensitivity.locked)
                Sensitivity = 0.3;
            if (!DestinationHeight.locked)
            {
                DestinationHeight = vessel.StableOrbitHeight() + 10000;
                DestinationHeight /= 1000;
            }
            if (!Roll.locked)
                Roll = 0;
            if (!Inclination.locked)
                Inclination = 0;
            if (!PressureCutoff.locked)
                PressureCutoff = 1200;
            SaveParameters();
        }

        private void DebugGUI(int windowID)
        {
            GUILayout.Box(PreflightInfo(getVessel));
            //GUI.DragWindow();
        }

        public void Launch()
        {
            StageController.topFairingDeployed = false;
            if (StageManager.CurrentStage == StageManager.StageCount)
                StageManager.ActivateNextStage();
            InitializeNumbers(getVessel);
            getVessel.OnFlyByWire += new FlightInputCallback(fly);
            Launching = true;
            PitchSet = false;
            DebugShow = false;
            program = AscentProgram.Landed;
            SaveParameters();
            LaunchName = new string(getVessel.vesselName.ToCharArray());
            LaunchBody = getVessel.mainBody;
            GetEnginePlates();
        }

        void GetEnginePlates()
        {
            enginePlates = new Dictionary<uint, EnginePlate>();
            for (int i = 0; i < getVessel.parts.Count; i++)
            {
                Part p = getVessel.parts[i];
                for (int i1 = 0; i1 < p.Modules.Count; i1++)
                {
                    ModuleDecouple mDecouple = p.Modules[i1] as ModuleDecouple;
                    enginePlates[p.flightID] = new EnginePlate(false, p);
                    if (mDecouple != null)
                    {
                        if (mDecouple.IsEnginePlate())
                        {
                            enginePlates[p.flightID].SetAsEnginePlate(true, p);
                            break;
                        }
                    }
                }

            }
        }

        double GetMaxThrust(Vessel vessel)
        {
            double thrust = 0;
            FuelFlowSimulation.Stats[] stats;
            if (vessel.mainBody.atmosphere && vessel.altitude < vessel.mainBody.atmosphereDepth)
                stats = stagestats.atmoStats;
            else
                stats = stagestats.vacStats;
            for (int i = stats.Length - 1; i >= 0; i--)
            {
                if (stats[i].startThrust > thrust)
                    thrust = stats[i].startThrust;
            }
            return thrust;
        }

        void FixedUpdate()
        {
            if (Launching)
            {
                stagestats.editorBody = getVessel.mainBody;
                vesselState.Update(getVessel);
                attitude.OnFixedUpdate();
                stagestats.OnFixedUpdate();
                stagestats.RequestUpdate(this);
                if (flightMapWindow.flightMap != null && Launching)
                    flightMapWindow.flightMap.UpdateMap(getVessel);
            }
            else
            {
                if (EnableStats && !getVessel.Landed && !getVessel.IsInStableOrbit())
                {
                    CalculateLosses(getVessel);
                    stagestats.editorBody = getVessel.mainBody;
                    vesselState.Update(getVessel);
                    attitude.OnFixedUpdate();
                    stagestats.OnFixedUpdate();
                    stagestats.RequestUpdate(this);
                }
                else if (EnableStats && !getVessel.Landed && getVessel.IsInStableOrbit())
                {
                    if (VectorLoss > 0.01)
                    {
                        Message = string.Format(
                            "Total Vector Loss:\t{0:0.00} m/s\n" +
                            "Total Loss:\t{1:0.00} m/s\n" +
                            "Total Burn:\t\t{2:0.0}\n\n",
                            VectorLoss,
                            TotalLoss,
                            TotalBurn
                            );
                    }
                    else
                        Message = "";

                    Message += string.Format(
                        "Apoapsis:\t\t{0}\n" +
                        "Periapsis:\t\t{1}\n" +
                        "Inclination:\t\t{2:0.0} °\n",
                        OrbitExtensions.FormatOrbitInfo(getVessel.orbit.ApA, getVessel.orbit.timeToAp),
                        OrbitExtensions.FormatOrbitInfo(getVessel.orbit.PeA, getVessel.orbit.timeToPe),
                        getVessel.orbit.inclination
                        );

                }
                else
                {
                    if (EnableStats && getVessel.Landed)
                    {
                        double diffUT = Planetarium.GetUniversalTime() - delayUT;
                        if (diffUT > 1 || Double.IsNaN(delayUT))
                        {
                            vesselState.Update(getVessel);
                            stagestats.OnFixedUpdate();
                            stagestats.RequestUpdate(this);
                            Message = PreflightInfo(getVessel);
                            delayUT = Planetarium.GetUniversalTime();
                        }
                    }
                }
            }
        }

        void Update()
        {
            if (Launching)
            {
                attitude.OnUpdate();
            }
            else
                CheckForLaunch();
        }


        const string KEYBINDCFG = "GameData/GravityTurn/PluginData/keybind.cfg";
        const string NODENAME = "GRAVITYTURN";
        const string KEYCODE = "keycode";
        const string USEKEYBIND = "useKeyBind";

        const KeyCode DefaultKeyBind = KeyCode.L;
        bool useKeyBind = true;
        KeyCode keyBind = DefaultKeyBind;
        public void LoadKeybind()
        {
            string path = KSPUtil.ApplicationRootPath + KEYBINDCFG;
            if (System.IO.File.Exists(path))
            {
                var keybindcfg = ConfigNode.Load(path);
                ConfigNode node = keybindcfg.GetNode(NODENAME);

                if (node.HasValue(KEYCODE))
                {
                    var keycode = SafeLoad(node.GetValue(KEYCODE), DefaultKeyBind.ToString());
                    keyBind = setActiveKeycode(keycode);
                }
                if (node.HasValue(USEKEYBIND))
                {
                    useKeyBind = SafeLoad(node.GetValue(USEKEYBIND), useKeyBind);
                }
            }

            // Write it out in case one or both were missing
            {
                ConfigNode file = new ConfigNode();
                ConfigNode node = new ConfigNode(NODENAME);
                node.AddValue(KEYCODE, DefaultKeyBind.ToString());
                node.AddValue(USEKEYBIND, useKeyBind);

                file.AddNode(node);
                file.Save(path);
            }

        }


        static string SafeLoad(string value, string oldvalue)
        {
            if (value == null)
                return oldvalue;
            return value;
        }
        static bool SafeLoad(string value, bool oldvalue)
        {
            if (value == null)
                return oldvalue;
            return bool.Parse(value);
        }

        public KeyCode setActiveKeycode(string keycode)
        {
            var activeKeycode = (KeyCode)Enum.Parse(typeof(KeyCode), keycode);
            if (activeKeycode == KeyCode.None)
            {
                activeKeycode = DefaultKeyBind;
            }

            return activeKeycode;
        }


        void CheckForLaunch()
        {
            if (FlightGlobals.ActiveVessel.Landed && mainWindow.WindowVisible && (useKeyBind &&  Input.GetKeyDown(keyBind)))
                Launch();
        }

        private float MaxAngle(Vessel vessel)
        {
            float angle = 100000 / (float)vesselState.dynamicPressure;
            float vertical = 90 + vessel.Pitch();
            angle = Mathf.Clamp(angle, 0, 35);
            if (angle > vertical)
                return vertical;
            return angle;
        }


        public string GetFlightMapFilename()
        {
            return LaunchDB.GetBaseFilePath(this.GetType(), string.Format("gt_vessel_{0}_{1}.png", LaunchName, LaunchBody.name));
        }

        public void Kill()
        {
            if (flightMapWindow.flightMap != null)
            {
                flightMapWindow.flightMap.WriteParameters(TurnAngle, StartSpeed);
                flightMapWindow.flightMap.WriteResults(DragLoss, GravityDragLoss, VectorLoss);
                Log("Flightmap with {0:0.00} loss", flightMapWindow.flightMap.TotalLoss());
                FlightMap previousLaunch = FlightMap.Load(GetFlightMapFilename(), this);
                if (getVessel.vesselName != "Untitled Space Craft" // Don't save the default vessel name
                    && getVessel.altitude > getVessel.mainBody.atmosphereDepth
                    && (previousLaunch == null
                    || previousLaunch.BetterResults(DragLoss, GravityDragLoss, VectorLoss))) // Only save the best result
                    flightMapWindow.flightMap.Save(GetFlightMapFilename());
            }
            Launching = false;
            getVessel.OnFlyByWire -= new FlightInputCallback(fly);
            FlightInputHandler.state.mainThrottle = 0;
            attitude.enabled = false;
            getVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
        }

        // this records an aborted launch as not sucessful
        public void RecordAbortedLaunch()
        {
            launchdb.RecordLaunch();
            launchdb.Save();
        }

        private float LaunchHeading(Vessel vessel)
        {
            return (float)MuUtils.HeadingForLaunchInclination(vessel.mainBody, Inclination, vessel.latitude, inclinationHeadingCorrectionSpeed);
        }

        private float ProgradeHeading(bool surface = true)
        {
            Quaternion current;
            if (surface)
                current = Quaternion.LookRotation(vesselState.surfaceVelocity.normalized, vesselState.up) * Quaternion.Euler(0, 0, Roll);
            else
                current = Quaternion.LookRotation(vesselState.orbitalVelocity.normalized, vesselState.up) * Quaternion.Euler(0, 0, Roll);
            //current *= vesselState.rotationSurface.Inverse();
            return (float)Vector3d.Angle(Vector3d.Exclude(vesselState.up, vesselState.surfaceVelocity), vesselState.north);
        }


        Quaternion RollRotation()
        {
            return Quaternion.AngleAxis(Roll, Vector3.forward);
        }

        static public void StoreTimeWarp()
        {
            previousTimeWarp = TimeWarp.CurrentRateIndex;
        }

        static public void RestoreTimeWarp()
        {
            if (previousTimeWarp != 0)
            {
                TimeWarp.fetch.Mode = TimeWarp.Modes.LOW;
                TimeWarp.SetRate(previousTimeWarp, false);
            }
            previousTimeWarp = 0;
        }

        public void ApplySpeedup(int rate)
        {
            if (EnableSpeedup)
            {
                TimeWarp.fetch.Mode = TimeWarp.Modes.LOW;
                TimeWarp.SetRate(previousTimeWarp < rate ? rate : previousTimeWarp, false);
            }
        }

        static public void StopSpeedup()
        {
            TimeWarp.SetRate(0, false);
        }

        static double delayUT = double.NaN;

        private void fly(FlightCtrlState s)
        {
            if (!Launching)
            {
                Kill();
                return;
            }
            DebugMessage = "";
            Vessel vessel = getVessel;
            if (program != AscentProgram.InCoasting && vessel.orbit.ApA > DestinationHeight * 1000 && vessel.altitude < vessel.StableOrbitHeight())
            {
                CalculateLosses(getVessel);
                // save launch, ignoring losses due to coasting losses, but so we get results earlier
                launchdb.RecordLaunch();
                launchdb.Save();
                program = AscentProgram.InCoasting;
                DebugMessage += "In Coasting program\n";
                Throttle.force(0);
                Log("minorbit {0}, {1}", vessel.mainBody.minOrbitalDistance, vessel.StableOrbitHeight());
                // time warp to speed up things (if enabled)
                ApplySpeedup(2);
            }
            else if (vessel.orbit.ApA > DestinationHeight * 1000 && vessel.altitude > vessel.StableOrbitHeight())
            {
                Log("minorbit {0}, {1}", vessel.mainBody.minOrbitalDistance, vessel.StableOrbitHeight());
                program = AscentProgram.InCircularisation;
                StopSpeedup();
                GravityTurner.Log("Saving launchDB");
                launchdb.RecordLaunch();
                launchdb.Save();
                Kill();
                DebugMessage += "In Circularisation program\n";
                if (mucore.Initialized)
                {
                    program = AscentProgram.InCircularisation;
                    mucore.CircularizeAtAP();
                }

                //button.SetFalse();
                toolbarControl.SetFalse();
            }
            else
            {
                double minInsertionHeight = vessel.mainBody.atmosphere ? vessel.StableOrbitHeight() / 4 : Math.Max(DestinationHeight * 667, vessel.StableOrbitHeight() * 0.667);
                if (EnableStageManager && stage != null)
                    stage.Update();

                if (vessel.orbit.ApA < DestinationHeight * 1000)
                    s.mainThrottle = Calculations.APThrottle(vessel.orbit.timeToAp, this);
                else
                    s.mainThrottle = 0;
                Log("mainThrottle: " + s.mainThrottle + ", FlightInputHandler.state.mainThrottle: " + FlightInputHandler.state.mainThrottle);
                FlightInputHandler.state.mainThrottle = s.mainThrottle;
                if (program == AscentProgram.InInitialPitch && PitchSet)
                {
                    if (vessel.ProgradePitch() + 90 >= TurnAngle - 0.1)
                    {
                        delayUT = double.NaN;
                        // continue any previous timewarp
                        RestoreTimeWarp();
                        ApplySpeedup(1);
                        program = AscentProgram.InTurn;
                        DebugMessage += "Turning now\n";
                    }
                }
                if (vessel.speed < StartSpeed)
                {
                    DebugMessage += "In Launch program\n";
                    program = AscentProgram.InLaunch;
                    if (vesselState.altitudeBottom > vesselState.vesselHeight)
                        attitude.attitudeTo(Quaternion.Euler(-90, LaunchHeading(vessel), 0) * RollRotation(), AttitudeReference.SURFACE_NORTH, this);
                    else
                        attitude.attitudeTo(Quaternion.Euler(-90, 0, vesselState.vesselHeading), AttitudeReference.SURFACE_NORTH, this);
                }
                else if (program == AscentProgram.InLaunch || program == AscentProgram.InInitialPitch)
                {
                    if (!PitchSet)
                    {
                        // remember and stop timewarp for pitching
                        StoreTimeWarp();
                        StopSpeedup();
                        PitchSet = true;
                        program = AscentProgram.InInitialPitch;
                        delayUT = Planetarium.GetUniversalTime();
                    }
                    DebugMessage += "In Pitch program\n";
                    double diffUT = Planetarium.GetUniversalTime() - delayUT;
                    float newPitch = Mathf.Min((float)(((double)TurnAngle * diffUT) / 5.0d + 2.0d), TurnAngle);
                    double pitch = (90d - vesselState.vesselPitch + vessel.ProgradePitch() + 90) / 2;
                    attitude.attitudeTo(Quaternion.Euler(-90 + newPitch, LaunchHeading(vessel), 0) * RollRotation(), AttitudeReference.SURFACE_NORTH, this);
                    DebugMessage += String.Format("TurnAngle: {0:0.00}\n", TurnAngle.value);
                    DebugMessage += String.Format("Target pitch: {0:0.00}\n", newPitch);
                    DebugMessage += String.Format("Current pitch: {0:0.00}\n", pitch);
                    DebugMessage += String.Format("Prograde pitch: {0:0.00}\n", vessel.ProgradePitch() + 90);
                }
                else if (vesselState.dynamicPressure > vesselState.maxQ * 0.5 || vesselState.dynamicPressure > PressureCutoff || vessel.altitude < minInsertionHeight)
                { // Still ascending, or not yet below the cutoff pressure or below min insertion heigt
                    DebugMessage += "In Turn program\n";
                    attitude.attitudeTo(Quaternion.Euler(vessel.ProgradePitch() - PitchAdjustment, LaunchHeading(vessel), 0) * RollRotation(), AttitudeReference.SURFACE_NORTH, this);
                }
                else
                {
                    // did we reach the desired inclination?
                    DebugMessage += String.Format("Insertion program\n");
                    Quaternion q = Quaternion.Euler(0 - PitchAdjustment, YawAdjustment, Roll);
                    // smooth out change from surface to orbital prograde
                    if (program != AscentProgram.InInsertion && program != AscentProgram.InCoasting)
                    {
                        // start timer
                        if (Double.IsNaN(delayUT))
                        {
                            // slow down timewarp
                            delayUT = Planetarium.GetUniversalTime();
                            StoreTimeWarp();
                            StopSpeedup();
                            // switch NavBall UI
                            FlightGlobals.SetSpeedMode(FlightGlobals.SpeedDisplayModes.Orbit);
                        }
                        double diffUT = Planetarium.GetUniversalTime() - delayUT;
                        //attitude.attitudeTo(q, AttitudeReference.ORBIT, this);
                        q.x = (attitude.lastAct.x * 8.0f + q.x) / 9.0f;
                        if (diffUT > 10 || (attitude.lastAct.x > 0.02 && diffUT > 2.0))
                        {
                            program = AscentProgram.InInsertion;
                            delayUT = double.NaN;
                            RestoreTimeWarp();
                            ApplySpeedup(2);
                        }
                    }
                    attitude.attitudeTo(q, AttitudeReference.ORBIT, this);
                }
                attitude.enabled = true;
                attitude.Drive(s);
                CalculateLosses(getVessel);
                DebugMessage += "-";
            }
        }

        string PreflightInfo(Vessel vessel)
        {
            //List to compare text width and display each stat with same way
            locStatStrList = new List<string[]>();

            string localizedStatsTWR = Localizer.Format("#autoLOC_GT_StatsTWR"); // Surface TWR:
            string statsTWRValue = string.Format("{0:0.00}", TWRWeightedAverage(2 * vessel.mainBody.GeeASL * DestinationHeight, vessel));
            string[] statsTWRArray = new string[2] { localizedStatsTWR, statsTWRValue };
            locStatStrList.Add(statsTWRArray);

            string localizedStatsMass = Localizer.Format("#autoLOC_GT_StatsMass"); // Mass:
            string statsMassValue = string.Format("{0:0.00} t", vesselState.mass);
            string[] statsMassArray = new string[2] { localizedStatsMass, statsMassValue };
            locStatStrList.Add(statsMassArray);

            string localizedStatsHeight = Localizer.Format("#autoLOC_GT_StatsHeight"); // Height:
            string statsHeightValue = string.Format("{0:0.0} m\n", vesselState.vesselHeight);
            string[] statsHeightArray = new string[2] { localizedStatsHeight, statsHeightValue };
            locStatStrList.Add(statsHeightArray);

            string localizedStatsDragArea = Localizer.Format("#autoLOC_GT_StatsDragArea"); // Drag area:
            string statsDragAreaValue = string.Format("{0:0.00}", vesselState.areaDrag);
            string[] statsDragAreaArray = new string[2] { localizedStatsDragArea, statsDragAreaValue };
            locStatStrList.Add(statsDragAreaArray);

            string localizedStatsDragCoef = Localizer.Format("#autoLOC_GT_StatsDragCoef"); // Drag coefficient:
            string statsDragCoefValue = string.Format("{0:0.00}", vesselState.dragCoef);
            string[] statsDragCoefArray = new string[2] { localizedStatsDragCoef, statsDragCoefValue };
            locStatStrList.Add(statsDragCoefArray);

            string localizedStatsDragCoefFwd = Localizer.Format("#autoLOC_GT_StatsDragCoefFwd"); // Drag coefficient fwd:
            string statsDragCoefFwdValue = string.Format("{0:0.00}", vessel.DragCubeCoefForward());
            string[] statsDragCoefFwdArray = new string[2] { localizedStatsDragCoefFwd, statsDragCoefFwdValue };
            locStatStrList.Add(statsDragCoefFwdArray);

            DragRatio.value = vesselState.areaDrag / vesselState.mass;
            string localizedStatsAreaMass = Localizer.Format("#autoLOC_GT_StatsAreaMass"); // area/mass:
            string statsAreaMassValue = string.Format("{0:0.00}", DragRatio.value);
            string[] statsAreaMassArray = new string[2] { localizedStatsAreaMass, statsAreaMassValue };
            locStatStrList.Add(statsAreaMassArray);

            // Keep info message for debug
            string info = "";

            info += localizedStatsTWR + statsTWRValue + "\n";
            info += localizedStatsMass + statsMassValue + "\n";
            info += localizedStatsHeight + statsHeightValue + "\n";
            info += localizedStatsDragArea + statsDragAreaValue + "\n";
            info += localizedStatsDragCoef + statsDragCoefValue + "\n";
            info += localizedStatsDragCoefFwd + statsDragCoefFwdValue + "\n";
            info += localizedStatsAreaMass + statsAreaMassValue;

            return info;
        }

        void CalculateLosses(Vessel vessel)
        {
            if (vesselState.mass == 0)
                return;

            double fwdAcceleration = Vector3d.Dot(vessel.acceleration, vesselState.forward.normalized);
            double GravityDrag = Vector3d.Dot(vesselState.gravityForce, -vessel.obt_velocity.normalized);
            double TimeInterval = Time.time - FlyTimeInterval;
            FlyTimeInterval = Time.time;
            HorizontalDistance += Vector3d.Exclude(vesselState.up, vesselState.orbitalVelocity).magnitude * TimeInterval;
            VelocityLost += ((vesselState.thrustCurrent / vesselState.mass) - fwdAcceleration) * TimeInterval;
            DragLoss += vesselState.drag * TimeInterval;
            GravityDragLoss += GravityDrag * TimeInterval;

            double VectorDrag = vesselState.thrustCurrent - Vector3d.Dot(vesselState.thrustVectorLastFrame, vessel.obt_velocity.normalized);
            VectorDrag = VectorDrag / vesselState.mass;
            VectorLoss += VectorDrag * TimeInterval;
            TotalBurn += vesselState.thrustCurrent / vesselState.mass * TimeInterval;

            double GravityDragLossAtAp = GravityDragLoss + vessel.obt_velocity.magnitude - vessel.orbit.getOrbitalVelocityAtUT(vessel.orbit.timeToAp + Planetarium.GetUniversalTime()).magnitude;
            TotalLoss = DragLoss + GravityDragLossAtAp + VectorLoss;
            if (vessel.CriticalHeatPart().CriticalHeat() > MaxHeat)
                MaxHeat = vessel.CriticalHeatPart().CriticalHeat();

            //List to compare text width and display each stat with same way
            locStatStrList = new List<string[]>();

            string localizedAirDrag = Localizer.Format("#autoLOC_GT_StatsAirDrag"); // Air Drag:\t\t{0:0.00} m/s²\n
            string airDragValue = string.Format("{0:0.00} m/s²", vesselState.drag);
            string[] airDragArray = new string[2] { localizedAirDrag, airDragValue };
            locStatStrList.Add(airDragArray);

            string localizedGravityDrag = Localizer.Format("#autoLOC_GT_StatsGravityDrag"); // GravityDrag:\t{1:0.00} m/s²\n
            string gravityDragValue = string.Format("{0:0.00} m/s²", GravityDrag);
            string[] gravityDragArray = new string[2] { localizedGravityDrag, gravityDragValue };
            locStatStrList.Add(gravityDragArray);

            string localizedThrustVectorDrag = Localizer.Format("#autoLOC_GT_StatsThrustVectorDrag"); // Thrust Vector Drag:\t{5:0.00} m/s²\n
            string thrustVectorDragValue = string.Format("{0:0.00} m/s²", VectorDrag);
            string[] thrustVectorDragArray = new string[2] { localizedThrustVectorDrag, thrustVectorDragValue };
            locStatStrList.Add(thrustVectorDragArray);

            string localizedAirDragLoss = Localizer.Format("#autoLOC_GT_StatsAirDragLoss"); // Air Drag Loss:\t{2:0.00} m/s\n
            string airDragLossValue = string.Format("{0:0.00} m/s", DragLoss);
            string[] airDragLossArray = new string[2] { localizedAirDragLoss, airDragLossValue };
            locStatStrList.Add(airDragLossArray);

            string localizedGravityDragLoss = Localizer.Format("#autoLOC_GT_StatsGravityDragLoss"); // Gravity Drag Loss:\t{3:0.00} -> {4:0.00} m/s @AP\n\n
            string localizedAtAP = Localizer.Format("#autoLOC_GT_StatsAtAP"); // @AP
            string gravityDragLossValue = string.Format("{0:0.00} -> {1:0.00} m/s {2}\n", GravityDragLoss, GravityDragLossAtAp, localizedAtAP);
            string[] gravityDragLossArray = new string[2] { localizedGravityDragLoss, gravityDragLossValue };
            locStatStrList.Add(gravityDragLossArray);

            string localizedTotalVectorLoss = Localizer.Format("#autoLOC_GT_StatsTotalVectorLoss"); // Total Vector Loss:\t{6:0.00} m/s\n
            string totalVectorLossValue = string.Format("{0:0.00} m/s", VectorLoss);
            string[] totalVectorLossArray = new string[2] { localizedTotalVectorLoss, totalVectorLossValue };
            locStatStrList.Add(totalVectorLossArray);

            string localizedTotalLoss = Localizer.Format("#autoLOC_GT_StatsTotalLoss"); // Total Loss:\t{7:0.00} m/s\n
            string totalLossValue = string.Format("{0:0.00} m/s", TotalLoss);
            string[] totalLossArray = new string[2] { localizedTotalLoss, totalLossValue };
            locStatStrList.Add(totalLossArray);

            string localizedTotalBurn = Localizer.Format("#autoLOC_GT_StatsTotalBurn"); // Total Burn:\t\t{8:0.0}\n\n
            string totalBurnValue = string.Format("{0:0.00} m/s\n", TotalBurn);
            string[] totalBurnArray = new string[2] { localizedTotalBurn, totalBurnValue };
            locStatStrList.Add(totalBurnArray);

            string localizedApoapsis = Localizer.Format("#autoLOC_GT_StatsApoapsis"); // Apoapsis:\t\t{9}\n
            string apoapsisValue = string.Format("{0}", OrbitExtensions.FormatOrbitInfo(vessel.orbit.ApA, vessel.orbit.timeToAp));
            string[] apoapsisArray = new string[2] { localizedApoapsis, apoapsisValue };
            locStatStrList.Add(apoapsisArray);

            string localizedPeriapsis = Localizer.Format("#autoLOC_GT_StatsPeriapsis"); // Periapsis:\t\t{10}\n
            string periapsisValue = string.Format("{0}", OrbitExtensions.FormatOrbitInfo(vessel.orbit.PeA, vessel.orbit.timeToPe));
            string[] periapsisArray = new string[2] { localizedPeriapsis, periapsisValue };
            locStatStrList.Add(periapsisArray);

            string localizedInclination = Localizer.Format("#autoLOC_GT_StatsInclination"); // Inclination:\t\t{11:0.0} °\n\n
            string inclinationValue = string.Format("{0:0.0} °\n", vessel.orbit.inclination);
            string[] inclinationArray = new string[2] { localizedInclination, inclinationValue };
            locStatStrList.Add(inclinationArray);

            string localizedDynamicPressure = Localizer.Format("#autoLOC_GT_StatsDynamicPressure"); // Dynamic Pressure:\t{12:0.00} Pa\n
            string dynamicPressureValue = string.Format("{0:0.0} Pa", vesselState.dynamicPressure);
            string[] dynamicPressureArray = new string[2] { localizedDynamicPressure, dynamicPressureValue };
            locStatStrList.Add(dynamicPressureArray);

            string localizedMaxQ = Localizer.Format("#autoLOC_GT_StatsMaxQ"); // Max Q:\t\t{13:0.00} Pa\n\n
            string maxQValue = string.Format("{0:0.0} Pa", vesselState.maxQ);
            string[] maxQArray = new string[2] { localizedMaxQ, maxQValue };
            locStatStrList.Add(maxQArray);
        }

        void LoadParameters()
        {
            ConfigNode savenode;
            try
            {
                savenode = ConfigNode.Load(ConfigFilename(getVessel));
                if (savenode != null)
                {
                    ConfigNode.LoadObjectFromConfig(this, savenode);
                }
                else
                {
                    // now try to get defaults
                    savenode = ConfigNode.Load(DefaultConfigFilename(getVessel));
                    if (savenode != null)
                    {
                        if (ConfigNode.LoadObjectFromConfig(this, savenode))
                        {
                            CalculateSettings(getVessel);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Vessel Load error " + ex.GetType());
            }
        }

        public void SaveParameters()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilename(getVessel)));
                ConfigNode savenode = ConfigNode.CreateConfigFromObject(this);
                // save this vehicle
                savenode.Save(ConfigFilename(getVessel));
            }
            catch (Exception)
            {
                Log("Exception, vessel NOT saved!");
            }
        }
        public void SaveDefaultParameters()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilename(getVessel)));
            ConfigNode savenode = ConfigNode.CreateConfigFromObject(this);
            // save defaults for new vehicles
            savenode.Save(DefaultConfigFilename(getVessel));

            Log("Defaults saved to " + DefaultConfigFilename(getVessel));
        }

        void OnDestroy()
        {
            try
            {
                Kill();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            DebugShow = false;
            //windowManager.OnDestroy();
            //ApplicationLauncher.Instance.RemoveModApplication(button);
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
            }
        }
    }
}
