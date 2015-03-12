(*  
    .NET introduced a variety of useful concurrency primitives including the barrier, 
    which blocks a number of threads until they have all reached the same point 
    before been allowed to progress.

    The System.Threading.Barrier class provides a conventional blocking barrier. 
    This is constructed with a given number of "participants". Participating 
    threads can block waiting for the barrier to roll by invoking the SignalAndWait method.
    When the number of blocked threads reaches the number of participants the barrier 
    rolls and all of the participant threads are released. 
    This class also provides AddParticipant and RemoveParticipant methods to 
    increase and decrease the current number of participants, respectively.

    Rather than using locks or other low-level concurrency primitives as 
    the synchronous .NET 4 Barrier does, we are passing a message to an Agent
    to convert concurrently posted messages into a sequence of messages. 
*)
namespace AsyncHelper

[<AutoOpenAttribute>]
module AsyncHelperModule =
        open System

        // The SignalAndWait message conveys a reply function of the 
        // type unit -> unit that allows the barrier to resume participant workflows.
        type BarrierInstruction =
                | SignalAndWait of (unit -> unit)  
                // registers a new participating workflow that will be resumed when its reply function is invoked and checks for a phase change
                | AddParticipant  
                // increases the expected number of participants and does not need to check for a phase change
                | RemoveParticipant 
                // message reduced the expected number of participants and checks for a phase change


        type BarrierAsync(n) =
            // check if the number of participant workflows waiting for a reply 
            // has reached the expected number of participants and, if so, their 
            // reply functions must be invoked to resume them all before continuing 
            // with zero participants
    
            // (int * int * (unit -> unit)) list -> (int * int * (unit -> unit)) list
            let checkPhase(participants, nReplies, replies) =
                if nReplies = participants then
                    for reply in replies do reply()
                    participants, 0, []
                else participants, nReplies, replies

            // the agent will be a state machine that never terminates so we can 
            // productively factor out a transition function that is required 
            // by the type system to return a new accumulator every time it is invoked, 
            // thus enforcing the constraint that the agent must always recur   
            let transition (participants, nReplies, replies) msg =
                  match msg with
                  | SignalAndWait reply ->  
                        checkPhase (participants, nReplies + 1, reply::replies)
                  | AddParticipant ->   
                        participants + 1, nReplies, replies
                  | RemoveParticipant ->    
                        checkPhase (participants+1, nReplies+1, replies)

            let cancelToken = new System.Threading.CancellationTokenSource()

            let agent = MailboxProcessor.Start((fun inbox ->
                            let rec loop acc = async {
                                    let! msg = inbox.Receive()
                                    return! loop(transition acc msg) }
                            loop(n, 0, [])), cancelToken.Token)
    
            interface IDisposable with
                member x.Dispose() =
                    cancelToken.Cancel()
                    (agent :> IDisposable).Dispose()            
    
            member x.AsyncSignalAndWait() = 
                  agent.PostAndAsyncReply(fun reply -> SignalAndWait reply.Reply)

            member x.AddParticipant() = agent.Post AddParticipant

            member x.RemoveParticipant() = agent.Post RemoveParticipant

        (********************************************************************************************************)
        (******************************* T E S T ****************************************************************)
        (********************************************************************************************************)
module TestBarrier =
        // this test requires the barrier to be able to handle 100001 participants which the .NET 4 Barrier 
        // is incapable of due to an internal 16-bit limitation.
        let test_BarrierAsync() = 
              for nAgents in [1; 1; 10; 100; 1000; 10000; 100000 ] do
                let timer = System.Diagnostics.Stopwatch.StartNew()
                use barrier = new BarrierAsync(nAgents+1)
                let nMsgs = 1000000 / nAgents

                let makeAgent _ =
                  new MailboxProcessor<_>(fun inbox ->
                    let rec loop n id =
                      async { let! id = inbox.Receive()
                              let n = n+1
                              if n=nMsgs then
                                do! barrier.AsyncSignalAndWait()
                              else
                                return! loop n id }
                    loop 0 [])
                // Agents<int list> []
                let agents = Array.init nAgents makeAgent
                for agent in agents do agent.Start()

                printfn "%fs to create %d agents" timer.Elapsed.TotalSeconds nAgents
                timer.Restart()

                for n in 1..nMsgs do
                  let msg = [n]
                  for agent in agents do agent.Post msg

                // The barrier is wating Async
                async { do! barrier.AsyncSignalAndWait() } |> Async.RunSynchronously

                // Run when all tasks are completed 
                printfn "%fs to post %d msgs" timer.Elapsed.TotalSeconds (nMsgs * nAgents)
                timer.Restart()

                // dispose all the agents
                for agent in agents do 
                    use agent = agent
                    ()

                printfn "%fs to dispose of %d agents\n" timer.Elapsed.TotalSeconds nAgents

        
        test_BarrierAsync()                
