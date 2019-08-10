open Akka.FSharp
open System

// the most basic configuration of remote actor system
let config = """
akka {
    actor {
        provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
    }
    remote.helios.tcp {
        transport-protocol = tcp
        port = 7000
        hostname = localhost
    }
}
"""

let configRemote = """
    akka {
        log-config-on-start = on
        stdout-loglevel = DEBUG
        loglevel = DEBUG

        actor.provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
        remote.helios.tcp {
            hostname = 10.211.55.2
            port = 9234
        }
    }"""

[<EntryPoint>]
let main _ =
    System.Console.Title <- "Remote: " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString()

    // remote system only listens for incoming connections
    // it will receive actor creation request from local-system (see: FSharp.Deploy.Local)
    use system = System.create "remote-system" (Configuration.parse config)

    Console.ForegroundColor <-
     ConsoleColor.Red

    printfn "Remote Actor %s listening..." system.Name

    System.Console.ReadLine() |> ignore
    0
