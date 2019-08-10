#if INTERACTIVE
#r "../../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "../../../packages/Akka.Remote/lib/netstandard1.6/Akka.Remote.dll"
#endif


open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp


// #Simplify Actor
// There is a simpler way to define an Actor

let system = ActorSystem.Create("FSharp")

let echoServer =
    spawn system "EchoServer"
    <| fun mailbox ->
            actor {
                let! message = mailbox.Receive()
                match box message with
                | :? string as msg -> printfn "Hello %s" msg
                | _ ->  failwith "unknown message"
            }

echoServer <! "F#!"

system.Dispose()
