namespace AkkaFlix

    open Akka.FSharp
   
    // Every user has a User-actor keeping track of what they
    // they are watching at any time.
    type User(user) =
        inherit Actor()

        let user = user
        let mutable watching = null

        // Incoming message handler
        override x.OnReceive message =
            match message with
            | :? string as asset -> 
                watching <- asset
                printfn "%s is watching %s" user watching
            | _ ->  failwith "Unknown message"