open System
open Microsoft.FSharp.Control

type Agent<'a> = MailboxProcessor<'a>

let myEvent = new Event<int>()

let ctx = System.Threading.SynchronizationContext.Current
let cancellationToken = new System.Threading.CancellationTokenSource()

 
let oneAgent =
       Agent.Start(fun inbox ->
         async { while true do
                   let! msg = inbox.Receive()
                   printfn "got message '%s'" msg } )
 
oneAgent.Post "hi"


let start<'T> (work : 'T -> unit) =
    Agent<obj>.Start(fun mb ->
        let rec loop () =
            async {
                let! msg = mb.Receive()
                match msg with
                | :? 'T as msg' -> work msg'
                | _ -> () // oops... undefined behaviour
                return! loop ()
            }
        loop () )

let printInt = start<int>(fun value -> printfn "Print: %d" value)
let printString = start<string>(fun value -> printfn "Print: %s" value)

printInt.Post(7)
printString.Post("Hello")


printInt.Post("Hello")
printString.Post(7)

// 100k agents
let alloftheagents =
        [ for i in 0 .. 100000 ->
           Agent.Start(fun inbox ->
             async { while true do
                       let! msg = inbox.Receive()
                       if i % 10000 = 0 then
                           printfn "agent %d got message '%s'" i msg })]
 
for agent in alloftheagents do
    agent.Post "ping!"


(******************************* AGENT ERROR HANDLING ***********************************)
let errorAgent =
       Agent<int * System.Exception>.Start(fun inbox ->
         async { while true do
                   let! (agentId, err) = inbox.Receive()
                   printfn "an error '%s' occurred in agent %d" err.Message agentId })
 

let agents10000 =
       [ for agentId in 0 .. 10000 ->
            let agent =
                new Agent<string>(fun inbox ->
                   async { while true do
                             let! msg = inbox.Receive()
                             if msg.Contains("agent 99") then
                                 failwith "fail!" })
            // Error Handling 
            agent.Error.Add(fun error -> errorAgent.Post (agentId,error))
            agent.Start()
            (agentId, agent) ]
 
for (agentId, agent) in agents10000 do
    agent.Post (sprintf "message to agent %d" agentId )



module Agent = // HELPER ERROR HANDLING
    let reportErrorsTo (supervisor: Agent<exn>) (agent: Agent<_>) =
           agent.Error.Add(fun error -> supervisor.Post error); agent
  
    let startAgent (agent: Agent<_>) = agent.Start(); agent

let supervisor' =
   Agent<System.Exception>.Start(fun inbox ->
     async { while true do
               let!err = inbox.Receive()
               printfn "an error '%s' occurred" err.Message })

let agent' =
   new Agent<int>(fun inbox ->
     async { while true do
               let! msg = inbox.Receive()
               if msg % 1000 = 0 then
                   failwith "I don't like that cookie!" })
   |> Agent.reportErrorsTo supervisor'
   |> Agent.startAgent

for i in [0..1000] do
    agent'.Post i


(******************************* AGENT IDISPOSABLE ***********************************)

type AgentDisposable<'T>(m:Agent<'T> -> Async<unit>, ?cancellationToken:System.Threading.CancellationTokenSource) =
    let cancellationToken = defaultArg cancellationToken (new System.Threading.CancellationTokenSource())    
    member x.Agent = Agent.Start(m, cancellationToken.Token)
    interface IDisposable with
        member x.Dispose() = 
            (x.Agent :> IDisposable).Dispose()
            cancellationToken.Cancel()


let agents = [1..100 * 1000]
             |> List.map( fun i ->
                    use a = new Agent<_>((fun n ->  async {
                        while true do
                            let! msg = n.Receive()
                            if i % 20000 = 0 then 
                                printfn "agent %d got message %s" i msg 
                                ctx.Post((fun _ -> myEvent.Trigger i), null) 
                            if i % 40000 = 0 then 
                                raise <| new System.Exception("My Error!") }), cancellationToken.Token )
                    a.Error.Add(fun _ -> printfn "Something wrong with agent %d" i)
                    a.Start()  
                    (a, { new System.IDisposable with
                            member x.Dispose() =
                                printfn "Disposing agent %d" i
                                cancellationToken.Cancel() }) )  
                            

