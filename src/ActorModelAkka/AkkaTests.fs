module AkkaTests

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka
open Akka.FSharp
open Akka.Actor
open System


type IO<'msg> = | Input

type Cont<'m> =
    | Func of ('m -> Cont<'m>)

module Cont =
    let raw (Func f) = f
    let eff f m = m |> raw f

type ActorBuilder() =
    member this.Bind(m : IO<'msg>, f :'msg -> _) = 
        Func (fun m -> f m)
    member this.ReturnFrom(x) = x
    
    member this.Zero() = fun () -> ()

let actor = ActorBuilder()

type FunActor<'m>(actor: IO<'m> -> Cont<'m>) =
    inherit Actor()
    
    let mutable state = actor Input

    override x.OnReceive(msg) =
        let message = msg :?> 'm
        state <- Cont.eff state message



module Actor =
    let system name =
        ActorSystem.Create(name)

    let spawn (system:ActorSystem) (f: (IO<'m> -> Cont<'m>))  =
       system.ActorOf(Props(Deploy.Local, typeof<FunActor<'m>>, [f]))
      

let system = Actor.system "Actors"


type Message =
    | Inc of int
    | Dec of int
    | Stop

let a = 
    Actor.spawn system
    <| fun recv ->
        let rec loop s =
            actor {
                let! msg = recv
                printfn "%d" s
                match msg with
                | Inc n ->
                     return! loop (s + n)
                | Dec n -> 
                    return! loop (s - n)
                | Stop -> return! stop ()
            }
        and stop () = actor {
            let! _ = recv
            printfn "I'm stopped"
            return! stop()
            }
        loop 0

[0..10] |> List.iter(fun _ -> a <! Inc 2)
[0..10] |> List.iter (fun _ -> a <! Dec 1)
a <! Stop
[0..10] |> List.iter (fun _ -> a <! Inc 1)

