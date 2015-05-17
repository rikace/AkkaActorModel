module Ask

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System
open System.IO


// Actor example with the ask function
// In the actor we use 'sender <!' to return the value.


let versionUrl = @"https://github.com/rikace/AkkaActorModel/blob/master/LICENSE.txt"

let system = ActorSystem.Create("Ask-System")


let fromUrlAsync (url:string) = async {
    use client = new System.Net.WebClient()
    let! response = client.AsyncDownloadString(Uri(url))
    return response }
    
let echoServer = 
    spawn system "ReadFileServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
               
               
                let sender = mailbox.Sender()
              
              
                match box message with
                | :? string as url -> 
                        // DO NOT RUN THIS CODE AT HOME!
                        async {
                            let! response = fromUrlAsync url
                            printfn "actor: done!"
                            
                            sender <! response } |> Async.Start 


                        return! loop() 
                | _ ->  failwith "unknown message"
            } 
        loop()


let (task:Async<string>) = (echoServer <? versionUrl)

let response = Async.RunSynchronously (task)
let siteSize = string(response) |> String.length

printfn "String size %d" siteSize

system.Shutdown()

