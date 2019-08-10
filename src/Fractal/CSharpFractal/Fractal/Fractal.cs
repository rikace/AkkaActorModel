using System;
using Akka.Actor;
using Akka.Routing;
using Akka;
using Akka.Configuration;
using AkkaFractalShared;
using FractalShared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AkkaFractal
{
    public static class Fractal
    {
        public static void Run(ActorSystem system)
        {
            var destination = @"./image.jpg";
            var w = 8000;
            var h = 8000;

            var img = new Image<Rgba32>(w, h);

            var split = 80;
            var ys = h / split;
            var xs = w / split;

            Action completed = () =>
            {
                img.Save(destination);
                Console.WriteLine("Tile render completed");
            };

            Action<RenderedTile> renderer = tile =>
            {
                var tileImage = tile.Bytes.ToBitmap();
                var xt = 0;
                for (int x = 0; x < xs; x++)
                {
                    int yt = 0;
                    for (int y = 0; y < ys; y++)
                    {
                        img[x + tile.X, y + tile.Y] = tileImage[x, y];
                        yt++;
                    }

                    xt++;
                }
            };

            // TODO
            // Complete the "displayTile" actor that uses the "render" lambda 
            // to generate and persist the image tiles

            // TODO
            // Complete the "displayTile" actor that uses the "render" lambda 
            // to generate and persist the image tiles.
            // This require to implement a new "DisplayTileActor" that uses the 
            // "renderer" as behavior
            //var displayTile = system.ActorOf(Props.Create<Displayctor>(), "display");


            //var displayTile = Akka.Actor.Nobody.Instance; 

            // TODO
            // increase the parallelism of the Actor "TileRenderActor"
            // var actor = system.ActorOf(Props.Create<TileRenderActor>(), "render");

            // .WithDispatcher("akka.actor.synchronized-dispatcher")
            var displayTile =
                system.ActorOf(
                    Props.Create(() => new DisplayTileActor(renderer, completed)), "display-tile");

            var actor = system.ActorOf(Props.Create<TileRenderActor>().WithRouter(FromConfig.Instance), "render");


            for (int y = 0; y < split; y++)
            {
                var yy = ys * y;
                for (int x = 0; x < split; x++)
                {
                    var xx = xs * x;
                    actor.Tell(new RenderTile(yy, xx, xs, ys), displayTile);
                }
            }

            actor.Tell(new Completed(), displayTile);

            Console.WriteLine("Tile render completed");
            Console.ReadLine();
        }
    }
}
