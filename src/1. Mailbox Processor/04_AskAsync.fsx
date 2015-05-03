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

let pipeTo receipient computation =
    Async.StartAsTask(computation).PipeTo(receipient)

let pipeFromTo sender receipient computation = 
    Async.StartAsTask(computation).PipeTo(receipient, sender)


let inline awaitTask (task: Task) = 
    // rethrow exception from preceding task if it fauled
    let continuation (t : Task) : unit =
        match t.IsFaulted with
        | true -> raise t.Exception
        | arg -> ()
    task.ContinueWith continuation |> Async.AwaitTask

let inline awaitTaskResult (task: Task<_>) = 
    let continuation (t : Task<_>)  =
        match t.IsFaulted with
        | true -> raise t.Exception
        | arg -> t.Result

    task.ContinueWith continuation |> Async.AwaitTask
 
let inline startAsPlainTask (work : Async<unit>) = Task.Factory.StartNew(fun () -> work |> Async.RunSynchronously)


let fromUrl (url:string) = async {
    use client = new System.Net.WebClient()
    let! response = client.AsyncDownloadString(new Uri(url))
    return response }


let handler (mailbox:Actor<obj>) message = 
                let sender = mailbox.Sender() 
                match box message with
                | :? string as url ->                       
                        async {
                            let! response = fromUrl url
                            printfn "actor: done!"
                            sender.Tell response } 
                            |!> mailbox.Self  // |!> is the infix operator for PipeTo
                            // Actors process messages one at a time...
                            // The goal behind PipeTo is to treat every async operation 
                            // just like any other method that can produce a message 
                            // for an actor's mailbox
                | _ ->  mailbox.Unhandled("unknown message")


let echoServer = spawn system "EchoServer" (actorOf2 handler)
 

for timeout in [10; 100; 250; 2500] do
    try
        let task = echoServer.Ask versionUrl |> awaitTaskResult

        let response = Async.RunSynchronously (task, timeout)
        let responseLength = string(response) |> String.length

        printfn "response: result has %d bytes" responseLength
    with :? TimeoutException ->
        printfn "ask: timeout!"




type Response ={Url:string; Response:string}

let handlerRes (mailbox:Actor<obj>) message = 
                let sender = mailbox.Sender() 
                match box message with
                | :? Response as res -> 
                        printfn "response: result has %d bytes" (res.Response |> String.length)
                        sender <! res.Response
                | :? string as url ->                       
                        async {
                            let! response = fromUrl url
                            printfn "actor: done!"
                            return {Url=url; Response= response} } 
                            |!> mailbox.Self 
                | _ ->  mailbox.Unhandled("unknown message")

let echoServerRes = spawn system "EchoServerRes" (actorOf2 handlerRes)

async{   
    let! taskRes = (echoServer <? versionUrl)
    let responseLength = string(taskRes) |> String.length
    printfn "response: result has %d bytes" responseLength } |> Async.Start
    
system.Shutdown()
