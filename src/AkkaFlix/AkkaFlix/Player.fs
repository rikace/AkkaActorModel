namespace AkkaFlix

    open Akka.Actor
    open Akka.FSharp

    // The Player-actor is the root actor that receives Play-events
    // and delegate them on to child-actors "Users" and "Reporting".
    type Player() = 
        inherit Actor()

        // Init child actors
        let player = Player.Context.ActorOf(Props(typedefof<Users>, Array.empty))
        let reporting = Player.Context.ActorOf(Props(typedefof<Reporting>, Array.empty))
    
        // Pass the Play-events on to child actors
        let notify event =
            player <! event
            reporting <! event

        // Incoming message handler
        override x.OnReceive message =
            match message with
            | :? PlayEvent as event -> notify event
            | _ ->  failwith "Unknown message"