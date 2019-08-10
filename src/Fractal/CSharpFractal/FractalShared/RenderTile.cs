using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkaFractalShared
{
    public class RenderTile
    {
        public RenderTile(int x, int y, int height, int width)
        {
            X = x;
            Y = y;
            Height = height;
            Width = width;
        }


        public int X { get; }
        public int Y { get; }
        public int Height { get; }
        public int Width { get; }

    }
}
