using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapExtractor
{
    public static class HMUtils
    {
        #region Constants
        /// <summary>
        /// Local URL to the maps folder
        /// </summary>
        public const string localMapsURL = "GameData/HeightmapExtractor/Plugins/PluginData/Maps";

        /// <summary>
        /// Local URL to the settings config
        /// </summary>
        public const string localConfigURL = "GameData/HeightmapExtractor/Plugins/PluginData/Settings.cfg";

        /// <summary>
        /// Local URL to the progressbar folder
        /// </summary>
        public const string localProgressbarURL = "GameData/HeightmapExtractor/Plugins/PluginData/Progressbar";

        /// <summary>
        /// Local URL to the Applauncher icon
        /// </summary>
        public const string localIconURL = "GameData/HeightmapExtractor/Plugins/PluginData/Extractor_Icon.png";
        #endregion

        #region Properties
        /// <summary>
        /// URL to the maps folder
        /// </summary>
        public static string MapsURL { get; }

        /// <summary>
        /// URL to the settings config
        /// </summary>
        public static string SettingsURL { get; }

        /// <summary>
        /// URL to the progressbar folder
        /// </summary>
        public static string ProgressbarURL { get; }

        /// <summary>
        /// Absolute URL to the Applauncher icon
        /// </summary>
        public static string IconURL { get; }

        /// <summary>
        /// Gets the current version of the assembly
        /// </summary>
        public static string AssemblyVersion { get; }
        #endregion

        #region Constructor
        static HMUtils()
        {
            MapsURL = Path.Combine(KSPUtil.ApplicationRootPath, localMapsURL);
            SettingsURL = Path.Combine(KSPUtil.ApplicationRootPath, localConfigURL);
            ProgressbarURL = Path.Combine(KSPUtil.ApplicationRootPath, localProgressbarURL);
            IconURL = Path.Combine(KSPUtil.ApplicationRootPath, localIconURL);

            Version version = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
            if (version.Revision == 0)
            {
                if (version.Build == 0) { AssemblyVersion = "v" + version.ToString(2); }
                AssemblyVersion = "v" + version.ToString(3);
            }
            AssemblyVersion = "v" + version;
        }
        #endregion

        #region Static methods
        public static void Print(string message)
        {
            Debug.Log("[HeightMapExtractor]: " + message);
        }

        public static void PrintError(string message)
        {
            Debug.LogError("[HeightMapExtractor]: " + message);
        }
        #endregion
    }
}
