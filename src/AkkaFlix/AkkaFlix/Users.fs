namespace AkkaFlix

    open Akka
    open Akka.Actor
    open Akka.FSharp
    open System.Collections.Generic

    // The Users-actor receives Play-events and sends a message to
    // the specific User-actor to keep it up to date.
    type Users() = 
        inherit Actor()
    
        let context = Users.Context
        let users = new Dictionary<string, IActorRef>();
    
        // Return the User-actor identified by username
        // If none is found, create a new User-actor
        let rec findOrSpawn username =
            match users.ContainsKey(username) with
            | true -> users.[username]
            | false ->
                users.Add(username, context.ActorOf(Props(typedefof<User>, [| username :> obj |])))
                findOrSpawn username

        // Get the actor for a specific user and message the asset to it
        let updateUser user asset =
            (findOrSpawn user) <! asset

        // Incoming message handler
        override x.OnReceive message =
            match message with
            | :? PlayEvent as event -> 
                updateUser event.User event.Asset
                printfn "Unique users: %d" users.Count 
            | _ ->  failwith "Unknown message"