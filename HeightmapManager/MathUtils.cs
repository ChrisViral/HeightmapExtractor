using System;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapManager
{
    public static class MathUtils
    {
        #region Methods
        /// <summary>
        /// Clamps a value between 0 and 1
        /// </summary>
        /// <param name="d">Value to clamp</param>
        public static double Clamp01(double d) => d < 1 ? (d > 0 ? d : 0) : 1;

        /// <summary>
        /// Linearly extrapolates a value between a two value by automatically detecting the maximum and the minimum
        /// </summary>
        /// <param name="d">percentage in between to evalutate (between 0 and 1)</param>
        /// <param name="a">First boudary value</param>
        /// <param name="b">Second boundary value</param>
        public static double Lerp(double d, double a, double b) => a != b && d > 0 ? (d < 1 ? (a > b ? b : a) + (Math.Abs(a - b) * d) : b) : a;

        /// <summary>
        /// Clamps a value between a minium and a maximum
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <param name="min">Minimal value</param>
        /// <param name="max">Maximal value</param>
        public static double ClampToRange(double value, double min, double max) => value < max && min < max ? (value > min ? value : min) : max;

        /// <summary>
        /// Clamps a value to the range of a signed interger
        /// </summary>
        /// <param name="value">Value to clamp</param>
        public static short ClampToInt16(double value) => value < short.MaxValue ? (value > short.MinValue ? (short)Math.Round(value) : short.MinValue) : short.MaxValue;
        #endregion
    }
}

