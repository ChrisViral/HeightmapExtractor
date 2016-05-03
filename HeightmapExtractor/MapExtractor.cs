using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using HeightmapManager;
using SaveFormat = HeightmapManager.Heightmap.SaveFormat;

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
        public static MapExtractor Instance { get; private set; }
        #endregion

        #region Static fields
        private static readonly Dictionary<string, SaveFormat> formats = new Dictionary<string, SaveFormat>(3)
        {
            { "IMAGE", SaveFormat.IMAGE },
            { "BINARY", SaveFormat.BINARY },
            { "BOTH", SaveFormat.BOTH }
        };
        #endregion

        #region Fields
        //Image settings
        private ConfigNode settings;
        private int height = 360, width = 720, pixelsPerFrame = 720;
        private List<CelestialBody> bodies;
        private string destinationURL;
        private bool invertLatitude, invertLongitude;
        private double latitudeOffset, longitudeOffset;
        private double startingLatitude = -90, endingLatitude = 90;
        private double startingLongitude = -180, endingLongitude = 180;
        private double minAltitude = double.MinValue, maxAltitude = double.MaxValue;
        private bool invertColours = true;
        private SaveFormat saveFormat;
        private Stopwatch timer, mapTimer;

        //GUI
        private readonly Texture2D bar = new Texture2D(1, 1, TextureFormat.ARGB32, false), background = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        private readonly int id = Guid.NewGuid().GetHashCode();
        private Progressbar progressbar = new Progressbar();
        private double time;
        private string currentBody, message;
        private Rect window;
        private bool visible;

        //Generation fields
        private int bodyIndex, x, y;
        private long resolution;
        private GenerationStates state = GenerationStates.NONE;
        private CelestialBody body;
        private short[,] values;
        private bool complete;
        #endregion

        #region Properties
        private bool extract;
        /// <summary>
        /// If the system is currently extracting maps
        /// </summary>
        public bool Extract => this.extract;

        /// <summary>
        /// Percentage of the generation that's complete
        /// </summary>
        private double AmountComplete
        {
            get
            {
                if (!this.extract) { return 1; }
                double done = (this.y * this.width) + this.x;
                double mapPercent = done / this.resolution;
                return (this.bodyIndex + mapPercent) / this.bodies.Count;
            }
        }

        /// <summary>
        /// Current latitude on the planet
        /// </summary>
        private double Latitude => (this.invertLatitude ? -1 : 1) * (((this.y * (this.endingLatitude - this.startingLatitude)) / this.height) + this.startingLatitude + this.latitudeOffset);

        /// <summary>
        /// Current longitude on the planet
        /// </summary>
        private double Longitude => (this.invertLongitude ? -1 : 1) * (((this.x * (this.endingLongitude - this.startingLongitude)) / this.width) + this.startingLongitude + this.longitudeOffset);

        /// <summary>
        /// Terrain altitude at the current position
        /// </summary>
        private double TerrainAltitude
        {
            get
            {
                if (this.body.pqsController == null) { return 0; }
                Vector3d radial = QuaternionD.AngleAxis(this.Longitude, Vector3d.down) * QuaternionD.AngleAxis(this.Latitude, Vector3d.forward) * Vector3d.right;
                return this.body.pqsController.GetSurfaceHeight(radial) - this.body.pqsController.radius;
            }
        }
        #endregion

        #region Methods
        private void Window(int id)
        {
            GUI.BeginGroup(new Rect(10, 10, 380, 150));
            if (this.complete) { GUI.Label(new Rect(0, 20, 380, 15), "Complete"); }
            else
            {
                GUI.Label(new Rect(0, 20, 380, 15), this.message);
                this.progressbar.SetValue(this.AmountComplete);
            }

            GUI.BeginGroup(new Rect(5, 50, 380, 30));
            this.progressbar.Draw();
            GUI.EndGroup();

            if (this.complete)
            {
                if (GUI.Button(new Rect(155, 80, 80, 25), "Close"))
                {
                    this.visible = false;
                    InputLockManager.RemoveControlLock("HeightmapExtractor");
                }
                GUI.Label(new Rect(240, 80, 140, 15), $"Total time: {this.time:0.000}s");
            }
            else
            {
                GUI.Label(new Rect(0, 80, 380, 20), "Currently creating heightmaps, This could be very long.\nPlease be patient and do not leave the scene.");
                GUI.Label(new Rect(0, 110, 380, 20), $"Current time: {this.timer.Elapsed.TotalSeconds:0.000}s");
            }
            GUI.EndGroup();
        }

        public void LoadConfig()
        {
            //Generation information
            if (!ConfigNode.Load(HMUtils.SettingsURL).TryGetNode("SETTINGS", ref this.settings)) { Instance.extract = false; return; }
            this.settings.TryGetValue("extractMaps", ref this.extract);
            this.settings.TryGetValue("mapHeight", ref this.height);
            this.settings.TryGetValue("mapWidth", ref this.width);
            this.settings.TryGetValue("maxPixelsPerFrame", ref this.pixelsPerFrame);
            this.resolution = Math.BigMul(this.height, this.width);

            string[] bodyList = new string[0];
            this.settings.TryGetValue("bodies", ref bodyList);
            if (bodyList.Length == 1 && bodyList[0].ToLower() == "all") { this.bodies = new List<CelestialBody>(FlightGlobals.Bodies.Where(b => b.pqsController != null)); }
            else { this.bodies = new List<CelestialBody>(FlightGlobals.Bodies.Where(b => bodyList.Contains(b.bodyName) && b.pqsController != null)); }
            if (this.bodies.Count == 0) { this.extract = false; return; }

            this.settings.TryGetValue("destinationURL", ref this.destinationURL);
            if (string.IsNullOrEmpty(this.destinationURL)) { this.destinationURL = HMUtils.MapsURL; }
            if (!Directory.Exists(this.destinationURL)) { Directory.CreateDirectory(this.destinationURL); }

            //Generation restrictions
            this.settings.TryGetValue("invertLatitude", ref this.invertLatitude);
            this.settings.TryGetValue("invertLongitude", ref this.invertLongitude);
            this.settings.TryGetValue("latitudeOffset", ref this.latitudeOffset);
            this.settings.TryGetValue("longitudeOffset", ref this.longitudeOffset);
            this.settings.TryGetValue("startingLatitude", ref this.startingLatitude);
            this.settings.TryGetValue("endingLatitude", ref this.endingLatitude);
            this.settings.TryGetValue("startingLongitude", ref this.startingLongitude);
            this.settings.TryGetValue("endingLongitude", ref this.endingLongitude);
            this.settings.TryGetValue("minAltitude", ref this.minAltitude);
            this.settings.TryGetValue("maxAltitude", ref this.maxAltitude);
            this.settings.TryGetValue("invertColours", ref this.invertColours);
            string saveType = string.Empty;
            if (!this.settings.TryGetValue("saveType", ref saveType) || !formats.TryGetValue(saveType.ToUpper(), out this.saveFormat)) { this.saveFormat = SaveFormat.BOTH; }
        }

        public void StartGeneration()
        {
            if (this.settings == null)
            {
                HMUtils.Print("Cannot start generation, settings node not found");
                Instance.extract = false;
                return;
            }
            this.extract = true;
            this.complete = false;
            InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "HeightmapExtractor");
            this.progressbar.SetValue(0);
            this.visible = true;
            this.bodyIndex = 0;
            this.state = GenerationStates.INITIATING;
            HMUtils.Print("Starting map extraction.");
            this.timer = Stopwatch.StartNew();
        }
        #endregion

        #region Functions
        private void Awake()
        {
            if (Instance == null) { Instance = this; }
            else { Destroy(this); }
        }

        private void Start()
        {
            LoadConfig();

            //Progressbar
            try
            {
                this.bar.LoadImage(File.ReadAllBytes(Path.Combine(HMUtils.ProgressbarURL, "progressbar.png")));
                this.background.LoadImage(File.ReadAllBytes(Path.Combine(HMUtils.ProgressbarURL, "progressbarBackground.png")));
            }
            catch (Exception e)
            {
                HMUtils.PrintError($"Encountered an error loading the progressbar images. Aborting process.\n{e.GetType().Name}\n{e.StackTrace}");
                this.extract = false;
                return;
            }

            //GUI initialization
            Rect backgroundPos = new Rect(0, 0, 374, 19);
            Rect barPos = new Rect(2, 2, 370, 15);
            this.progressbar = new Progressbar(backgroundPos, barPos, this.background, this.bar);
            this.window = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 75, 400, 150);

            if (this.extract)
            {
                //Extraction startup
                StartGeneration();
            }
        }

        private void Update()
        {
            if (this.extract)
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
                            HMUtils.Print("Extracting heightmap of " + this.currentBody);
                            this.mapTimer = Stopwatch.StartNew();
                            return;
                        }

                    case GenerationStates.GENERATING:
                        {
                            //Reading values line by line
                            do
                            {
                                double alt = MathUtils.ClampToRange(this.TerrainAltitude, this.minAltitude, this.maxAltitude);
                                this.values[this.y, this.x] = MathUtils.ClampToInt16(alt);
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
                            HMUtils.Print($"Map generation for {this.currentBody} complete, saving file...");
                            try
                            {
                                Heightmap map = new Heightmap(this.values, this.invertColours);
                                map.Save(Path.Combine(this.destinationURL, this.currentBody), this.saveFormat);
                                HMUtils.Print($"Correctly saved map for {this.currentBody} in {this.mapTimer.Elapsed.TotalSeconds:0.000}s");
                            }
                            catch (Exception e)
                            {
                                this.mapTimer.Stop();
                                HMUtils.PrintError($"Could not save heightmap for {this.currentBody}\n{e.GetType().Name}\n{e.StackTrace}");
                                this.extract = false;
                                return;
                            }

                            this.bodyIndex++;

                            if (this.bodyIndex >= this.bodies.Count)
                            {
                                //Finishing up
                                this.extract = false;
                                this.settings.SetValue("extractMaps", bool.FalseString);
                                ConfigNode node = new ConfigNode();
                                node.AddNode(this.settings);
                                node.Save(HMUtils.SettingsURL);
                                this.timer.Stop();
                                this.time = this.timer.Elapsed.TotalSeconds;
                                this.state = GenerationStates.NONE;
                                this.progressbar.SetValue(1);
                                HMUtils.Print($"Total map generation time: {this.time:0.000}s");
                                this.complete = true;
                            }
                            else { this.state = GenerationStates.INITIATING; }
                            return;
                        }
                }
            }
        }

        private void OnGUI()
        {
            if (this.visible)
            {
                GUI.skin = HighLogic.Skin;
                this.window = GUI.Window(this.id, this.window, Window, "Heightmap Extractor " + HMUtils.AssemblyVersion);
            }
        }
        #endregion
    }
}
