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
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class MapExtractor : MonoBehaviour
    {
        private enum GenerationStates
        {
            NONE,
            INITIATING,
            GENERATING,
            SAVING
        }

        #region Fields
        //Image settings
        private ConfigNode settings = new ConfigNode();
        private ushort height = 360, width = 720, pixelsPerFrame = 720;
        private List<CelestialBody> bodies = new List<CelestialBody>();
        private bool extract = false;
        private string destinationURL = string.Empty;
        private bool invertLatitude = false, invertLongitude = false;
        private short latitudeOffset = 0, longitudeOffset = 0;
        private short startingLatitude = 90, endingLatitude = -90;
        private short startingLongitude = -180, endingLongitude = 180;
        private short minAltitude = short.MinValue, maxAltidue = short.MaxValue;
        private bool invertColours = true;
        private string saveType = "BOTH";
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

        //Debug GUI
        private Rect debug = new Rect(100, 100, 150, 100);
        private int debugId = Guid.NewGuid().GetHashCode();

        //Generation fields
        private int bodyIndex = 0, x = 0, y = 0;
        private long resolution = 0;
        private GenerationStates state = GenerationStates.NONE;
        private CelestialBody body = null;
        private short[,] values = new short[0, 0];
        private Color[] image = new Color[0];
        private bool complete = false;
        #endregion

        #region Properties
        /// <summary>
        /// Percentage of the generation that's complete
        /// </summary>
        private double amountComplete
        {
            get
            {
                if (!this.extract) { return 1; }
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
            get { return (this.invertLatitude ? -1 : 1) * (((this.y * (this.startingLatitude - this.endingLatitude)) / (double)this.height) - 90 + latitudeOffset);}
        }

        /// <summary>
        /// Current longitude on the planet
        /// </summary>
        private double longitude
        {
            get { return (invertLongitude ? -1 : 1) * (((x * (this.endingLongitude - this.startingLongitude)) / (double)this.width) - 180 + longitudeOffset); }
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
            //Generation information
            if (!ConfigNode.Load(Utils.settingsURL).TryGetNode("SETTINGS", ref this.settings)) { this.extract = false; return; }
            if (!this.settings.TryGetValue("extractMaps", ref this.extract) || !this.extract) { return; }
            this.settings.TryGetValue("mapHeight", ref this.height);
            this.settings.TryGetValue("mapWidth", ref this.width);
            this.settings.TryGetValue("maxPixelsPerFrame", ref this.pixelsPerFrame);
            this.resolution = (long)this.height * (long)this.width;
            string[] bodyList = new string[0];
            this.settings.TryGetValue("bodies", ref bodyList);
            if (bodyList.Length == 1 && bodyList[0].ToLower() == "all") { this.bodies = new List<CelestialBody>(FlightGlobals.Bodies.Where(b => b.pqsController != null)); }
            else { this.bodies = new List<CelestialBody>(FlightGlobals.Bodies.Where(b => bodyList.Contains(b.bodyName) && b.pqsController != null)); }
            if (this.bodies.Count == 0) { this.extract = false; return; }
            this.settings.TryGetValue("destinationURL", ref this.destinationURL);
            if (string.IsNullOrEmpty(destinationURL)) { this.destinationURL = Utils.mapsURL; }
            if (!Directory.Exists(destinationURL)) { Directory.CreateDirectory(destinationURL); }

            //Progressbar
            try
            {
                this.bar.LoadImage(File.ReadAllBytes(Path.Combine(Utils.progressbarURL, "progressbar.png")));
                this.background.LoadImage(File.ReadAllBytes(Path.Combine(Utils.progressbarURL, "progressbarBackground.png")));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(String.Format("[HeightmapExtractor]: Encountered an error loading the progressbar images. Aborting process.\n{0}\n{1}", e.GetType().Name, e.StackTrace));
                this.extract = false;
                return;
            }
            
            //Generation fields
            this.settings.TryGetValue("invertLatitude", ref this.invertLatitude);
            this.settings.TryGetValue("invertLongitude", ref this.invertLongitude);
            this.settings.TryGetValue("latitudeOffset", ref this.latitudeOffset);
            this.settings.TryGetValue("longitudeOffset", ref this.longitudeOffset);
            this.settings.TryGetValue("startingLatitude", ref this.startingLatitude);
            this.settings.TryGetValue("endingLatitude", ref this.endingLatitude);
            this.settings.TryGetValue("startingLongitude", ref this.startingLongitude);
            this.settings.TryGetValue("endingLongitude", ref this.endingLongitude);
            this.settings.TryGetValue("invertColours", ref this.invertColours);
            this.settings.TryGetValue("saveType", ref this.saveType);
            switch(this.saveType.ToUpper())
            {
                case "IMAGE":
                    this.saveFormat = Heightmap.SaveFormat.IMAGE; break;

                case "BINARY":
                    this.saveFormat = Heightmap.SaveFormat.BINARY; break;

                case "BOTH":
                default:
                    this.saveFormat = Heightmap.SaveFormat.BOTH; break;
            }

            //GUI initialization
            Rect backgroundPos = new Rect(0, 0, 374, 19);
            Rect barPos = new Rect(2, 2, 370, 15);
            this.progressbar = new Progressbar(backgroundPos, barPos, background, bar);
            this.progressbar.SetValue(0);
            this.window = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 75, 400, 150);

            //Extraction startup
            InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "Heightmap Extractor");
            this.visible = true;
            this.state = GenerationStates.INITIATING;
            print("[HeightmapExtractor]: Starting map extraction.");
            this.timer.Start();
        }
        #endregion

        #region Functions
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
                            this.image = new Color[this.resolution];
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
                                this.values[y, x] = Utils.ClampToRange((int)Math.Round(terrainAltitude), this.minAltitude, this.maxAltidue);
                                x++;
                            }
                            while (this.x < this.width && this.x % this.pixelsPerFrame != 0);

                            if (this.x >= this.width)
                            {
                                y++;
                                x = 0;
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
                                node.Save(Utils.settingsURL);
                                this.timer.Stop();
                                this.time = this.timer.Elapsed.TotalSeconds;
                                this.state = GenerationStates.NONE;
                                this.progressbar.SetValue(1);
                                print(String.Format("[HeightmapExtractor]: Total map generation time: {0:0.000}s", this.time));
                            }
                            else { this.state = GenerationStates.INITIATING; }
                            return;
                        }

                    case GenerationStates.NONE:
                    default:
                        break;
                }
                if (!this.complete) { this.complete = true; }
            }
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            if (this.visible)
            {
                this.window = GUI.Window(this.id, this.window, Window, "Heightmap Extractor " + Utils.assemblyVersion, skins.window);
            }

            #region DEBUG
            #if DEBUG
            this.debug = GUILayout.Window(this.dId, this.debug, Debug, "Debug window");
            #endif
            #endregion
        }

        private void Window(int id)
        {
            GUI.BeginGroup(new Rect(10, 10, 380, 150));          
            if (amountComplete == 1d && this.complete) { GUI.Label(new Rect(0, 20, 380, 15), "Complete", skins.label); }
            else
            {
                GUI.Label(new Rect(0, 20, 380, 15), this.message, skins.label);
                this.progressbar.SetValue(amountComplete);
            }

            GUI.BeginGroup(new Rect(5, 50, 380, 30));
            this.progressbar.Draw();
            GUI.EndGroup();

            if (this.amountComplete == 1d && this.complete)
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

        private void Debug(int id)
        {
            if (GUILayout.Button("Load from constructor"))
            {
                try
                {
                    string path = Path.Combine(Utils.mapsURL, "Kerbin_raw.bin");
                    print(path);
                    Heightmap map = new Heightmap(path);
                    print(map.height);
                    print(map.width);
                    print(map.size);
                    print(map.ToByteArray().LongLength);
                }
                catch(Exception e)
                {
                    print(e.InnerException == null);
                    print(String.Format("{0}\n{1}\n{2}", e.GetType().Name, e.Message, e.StackTrace));
                    if (e.InnerException != null)
                    {
                        print(String.Format("{0}\n{1}\n{2}", e.InnerException.GetType().Name, e.InnerException.Message, e.InnerException.StackTrace));
                    }
                }
            }

            if (GUILayout.Button("Load from static method"))
            {
                try
                {
                    string path = Path.Combine(Utils.mapsURL, "Kerbin_raw.bin");
                    print(path);
                    Heightmap map = Heightmap.CreateNewFromBinary(path);
                    print(map.height);
                    print(map.width);
                    print(map.size);
                    print(map.ToByteArray().LongLength);
                }
                catch (Exception e)
                {
                    print(e.InnerException == null);
                    print(String.Format("{0}\n{1}\n{2}", e.GetType().Name, e.Message, e.StackTrace));
                    if (e.InnerException != null)
                    {
                        print(String.Format("{0}\n{1}\n{2}", e.InnerException.GetType().Name, e.InnerException.Message, e.InnerException.StackTrace));
                    }
                }
            }

            if (GUILayout.Button("Print pixel info"))
            {
                Heightmap map = new Heightmap(Path.Combine(Utils.mapsURL, "Kerbin_raw.bin"));
                print("Extracting from Kerbin_raw.bin");
                print("Bilinear pixel coords: (0.337, 0.729)");
                print("Actual coords: (970.56, 1049.76) or (58.68°E, 41.22°S)");
                Vector3d radial = QuaternionD.AngleAxis(-58.68, Vector3d.down) * QuaternionD.AngleAxis(41.22, Vector3d.forward) * Vector3d.right;
                PQS kerbin = FlightGlobals.Bodies.Single(b => b.bodyName == "Kerbin").pqsController;
                double alt = kerbin.GetSurfaceHeight(radial) - kerbin.radius;
                print("PQS altitude: " + alt);
                Stopwatch t = Stopwatch.StartNew();
                double p = map.ReadPixelBilinear(0.337, 0.729);
                t.Stop();
                print("Bilinear read: " + p);
                print("Time to read bilinear pixel: " + t.Elapsed.TotalMilliseconds + "ms");
                short A = map[970, 1049], B = map[971, 1049], C = map[970, 1050], D = map[971, 1050];
                print("(970, 1049): " + A);
                print("(971, 1049): " + B);
                print("(970, 1050): " + C);
                print("(971, 1050): " + D);
                double x = MathHelp.Lerp(0.56, A, B), y = MathHelp.Lerp(0.56, C, D);
                print("Top lerp: " + x);
                print("Bottom lerp: " + y);
                double z = MathHelp.Lerp(0.76, x, y);
                print("Final value: " + z);
                print("Bilinear is equal: " + (z == p));
            }

            GUI.DragWindow();
        }
        #endregion
    }
}
