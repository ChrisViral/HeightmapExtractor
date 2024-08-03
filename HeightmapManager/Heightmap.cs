using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

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

        #region Static fields
        /// <summary>
        /// The accepted extension formats for the heightmap binary files
        /// </summary>
        private static readonly string[] acceptedExtensions = { ".bin", ".raw", ".dat" };
        #endregion

        #region Properties
        /// <summary>
        /// Width in pixels of the heightmap
        /// </summary>
        public ushort Width { get; }

        /// <summary>
        /// Height in pixels of the heightmap
        /// </summary>
        public ushort Height { get; }

        /// <summary>
        /// Total size of the heightmap in pixels
        /// </summary>
        public int Size => this.Width * this.Height;

        /// <summary>
        /// Two dimensional array of all the values of the heightmap in (y, x) coordinates
        /// </summary>
        public short[,] Pixels { get; private set; }

        /// <summary>
        /// If true, high points are black and low points white, else high points are white and low points black
        /// </summary>
        public bool InvertColours { get; set; }
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
            get { return this.Pixels[y, x]; }
            set { this.Pixels[y, x] = value; }
        }

        /// <summary>
        /// Accesses the height value at a precise coordinates on the heightmap,
        /// where (0, 0) is the top left corner
        /// </summary>
        /// <param name="v">Two dimensional coordinates vector</param>
        public short this[Vector2 v]
        {
            get { return this.Pixels[(int)v.y, (int)v.x]; }
            set { this.Pixels[(int)v.y, (int)v.x] = value; }
        }

        /// <summary>
        /// Accesses the height value at a precise coordinates on the heightmap,
        /// where (0, 0) is the top left corner
        /// </summary>
        /// <param name="v">Two dimensional coordinates vector</param>
        public short this[Vector2d v]
        {
            get { return this.Pixels[(int)v.y, (int)v.x]; }
            set { this.Pixels[(int)v.y, (int)v.x] = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an empty heightmap object
        /// </summary>
        public Heightmap()
        {
            this.Width = 1;
            this.Height = 1;
            this.Pixels = new short[1, 1];
            this.InvertColours = true;
        }

        /// <summary>
        /// Create an empty Heightmap of the given size
        /// </summary>
        /// <param name="width">Width of the heightmap in pixels</param>
        /// <param name="height">Height of the heightmap in pixels</param>
        /// <param name="invertColours">Inverts the block/white gradient used for the image maps</param>
        public Heightmap(ushort width, ushort height, bool invertColours)
        {
            this.Width = width;
            this.Height = height;
            this.Pixels = new short[height, width];
            this.InvertColours = invertColours;
        }

        /// <summary>
        /// Creates a Heightmap from the given two dimensional array in (y, x) coordinates
        /// </summary>
        /// <param name="values">Two dimantional array of heights</param>
        /// <param name="invertColours">Inverts the block/white gradient used for the image maps</param>
        public Heightmap(short[,] values, bool invertColours)
        {
            this.Width = (ushort)values.GetLength(1);
            this.Height = (ushort)values.GetLength(0);
            this.Pixels = values;
            this.InvertColours = invertColours;
        }

        /// <summary>
        /// Creates a Heightmap from an single dimensional array and given size parameters
        /// </summary>
        /// <param name="values">Single dimensional indexed structure containing all the values</param>
        /// <param name="width">Width of the heightmap in pixels</param>
        /// <param name="height">Height of the heightmap in pixels</param>
        /// <param name="invertColours">Inverts the block/white gradient used for the image maps</param>
        public Heightmap(IList<short> values, ushort width, ushort height, bool invertColours)
        {
            this.Width = width;
            this.Height = height;
            this.Pixels = new short[height, width];
            this.InvertColours = invertColours;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    this.Pixels[y, x] = values[(y * width) + x];
                }
            }
        }

        /// <summary>
        /// Loads a heightmap from a given binary file
        /// </summary>
        /// <param name="path">Absolute path to the heightmap binary file</param>
        public Heightmap(string path)
        {
            if (path == null) { throw new ArgumentNullException(nameof(path), "The path cannot be null"); }
            if (!File.Exists(path)) { throw new FileNotFoundException("The following file was not found on the system.", path); }
            if (!acceptedExtensions.Contains(Path.GetExtension(path).ToLower())) { throw new NotSupportedException("Only .bin, .raw, and .dat formats supported."); }

            byte[] data = File.ReadAllBytes(path);
            this.Width = BitConverter.ToUInt16(data, 0);
            this.Height = BitConverter.ToUInt16(data, 2);
            this.InvertColours = true;

            if (data.LongLength != (2L * this.Width * this.Height) + 4) { throw new FormatException("Binary data is of incorrect lenght or incorrectly formatted."); }

            this.Pixels = new short[this.Height, this.Width];
            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    int i = (((y * this.Width) + x) * 2) + 4;
                    this.Pixels[y, x] = BitConverter.ToInt16(data, i);
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
        public void SetPixel(int x, int y, short value) => this.Pixels[y, x] = value;

        /// <summary>
        /// Sets all the pixels of the map according to the given single dimensional array
        /// </summary>
        /// <param name="values">Single dimensional array of heights to set the heightmap to</param>
        public void SetPixels(short[] values)
        {
            if (values.Length == this.Size)
            {
                for (int y = 0; y < this.Height; y++)
                {
                    for (int x = 0; x < this.Width; x++)
                    {
                        this.Pixels[y, x] = values[(y * this.Width) + x];
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
            if (values.GetLength(0) == this.Height && values.GetLength(1) == this.Width)
            {
                this.Pixels = values;
            }
        }

        /// <summary>
        /// Reads the pixel at the given coordinates
        /// </summary>
        /// <param name="x">X coordinates of the pixel to read</param>
        /// <param name="y">Y coordinate of the pixel to read</param>
        public short ReadPixel(int x, int y) => this.Pixels[y, x];

        /// <summary>
        /// Reads the pixels on the map through bilinear extrapolation
        /// </summary>
        /// <param name="x">X coordinate on the map (between 0 and 1)</param>
        /// <param name="y">Y coordinate on the map (between 0 and 1)</param>
        public double ReadPixelBilinear(double x, double y)
        {
            //Position on the map
            Vector2d pos = new Vector2d(MathUtils.Clamp01(x) * this.Width, MathUtils.Clamp01(y) * this.Height);
            //Decimal fraction between pixels
            Vector2d pPos = new Vector2d(pos.x - Math.Truncate(pos.x), pos.y - Math.Truncate(pos.y));
            if (pPos.magnitude == 0) { return this.Pixels[(int)pos.y, (int)pos.x]; }
            //Pixel positions
            int x1 = (int)Math.Floor(pos.x), x2 = (int)Math.Ceiling(pos.x);
            int y1 = (int)Math.Floor(pos.y), y2 = (int)Math.Ceiling(pos.y);
            //First linear interpolation on the x axis
            Vector2d xPos = new Vector2d(MathUtils.Lerp(pPos.x, this.Pixels[y1, x1], this.Pixels[y1, x2]), MathUtils.Lerp(pPos.x, this.Pixels[y2, x1], this.Pixels[y2, x2]));
            //Second linear interpolation between the first two axis
            return MathUtils.Lerp(pPos.y, xPos.x, xPos.y);
        }

        /// <summary>
        /// Returns a single dimensional array of all the pixels in the heightmap
        /// </summary>
        public short[] ReadPixels()
        {
            short[] values = new short[this.Size];
            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    values[(y * this.Width) + x] = this.Pixels[y, x];
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
            short[,] copy = new short[this.Height, this.Width];
            Array.Copy(this.Pixels, copy, this.Pixels.Length);
            return copy;
        }

        /// <summary>
        /// Returns a Color array representing the grayscale object
        /// </summary>
        public Color[] ToColorArray()
        {
            short[] values = ReadPixels();
            Color[] pixels = new Color[this.Size];
            int min = values.Min();
            int range = values.Max() - min;
            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    int index = (y * this.Width) + x;
                    float c = (values[index] - min) / (float)range;
                    if (this.InvertColours) { c = 1 - c; }
                    pixels[index] = new Color(c, c, c);
                }
            }
            return pixels;
        }

        /// <summary>
        /// Returns a Texture2D representing the grayscale object
        /// </summary>
        public Texture2D ToTexture2D()
        {
            Texture2D map = new(this.Width, this.Height, TextureFormat.RGB24, false);
            map.SetPixels(ToColorArray());
            map.Apply();
            return map;
        }

        /// <summary>
        /// Returns a byte array of all the values of the object with the width and height appeneded to the front
        /// </summary>
        public byte[] ToByteArray()
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(this.Width));
            data.AddRange(BitConverter.GetBytes(this.Height));
            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    data.AddRange(BitConverter.GetBytes(this.Pixels[y, x]));
                }
            }
            return data.ToArray();
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
                    SaveAsBinary(path); break;

                case SaveFormat.IMAGE:
                    SaveAsImage(path); break;

                case SaveFormat.BOTH:
                    SaveBoth(path); break;
            }
        }

        /// <summary>
        /// Saves the heightmap to a binary file
        /// </summary>
        /// <param name="path">Absolute path to save the heightmap to</param>
        private void SaveAsBinary(string path)
        {
            if (!Path.HasExtension(path)) { path += "_raw.bin"; }
            else if (!acceptedExtensions.Contains(Path.GetExtension(path)?.ToLower())) { path = Path.ChangeExtension(path, ".bin"); }
            File.WriteAllBytes(path, ToByteArray());
        }

        /// <summary>
        /// Saves the heightmap as a grayscale image
        /// </summary>
        /// <param name="path">Absolute path to save the heightmap to</param>
        private void SaveAsImage(string path)
        {
            if (!Path.HasExtension(path)) { path += ".png"; }
            if (Path.GetExtension(path).ToLower() != ".png") { path = Path.ChangeExtension(path, ".png"); }
            Texture2D map = ToTexture2D();
            File.WriteAllBytes(path, map.EncodeToPNG());
            Object.Destroy(map);
        }

        /// <summary>
        /// Saves the heightmap to both a grayscale image and a binary file
        /// </summary>
        /// <param name="path">Absolute path to save the heightmap to</param>
        private void SaveBoth(string path)
        {
            if (Path.HasExtension(path)) { path = Path.ChangeExtension(path, null); }
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(this.Width));
            data.AddRange(BitConverter.GetBytes(this.Height));
            short[] values = ReadPixels();
            Color[] pixels = new Color[this.Size];
            int min = values.Min();
            int range = values.Max() - min;
            for (int y = 0; y < this.Height; y++)
            {
                for (int x = 0; x < this.Width; x++)
                {
                    data.AddRange(BitConverter.GetBytes(this[x, y]));
                    int index = (y * this.Width) + x;
                    float shade = this.InvertColours ? 1 - ((values[index] - min) / (float)range) : (values[index] - min) / (float)range;
                    pixels[index] = new Color(shade, shade, shade);
                }
            }
            File.WriteAllBytes(path + "_raw.bin", data.ToArray());
            Texture2D map = new(this.Width, this.Height, TextureFormat.RGB24, false);
            map.SetPixels(pixels);
            map.Apply();
            File.WriteAllBytes(path + ".png", map.EncodeToPNG());
            Object.Destroy(map);
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        public override string ToString() => $"Heightmap: [Width: {this.Width}], [Height: {this.Height}], [Colours inverted: {this.InvertColours}]";
        #endregion
    }
}
