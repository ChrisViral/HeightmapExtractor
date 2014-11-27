using System;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapExtractor
{
    public static class ConfigNodeExtensions
    {
        #region Methods
        /// <summary>
        /// Sees if the ConfigNode has a named node and stores it in the ref value
        /// </summary>
        /// <param name="name">Name of the node to find</param>
        /// <param name="result">Value to store the result in</param>
        public static bool TryGetNode(this ConfigNode node, string name, ref ConfigNode result)
        {
            if (node.HasNode(name))
            {
                result = node.GetNode(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the ConfigNode has the specified value and stores it within the ref. Ref is untouched if value not present.
        /// </summary>
        /// <param name="name">Name of the value searched for</param>
        /// <param name="value">Value to assign</param>
        public static bool TryGetValue(this ConfigNode node, string name, ref string value)
        {
            if (node.HasValue(name))
            {
                value = node.GetValue(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the ConfigNode has a value of the given name and stores it as an array within the given ref value. Does not touch the ref if not.
        /// </summary>
        /// <param name="name">Name of the value to look for</param>
        /// <param name="value">Value to store the result in</param>
        public static bool TryGetValue(this ConfigNode node, string name, ref string[] value)
        {
            if (node.HasValue(name))
            {
                value = Utils.ParseArray(node.GetValue(name));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the ConfigNode contains the value and sotres it in the ref if it is parsable. Value is left unchanged if not.
        /// </summary>
        /// <param name="name">Name of the value to look for</param>
        /// <param name="value">Value to store the result in</param>
        public static bool TryGetValue(this ConfigNode node, string name, ref ushort value)
        {
            ushort result;
            if (node.HasValue(name) && ushort.TryParse(node.GetValue(name), out result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the ConfigNode contains the value and sotres it in the ref if it is parsable. Value is left unchanged if not.
        /// </summary>
        /// <param name="name">Name of the value to look for</param>
        /// <param name="value">Value to store the result in</param>
        public static bool TryGetValue(this ConfigNode node, string name, ref short value)
        {
            short result;
            if (node.HasValue(name) && short.TryParse(node.GetValue(name), out result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the ConfigNode contains the value and sotres it in the ref if it is parsable. Value is left unchanged if not.
        /// </summary>
        /// <param name="name">Name of the value to look for</param>
        /// <param name="value">Value to store the result in</param>
        public static bool TryGetValue(this ConfigNode node, string name, ref double value)
        {
            double result;
            if (node.HasValue(name) && double.TryParse(node.GetValue(name), out result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sees if the ConfigNode has a given value, and tries to store it in the ref if it's parseable
        /// </summary>
        /// <param name="name">Name of the value to get</param>
        /// <param name="value">Value to store the result in</param>
        public static bool TryGetValue(this ConfigNode node, string name, ref bool value)
        {
            bool result;
            if (node.HasValue(name) && bool.TryParse(node.GetValue(name), out result))
            {
                value = result;
                return true;
            }
            return false;
        }
        #endregion
    }
}
