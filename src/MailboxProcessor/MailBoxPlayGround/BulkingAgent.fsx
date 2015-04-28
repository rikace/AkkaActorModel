
open System
open System.Collections.Generic
open System.Threading




(*  Aggregating messages into bulks
    Bulk specified number of messages
    Emit bulk after timeout

    Uses of bulking agent
    Grouping data for further processing
    Writing live data to a database *)

//  new BulkingAgent    : int -> int -> BulkingAgent
//  member Enqueue      : 'T -> unit
//  member BulkProduced : Event<'T[]>

type Agent<'T> = MailboxProcessor<'T>

/// Agent that implements bulking of incomming messages
/// A bulk is produced (using an event) either after enough incomming
/// mesages are collected or after the specified time (whichever occurs first)
type BulkingAgent<'T>(bulkSize, timeout, ?eventContext:SynchronizationContext) = 
  // Used to report the aggregated bulks to the user
  let bulkEvent = new Event<'T[]>()
  let cts = new CancellationTokenSource()

  let agent : Agent<'T> = Agent.Start((fun agent -> 
    

    let reportBatch batch =
        match eventContext with 
        | None ->  async { bulkEvent.Trigger(batch) } |> Async.Start // Reporting by ThreadPool (extra overhead)
        | Some ctx -> ctx.Post((fun _ -> bulkEvent.Trigger(batch)), null)

    /// Triggers event using the specified synchronization context
    /// (or directly, if no synchronization context is specified)
    let reportBatch' batch =
        match eventContext with 
        // No synchronization context - trigger as in the first case
        | None -> bulkEvent.Trigger(batch)
            // Use the 'Post' method of the context to trigger the event
        | Some ctx -> ctx.Post((fun _ -> bulkEvent.Trigger(batch)), null)


    // Represents the control loop of the agent
    // - start  The time when last bulk was processed
    // - list   List of items in current bulk
    let rec loop (start:DateTime) (list:_ list) = async {
      if (DateTime.Now - start).TotalMilliseconds > float timeout then
        // Timed-out - report bulk if there is some message & reset time
        if list.Length > 1 then 
          bulkEvent.Trigger(list |> Array.ofList)
        return! loop DateTime.Now []
      else
        // Try waiting for a message
        let! msg = agent.TryReceive(timeout = timeout / 25)
        match msg with 
        | Some(msg) when list.Length + 1 = bulkSize ->
            // Bulk is full - report it to the user
            bulkEvent.Trigger(msg :: list |> Array.ofList)
            return! loop DateTime.Now []
        | Some(msg) ->
            // Continue collecting more mesages
            return! loop start (msg::list)
        | None -> 
            // Nothing received - check time & retry
            return! loop start list }
    loop DateTime.Now [] ), cts.Token)

  [<CLIEventAttribute>]
  member x.BulkProduced = bulkEvent.Publish
  
  member x.Enqueue v = agent.Post(v)

  interface IDisposable with
         member x.Dispose() = cts.Cancel()


let sync = System.Threading.SynchronizationContext.Current
let proc = new BulkingAgent<int>(10, 5000, sync)
proc.BulkProduced |> Observable.add(fun i -> printfn "Thread Id %d : value passed %A" System.Threading.Thread.CurrentThread.ManagedThreadId i)
async { for i in 0 .. 1000 do  proc.Enqueue(i) } |> Async.Start


open System 
open System.Drawing
open System.Windows.Forms

let frm = new Form()
let lbl = new Label(Dock = DockStyle.Fill)
frm.Controls.Add(lbl)
frm.Show()

// Create agent for bulking KeyPress events
let ag = new BulkingAgent<_>(5000, 5)
frm.KeyPress.Add(fun e -> ag.Enqueue(e.KeyChar))
ag.BulkProduced
    |> Event.map (fun chars -> new String(chars))
    |> Event.scan (+) ""
    |> Event.add (fun str -> lbl.Text <- str)


