namespace Akka.Client

module Actors =
    open System
    open Akka.FSharp
    open System.IO
    open Akka.Actor
        
  
    // create remote deployment configuration for actor system available under `actorPath`
    let remoteDeploy systemPath = 
        let address = 
            match ActorPath.TryParseAddress systemPath with
            | false, _ -> failwith "ActorPath address cannot be parsed"
            | true, a -> a
        Deploy(RemoteScope(address))
    let deployRemotely address = Deploy(RemoteScope (Address.Parse address))
    let spawnRemote systemOrContext remoteSystemAddress actorName expr =
        spawne systemOrContext actorName expr [SpawnOption.Deploy (deployRemotely remoteSystemAddress)]
         
    module RemoteKMeans =

        let kmExpr remoteSystemAddress (system : ActorSystem) = 
            spawnRemote system remoteSystemAddress "remote" 
             <@
                let rec nest (n:int) (f:float [] [] -> float [] []) (x:float[] []) =  
                    let f_x = f x
                    if f_x=x || n=0 then x else nest (n-1) f f_x 


                let random (data: float [] []) (k:int) =
                        let rand = System.Random().NextDouble
                        let gen (i:int) (d:float) =
                          let xs = data |> Seq.map (fun u -> u.[i])
                          let x = rand()
                          x * Seq.min xs + (1.0 - x) * Seq.max xs
                        Array.init k (fun _ -> Array.mapi gen data.[0]) 

                let kmeans (data: float [] []) (distance:float[]-> float [] -> float) (k:int) =
                        printfn "Processing kmeans"
                        let nearestCentroid centroids u = Array.minBy (distance u) centroids
                        let iteration (centroids: float [] []) =
                          [|for _, us in Seq.groupBy (nearestCentroid centroids) data do
                              let n = Seq.length us
                              if n <> 0 then
                                yield Array.mapi (fun i _ -> us |> Seq.averageBy (fun u -> u.[i])) data.[0]|]
                        random data k
                        |> nest 100 iteration
                        |> fun centroids -> Seq.groupBy (nearestCentroid centroids) data 

                let euclidean f1 f2 = //: (float[] -> float[] -> float) =
                        printfn "Processing euclidean"
                        Array.fold2 (fun x u v -> x + pown (u - v) 2) 0.0 f1 f2

                let clusters (irisData:seq<float[] * int>) : float[][][] =
                        let k = 3
                        printfn "Processing clusters"
                        // seq { while true do yield kmeans [|for u, o in irisData -> u|] euclidean k }
                        seq { for i = 0 to 10 do yield kmeans [|for u, o in irisData -> u|] euclidean k }
                        
                        |> Seq.find (fun xs -> Seq.length xs = k) 
                        |> Seq.map(snd >> Seq.toArray)
                        |> Seq.toArray
                    
                fun mailbox -> 
                let rec loop() : Cont<(float[] * int)[], unit> = 
                    actor { 
                        let! (msg:((float[] * int)[])) = mailbox.Receive()
                        printfn "remote address - %A" (mailbox.Sender().Path)
                        printfn "Received Data size : %d" (Seq.length msg)
                        let data = clusters msg
                        printfn "Remote actor sending data %A" msg
                        mailbox.Sender() <! data
                        printfn "Remote actor sent data"
                        return! loop()
                    }
                loop() @>  
        
        let irisData: Async<(float[] * int)[]> = async {
            use client = new System.Net.WebClient()
            let url = "http://archive.ics.uci.edu/ml/machine-learning-databases/iris/iris.data"
            let (|Float|_|) string = try Some(Float(float string)) with _ -> None
            let (|Species|_|) specy = 
              match specy with
              | "Iris-setosa" -> Some(Species 0)
              | "Iris-versicolor" -> Some(Species 1)
              | "Iris-virginica" -> Some(Species 2)
              | _ -> None
            let! data = client.DownloadStringTaskAsync url |> Async.AwaitTask
            return
                [| for line in data.Split[|'\n'|] do
                     match line.Split[|','|] with
                     | [|Float w; Float x; Float y; Float z; Species species|] -> yield [|w; x; y; z|], species
                     | _ -> () |] 
            }