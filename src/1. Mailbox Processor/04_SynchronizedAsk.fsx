#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open System.Threading.Tasks
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

// #Synchronized Return
// Actors are very suitable for long-running operations, like getting resources over a network.
//
// This example creates a Task with the ask function.
//
// In the actor we use 'sender <!' to return the value.
//
// #Asynchronous Return
// Asynchronous operations can provide better performance. 
// A Task in F# is very powerful, it can execute asynchronously.
// It can also set a in milliseconds to wait for the result of the computation 
// before raising a `TimeoutException`.

let versionUrl = @"https://github.com/rikace/AkkaActorModel/blob/master/LICENSE.txt"

let system = System.create "FSharp" <| Configuration.load()

let fromUrl (url:string) =
    use client = new System.Net.WebClient()
    let response = client.DownloadString(url)
    response

let echoServer = 
    spawn system "EchoServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
               
                let sender = mailbox.Sender()
               
                match box message with
                | :? string as url -> 
                    let response = fromUrl url
                    printfn "actor: done!"
                   
                    sender <! response
                   
                    return! loop()
                | _ ->  failwith "unknown message"
            } 
        loop()




for timeout in [10; 100; 250; 2500] do
    try
                    // echoServer.Ask
        let (task:Async<obj>) = (echoServer <? versionUrl) // <? infix operator 

        let response = Async.RunSynchronously (task, timeout)
        let responseLength = string(response) |> String.length

        printfn "response: result has %d bytes" responseLength
    with :? TimeoutException ->
        printfn "ask: timeout!"

system.Shutdown()