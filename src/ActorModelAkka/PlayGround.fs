namespace ActorModelAkka

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System

module FSharpAkkaApi =

    // create Actor
    // this function differs from it's C# equivalent by providing additional F#-specific features
    // i.e. serializers allowing to serialize F# quotations for remote deployment process.
    let system = System.create "my-system" (Configuration.load())

    let system' = System.create "my-system" (Configuration.parse("""
                                akka {
                                    actor {
                                            # collection of loggers used inside actor system
                                            # specified by fully-qualified type name
                                            loggers = [ "Akka.Event.DefaultLogger, Akka" ]
                                        
                                            # Options: OFF, ERROR, WARNING, INFO, DEBUG
                                            logLevel = "DEBUG"
                                        }
                                }"""));


    // Actor Computation Expression
    // F# defines an actor's logic in a functional way
    // using actor computation expression. 
    // The expression inside the actor is expected to be a self-invoking recursive function
    // Invoking an other functions while maintaining recursive cycle is allowed
    // i.e. to change actor's behavior or even to create more advanced constructs like Finite State Machines
    let aref = 
        spawn system "my-actor" 
            (fun mailbox -> 
                let rec loop() = actor {
                    let! message = mailbox.Receive()
                    // handle an incoming message
                    return! loop()
                }
                loop())



    // These are shorthand functions to define message handler's behavior
    
    // the function takes a message as parameter and the Mailbox is injected by spawning functions
    // actorOf (fn : 'Message -> unit) (mailbox : Actor<'Message>) : Cont<'Message, 'Returned> 

    // the function takes a message and an Actor instance as the parameters. 
    // Mailbox parameter is injected by spawning functions.
    // actorOf2 (fn : Actor<'Message> -> 'Message -> unit) (mailbox : Actor<'Message>) : Cont<'Message, 'Returned> 

    let handleMessage (mailbox: Actor<'a>) msg =
        match msg with
        | Some x -> printf "%A" x
                    let sender = mailbox.Sender() 
                    sender <! (sprintf "%A Akka.NET!!" x)
        | None ->   let sender = mailbox.Sender() 
                    sender <! sprintf "Nothing :("

    let aref' = spawn system "my-actor" (actorOf2 handleMessage)
    let useless = spawn system "useless-actor" (actorOf (fun msg -> ()))

    aref' <! ( ("Hello!" :> obj) |> Some)
    aref' <! None

    system.Shutdown()
    
    
    // Spawning Actors
    // To specify thr behavior of an Actor, you may use spawnOpt and spawne 
    // both taking a list of SpawnOption values
    
    let myFunc msg = 
        printf "%A" msg        

    let remoteRef = 
        spawne system "my-remoteActor" <@ actorOf myFunc @> 
            [SpawnOption.Deploy (Deploy(RemoteScope(Address.Parse "akka.tcp://remote-system@127.0.0.1:9000/")))]

    remoteRef <! "Hello!!"

//    let request = remoteRef <? "Hello!"
//
//    async { let! response = request 
//       printfn "Reply from remote %s" (string(response)) }
//       |> Async.Start

    //  Actor selection
    // Actors may be referenced through actor path selection
    // You may select an actor with known path using select function:

    // select (path : string) (selector : ActorRefFactory) : ActorSelection
    // where path is an URI used to recognize actor path
    // and the selector is either actor system or actor itself    
    let arefSelect = spawn system "my-actor" (actorOf2 (fun mailbox m -> printfn "%A said %s" (mailbox.Self.Path) m))
    arefSelect <! "one"
    let aref2 = select "akka://my-system/user/my-actor" system
    aref2 <! "two"



    //Monitoring
    //Actors and Inboxes may be used to monitor lifetime of other actors. This is done by monitor/demonitor functions:
    //
    //monitor (subject: ActorRef) (watcher: ICanWatch) : ActorRef - starts monitoring a referred actor.
    //demonitor (subject: ActorRef) (watcher: ICanWatch) : ActorRef - stops monitoring of the previously monitored actor.
    //Monitored actors will automatically send a Terminated message to their watchers when they die.



    // Actor supervisor strategies
    // Actors have a place in their system's hierarchy trees. 
    // To manage failures done by the child actors, their parents/supervisors may decide to use specific supervisor strategies

    // Strategy.OneForOne 
    // Strategy.AllForOne 

    let arefOpt = 
        spawnOpt system "my-actor" (actorOf myFunc) 
            [ SpawnOption.SupervisorStrategy (Strategy.OneForOne (fun error -> 
                match error with
                | :? ArithmeticException -> Directive.Escalate
                | _ -> SupervisorStrategy.DefaultDecider error )) ]


    // Interop with Task Parallel Library
    // use pipeTo function (abbreviations <!| and |!> operators) 
    // to inform actor about tasks ending their processing pipelines
    // Piping functions used on tasks will move async result directly to the mailbox of a target actor.

    open System.IO


    let handler (mailbox: Actor<obj>) msg = 
        match box msg with
        | :? FileInfo as fi -> 
            let reader = new StreamReader(fi.OpenRead())
            reader.AsyncReadToEnd() |!> mailbox.Self 
        | :? string as content ->
            printfn "File content: %s" content
        | _ -> mailbox.Unhandled()

    let arefHandler = spawn system "my-actor" (actorOf2 handler)
    arefHandler <! new FileInfo "Akka.xml"