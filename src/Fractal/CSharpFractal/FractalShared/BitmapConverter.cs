using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace AkkaFractalShared
{
    public static class BitmapConverter
    {
        public static byte[] ToByteArray(this Image<Rgba32> imageIn)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                imageIn.SaveAsPng(ms);
                return ms.ToArray();
            }
        }

        public static Image<Rgba32> ToBitmap(this byte[] byteArrayIn)
        {
            using (MemoryStream ms = new MemoryStream(byteArrayIn))
            {
                var returnImage = Image.Load(byteArrayIn);
                return returnImage;
            }
        }
    }
}
