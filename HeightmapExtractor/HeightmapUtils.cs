using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Diagnostics;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapExtractor
{
    public static class HeightmapUtils
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
        public static string mapsURL
        {
            get { return Path.Combine(KSPUtil.ApplicationRootPath, localMapsURL); }
        }

        /// <summary>
        /// URL to the settings config
        /// </summary>
        public static string settingsURL
        {
            get { return Path.Combine(KSPUtil.ApplicationRootPath, localConfigURL); }
        }

        /// <summary>
        /// URL to the progressbar folder
        /// </summary>
        public static string progressbarURL
        {
            get { return Path.Combine(KSPUtil.ApplicationRootPath, localProgressbarURL); }
        }

        /// <summary>
        /// Absolute URL to the Applauncher icon
        /// </summary>
        public static string iconURL
        {
            get { return Path.Combine(KSPUtil.ApplicationRootPath, localIconURL); }
        }

        /// <summary>
        /// Gets the current version of the assembly
        /// </summary>
        public static string assemblyVersion
        {
            get
            {
                System.Version version = new System.Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
                if (version.Revision == 0)
                {
                    if (version.Build == 0) { return "v" + version.ToString(2); }
                    return "v" + version.ToString(3);
                }
                return "v" + version.ToString();
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Separates a string at each comma and returns all the elements into an array
        /// </summary>
        /// <param name="text">String to separate</param>
        public static string[] ParseArray(string text)
        {
            return text.Split(',').Select(s => s.Trim()).ToArray();
        }
        #endregion
    }
}
