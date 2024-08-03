using System;
using System.Collections;
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
        #region Instance
        public static MapExtractor Instance { get; private set; }
        #endregion

        #region Static fields
        private const double MAX_FRAME_TIME = 1d / 30; // 30fps
        private const double DEG_TO_RAD = Math.PI / 180d;
        private static readonly Dictionary<string, SaveFormat> formats = new(3)
        {
            { "IMAGE", SaveFormat.IMAGE },
            { "BINARY", SaveFormat.BINARY },
            { "BOTH", SaveFormat.BOTH }
        };

        private static double ElapsedTime => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        #endregion

        #region Fields
        //Image settings
        private ConfigNode settings;
        private int height = 1024, width = 2048;
        private List<CelestialBody> bodies;
        private string destinationURL;
        private bool invertLatitude, invertLongitude;
        private double longitudeOffset = -180d;
        private bool invertColours = true;
        private SaveFormat saveFormat;
        private Stopwatch timer;

        //GUI
        private Texture2D bar, background;
        private readonly int id = Guid.NewGuid().GetHashCode();
        private Progressbar progressbar = new Progressbar();
        private double time;
        private string message;
        private Rect window;
        private bool visible;

        //Generation fields
        private int bodyIndex, x, y;
        private long resolution;
        #endregion

        #region Properties
        /// <summary>
        /// If the system is currently extracting maps
        /// </summary>
        public bool Extracting { get; private set; }

        /// <summary>
        /// Percentage of the generation that's complete
        /// </summary>
        private double AmountComplete
        {
            get
            {
                if (!this.Extracting) { return 1d; }
                double done = (this.y * this.width) + this.x;
                double mapPercent = done / this.resolution;
                return (this.bodyIndex + mapPercent) / this.bodies.Count;
            }
        }

        /// <summary>
        /// Current latitude on the planet
        /// </summary>
        private double Latitude => (this.invertLatitude ? -1 : 1) * (((this.y * 180d) / this.height) - 90d);

        /// <summary>
        /// Current longitude on the planet
        /// </summary>
        private double Longitude => (this.invertLongitude ? -1 : 1) * (((this.x * 360d) / this.width) + this.longitudeOffset);
        #endregion

        #region Methods
        private void Window(int id)
        {
            GUI.BeginGroup(new Rect(10, 10, 380, 150));
            if (!this.Extracting) { GUI.Label(new Rect(0, 20, 380, 15), "Complete"); }
            else
            {
                GUI.Label(new Rect(0, 20, 380, 15), this.message);
                this.progressbar.SetValue(this.AmountComplete);
            }

            GUI.BeginGroup(new Rect(5, 50, 380, 30));
            this.progressbar.Draw();
            GUI.EndGroup();

            if (!this.Extracting)
            {
                if (GUI.Button(new Rect(155, 80, 80, 25), "Close"))
                {
                    this.visible = false;
                    InputLockManager.RemoveControlLock(nameof(MapExtractor));
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
            if (!ConfigNode.Load(HMUtils.SettingsURL).TryGetNode("SETTINGS", ref this.settings)) { Instance.Extracting = false; return; }
            this.settings.TryGetValue("mapHeight", ref this.height);
            this.settings.TryGetValue("mapWidth", ref this.width);
            this.resolution = Math.BigMul(this.height, this.width);

            string[] bodyList = [];
            this.settings.TryGetValue("bodies", ref bodyList);
            this.bodies = bodyList.Length switch
            {
                0                           => [],
                1 when bodyList[0] is "all" => [..FlightGlobals.Bodies.Where(b => b.pqsController)],
                _                           => [..FlightGlobals.Bodies.Where(b => bodyList.Contains(b.bodyName) && b.pqsController)]
            };

            this.settings.TryGetValue("destinationURL", ref this.destinationURL);
            if (string.IsNullOrEmpty(this.destinationURL)) { this.destinationURL = HMUtils.MapsURL; }
            if (!Directory.Exists(this.destinationURL)) { Directory.CreateDirectory(this.destinationURL); }

            //Generation restrictions
            this.settings.TryGetValue("invertLatitude", ref this.invertLatitude);
            this.settings.TryGetValue("invertLongitude", ref this.invertLongitude);
            this.settings.TryGetValue("longitudeOffset", ref this.longitudeOffset);
            this.settings.TryGetValue("invertColours", ref this.invertColours);
            string saveType = string.Empty;
            if (!this.settings.TryGetValue("saveType", ref saveType) || !formats.TryGetValue(saveType.ToUpper(), out this.saveFormat)) { this.saveFormat = SaveFormat.BOTH; }
        }

        public void StartGeneration()
        {
            if (this.settings == null)
            {
                HMUtils.Print("Cannot start generation, settings node not found");
                return;
            }

            StartCoroutine(Generate());
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
                this.bar = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                this.bar.LoadImage(File.ReadAllBytes(Path.Combine(HMUtils.ProgressbarURL, "progressbar.png")));

                this.background = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                this.background.LoadImage(File.ReadAllBytes(Path.Combine(HMUtils.ProgressbarURL, "progressbarBackground.png")));
            }
            catch (Exception e)
            {
                HMUtils.PrintError($"Encountered an error loading the progressbar images. Aborting process.\n{e.GetType().Name}\n{e.StackTrace}");
                return;
            }

            //GUI initialization
            Rect backgroundPos = new(0, 0, 374, 19);
            Rect barPos = new(2, 2, 370, 15);
            this.progressbar = new Progressbar(backgroundPos, barPos, this.background, this.bar);
            this.window = new Rect((Screen.width / 2f) - 200f, (Screen.height / 2f) - 75f, 400f, 150f);

            if (this.Extracting)
            {
                //Extraction startup
                StartGeneration();
            }
        }

        private IEnumerator Generate()
        {
            this.Extracting  = true;
            this.progressbar.SetValue(0f);
            this.visible   = true;
            InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, nameof(MapExtractor));
            HMUtils.Print("Starting map extraction.");
            this.timer = Stopwatch.StartNew();
            double nextFrameTime = ElapsedTime + MAX_FRAME_TIME;
            for (this.bodyIndex = 0; this.bodyIndex < this.bodies.Count; this.bodyIndex++)
            {
                CelestialBody body = this.bodies[this.bodyIndex];
                if (!body.pqsController)
                {
                    HMUtils.Print($"{body.bodyName} has no PQS, skipping.");
                    continue;
                }

                string currentBodyName = body.bodyName;
                PQS bodyPQS = body.pqsController;
                double bodyRadius = bodyPQS.radius;
                short[,] values = new short[this.height, this.width];
                this.message = "Extracting " + currentBodyName;
                HMUtils.Print("Extracting heightmap of " + currentBodyName);
                Stopwatch mapTimer = Stopwatch.StartNew();

                for (this.y = 0; this.y < this.height; this.y++)
                {
                    double latitude = this.Latitude;
                    Vector3d latitudeVector = QuaternionD.AngleAxis(this.Latitude, Vector3d.forward) * Vector3d.right;
                    int jump = (int)Math.Min(1d / Math.Cos(latitude * DEG_TO_RAD), this.width);
                    for (this.x = 0; this.x < this.width; this.x += jump)
                    {
                        Vector3d radial = QuaternionD.AngleAxis(this.Longitude, Vector3d.down) * latitudeVector;
                        double terrainAltitude = bodyPQS.GetSurfaceHeight(radial) - bodyRadius;
                        short alt = MathUtils.ClampToInt16(terrainAltitude);
                        int max = Math.Min(this.x + jump, this.width);
                        for (int sweep = this.x; sweep < max; sweep++)
                        {
                            values[this.y, sweep] = alt;
                        }

                        if (ElapsedTime > nextFrameTime)
                        {
                            nextFrameTime = ElapsedTime + MAX_FRAME_TIME;
                            yield return null;
                        }
                    }
                }

                //Saving file
                HMUtils.Print($"Map generation for {currentBodyName} complete, saving file...");
                try
                {
                    Heightmap map = new(values, this.invertColours);
                    map.Save(Path.Combine(this.destinationURL, currentBodyName), this.saveFormat);
                    HMUtils.Print($"Correctly saved map for {currentBodyName} in {mapTimer.Elapsed.TotalSeconds:0.000}s");
                }
                catch (Exception e)
                {
                    mapTimer.Stop();
                    HMUtils.PrintError($"Could not save heightmap for {currentBodyName}\n{e.GetType().Name}\n{e.StackTrace}");
                }

                if (ElapsedTime > nextFrameTime)
                {
                    nextFrameTime = ElapsedTime + MAX_FRAME_TIME;
                    yield return null;
                }
            }

            //Finishing up
            this.Extracting = false;
            this.timer.Stop();
            this.time  = this.timer.Elapsed.TotalSeconds;
            this.progressbar.SetValue(1f);
            HMUtils.Print($"Total map generation time: {this.time:0.000}s");
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
