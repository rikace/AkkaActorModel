using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkkaFractalShared;


namespace FractalShared
{
//    public class DisplayTileActor : ReceiveActor
//    {
//        private Action<RenderedTile> _renderer;
//
//        public DisplayTileActor(Action<RenderedTile> renderer)
//        {
//            _renderer = renderer;
//            Receive<RenderedTile>(tile => { _renderer(tile); });
//        }
//    }
    
    public class DisplayTileActor : ReceiveActor
    {
        private readonly Action<RenderedTile> _action;
        private readonly Action _completed;

        public DisplayTileActor(Action<RenderedTile> action, Action completed)
        {
            _action = action;
            _completed = completed;

            Receive<RenderedTile>(tile => _action(tile));
            Receive<Completed>(_ => _completed());
        }
    }
}
