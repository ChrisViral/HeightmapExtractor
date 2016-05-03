using UnityEngine;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapExtractor
{
    public class Progressbar
    {
        #region Fields
        private Texture2D background, bar;
        private Rect bgPosition, barPosition;
        private double filled;
        #endregion

        #region Constructor
        /// <summary>
        /// Generates a new ProgressBar
        /// </summary>
        public Progressbar() { }

        /// <summary>
        /// Generates a new progressbar with the specified parameters
        /// </summary>
        /// <param name="bgPosition">Size and position of the background of the bar</param>
        /// <param name="barPosition">Size and position of the progress bar</param>
        /// <param name="background">Texture of the background of the bar</param>
        /// <param name="bar">Texture for the progressbar</param>
        public Progressbar(Rect bgPosition, Rect barPosition, Texture2D background, Texture2D bar)
        {
            this.bgPosition = bgPosition;
            this.barPosition = barPosition;
            this.background = background;
            this.bar = bar;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the value of the bar between 0 and 1
        /// </summary>
        /// <param name="value">Percentage of the bar filled</param>
        public void SetValue(double value)
        {
            this.filled = value > 0 ? (value < 1 ? value : 1) : 0;
        }

        /// <summary>
        /// Sets the value of the bar within a given range
        /// </summary>
        /// <param name="value">Current value</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        public void SetValue(double value, double min, double max)
        {
            this.filled = value < max && min < max ? (value > min ? (value - min) / (max - min) : 0) : 1;
        }

        /// <summary>
        /// Changes the GUIStyle of the background box
        /// </summary>
        /// <param name="background">GUIStyle of the box</param>
        public void SetBackground(Texture2D background)
        {
            this.background = background;
        }

        /// <summary>
        /// Changes the texture of the progress bar
        /// </summary>
        /// <param name="bar">Texture of the progress bar</param>
        public void SetBar(Texture2D bar)
        {
            this.bar = bar;
        }

        /// <summary>
        /// Changes the size and position of the bar
        /// </summary>
        /// <param name="bgPosition">Size and position of the background of the bar</param>
        /// <param name="barPosition">Size and position of the progress bar</param>
        public void SetPosition(Rect bgPosition, Rect barPosition)
        {
            this.bgPosition = bgPosition;
            this.barPosition = barPosition;
        }

        /// <summary>
        /// Call this in OnGUI() to draw the ProgressBar
        /// </summary>
        public void Draw()
        {
            GUI.DrawTexture(this.bgPosition, this.background);
            GUI.DrawTexture(new Rect(this.barPosition.x, this.barPosition.y, (float)(this.barPosition.width * this.filled), this.barPosition.height), this.bar);
        }
        #endregion
    }
}
