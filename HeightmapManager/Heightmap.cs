using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

/* HeightmapExtractor was made by Christophe Savard (stupid_chris), and is licensed under
 * CC-BY-NC-SA 4.0 INTL. If you have any questions about this, please contact me on the forums. */

namespace HeightmapManager
{
    /// <summary>
    /// A class to create and manipulate heightmaps, load and save them to raw binary data or save them to 8bpp grayscale .png
    /// </summary>
    public class Heightmap
    {
        /// <summary>
        /// Different data saving options
        /// </summary>
        public enum SaveFormat
        {
            /// <summary>
            /// Grayscale image only
            /// </summary>
            IMAGE,

            /// <summary>
            /// Binary file only
            /// </summary>
            BINARY,

            /// <summary>
            /// Both grayscale image and binary file
            /// </summary>
            BOTH
        }

        #region Arrays
        private string[] acceptedExtensions = { ".bin", ".raw", ".dat" };
        #endregion

        #region Properties
        private readonly ushort _width;
        /// <summary>
        /// Width in pixels of the heightmap
        /// </summary>
        public ushort width
        {
            get { return this._width; }
        }

        private readonly ushort _height;
        /// <summary>
        /// Height in pixels of the heightmap
        /// </summary>
        public ushort height
        {
            get { return this._height; }
        }

        /// <summary>
        /// Total size of the heightmap in pixels
        /// </summary>
        public int size
        {
            get { return this._width * this._height; }
        }

        /// <summary>
        /// Two dimensional array of all the values of the heightmap in (y, x) coordinates
        /// </summary>
        public short[,] pixels { get; private set; }
        #endregion

