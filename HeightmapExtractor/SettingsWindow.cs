using System;
using System.IO;
using UnityEngine;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapExtractor
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class SettingsWindow : MonoBehaviour
    {
        #region Fields
        private GUISkin skins = HighLogic.Skin;
        private int id = Guid.NewGuid().GetHashCode();
        private bool visible = false, showing = true;
        private Rect window = new Rect();
        private Texture2D buttonTexture = new Texture2D(38, 38);
        private ApplicationLauncherButton button = new ApplicationLauncherButton();
        #endregion

        #region Methods
        private void AddButton()
        {
            if (ApplicationLauncher.Ready)
            {
                button = ApplicationLauncher.Instance.AddModApplication(
                    Show,
                    Hide,
                    Empty,
                    Empty,
                    Empty,
                    Empty,
                    ApplicationLauncher.AppScenes.SPACECENTER,
                    (Texture)buttonTexture);
            }
        }

        private void Show()
        {
            this.visible = true;
        }

        private void Hide()
        {
            this.visible = false;
        }

        private void Empty() { }

        private void HideUI()
        {
            this.showing = false;
        }

        private void ShowUI()
        {
            this.showing = true;
        }
        #endregion

        #region Initialization
        private void Awake()
        {
            this.window = new Rect(100, 100, 330, 130);
            this.buttonTexture.LoadImage(File.ReadAllBytes(Utils.iconURL));

            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            GameEvents.onShowUI.Add(ShowUI);
            GameEvents.onHideUI.Add(HideUI);
            GameEvents.onGUIAstronautComplexSpawn.Add(HideUI);
            GameEvents.onGUIAstronautComplexDespawn.Add(ShowUI);
            GameEvents.onGUIRnDComplexSpawn.Add(HideUI);
            GameEvents.onGUIRnDComplexDespawn.Add(ShowUI);
            GameEvents.onGUIMissionControlSpawn.Add(HideUI);
            GameEvents.onGUIMissionControlDespawn.Add(ShowUI);
            GameEvents.onGUIAdministrationFacilitySpawn.Add(HideUI);
            GameEvents.onGUIAdministrationFacilityDespawn.Add(ShowUI);
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddButton);
            GameEvents.onShowUI.Remove(ShowUI);
            GameEvents.onHideUI.Remove(HideUI);
            GameEvents.onGUIAstronautComplexSpawn.Remove(HideUI);
            GameEvents.onGUIAstronautComplexDespawn.Remove(ShowUI);
            GameEvents.onGUIRnDComplexSpawn.Remove(HideUI);
            GameEvents.onGUIRnDComplexDespawn.Remove(ShowUI);
            GameEvents.onGUIMissionControlSpawn.Remove(HideUI);
            GameEvents.onGUIMissionControlDespawn.Remove(ShowUI);
            GameEvents.onGUIAdministrationFacilitySpawn.Remove(HideUI);
            GameEvents.onGUIAdministrationFacilityDespawn.Remove(ShowUI);

            ApplicationLauncher.Instance.RemoveModApplication(button);
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            if (this.showing && this.visible)
            {
                this.window = GUILayout.Window(this.id, this.window, Window, "HeightmapExtractor Controller " + Utils.assemblyVersion, skins.window);
            }
        }

        private void Window(int id)
        {
            GUI.DragWindow(new Rect(0, 0, window.width, 20));

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUI.enabled = !MapExtractor.instance.extract;
            if (GUILayout.Button("Reload config", skins.button))
            {
                MapExtractor.LoadConfig();
                print("[HeightmapExtractor]: Reloaded settings config");
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Restart generation", skins.button))
            {
                MapExtractor.StartGeneration();
            }
            GUILayout.FlexibleSpace();

            GUI.enabled = true;
            if (GUILayout.Button("Close", skins.button))
            {
                button.SetFalse();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
        #endregion
    }
}