for (agent,idisposable) in agents do
    agent.Post "ciao"
    idisposable.Dispose()

for (agent,idisposable) in agents do    
    idisposable.Dispose()
    (agent :> System.IDisposable).Dispose()


let agents' = [1..100 * 1000]
             |> List.map( fun i ->
                    use a = new AgentDisposable<_>(fun n ->  async {
                        while true do
                            let! msg = n.Receive()
                            if i % 20000 = 0 then 
                                printfn "agent %d got message %s" i msg 
                                ctx.Post((fun _ -> myEvent.Trigger i), null) 
                            if i % 40000 = 0 then 
                                raise <| new System.Exception("My Error!") })
                    a.Agent.Error.Add(fun _ -> printfn "Something wrong with agent %d" i)
                    a.Agent.Start()
                    a )
                            

for agent in agents' do
    agent.Agent.Post "ciao"
    (agent :> System.IDisposable).Dispose()


(******************************* AGENT GET REPLY ***********************************)                  
type Message = 
    | Execute of string * AsyncReplyChannel<string>
    | AsyncExecute of string * AsyncReplyChannel<string>

let formatString = "Received message: {0}" 

let agent = Agent<Message>.Start(fun inbox ->
    let rec loop () =
        async {
                let! msg = inbox.Receive();
                match msg with 
                | Execute(message, replyChannel) -> replyChannel.Reply(String.Format(formatString, message))
                | AsyncExecute(message, replyChannel) -> (*Async*)
                        async {    // delay computation
                                do! Async.Sleep 2000                        
                                replyChannel.Reply(String.Format(formatString, message)) } |> Async.Start // <<== !!
                do! loop ()
        }
    loop ())

//PostAndReply blocks
let input = "Hello dear Agent!"
let message = agent.PostAndReply(fun replyChannel -> Execute(input, replyChannel))
// Execute the Reply channel Async
let messageAsync = agent.PostAndAsyncReply(fun replyChannel -> AsyncExecute(input, replyChannel))

Async.Start(async { let! reply = messageAsync |> Async.StartChild
                    let! msg = reply
                    printfn "Reply received: %s" msg } )

Async.StartWithContinuations(messageAsync, 
        (fun reply -> printfn "Reply Async received: %s" reply), //continuation
        (fun _ -> ()), //exception
        (fun _ -> ())) //cancellation


(******************************* AGENT LOCK FREE ***********************************)                  
type Fetch<'T> = AsyncReplyChannel<'T>

type Msg<'key,'value> = 
    | Push of 'key * 'value
    | Pull of 'key * Fetch<'value>

module LockFree = 
    let (lockfree:Agent<Msg<string,string>>) = Agent.Start(fun sendingInbox -> 
        let cache = System.Collections.Generic.Dictionary<string, string>()
        let rec loop () = async {
            let! message = sendingInbox.Receive()
            match message with 
                | Push (key,value) -> cache.[key] <- value
                | Pull (key,fetch) -> fetch.Reply cache.[key]
            return! loop ()
            }
        loop ())

(******************************* AGENT COUNTER ***********************************)                  
let counter =new Agent<_>(fun inbox ->
                let rec loop n =
                    async {printfn "n = %d, waiting..." n
                           let! msg = inbox.Receive()
                           return! loop (n + msg)}
                loop 0)

counter.Start();;
//n = 0, waiting...
counter.Post(1);;
//n = 1, waiting...
counter.Post(2);;
//n = 3, waiting...
counter.Post(1);;
//n = 4, waiting...


(******************************* AGENT SCANNING ***********************************)                  

type internal MessageCounter = 
        | Increment of int 
        | Fetch of AsyncReplyChannel<int> 
        | Stop
        | Pause
        | Resume

let cancel = new System.Threading.CancellationTokenSource()

