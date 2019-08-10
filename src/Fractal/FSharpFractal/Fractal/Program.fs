open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.IO
open Akka
open Akka.Configuration
open Akka.FSharp
open Akka.Actor
open Akka.Routing
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
open Fractal.Shared

module Fractal =
    let run (system: ActorSystem) =

        let w = 8000
        let h = 8000

        let img = new Image<Rgba32>(w, h);

        let split = 80
        let ys = h / split
        let xs = w / split

        let render tile =
            let tileImage = BitmapConverter.toBitmap tile.Bytes
            let mutable xt = 0
            for x = 0 to xs - 1 do
                let mutable yt = 0
                for y = 0 to ys - 1 do
                    img.[x + tile.X, y + tile.Y] <- tileImage.[x, y]
                    yt <- yt + 1
                xt <- xt + 1



        let displayTile =
            spawnOpt system "display-tile" (fun mailbox ->
                let rec loop() =
                    actor {
                        let! (msg : RenderedTile) = mailbox.Receive()
                        render msg
                        return! loop()
                    }
                loop()) [ SpawnOption.Dispatcher "akka.actor.synchronized-dispatcher" ]

    //        let displayTile =
    //            spawnOpt system "display-tile" (fun mailbox ->
    //                let rec loop() =
    //                    actor {
    //                        let! (bytes, x, y) = mailbox.Receive()
    //                        renderer bytes x y
    //                        return! loop()
    //                    }
    //                loop()) [ SpawnOption.Dispatcher "akka.actor.synchronized-dispatcher" ]

    //        let actor = system.ActorOf<TileRenderActor>("render")
    //
    //        let actor1 = system.ActorOf<TileRenderActor>("render1")
    //        let actor2 = system.ActorOf<TileRenderActor>("render2")
    //        let actor3 = system.ActorOf<TileRenderActor>("render3")
    //        let actor4 = system.ActorOf<TileRenderActor>("render4")
    //
    //        let actor = system.ActorOf(Props.Empty.WithRouter(new RoundRobinGroup([|actor1; actor2; actor3; actor4|])))

        let deployment = Deploy (RemoteScope (Address.Parse "akka.tcp://worker@127.0.0.1:8091/user/render"))
        let router = RoundRobinPool 16

        let actor = spawne system "render" <@ actorOf2 tileRenderer @>
                        [ SpawnOption.Deploy deployment; SpawnOption.Router router;
                          SpawnOption.SupervisorStrategy(Strategy.OneForOne(fun _ -> Directive.Restart)) ]

    //        let actor =
    //            spawne system "render"
    //            <| <@ actorOf2(fun mailbox msg ->
    //                    match msg with
    //                    | x, y, w, h ->
    //                        logInfof mailbox "%A rendering %d , %d" mailbox.Self x y
    //
    //                        let res = Mandelbrot.Set(x, y, w, h, 4000, 4000, 0.5, -2.5, 1.5, -1.5)
    //
    //                        use mem = new MemoryStream()
    //                        res.Save(mem, System.Drawing.Imaging.ImageFormat.Png)
    //                        mailbox.Sender() <! (mem.ToArray(), x, y)
    //                        mem.Close()
    //
    //                    | _ -> mailbox.Unhandled msg) @>
    //            <| [ SpawnOption.Deploy deployment; SpawnOption.Router router ]

            // MessageBox.Show(form, "Click ok to Start") |> ignore

        for y = 0 to split do
            let yy = ys * y
            for x = 0 to split do
                let xx = xs * x
                actor.Tell({ X = yy; Y = xx; Height = xs; Width = ys; }, displayTile)

[<EntryPoint>]
let main args =
    let config =
        Configuration.parse """
            akka {
                log-config-on-start = on
                stdout-loglevel = DEBUG
                loglevel = ERROR
                actor {
                    provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                    debug {
                      receive = on
                      autoreceive = on
                      lifecycle = on
                      event-stream = on
                      unhandled = on
                    }
                    deployment {
                        /render {
                            router = round-robin-pool
                            nr-of-instances = 16
                        }
                    }
                }
                remote {
                    helios.tcp {
                        transport-class = ""Akka.Remote.Transport.Helios.HeliosTcpTransport, Akka.Remote""
		                applied-adapters = []
		                transport-protocol = tcp
		                port = 0
		                hostname = localhost
                    }
                }
            }

            """

    let system = System.create "fractal" (config)


    0
