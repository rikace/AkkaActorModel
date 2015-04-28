
open System
open System.IO
open System.Threading
open System.Net


(*  Agent that can be used for controlling the number of concurrently executing asynchronous workflows. 
    The agent runs a specified number of operations concurrently and queues remaining pending requests. 
    The queued work items are started as soon as one of the previous items completes.
*)
type internal ThrottlingAgentMessage<'T> = 
  | Completed
  | Work of Async<unit>

    
/// Represents an agent that runs operations in concurrently. When the number
/// of concurrent operations exceeds 'limit', they are queued and processed later
type ThrottlingAgent<'T>(limit) = 
  let agent = MailboxProcessor<ThrottlingAgentMessage<'T>>.Start(fun agent -> 
    let rec waiting () = 
      agent.Scan(function
        | Completed -> Some(working (limit - 1))
        | _ -> None)
  
    and working count = async { 
      let! msg = agent.Receive()

      


      match msg with 
      | Completed -> return! working (count - 1)
     
     
      | Work work ->  async { try do! work 
                              finally agent.Post(Completed) }
                      |> Async.Start
                      if count < limit then return! working (count + 1)
                      else return! waiting () }
    working 0)      

  member x.DoWork(work) = agent.Post (Work work)

  // Async Reply Channel in different Thread
  member x.DoWorkWithResult(work) f = Async.StartWithContinuations(
                                            agent.PostAndAsyncReply (fun reply -> WorkWithResult(work, reply))
                                           , f
                                           ,(fun exn -> ())
                                           ,(fun cancel -> ()))
                                                                


let agent= ThrottlingAgent<string>(2)

let httpAsync(url:string) = 
    async { let req = WebRequest.Create(url)                 
            let! resp = req.AsyncGetResponse()
            use stream = resp.GetResponseStream() 
            use reader = new StreamReader(stream) 
            let! http = reader.ReadToEndAsync() |> Async.AwaitTask
            printfn "Thread id %d - http len %d" Thread.CurrentThread.ManagedThreadId http.Length }

let urls = 
    [ "http://www.live.com"; 
        "http://news.live.com"; 
        "http://www.yahoo.com"; 
        "http://news.yahoo.com"; 
        "http://www.google.com"; 
        "http://news.google.com"; ] 

[ for url in urls -> httpAsync url ]
|> List.iter(fun h -> agent.DoWork(h))

// Starting thread is different 
let actionOne =async {  printfn "Running Action 1.. Thread %d" Thread.CurrentThread.ManagedThreadId
                        do! Async.Sleep 5000
                        return "Action 1 Completed" }

let actionTwo = async { printfn "Running Action2.. Thread %d" Thread.CurrentThread.ManagedThreadId
                        do! Async.Sleep 1500
                        return  "Action 2 Completed"}

let actionThree = async {   printfn "Running Action 3.. Thread %d" Thread.CurrentThread.ManagedThreadId
                            return  "Action 3 Completed" }

let completedF =  (fun x -> printfn "Thread id %d - %s" Thread.CurrentThread.ManagedThreadId x )
agent.DoWorkWithResult(actionOne) (completedF)
agent.DoWorkWithResult(actionTwo) (completedF)
agent.DoWorkWithResult(actionThree) (completedF)