        #region Indexers
        /// <summary>
        /// Accesses the height value at a precise coordinates on the heightmap,
        /// where (0, 0) is the top left corner
        /// </summary>
        /// <param name="x">X coordinate of the pixel to access</param>
        /// <param name="y">Y coordinate of the pixel to access</param>
        public short this[int x, int y]
        {
            get { return this.pixels[y, x]; }
            set { this.pixels[y, x] = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create an empty Heightmap of the given size
        /// </summary>
        /// <param name="width">Width of the heightmap in pixels</param>
        /// <param name="height">Height of the heightmap in pixels</param>
        public Heightmap(int width, int height)
        {
            this._width = (ushort)width;
            this._height = (ushort)height;
            this.pixels = new short[height, width];
        }

        /// <summary>
        /// Creates a Heightmap from the given two dimensional array in (y, x) coordinates
        /// </summary>
        /// <param name="values">Two dimantional array of heights</param>
        public Heightmap(short[,] values)
        {
            this._width = (ushort)values.GetLength(1);
            this._height = (ushort)values.GetLength(0);
            this.pixels = values;
        }

        /// <summary>
        /// Creates a Heightmap from an single dimensional array and given size parameters
        /// </summary>
        /// <param name="values">Single dimensional array of heights</param>
        /// <param name="width">Width of the heightmap in pixels</param>
        /// <param name="height">Height of the heightmap in pixels</param>
        public Heightmap(short[] values, int width, int height)
        {
            this._width = (ushort)width;
            this._height = (ushort)height;
            this.pixels = new short[this._height, this._width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    this.pixels[y, x] = values[(y * width) + x];
                }
            }
        }

        /// <summary>
        /// Loads a heightmap from a given binary file
        /// </summary>
        /// <param name="path">Absolute path to the heightmap binary file</param>
        public Heightmap(string path)
        {
            if (!this.acceptedExtensions.Contains(Path.GetExtension(path).ToLower())) { throw new NotSupportedException("Only .bin, .raw, and .dat format supported."); }

            byte[] data = File.ReadAllBytes(path);
            this._width = BitConverter.ToUInt16(data, 0);
            this._height = BitConverter.ToUInt16(data, 2);

            if (data.Length != ((this._width * this._height * 2) + 4)) { throw new InvalidDataException("Binary data is of incorrect lenght or incorrectly formatted."); }

            this.pixels = new short[this._height, this._width];
            for (int y = 0; y < this._height; y++)
            {
                for (int x = 0; x < this._width; x++)
                {
                    int i = (((y * this._width) + x) * 2) + 4;
                    this.pixels[y, x] = BitConverter.ToInt16(data, i);
                }
            }

        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the given pixel to the appointed value
        /// </summary>
        /// <param name="x">X coordinate of the pixel to modify</param>
        /// <param name="y">Y coordinate of the pixel to modify</param>
        /// <param name="value">Value to set the pixel to</param>
        public void SetPixel(int x, int y, short value)
        {
            this.pixels[y, x] = value;
        }

        /// <summary>
        /// Sets all the pixels of the map according to the given single dimensional array
        /// </summary>
        /// <param name="values">Single dimensional array of heights to set the heightmap to</param>
        public void SetPixels(short[] values)
        {
            if (values.Length == this.size)
            {
                for (int y = 0; y < this._height; y++)
                {
                    for (int x = 0; x < this._width; x++)
                    {
                        this.pixels[y, x] = values[(y * this._width) + x];
                    }
                }
            }
        }

        /// <summary>
        /// Sets all the pixels of the map according to the given two dimensional array where the first parameter is height and second lenght
        /// </summary>
        /// <param name="values">Two dimensional array of heights to set the heightmap to</param>
        public void SetPixels(short[,] values)
        {
            if (values.GetLength(0) == this._height && values.GetLength(1) == this._width)
            {
                this.pixels = values;
            }
        }

        /// <summary>
        /// Reads the pixel at the given coordinates
        /// </summary>
        /// <param name="x">X coordinates of the pixel to read</param>
        /// <param name="y">Y coordinate of the pixel to read</param>
        public short ReadPixel(int x, int y)
        {
            return this.pixels[y, x];
        }

        /// <summary>
        /// Returns a single dimensional array of all the pixels in the heightmap
        /// </summary>
        public short[] ReadPixels()
        {
            short[] values = new short[this.size];
            for (int y = 0; y < this._height; y++)
            {
                for (int x = 0; x < this._width; x++)
                {
                    values[(y * this._width) + x] = this.pixels[y, x];
                }
            }
            return values;
        }
    
        /// <summary>
        /// Returns a single dimensional array of all the pixels in the heightmap,
        /// where the first parameter is the height and the second the lenght
        /// </summary>
        public short[,] ReadPixels2D()
        {
            return this.pixels;
        }
        #endregion

        #region Save
        /// <summary>
        /// Saves the heightmap to the disk
        /// </summary>
        /// <param name="path">Absolute path to save the heightmap to</param>
        /// <param name="format">Which formats to save the heightmap to</param>
        public void Save(string path, SaveFormat format)
        {
            switch (format)
            {
                case SaveFormat.BINARY:
                    SaveAsBinary(path + "_raw.bin"); break;

                case SaveFormat.IMAGE:
                    SaveAsImage(path + ".png"); break;

                case SaveFormat.BOTH:
                    SaveBoth(path); break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Saves the heightmap to a binary file
        /// </summary>
        /// <param name="path">Absolute path to save the heightmap to</param>
        private void SaveAsBinary(string path)
        {
            if (!Path.HasExtension(path) || !this.acceptedExtensions.Contains(Path.GetExtension(path).ToLower())) { Path.ChangeExtension(path, ".bin"); }
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(this._width));
            data.AddRange(BitConverter.GetBytes(this._height));
            for (int y = 0; y < this._height; y++)
            {
                for (int x = 0; x < this._width; x++)
                {
                    data.AddRange(BitConverter.GetBytes(this.pixels[y, x]));
                }
            }
            File.WriteAllBytes(path, data.ToArray());
        }

        /// <summary>
        /// Saves the heightmap as a grayscale image
        /// </summary>
        /// <param name="path">Absolute path to save the heightmap to</param>
        private void SaveAsImage(string path)
        {
            if (!Path.HasExtension(path) || Path.GetExtension(path).ToLower() != ".png") { Path.ChangeExtension(path, ".png"); }
            short[] values = ReadPixels();
            Color[] pixels = new Color[this.size];
            short min = values.Min();
            int range = values.Max() - min;
            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {
                    int index = (y * this.width) + x;
                    float shade = 1f - (((float)(values[index] - min)) / (float)range);
                    pixels[index] = new Color(shade, shade, shade);
                }
            }
            Texture2D map = new Texture2D(this._width, this._height, TextureFormat.ARGB32, false);
            map.SetPixels(pixels);
            map.Apply();
            map.Compress(true);
            File.WriteAllBytes(path, map.EncodeToPNG());
            Texture2D.Destroy(map);
        }

        /// <summary>
        /// Saves the heightmap to both a grayscale image and a binary file
        /// </summary>
        /// <param name="path">Absolute path to save the heightmap to</param>
        private void SaveBoth(string path)
        {
            if (Path.HasExtension(path)) { Path.ChangeExtension(path, null); }
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(this._width));
            data.AddRange(BitConverter.GetBytes(this._height));
            short[] values = ReadPixels();
            Color[] pixels = new Color[this.size];
            short min = values.Min();
            int range = values.Max() - min;
            for (int y = 0; y < this._height; y++)
            {
                for (int x = 0; x < this._width; x++)
                {
                    data.AddRange(BitConverter.GetBytes(this[x, y]));
                    int index = (y * this.width) + x;
                    float shade = 1f - (((float)(values[index] - min)) / (float)range);
                    pixels[index] = new Color(shade, shade, shade);
                }
            }
            File.WriteAllBytes(path + "_raw.bin", data.ToArray());
            Texture2D map = new Texture2D(this._width, this._height, TextureFormat.ARGB32, false);
            map.SetPixels(pixels);
            map.Apply();
            File.WriteAllBytes(path + ".png", map.EncodeToPNG());
            Texture2D.Destroy(map);
        }
        #endregion
    }
}
