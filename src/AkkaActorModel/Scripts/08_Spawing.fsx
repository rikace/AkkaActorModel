#if INTERACTIVE
#r "../../../packages/Akka/lib/netstandard1.6/Akka.dll"
#r "../../../packages/Akka.FSharp/lib/netstandard2.0/Akka.FSharp.dll"
#r "../../../packages/Akka.Remote/lib/netstandard1.6/Akka.Remote.dll"
#endif


open Akka.FSharp
open Akka.Actor
open System

type Message =
    | Increment
    | Print

let system = System.create "example3" <| Configuration.load()

let spawn_printer system name =
    spawn system name <|
        fun mailbox ->
            let rec loop () =
                actor {
                    let! msg = mailbox.Receive ()
                    printfn "From Child : %s" msg
                    return! loop ()
                }
            loop ()

let actor = spawn system "parent" <| fun mailbox ->

        // Spawning a Child
        let child = spawn_printer mailbox "child"

        let rec loop () =
            actor {
                let! msg = mailbox.Receive ()
                printfn "From Parent : %s" msg
                child <! msg
                return! loop ()
            }
        loop ()

// Note -> The path of the child has the parent

// Note relation between child and parent in the Path
let child = system.ActorSelection("akka://example3/user/parent/child")

child <! "Hello from the child"

let parent = system.ActorSelection("akka://example3/user/parent")

parent <! "Hello from the parent"




