module AsyncDAG =
    open System
    open System.Collections.Generic
    open System.Threading
    open Microsoft.FSharp.Collections

//    type IDAGManager =
//        [<CLIEventAttribute>]
//        abstract member OnOperationComplete : IEvent<ActionDetails>
//        abstract member Execute : unit -> unit 
//        abstract member AddOperation : int -> (unit -> unit) -> int array

    type ActionDetails =
        { Context : System.Threading.ExecutionContext option
          Dependencies : int array
          Id : int
          Operation : unit -> unit
          NumRemainingDependencies : int option
          Start : DateTimeOffset option
          End : DateTimeOffset option }

    type DAGMessage =
        | AddOperation of int * ActionDetails
        | QueueOperation of ActionDetails
        | Execute
        
    type DAGManager() =
        let onOperationComplete = new Event<ActionDetails>()

        let rec getDependentOperation (dep (*dependencies*) : int list) 
                                      (ops (*operations*) : Dictionary<int, ActionDetails>) acc =
            match dep with
            | [] -> acc
            | h :: t -> let op = ops.[h]
                        let nrd = function
                                    | Some(n) -> Some(n - 1)
                                    | None -> None
                        let op' = { op with NumRemainingDependencies = nrd op.NumRemainingDependencies }
                        ops.[h] <- op' 
                        if op'.NumRemainingDependencies.Value = 0 then getDependentOperation t ops (op' :: acc)
                        else getDependentOperation t ops acc

        let operationManager =
            MailboxProcessor.Start(fun inbox ->
                let rec loop (operations : Dictionary<int, ActionDetails>) (dependencies : Dictionary<int, int list>) =
                    async {
                        let! msg = inbox.Receive()
                        match msg with
                        | Execute ->
                            let dependenciesFromTo = new Dictionary<int, int list>()
                            let operations' = new Dictionary<int, ActionDetails>()
                            // Check and create dependencies
                            for KeyValue(key, value) in operations do
                                let operation' =
                                    { value with NumRemainingDependencies = Some(value.Dependencies.Length) }
                                for from in operation'.Dependencies do
                                    let exists, lstDependencies = dependenciesFromTo.TryGetValue(from)
                                    if not (exists) then 
                                        dependenciesFromTo.Add(from, [ operation'.Id ])
                                    else
                                        let lst = operation'.Id :: lstDependencies
                                        dependenciesFromTo.[from] <- lst
                                operations'.Add(key, operation')
                                                
                            let filteredOps' =
                                operations' |> Seq.filter (fun kv ->
                                                   match kv.Value.NumRemainingDependencies with                                                 
                                                   | Some(n) when n = 0 -> true
                                                   | _ -> false)
                            filteredOps' |> Seq.iter (fun op -> (*queue dependencies*) inbox.Post(QueueOperation(op.Value)))
                            return! loop operations' dependenciesFromTo
                        
                        | QueueOperation(op) ->
                            async { let start' = DateTimeOffset.Now
                                    match op.Context with
                                    | Some(ctx) ->
                                        ExecutionContext.Run(ctx.CreateCopy(),
                                                             (fun op ->
                                                             let opCtx = (op :?> ActionDetails)
                                                             (opCtx.Operation())), op)
                                    | None -> op.Operation()
                                   
                                    let end' = DateTimeOffset.Now
                                    let op' = { op with Start = Some(start')
                                                        End = Some(end') }

                                    onOperationComplete.Trigger op'
                                    let exists, lstDependencies = dependencies.TryGetValue(op.Id)
                                    if exists && lstDependencies.Length > 0 then
                                        let dependentOperation' = getDependentOperation lstDependencies operations []
                                        dependencies.Remove(op.Id) |> ignore
                                        dependentOperation'
                                        |> Seq.iter (fun nestedOp -> inbox.Post(QueueOperation(nestedOp))) }
                            |> Async.Start
                            return! loop operations dependencies
                        | AddOperation(id, op) -> operations.Add(id, op)
                                                  return! loop operations dependencies
                    }
                loop (new Dictionary<int, ActionDetails>(HashIdentity.Structural)) (new Dictionary<int, int list>(HashIdentity.Structural)))
       
        [<CLIEventAttribute>]
        member this.OnOperationComplete = onOperationComplete.Publish

        member this.Execute() = operationManager.Post(Execute)
      
        member this.AddOperation(id, operation, [<ParamArrayAttribute>] dependencies : int array) =
            let data =
                { Context = Some(ExecutionContext.Capture())
                  Dependencies = dependencies
                  Id = id
                  Operation = operation
                  NumRemainingDependencies = None
                  Start = None
                  End = None }
            operationManager.Post(AddOperation(id, data))

module TestAsyncDependencies =
   
    let acc1() = printfn "action 1"; System.Threading.Thread.Sleep 2000; printfn "action 1 completed"
    let acc2() = printfn "action 2"; System.Threading.Thread.Sleep 3000; printfn "action 2 completed"
    let acc3() = printfn "action 3"
    let acc4() = printfn "action 4"; System.Threading.Thread.Sleep 5000; printfn "action 4 completed"
    let acc5() = printfn "action 5"
    let acc6() = printfn "action 6"
    let acc7() = printfn "action 7"
    let acc8() = printfn "action 8"
    let acc9() = printfn "action 9"
    let acc10() = printfn "action 10"
    let acc11() = printfn "action 11"

    let dagManager = AsyncDAG.DAGManager()
    //dagManager.OnOperationComplete.Add(fun op -> printfn "Completed %d" op.Id)
    dagManager.AddOperation(1, acc1, 3)
    dagManager.AddOperation(2, acc2, 1)
    dagManager.AddOperation(3, acc3)
    dagManager.AddOperation(4, acc4, 2, 3)   
    dagManager.AddOperation(5, acc5, 3, 4)
    dagManager.AddOperation(6, acc6, 1, 3)
    dagManager.AddOperation(7, acc7, 1)
    dagManager.AddOperation(8, acc8, 2, 3)
    dagManager.AddOperation(9, acc9, 1, 4, 7)
    dagManager.AddOperation(10, acc10, 2, 4, 7)
    dagManager.AddOperation(11, acc11)
    //dagManager.AddOperation(12, acc11, 14)
    dagManager.Execute()

    //  action 3 || action 11
    //  action 1
    //  action 1 completed
    //  action 6 || 7
    //  action 2
    //  action 2 completed
    //  action 8
    //  action 4
    //  action 4 completed
    //  action 5
    //  action 10 || action 9


  
