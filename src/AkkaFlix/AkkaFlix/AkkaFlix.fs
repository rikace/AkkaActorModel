(*
    AkkaFlix, a streaming company, needs a scalable backend system.

    ---

    Their backend needs to handle an event called "PlayEvent" that will occur every time one 
    of their users start watching a video asset. The Play-event carries two pieces of 
    information, the "User" (username) and the "Asset" (name of the video).
 
    The PlayEvent is rich in the sense that it can be used for many purposes, the company 
    requires that the system uses the event to perform a couple of business critical tasks:
 
    - Keeps track of how many people are streaming, for statistics.

    - Keeps track of what the individual user is watching, for use by the user interface
      "You are currently watching"-feature.

    - Keeps track of how many times the individual video assets have been streamed, 
      for reporting to content owners.
 
    ---

    We will handle this by creating a hierarchy of actors. At the base a "Player"-actor will 
    receive the event an send it on to two child-actors: "Users" and "Reporting". Users will 
    create a child-actor "User" for each user.
 
    - "User"          Keep track of what the individual user is watching
    - "Users"         Keeps track of how many are watching
    - "Reporting"     Keeps track of how many times assets have been watched
 
    As the events arrive they are tunnelled down through the hierarchy, and the model is 
    kept up-to-date.
 
    ---

    The data is "queried" by outputting the state to the console when it changes. The random 
    arrival of the text in the console illustrates the parallel nature of the actor model.
*)

namespace AkkaFlix

    open System
    open Akka.Actor
    open Akka.FSharp

    module AkkaFlix =

        // Prepare some test data
        let users = [| "Jack"; "Jill"; "Tom"; "Jane"; "Steven"; "Jackie" |]
        let assets = [| "The Sting"; "The Italian Job"; "Lock, Stock and Two Smoking Barrels"; "Inside Man"; "Ronin" |]

        let rnd = System.Random()

        // Send a Play-event with a randomly selected user and asset to the Player-actor.
        // Continue sending every time a key other than [Esc] is pressed.
        let rec loop player =
            
            player <! { User = users.[rnd.Next(users.Length)] ; Asset = assets.[rnd.Next(assets.Length)] }    

            match Console.ReadKey().Key with
            | ConsoleKey.Escape -> ()
            | _ -> loop player

        // Create an actor system and the root Player-actor and pass it to the
        // input loop to start sending Play-events to it.
        [<EntryPoint>]
        let main argv =    
            let system = System.create "akkaflix" (Configuration.load())
            let player = system.ActorOf(Props(typedefof<Player>, Array.empty))
    
            loop player

            system.Shutdown()
            0
