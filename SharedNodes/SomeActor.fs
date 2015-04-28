namespace SharedNodes

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open Akka.FSharp
open Akka.Actor
open Akka.Remote
open Akka.Configuration


type SomeActor() =
     inherit Actor()

     override x.OnReceive(message) =
        let senderAddress = ``base``.Self.Path.ToStringWithAddress()
        let originalColor = Console.ForegroundColor

        Console.ForegroundColor <- if senderAddress.Contains("localactor") then 
                                      ConsoleColor.Red
                                   else ConsoleColor.Green  

        printfn "%s got %A" senderAddress message

        Console.ForegroundColor <- originalColor

