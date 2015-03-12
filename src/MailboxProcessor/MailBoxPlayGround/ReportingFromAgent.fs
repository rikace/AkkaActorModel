module ReportingFromAgent

// This snippet shows different options for reporting events from an F# agent. 
// The options include triggering the event directly, using a thread pool or using a specified synchronization context.


/// Type alias that gives convenient name to F# agent type
type Agent<'T> = MailboxProcessor<'T>

module Template =
    /// Type alias that gives convenient name to F# agent type
    type Agent<'T> = MailboxProcessor<'T>
    
    /// Agent that implements batch processing
    type BatchProcessor<'T>(count) =
      // Event used to report aggregated batches to the user
      let batchEvent = new Event<'T[]>()
      // Trigger event on the thread where the agent is running
      let reportBatch batch =
        try
          // If the handler throws, we need to handle the exception
          batchEvent.Trigger(batch)
        with e ->
          printfn "Event handler failed: %A" e
    
      // Start an agent that implements the batching
      let agent = Agent<'T>.Start(fun inbox -> async {
        while true do
          // Repeatedly allocate a new queue 
          let queue = new ResizeArray<_>()
          // Add specified number of messages to the queue
          for i in 1 .. count do
            let! msg = inbox.Receive()
            queue.Add(msg)
          // Report the batch as an array
          reportBatch (queue.ToArray()) })
    
      /// Event that is triggered when a batch is collected
      member x.BatchProduced = batchEvent.Publish
      /// The method adds one object to the agent
      member x.Post(value) = agent.Post(value)

module ThreadPool =
    /// Agent that implements batch processing
    type BatchProcessor<'T>(count) =
      // Event used to report aggregated batches to the user
      let batchEvent = new Event<'T[]>()
      // Trigger event in a thread pool
      let reportBatch batch =
        // Create simple workflow & start it in the background
        async { batchEvent.Trigger(batch) } 
        |> Async.Start
    
      // Start an agent that implements the batching
      let agent = Agent<'T>.Start(fun inbox -> async {
        while true do
          // Repeatedly allocate a new queue 
          let queue = new ResizeArray<_>()
          // Add specified number of messages to the queue
          for i in 1 .. count do
            let! msg = inbox.Receive()
            queue.Add(msg)
          // Report the batch as an array
          reportBatch (queue.ToArray()) })
    
      /// Event that is triggered when a batch is collected
      member x.BatchProduced = batchEvent.Publish
      /// The method adds one object to the agent
      member x.Post(value) = agent.Post(value)

open System.Threading

/// Agent that implements batch processing (eventContext can 
/// be provided to specify synchronization context for event reporting)
type BatchProcessor<'T>(count, ?eventContext:SynchronizationContext) =
  /// Event used to report aggregated batches to the user
  let batchEvent = new Event<'T[]>()

  /// Triggers event using the specified synchronization context
  /// (or directly, if no synchronization context is specified)
  let reportBatch batch =
    match eventContext with 
    | None -> 
        // No synchronization context - trigger as in the first case
        batchEvent.Trigger(batch)
    | Some ctx ->
        // Use the 'Post' method of the context to trigger the event
        ctx.Post((fun _ -> batchEvent.Trigger(batch)), null)

  (*[omit:(unchanged agent body)]*)
  // Start an agent that implements the batching
  let agent = Agent<'T>.Start(fun inbox -> async {
    while true do
      // Repeatedly allocate a new queue 
      let queue = new ResizeArray<_>()
      // Add specified number of messages to the queue
      for i in 1 .. count do
        let! msg = inbox.Receive()
        queue.Add(msg)
      // Report the batch as an array
      reportBatch (queue.ToArray()) })(*[/omit]*)

  /// Event that is triggered when a batch is collected
  member x.BatchProduced = batchEvent.Publish
  /// The method adds one object to the agent
  member x.Post(value) = agent.Post(value)

// Agent that will trigger events on the current (GUI) thread
let sync = SynchronizationContext.Current
let proc = BatchProcessor<_>(10, sync)

// Start some background work that will report batches to GUI thread
async {
  for i in 0 .. 1000 do 
    proc.Post(i) } |> Async.Start
