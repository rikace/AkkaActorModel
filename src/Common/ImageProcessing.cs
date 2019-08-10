using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageProcessing
{
    public static class ImageHandler
    {
        public enum ColorFilterTypes
        {
            Red,
            Green,
            Blue,
            Gray
        };

        public static Image<Rgba32> SetColorFilter(Image<Rgba32> image, ColorFilterTypes colorFilterType)
        {
            Image<Rgba32> bmap = (Image<Rgba32>)image.Clone();
            Rgba32 c;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    c = bmap[i, j];
                    int nPixelR = 0;
                    int nPixelG = 0;
                    int nPixelB = 0;
                    if (colorFilterType == ColorFilterTypes.Red)
                    {
                        nPixelR = c.R;
                        nPixelG = c.G - 255;
                        nPixelB = c.B - 255;
                    }
                    else if (colorFilterType == ColorFilterTypes.Green)
                    {
                        nPixelR = c.R - 255;
                        nPixelG = c.G;
                        nPixelB = c.B - 255;
                    }
                    else if (colorFilterType == ColorFilterTypes.Blue)
                    {
                        nPixelR = c.R - 255;
                        nPixelG = c.G - 255;
                        nPixelB = c.B;
                    }
                    else
                        return SetGrayscale(bmap);

                    nPixelR = Math.Max(nPixelR, 0);
                    nPixelR = Math.Min(255, nPixelR);

                    nPixelG = Math.Max(nPixelG, 0);
                    nPixelG = Math.Min(255, nPixelG);

                    nPixelB = Math.Max(nPixelB, 0);
                    nPixelB = Math.Min(255, nPixelB);

                    bmap[i, j] = new Rgba32((byte)nPixelR, (byte)nPixelG, (byte)nPixelB);
                }
            }
            return (Image<Rgba32>)bmap.Clone();
        }

        public static Image<Rgba32> SetGrayscale(Image<Rgba32> image)
        {
            Image<Rgba32> bmap = (Image<Rgba32>)image.Clone();
            Rgba32 c;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    c = bmap[i, j];
                    byte gray = (byte)(.299 * c.R + .587 * c.G + .114 * c.B);

                    bmap[i, j] = new Rgba32(gray, gray, gray);
                }
            }
            return (Image<Rgba32>)bmap.Clone();
        }


        public static Image<Rgba32> Resize(Image<Rgba32> image, int newWidth, int newHeight)
        {
            if (newWidth != 0 && newHeight != 0)
            {
                var temp = (Image<Rgba32>)image;
                var bmap = new Image<Rgba32>(newWidth, newHeight);

                double nWidthFactor = (double)temp.Width / (double)newWidth;
                double nHeightFactor = (double)temp.Height / (double)newHeight;

                double fx, fy, nx, ny;
                int cx, cy, fr_x, fr_y;
                var color1 = new Rgba32();
                var color2 = new Rgba32();
                var color3 = new Rgba32();
                var color4 = new Rgba32();
                byte nRed, nGreen, nBlue;

                byte bp1, bp2;

                for (int x = 0; x < bmap.Width; ++x)
                {
                    for (int y = 0; y < bmap.Height; ++y)
                    {

                        fr_x = (int)Math.Floor(x * nWidthFactor);
                        fr_y = (int)Math.Floor(y * nHeightFactor);
                        cx = fr_x + 1;
                        if (cx >= temp.Width) cx = fr_x;
                        cy = fr_y + 1;
                        if (cy >= temp.Height) cy = fr_y;
                        fx = x * nWidthFactor - fr_x;
                        fy = y * nHeightFactor - fr_y;
                        nx = 1.0 - fx;
                        ny = 1.0 - fy;

                        color1 = temp[fr_x, fr_y];
                        color2 = temp[cx, fr_y];
                        color3 = temp[fr_x, cy];
                        color4 = temp[cx, cy];

                        // Blue
                        bp1 = (byte)(nx * color1.B + fx * color2.B);

                        bp2 = (byte)(nx * color3.B + fx * color4.B);

                        nBlue = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

                        // Green
                        bp1 = (byte)(nx * color1.G + fx * color2.G);

                        bp2 = (byte)(nx * color3.G + fx * color4.G);

                        nGreen = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

                        // Red
                        bp1 = (byte)(nx * color1.R + fx * color2.R);

                        bp2 = (byte)(nx * color3.R + fx * color4.R);

                        nRed = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

                        bmap[x, y] = new Rgba32(255, nRed, nGreen, nBlue);
                    }
                }
                return (Image<Rgba32>)bmap.Clone();
            }
            return image;
        }
    }
}
