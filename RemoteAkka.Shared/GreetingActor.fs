[<AutoOpenAttribute>]
module GreetingModule

open Akka
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration
open System
open Akka.Configuration


type Greet(who:string) =
    member x.Who = who

//[<CLIMutableAttribute>]
//type Greet = 
//    { Who : string }

type GreetingActor() as g = 
    inherit ReceiveActor()
    do 
        
        g.Receive<Greet>(fun (greet : Greet) -> 
            Console.ForegroundColor <- ConsoleColor.Red
            printfn "Hello %s" greet.Who)

