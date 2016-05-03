using System.IO;
using KSP.UI.Screens;
using UnityEngine;

namespace HeightmapExtractor
{

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class ToolbarManager : MonoBehaviour
    {
        #region Static fields
        private static Texture2D buttonTexture;
        private static ApplicationLauncherButton button;
        private static GameObject go;
        private static SettingsWindow settings;
        private static bool added;
        #endregion

        #region Methods
        private void AddButton()
        {
            if (!added)
            {
                go = new GameObject("SettingsWindow", typeof(SettingsWindow));
                DontDestroyOnLoad(go);
                settings = go.GetComponent<SettingsWindow>();
                button = ApplicationLauncher.Instance.AddModApplication(settings.Show, settings.Hide,
                         Empty, Empty, Empty, Empty, ApplicationLauncher.AppScenes.SPACECENTER, buttonTexture);
                added = true;
            }
        }

        private void RemoveButton()
        {
            if (added)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);
                Destroy(button);
                Destroy(go);
                added = false;
            }
        }

        private void Empty() { }
        #endregion

        #region Static methods
        public static void SetFalse() => button.SetFalse();
        #endregion

        #region Functions
        private void Awake()
        {
            buttonTexture = new Texture2D(38, 38, TextureFormat.ARGB32, false);
            buttonTexture.LoadImage(File.ReadAllBytes(HMUtils.IconURL));
            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveButton);
        }
        #endregion
    }
}