// Agent + Scan = State Machine
type CountingAgent() =
    let counter = MailboxProcessor.Start((fun inbox ->
         let rec blocked(n) =           
            printfn "Blocking"
            inbox.Scan(fun msg ->
            match msg with
            | Resume -> Some(async {
                printfn "Resuming"
                return! processing(n) })
            | _ -> None)
         and processing(n) = async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Increment m -> return! processing(n + m)
                    | Stop -> return ()
                    | Resume -> return! processing(n)
                    | Pause -> return! blocked(n)
                    | Fetch replyChannel  ->    do replyChannel.Reply n
                                                return! processing(n) }
         processing(0)), cancel.Token)

    member a.Increment(n) = counter.Post(Increment n)
    member a.Stop() = counter.Post Stop
    member a.Pause() = counter.Post Pause
    member a.Resume() = counter.Post Resume
    member a.Fetch() = counter.PostAndReply(fun replyChannel -> Fetch replyChannel)


let counterInc = new CountingAgent();;
counterInc.Increment(1);;
counterInc.Fetch();;
counterInc.Pause();;
counterInc.Increment(2);;
counterInc.Fetch();;
counterInc.Resume();;
counterInc.Fetch();;
counterInc.Stop();;



(*
In the echoAgent example, we used the Receive method to get messages from the underlying queue. 
In many cases, Receive is appropriate, but it makes it difficult to filter messages because it removes them from the queue. 
To selectively process messages, you might consider using the Scan method instead.
Scanning for messages follows a different pattern than receiving them directly. Rather than processing the messages inline and always returning an asynchronous workflow,
the Scan method accepts a filtering function that accepts a message and returns an Async<'T> option. 
In other words, when the message is something you want to process, you return Some<Async<'T>; otherwise, you return None.
*)

type MessageScan = 
    | Message of obj

let echoAgent =
  Agent<MessageScan>.Start(fun inbox ->
    let rec loop () =
      inbox.Scan(fun (Message(x)) ->
       match x with
       | :? string
       | :? int ->
         Some (async { printfn "%O" x
                       return! loop() })
       | _ -> printfn "<not handled>"; None)
    loop())

Message "nuqneH" |> echoAgent.Post
Message 123 |> echoAgent.Post
Message [ 1; 2; 3 ] |> echoAgent.Post // not handled

(* Scanning for messages does offer some flexibility with how you process messages, 
   but you need to be mindful of what you’re posting to the agent because messages not processed by Scan remain in the queue. 
   As the queue size increases, scans will take longer to complete, so you can quickly run into performance issues using this approach if you’re not careful *)


(*******************************  Comunication between Agents **********************************)                  
type MessageCom = Finished | Msg of int * MessageCom MailboxProcessor

let ping iters (outbox : MessageCom Agent) =
    Agent<MessageCom>.Start(fun inbox -> 
        let rec loop n = async { 
            match n with
            | 0 -> outbox.Post Finished
                   printfn "ping finished"
                   return ()
            | _ -> outbox.Post( Msg(n, inbox) )
                   let! msg = inbox.Receive()
                   printfn "ping received pong"
                   return! loop(n-1)}
        loop iters)
            
let pong() =
    Agent<MessageCom>.Start(fun inbox -> 
        let rec loop () = async { 
            let! msg = inbox.Receive()
            match msg with
            | Finished -> 
                printfn "pong finished"
                return ()
            | Msg(n, outbox) -> 
                printfn "pong received ping"
                outbox.Post( Msg(n, inbox) )
                return! loop() }
                    
        loop())

ping 100 <| pong() |> ignore

(*******************************  MAPPING AGENTS **********************************)                  
let (-->) agent1 agent2 = agent1 agent2

let MapToAgent f (target:Agent<_>) = Agent<_>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                target.Post (f msg)
                return! loop () }
            loop () )

let IterateAgent f = Agent<_>.Start(fun inbox ->
            let rec loop () = async {
                    let! msg = inbox.Receive()
                    f msg
                    return! loop () }
            loop () )

let test = MapToAgent(fun msg -> msg + 100) --> IterateAgent (fun msg -> printfn "message %d" msg)

test.Post 0
test.Post 0
test.Post 2
test.Post 4
for i in [0..100] do test.Post i
