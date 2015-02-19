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
        public static double Clamp01(double d)
        {
            if (d >= 1) { return 1; }
            else if (d <= 0) { return 0; }
            return d;
        }

        /// <summary>
        /// Linearly extrapolates a value between a two value by automatically detecting the maximum and the minimum
        /// </summary>
        /// <param name="d">percentage in between to evalutate (between 0 and 1)</param>
        /// <param name="a">First boudary value</param>
        /// <param name="b">Second boundary value</param>
        public static double Lerp(double d, double a, double b)
        {
            if (a == b || d <= 0) { return a; }
            else if (d >= 1) { return b; }
            return (a > b ? b : a) + (Math.Abs(a - b) * d);
        }

        /// <summary>
        /// Clamps a value between a minium and a maximum
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <param name="min">Minimal value</param>
        /// <param name="max">Maximal value</param>
        public static double ClampToRange(double value, double min, double max)
        {
            if (value >= max || min == max) { return max; }
            else if (value <= min) { return min; }
            return value;
        }

        /// <summary>
        /// Clamps a value to the range of a signed interger
        /// </summary>
        /// <param name="value">Value to clamp</param>
        public static short ClampToInt16(double value)
        {
            if (value >= 32767) { return (short)32767; }
            else if (value <= -32768) { return (short)-32768; }
            return (short)Math.Round(value);
        }
        #endregion
    }
}

