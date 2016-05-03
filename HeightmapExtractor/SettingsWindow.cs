using System;
using System.IO;
using UnityEngine;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapExtractor
{
    public class SettingsWindow : MonoBehaviour
    {
        #region Fields
        private readonly int id = Guid.NewGuid().GetHashCode();
        private bool visible, hid;
        private Rect window, drag;
        #endregion

        #region Methods
        internal void Show() => this.visible = true;

        internal void Hide() => this.visible = false;

        private void HideUI() => this.hid = true;

        private void ShowUI() => this.hid = false;

        private void Window(int id)
        {
            GUI.DragWindow(this.drag);

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUI.enabled = !MapExtractor.Instance.Extract;
            if (GUILayout.Button("Reload config"))
            {
                MapExtractor.Instance.LoadConfig();
                HMUtils.Print("[HeightmapExtractor]: Reloaded settings config");
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Restart generation"))
            {
                MapExtractor.Instance.StartGeneration();
            }
            GUILayout.FlexibleSpace();

            GUI.enabled = true;
            if (GUILayout.Button("Close"))
            {
                ToolbarManager.SetFalse();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
        #endregion

        #region Functions
        private void Awake()
        {
            this.window = new Rect(100, 100, 330, 130);
            this.drag = new Rect(0, 0, 330, 30);

            GameEvents.onShowUI.Add(ShowUI);
            GameEvents.onHideUI.Add(HideUI);
            GameEvents.onGUIAstronautComplexDespawn.Add(ShowUI);
            GameEvents.onGUIAstronautComplexSpawn.Add(HideUI);
            GameEvents.onGUIRnDComplexDespawn.Add(ShowUI);
            GameEvents.onGUIRnDComplexSpawn.Add(HideUI);
            GameEvents.onGUIMissionControlDespawn.Add(ShowUI);
            GameEvents.onGUIMissionControlSpawn.Add(HideUI);
            GameEvents.onGUIAdministrationFacilityDespawn.Add(ShowUI);
            GameEvents.onGUIAdministrationFacilitySpawn.Add(HideUI);
        }

        private void OnGUI()
        {
            if (this.visible && !this.hid)
            {
                GUI.skin = HighLogic.Skin;
                this.window = GUILayout.Window(this.id, this.window, Window, "HeightmapExtractor Controller " + HMUtils.AssemblyVersion);
            }
        }
        #endregion
    }
}
