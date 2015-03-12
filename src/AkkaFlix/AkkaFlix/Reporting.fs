namespace AkkaFlix

    open System.Collections.Generic
    open Akka.FSharp

    // Reporting receives Play-events and keeps a counter for views
    // of each unique asset to report how many times the assets have been watched.
    type Reporting() =
        inherit Actor()
    
        let counters = new Dictionary<string, int>();

        // Increment the view count for an asset, or create a new
        // counter if the asset is viewed for the first time.
        let registerView asset =
            match counters.ContainsKey(asset) with
            | true -> counters.[asset] <- counters.[asset] + 1
            | false -> counters.Add(asset, 1)

        let printReport h =
            h
            |> Seq.sortBy (fun (KeyValue(k, v)) -> -v) 
            |> Seq.iter (fun (KeyValue(k, v)) -> printfn "%d\t%s" v k)
        
        // Incoming message handler
        override x.OnReceive message =
            match message with
            | :? PlayEvent as event -> 
                registerView event.Asset
                printReport counters
            | _ ->  failwith "Unknown message"