﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRGB.NET.Models
{
    /// <summary>
    /// Color class containing three values for red, green and blue.
    /// </summary>
    public class Color : IEquatable<Color>
    {
        /// <summary>
        /// Red value of the color.
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// Green value of the color.
        /// </summary>
        public byte G { get; set; }

        /// <summary>
        /// Blue value of the color.
        /// </summary>
        public byte B { get; set; }

        /// <summary>
        /// Constructs a new color.
        /// </summary>
        public Color() {
            R = 0;
            G = 0;
            B = 0;
        }

        /// <summary>
        /// Constructs a new color with the specified red, green, and blue values.
        /// </summary>
        public Color(byte red = 0, byte green = 0, byte blue = 0)
        {
            R = red;
            G = green;
            B = blue;
        }

        /// <summary>
        /// Method used to create a color from HSV values.
        /// </summary>
        /// <param name="hue">Hue ranges from 0 to 360, input range wraps automatically.</param>
        /// <param name="saturation">Ranges from 0.0 to 1.0.</param>
        /// <param name="value">Ranges from 0.0 to 1.0.</param>
        /// <returns>The color converted to RGB.</returns>
        public static Color FromHsv(double hue, double saturation, double value)
        {
            if (saturation < 0 || saturation > 1)
                throw new ArgumentOutOfRangeException(nameof(saturation));
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(nameof(value));

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            if (hi < 0)
            {
                hi += 6;
            }
            double f = (hue / 60) - Math.Floor(hue / 60);

            value *= 255;
            var v = Convert.ToByte(value);
            var p = Convert.ToByte(value * (1 - saturation));
            var q = Convert.ToByte(value * (1 - (f * saturation)));
            var t = Convert.ToByte(value * (1 - ((1 - f) * saturation)));

            switch (hi)
            {
                case 0:
                    return new Color(v, t, p);
                case 1:
                    return new Color(q, v, p);
                case 2:
                    return new Color(p, v, t);
                case 3:
                    return new Color(p, q, v);
                case 4:
                    return new Color(t, p, v);
                default:
                    return new Color(v, p, q);
            }
        }

        /// <summary>
        /// Converts a color to HSV.
        /// </summary>
        /// <returns>Tuple with the HSV values.</returns>
        public (double h, double s, double v) ToHsv()
        {
            var max = Math.Max(R, Math.Max(G, B));
            var min = Math.Min(R, Math.Min(G, B));

            var delta = max - min;

            var hue = 0d;
            if (delta != 0)
            {
                if (R == max) hue = (G - B) / (double)delta;
                else if (G == max) hue = 2d + ((B - R) / (double)delta);
                else if (B == max) hue = 4d + ((R - G) / (double)delta);
            }

            hue *= 60;
            if (hue < 0.0) hue += 360;

            var saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            var value = max / 255d;

            return (hue, saturation, value);
        }

        /// <summary>
        /// Decodes a byte array into a color array.
        /// Increments the offset accordingly.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="colorCount"></param>
        /// <returns>An array of Colors decoded from bytes.</returns>
        internal static Color[] Decode(BinaryReader reader, ushort colorCount)
        {
            var colors = new Color[colorCount];

            for (int i = 0; i < colorCount; i++)
            {
                colors[i] = new Color
                {
                    R = reader.ReadByte(),
                    G = reader.ReadByte(),
                    B = reader.ReadByte()
                    //Alpha = reader.ReadByte()
                };
                var unusedAlpha = reader.ReadByte();
            }
            return colors;
        }

        /// <summary>
        /// Encodes a color into a 4 byte array
        /// </summary>
        internal byte[] Encode()
        {
            return new byte[]
            {
                R,
                G,
                B,
                0
            };
        }

        internal void CopyTo(byte[] array, int index)
        {
            array[index + 0] = R;
            array[index + 1] = G;
            array[index + 2] = B;
            array[index + 3] = 0;
        }

        /// <summary>
        /// Generates a smooth rainbow with the given amount of colors.
        /// Uses HSV conversion to get a hue-based rainbow.
        /// </summary>
        /// <param name="amount">How many colors to generate.</param>
        /// <param name="hueStart">The hue of the first color, 0 to 360.</param>
        /// <param name="huePercent">How much of the hue scale to use. 1.0 represents the full range.</param>
        /// <param name="saturation">The HSV saturation of the colors, 0.0 to 1.0.</param>
        /// <param name="value">The HSV value of the colors, 0.0 to 1.0.</param>
        /// <returns>An collection of Colors in a rainbow pattern.</returns>
        public static IEnumerable<Color> GetHueRainbow(int amount, double hueStart = 0, double huePercent = 1.0,
                                                                double saturation = 1.0, double value = 1.0) =>
            Enumerable.Range(0, amount)
                      .Select(i => FromHsv(hueStart + (360.0d * huePercent / amount * i), saturation, value));

        /// <summary>
        /// Generates a smooth rainbow with the given amount of colors.
        /// Uses sine waves to generate the pattern.
        /// </summary>
        /// <param name="amount">How many colors to generate.</param>
        /// <param name="floor">The least bright any given RGB value can be.</param>
        /// <param name="width">The brightness variation of any given RGB value.</param>
        /// <param name="range">How much of the sine wave is used to generate the colors. Decrese this value to get a fraction of the spectrum. In percent.</param>
        /// <param name="offset">The value the first color of the sequence will be generated with.</param>
        /// <returns>A collection of Colors in a rainbow pattern.</returns>
        public static IEnumerable<Color> GetSinRainbow(int amount, int floor = 127, int width = 128, double range = 1.0, double offset = Math.PI / 2) =>
            Enumerable.Range(0, amount)
                      .Select(i => new Color(
                            (byte)(floor + width * Math.Sin(offset + (2 * Math.PI * range) / amount * i + 0)),
                            (byte)(floor + width * Math.Sin(offset + (2 * Math.PI * range) / amount * i + (2 * Math.PI / 3))),
                            (byte)(floor + width * Math.Sin(offset + (2 * Math.PI * range) / amount * i + (4 * Math.PI / 3)))
                            ));

        /// <summary>
        /// Returns a new object with the same color value
        /// </summary>
        public Color Clone() => new Color(R, G, B);

        ///<inheritdoc/>
        public override string ToString() => $"R:{R}, G:{G}, B:{B} ";

        ///<inheritdoc/>
        public bool Equals(Color other) =>
            this.R == other.R &&
            this.G == other.G &&
            this.B == other.B;
        
        /// <summary>
        /// Gets a color between <paramref name="a"/> and <paramref name="b"/> using the value <paramref name="t"/>.
        /// </summary>
        /// <param name="a">First color</param>
        /// <param name="b">Second Color</param>
        /// <param name="t">Linear interpolation value, typically between 0 and 1.</param>
        /// <returns>The new color.</returns>
        public static Color Lerp(Color a, Color b, float t) =>
            new Color(
                (byte)(a.R + ((b.R - a.R) * t)),
                (byte)(a.G + ((b.G - a.G) * t)),
                (byte)(a.B + ((b.B - a.B) * t))
            );

        /// <summary>
        /// Gets the color complement of <paramref name="c"/>, where <paramref name="c"/> and the result added
        /// together will be white (R=G=B=255). In other words, the result will have hue rotated 180 degrees
        /// with the same saturation.
        /// </summary>
        /// <param name="c">The color.</param>
        /// <returns>The complement to <paramref name="c"/>.</returns>
        public static Color Complement(Color c) =>
            new Color(
                (byte)(255 - c.R),
                (byte)(255 - c.G),
                (byte)(255 - c.B)
            );
    }
}
