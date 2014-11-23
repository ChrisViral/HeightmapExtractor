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
        private int bodyIndex = 0, xIndex = 0, yIndex = 0;
        private long resolution = 0;
        private GenerationStates state = GenerationStates.NONE;
        private CelestialBody body = null;
        private short[,] values = new short[0, 0];
        private Color[] image = new Color[0];
        private bool complete = false;
        #endregion

        #region Properties
        private double amountComplete
        {
            get
            {
                if (!this.extract) { return 1; }
                double done = (yIndex * this.width) + xIndex;
                double mapPercent = done / (double)resolution;
                return ((double)bodyIndex + mapPercent) / (double)bodies.Count;
            }
        }

        private double latitude
        {
            get { return ((this.yIndex * 180d) / (double)this.height) - 90d;}
        }

        private double longitude
        {
            get { return ((xIndex * 360d) / (double)this.width) - 180d; }
        }

        private int index
        {
            get { return (this.yIndex * this.width) + this.xIndex; }
        }

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

            Rect backgroundPos = new Rect(0, 0, 374, 19);
            Rect barPos = new Rect(2, 2, 370, 15);
            this.progressbar = new Progressbar(backgroundPos, barPos, background, bar);
            this.progressbar.SetValue(0);
            this.window = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 75, 400, 150);

            this.timer.Start();
            InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "Heightmap Extractor");
            this.visible = true;
            this.state = GenerationStates.INITIATING;
            print("[Heightmap Extractor]: Starting map extraction.");
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
                            this.body = this.bodies[this.bodyIndex];
                            this.xIndex = 0;
                            this.yIndex = 0;
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
                            do
                            {
                                this.values[yIndex, xIndex] = Utils.ClampToInt16((int)Math.Round(terrainAltitude));
                                xIndex++;
                            }
                            while (this.xIndex < this.width && this.xIndex % this.pixelsPerFrame != 0);

                            if (this.xIndex >= this.width)
                            {
                                yIndex++;
                                xIndex = 0;
                                if (this.yIndex >= this.height)
                                {
                                    this.state = GenerationStates.SAVING;
                                    this.message = "Saving " + this.currentBody;
                                }
                            }
                            return;
                        }

                    case GenerationStates.SAVING:
                        {
                            print(String.Format("[HeightmapExtractor]: Map generation for {0} complete, saving file...", this.currentBody));
                            try
                            {
                                Heightmap map = new Heightmap(this.values);
                                map.Save(Path.Combine(destinationURL, currentBody), Heightmap.SaveFormat.BOTH);
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
                                this.extract = false;
                                this.settings.SetValue("extractMaps", bool.FalseString);
                                ConfigNode node = new ConfigNode();
                                node.AddNode(this.settings);
                                node.Save(Utils.settingsURL);
                                this.state = GenerationStates.NONE;
                                this.timer.Stop();
                                this.time = this.timer.Elapsed.TotalSeconds;
                                this.progressbar.SetValue(1);
                                InputLockManager.RemoveControlLock("HeightmapExtractor");
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
        }

        private void Window(int id)
        {
            GUI.BeginGroup(new Rect(10, 10, 380, 150));
            if (this.amountComplete != 1d)
            {
                GUI.Label(new Rect(0, 20, 380, 15), this.message, skins.label);
                this.progressbar.SetValue(amountComplete);
            }
            else { GUI.Label(new Rect(0, 20, 380, 15), "Complete", skins.label); }

            GUI.BeginGroup(new Rect(5, 50, 380, 30));
            this.progressbar.Draw();
            GUI.EndGroup();

            if (this.amountComplete == 1d && this.complete)
            {
                if (GUI.Button(new Rect(155, 80, 80, 25), "Close", skins.button))
                {
                    this.visible = false;
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
