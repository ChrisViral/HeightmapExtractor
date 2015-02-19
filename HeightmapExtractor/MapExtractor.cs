using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using HeightmapManager;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapExtractor
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class MapExtractor : MonoBehaviour
    {
        /// <summary>
        /// Heightmap generation states
        /// </summary>
        private enum GenerationStates
        {
            /// <summary>
            /// Idle
            /// </summary>
            NONE,

            /// <summary>
            /// Setting up for new body
            /// </summary>
            INITIATING,

            /// <summary>
            /// Generating heightmap
            /// </summary>
            GENERATING,

            /// <summary>
            /// Saving heightmap
            /// </summary>
            SAVING
        }

        #region Instance
        private static MapExtractor _instance = null;
        public static MapExtractor instance
        {
            get { return _instance; }
        }
        #endregion

        #region Fields
        //Image settings
        private ConfigNode settings = new ConfigNode();
        private ushort height = 360, width = 720, pixelsPerFrame = 720;
        private List<CelestialBody> bodies = new List<CelestialBody>();
        private string destinationURL = string.Empty;
        private bool invertLatitude = false, invertLongitude = false;
        private double latitudeOffset = 0, longitudeOffset = 0;
        private double startingLatitude = -90, endingLatitude = 90;
        private double startingLongitude = -180, endingLongitude = 180;
        private double minAltitude = double.MinValue, maxAltitude = double.MaxValue;
        private bool invertColours = true;
        private Heightmap.SaveFormat saveFormat = Heightmap.SaveFormat.BOTH;
        private Stopwatch timer = new Stopwatch(), mapTimer = new Stopwatch();

        //GUI
        private Texture2D bar = new Texture2D(1, 1), background = new Texture2D(1, 1);
        private Progressbar progressbar = new Progressbar();
        private double time = 0d;
        private string currentBody = string.Empty, message = string.Empty;
        private int id = Guid.NewGuid().GetHashCode();
        private Rect window = new Rect();
        private GUISkin skins = HighLogic.Skin;
        private bool visible = false;

        //Generation fields
        private int bodyIndex = 0, x = 0, y = 0;
        private long resolution = 0;
        private GenerationStates state = GenerationStates.NONE;
        private CelestialBody body = null;
        private short[,] values = new short[0, 0];
        private bool complete = false;
        #endregion

        #region Properties
        private bool _extract = false;
        /// <summary>
        /// If the system is currently extracting maps
        /// </summary>
        public bool extract
        {
            get { return this._extract; }
        }

        /// <summary>
        /// Percentage of the generation that's complete
        /// </summary>
        private double amountComplete
        {
            get
            {
                if (!this._extract) { return 1; }
                double done = (y * this.width) + x;
                double mapPercent = done / (double)resolution;
                return ((double)bodyIndex + mapPercent) / (double)bodies.Count;
            }
        }

        /// <summary>
        /// Current latitude on the planet
        /// </summary>
        private double latitude
        {
            get { return (this.invertLatitude ? -1 : 1) * (((this.y * (this.endingLatitude - this.startingLatitude)) / (double)this.height) + this.startingLatitude + latitudeOffset); }
        }

        /// <summary>
        /// Current longitude on the planet
        /// </summary>
        private double longitude
        {
            get { return (invertLongitude ? -1 : 1) * (((x * (this.endingLongitude - this.startingLongitude)) / (double)this.width) + this.startingLongitude + longitudeOffset); }
        }

        /// <summary>
        /// Current position index
        /// </summary>
        private int index
        {
            get { return (this.y * this.width) + this.x; }
        }

        /// <summary>
        /// Terrain altitude at the current position
        /// </summary>
        private double terrainAltitude
        {
            get
            {
                if (body.pqsController == null) { return 0; }
                Vector3d radial = QuaternionD.AngleAxis(longitude, Vector3d.down) * QuaternionD.AngleAxis(latitude, Vector3d.forward) * Vector3d.right;
                return body.pqsController.GetSurfaceHeight(radial) - body.pqsController.radius;
            }
        }
        #endregion

        #region Initialization
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        private void Start()
        {
            LoadConfig();

            //Progressbar
            try
            {
                this.bar.LoadImage(File.ReadAllBytes(Path.Combine(HeightmapUtils.progressbarURL, "progressbar.png")));
                this.background.LoadImage(File.ReadAllBytes(Path.Combine(HeightmapUtils.progressbarURL, "progressbarBackground.png")));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(String.Format("[HeightmapExtractor]: Encountered an error loading the progressbar images. Aborting process.\n{0}\n{1}", e.GetType().Name, e.StackTrace));
                this._extract = false;
                return;
            }

            //GUI initialization
            Rect backgroundPos = new Rect(0, 0, 374, 19);
            Rect barPos = new Rect(2, 2, 370, 15);
            this.progressbar = new Progressbar(backgroundPos, barPos, background, bar);
            this.window = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 75, 400, 150);

            if (this._extract)
            {
                //Extraction startup
                StartGeneration();
            }
        }
        #endregion

        #region Functions
        private void Update()
        {
            if (this._extract)
            {
                switch (this.state)
                {
                    case GenerationStates.INITIATING:
                        {
                            //Setting up for new body
                            this.body = this.bodies[this.bodyIndex];
                            this.x = 0;
                            this.y = 0;
                            this.currentBody = this.body.bodyName;
                            this.message = "Extracting " + this.currentBody;
                            this.values = new short[this.height, this.width];
                            this.state = GenerationStates.GENERATING;
                            print("[HeightmapExtractor]: Extracting heightmap of " + this.currentBody);
                            this.mapTimer = Stopwatch.StartNew();
                            return;
                        }

                    case GenerationStates.GENERATING:
                        {
                            //Reading values line by line
                            do
                            {
                                double alt = MathUtils.ClampToRange(this.terrainAltitude, this.minAltitude, this.maxAltitude);
                                this.values[y, x] = MathUtils.ClampToInt16(alt);
                                this.x++;
                            }
                            while (this.x < this.width && this.x % this.pixelsPerFrame != 0);

                            if (this.x >= this.width)
                            {
                                this.y++;
                                this.x = 0;
                                if (this.y >= this.height)
                                {
                                    this.state = GenerationStates.SAVING;
                                    this.message = "Saving " + this.currentBody;
                                }
                            }
                            return;
                        }

                    case GenerationStates.SAVING:
                        {
                            //Saving file
                            print(String.Format("[HeightmapExtractor]: Map generation for {0} complete, saving file...", this.currentBody));
                            try
                            {
                                Heightmap map = new Heightmap(this.values, this.invertColours);
                                map.Save(Path.Combine(destinationURL, currentBody), saveFormat);
                                print(String.Format("[HeightmapExtractor]: Correctly saved map for {0} in {1:0.000}s", this.currentBody, this.mapTimer.Elapsed.TotalSeconds));
                            }
                            catch (Exception e)
                            {
                                this.mapTimer.Stop();
                                UnityEngine.Debug.LogError(String.Format("[HeightmapExtractor]: Could not save heightmap for {0}\n{1}\n{2}", this.currentBody, e.GetType().Name, e.StackTrace));
                                this._extract = false;
                                return;
                            }

                            this.bodyIndex++;

                            if (this.bodyIndex >= this.bodies.Count)
                            {
                                //Finishing up
                                this._extract = false;
                                this.settings.SetValue("extractMaps", bool.FalseString);
                                ConfigNode node = new ConfigNode();
                                node.AddNode(this.settings);
                                node.Save(HeightmapUtils.settingsURL);
                                this.timer.Stop();
                                this.time = this.timer.Elapsed.TotalSeconds;
                                this.state = GenerationStates.NONE;
                                this.progressbar.SetValue(1);
                                print(String.Format("[HeightmapExtractor]: Total map generation time: {0:0.000}s", this.time));
                                this.complete = true;
                            }
                            else { this.state = GenerationStates.INITIATING; }
                            return;
                        }

                    case GenerationStates.NONE:
                    default:
                        return;
                }
            }
        }
        #endregion

        #region Static methods
        public static void LoadConfig()
        {
            //Generation information
            if (!ConfigNode.Load(HeightmapUtils.settingsURL).TryGetNode("SETTINGS", ref _instance.settings)) { _instance._extract = false; return; }
            _instance.settings.TryGetValue("extractMaps", ref _instance._extract);
            _instance.settings.TryGetValue("mapHeight", ref _instance.height);
            _instance.settings.TryGetValue("mapWidth", ref _instance.width);
            _instance.settings.TryGetValue("maxPixelsPerFrame", ref _instance.pixelsPerFrame);
            _instance.resolution = (long)_instance.height * (long)_instance.width;
            string[] bodyList = new string[0];
            _instance.settings.TryGetValue("bodies", ref bodyList);
            if (bodyList.Length == 1 && bodyList[0].ToLower() == "all") { _instance.bodies = new List<CelestialBody>(FlightGlobals.Bodies.Where(b => b.pqsController != null)); }
            else { _instance.bodies = new List<CelestialBody>(FlightGlobals.Bodies.Where(b => bodyList.Contains(b.bodyName) && b.pqsController != null)); }
            if (_instance.bodies.Count == 0) { _instance._extract = false; return; }
            _instance.settings.TryGetValue("destinationURL", ref _instance.destinationURL);
            if (string.IsNullOrEmpty(_instance.destinationURL)) { _instance.destinationURL = HeightmapUtils.mapsURL; }
            if (!Directory.Exists(_instance.destinationURL)) { Directory.CreateDirectory(_instance.destinationURL); }

            //Generation restrictions
            _instance.settings.TryGetValue("invertLatitude", ref _instance.invertLatitude);
            _instance.settings.TryGetValue("invertLongitude", ref _instance.invertLongitude);
            _instance.settings.TryGetValue("latitudeOffset", ref _instance.latitudeOffset);
            _instance.settings.TryGetValue("longitudeOffset", ref _instance.longitudeOffset);
            _instance.settings.TryGetValue("startingLatitude", ref _instance.startingLatitude);
            _instance.settings.TryGetValue("endingLatitude", ref _instance.endingLatitude);
            _instance.settings.TryGetValue("startingLongitude", ref _instance.startingLongitude);
            _instance.settings.TryGetValue("endingLongitude", ref _instance.endingLongitude);
            _instance.settings.TryGetValue("minAltitude", ref _instance.minAltitude);
            _instance.settings.TryGetValue("maxAltitude", ref _instance.maxAltitude);
            _instance.settings.TryGetValue("invertColours", ref _instance.invertColours);
            string saveType = string.Empty;
            _instance.settings.TryGetValue("saveType", ref saveType);

            switch (saveType.ToUpper())
            {
                case "IMAGE":
                    _instance.saveFormat = Heightmap.SaveFormat.IMAGE; break;

                case "BINARY":
                    _instance.saveFormat = Heightmap.SaveFormat.BINARY; break;

                case "BOTH":
                default:
                    _instance.saveFormat = Heightmap.SaveFormat.BOTH; break;
            }
        }

        public static void StartGeneration()
        {
            if (_instance.settings == null)
            {
                print("[HeightmapExtractor]: Cannot start generation, settings node not found");
                _instance._extract = false;
                return;
            }
            _instance._extract = true;
            _instance.complete = false;
            InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "HeightmapExtractor");
            _instance.progressbar.SetValue(0);
            _instance.visible = true;
            _instance.bodyIndex = 0;
            _instance.state = GenerationStates.INITIATING;
            print("[HeightmapExtractor]: Starting map extraction.");
            _instance.timer = Stopwatch.StartNew();
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            if (this.visible)
            {
                this.window = GUI.Window(this.id, this.window, Window, "Heightmap Extractor " + HeightmapUtils.assemblyVersion, skins.window);
            }
        }

        private void Window(int id)
        {
            GUI.BeginGroup(new Rect(10, 10, 380, 150));          
            if (this.complete) { GUI.Label(new Rect(0, 20, 380, 15), "Complete", skins.label); }
            else
            {
                GUI.Label(new Rect(0, 20, 380, 15), this.message, skins.label);
                this.progressbar.SetValue(amountComplete);
            }

            GUI.BeginGroup(new Rect(5, 50, 380, 30));
            this.progressbar.Draw();
            GUI.EndGroup();

            if (this.complete)
            {
                if (GUI.Button(new Rect(155, 80, 80, 25), "Close", skins.button))
                {
                    this.visible = false;
                    InputLockManager.RemoveControlLock("HeightmapExtractor");
                }
                GUI.Label(new Rect(240, 80, 140, 15), String.Format("Total time: {0:0.000}s", this.time), skins.label);
            }
            else
            {
                GUI.Label(new Rect(0, 80, 380, 20), "Currently creating heightmaps, This could be very long.\nPlease be patient and do not leave the scene.", skins.label);
                GUI.Label(new Rect(0, 110, 380, 20), String.Format("Current time: {0:0.000}s", this.timer.Elapsed.TotalSeconds), skins.label);
            }
            GUI.EndGroup();
        }
        #endregion
    }
}
