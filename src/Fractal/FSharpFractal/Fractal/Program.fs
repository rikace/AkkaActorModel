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

        let w = 4000
        let h = 4000

        let img = new Image<Rgba32>(w, h);

        let split = 20
        let ys = h / split
        let xs = w / split

        let render tile =
            let tileImage = BitmapConverter.toBitmap tile.Bytes
            let mutable xt = 0
            for x = tile.X to split - 1 do
                let mutable yt = 0
                for y = tile.Y to split - 1 do
                    printfn "Size X %d  y %d  width %d    len %d" x y (tile.X + tileImage.Width - 1 ) (tile.Y + tileImage.Height - 1 ) 
                    img.[x, y] <- tileImage.[x, y]
                    yt <- yt + 1
                xt <- xt + 1



        let displayTile =
            spawn system "display-tile" (fun mailbox ->
                let rec loop() =
                    actor {
                        let! (msg : RenderedTile) = mailbox.Receive()
                        render msg
                        return! loop()
                    }
                loop()) 

        let deployment = Deploy (RemoteScope (Address.Parse "akka.tcp://worker@127.0.0.1:8091/user/render"))
        let router = RoundRobinPool 16

        let actor = spawne system "render" <@ actorOf2 tileRenderer @>
                        [ SpawnOption.Deploy deployment; SpawnOption.Router router;
                          SpawnOption.SupervisorStrategy(Strategy.OneForOne(fun _ -> Directive.Restart)) ]


        for y = 0 to split do
            let yy = ys * y
            for x = 0 to split do
                let xx = xs * x
                actor.Tell({ X = yy; Y = xx; Height = ys; Width = xs; }, displayTile)

       
[<EntryPoint>]
let main args =
    let config =
        Configuration.parse """
            akka {
                log-config-on-start = on
                stdout-loglevel = DEBUG
                loglevel = ERROR
                actor {
                    provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
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

    use system = System.create "fractal" (config)
    
    Fractal.run system
    
    Console.ReadLine() |> ignore
    
    system.WhenTerminated.Wait()

    0
