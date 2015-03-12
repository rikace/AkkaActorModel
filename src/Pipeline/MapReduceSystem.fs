namespace MapReduceSystem 

open System
open System.Threading
open System.Collections
open System.Collections.Generic
open AgentSystem.LAgent

// Implementation
open System.Threading
open System.Collections.Generic

module MapReduce =
    type MapperMessage<'in_k, 'in_v, 'out_k, 'out_v> =
    | Process of 'in_k * 'in_v
    | InitMapper of ControlAgent<'in_k, 'in_v, 'out_k, 'out_v> * ReducerAgent<'in_k, 'in_v, 'out_k, 'out_v> array
    | Done
    and ControlMessage<'in_k, 'in_v, 'out_k, 'out_v> =
    | MapDone of int * BitArray 
    | InitControl of MapperAgent<'in_k, 'in_v, 'out_k, 'out_v> array * ReducerAgent<'in_k, 'in_v, 'out_k, 'out_v> array * ControlAgent<'in_k, 'in_v, 'out_k, 'out_v>
    | ReductionDone of int
    and ReducerMessage<'in_k, 'in_v, 'out_k, 'out_v> =
    | Add of 'out_k * 'out_v
    | InitReducer of ControlAgent<'in_k, 'in_v, 'out_k, 'out_v>
    | StartReduction
    and ReceiverMessage<'out_k, 'out_v> =
    | Reduced of 'out_k * 'out_v
    | MapReduceDone

    and MapperState<'in_k, 'in_v, 'out_k, 'out_v> = MapperState of BitArray * ControlAgent<'in_k, 'in_v, 'out_k, 'out_v> * ReducerAgent<'in_k, 'in_v, 'out_k, 'out_v> array
    and ReducerState<'in_k, 'in_v, 'out_k, 'out_v> = ReducerState of List<'out_k * 'out_v> * ControlAgent<'in_k, 'in_v, 'out_k, 'out_v>
    and ControlState<'in_k, 'in_v, 'out_k, 'out_v> = ControlState of BitArray * BitArray * BitArray * MapperAgent<'in_k, 'in_v, 'out_k, 'out_v> array * ReducerAgent<'in_k, 'in_v, 'out_k, 'out_v> array * ControlAgent<'in_k, 'in_v, 'out_k, 'out_v>

    and MapperAgent<'in_k, 'in_v, 'out_k, 'out_v> = AsyncAgent<MapperMessage<'in_k, 'in_v, 'out_k, 'out_v>, MapperState<'in_k, 'in_v, 'out_k, 'out_v>>
    and ReducerAgent<'in_k, 'in_v, 'out_k, 'out_v> = AsyncAgent<ReducerMessage<'in_k, 'in_v, 'out_k, 'out_v>, ReducerState<'in_k, 'in_v, 'out_k, 'out_v>>
    and ControlAgent<'in_k, 'in_v, 'out_k, 'out_v> = AsyncAgent<ControlMessage<'in_k, 'in_v, 'out_k, 'out_v>, ControlState<'in_k, 'in_v, 'out_k, 'out_v>> 


    let mapReduce   (inputs:seq<'in_key * 'in_value>)
                    (map:'in_key -> 'in_value -> seq<'out_key * 'out_value>)
                    (reduce:'out_key -> seq<'out_value> -> seq<'reducedValues>)
                    outputAgent
                    M R partitionF =                
      
      
        let len = inputs |> Seq.length
        let M = if len < M then len else M
    
        let BAtoSeq (b:BitArray) = [for x in b do yield x]

        let mapF i  =
            fun msg (MapperState(reducerTracker:BitArray, controlAgent, (reducers:ReducerAgent<'in_key, 'in_value, 'out_key, 'out_value> array))) ->
            match msg with
            | Process(inKey, inValue) ->
                let outKeyValues = map inKey inValue
                outKeyValues |> Seq.iter (fun (outKey, outValue) ->
                                            let reducerUsed = partitionF outKey R
                                            reducerTracker.Set(reducerUsed, true)
                                            reducers.[reducerUsed] <-- Add(outKey, outValue))
                MapperState(reducerTracker, controlAgent, reducers)
            | InitMapper(c, reds) ->
                MapperState(reducerTracker, c, reds)   
            | Done ->
                //printfn "In Done"
                controlAgent <-- MapDone(i, reducerTracker)
                MapperState(reducerTracker, controlAgent, reducers)
    
        let reduceF i =
            fun msg (ReducerState(workItems: List<'out_key * 'out_value>, controlAgent)) ->
            match msg with
            | StartReduction   ->
                workItems
                |> Seq.groupBy fst
                |> Seq.sortBy fst
                |> Seq.map (fun (key, values) -> (key, reduce key (values |> Seq.map snd)))
                |> Seq.iter (fun (key, value) -> outputAgent <-- Reduced(key, value))
                controlAgent <-- ReductionDone(i) 
                ReducerState(workItems, controlAgent)
            | InitReducer(c)   ->
                ReducerState(workItems, c)      
            | Add(outKey, outValue) ->
                workItems.Add( (outKey, outValue))
                ReducerState(workItems, controlAgent)
        let controlF   =
            fun msg (ControlState(mapperTracker:BitArray, reducerUsedByMappers:BitArray, reducerDone:BitArray, mappers, (reducers:ReducerAgent<'in_key, 'in_vvalue, 'out_key, 'out_value> array), me)) ->
            match msg with
            | MapDone(i, reducerTracker) ->
                mapperTracker.Set(i, true)
                let reducerUsedByMappers = reducerUsedByMappers.Or(reducerTracker)
                if not( BAtoSeq mapperTracker |> Seq.exists(fun bit -> bit = false)) then
                    BAtoSeq reducerUsedByMappers |> Seq.iteri (fun i r -> if r = true then reducers.[i] <-- StartReduction)
                    mappers |> Seq.iter (fun m -> m <-! Stop)
                ControlState(mapperTracker, reducerUsedByMappers, reducerDone, mappers, reducers, me)
            | InitControl(maps, reds, m) ->
                ControlState(mapperTracker, reducerUsedByMappers, reducerDone, maps, reds, m)
            | ReductionDone(i)  ->
                reducerDone.Set(i, true)
                if BAtoSeq reducerDone |> Seq.forall2 (fun x y -> x = y) (BAtoSeq reducerUsedByMappers) then
                    outputAgent <-- MapReduceDone
                    reducers |> Seq.iter (fun r -> r <-! Stop)
                    me <-! Stop
                ControlState(mapperTracker, reducerUsedByMappers, reducerDone, mappers, reducers, me)

        let mappers = Array.init M (fun i -> spawnAgent (mapF i) (MapperState(new BitArray(R, false),
                                                                        Unchecked.defaultof<ControlAgent<'in_key, 'in_value, 'out_key, 'out_value>>,
                                                                        Unchecked.defaultof<ReducerAgent<'in_key, 'in_value, 'out_key, 'out_value> array>) ))                
        let reducers = Array.init R (fun i -> spawnAgent (reduceF i) (ReducerState(new List<'out_key * 'out_value>(),
                                                                        Unchecked.defaultof<ControlAgent<'in_key, 'in_value, 'out_key, 'out_value>>)))
        let rec controlAgent = spawnAgent controlF (ControlState(new BitArray(M, false), new BitArray(R, false), new BitArray(R, false),
                                                                        Unchecked.defaultof<MapperAgent<'in_key, 'in_value, 'out_key, 'out_value> array>,
                                                                        Unchecked.defaultof<ReducerAgent<'in_key, 'in_value, 'out_key, 'out_value> array>,
                                                                        Unchecked.defaultof<ControlAgent<'in_key, 'in_value, 'out_key, 'out_value>>))
    
        mappers |> Array.iter (fun m -> m <-- InitMapper(controlAgent, reducers))
        reducers |> Array.iter (fun r -> r <-- InitReducer(controlAgent))
        controlAgent <-- InitControl(mappers, reducers, controlAgent)
    
        inputs |> Seq.iteri (fun i (inKey, inValue) -> mappers.[i % M] <-- Process(inKey, inValue))
        mappers |> Seq.iter (fun m -> m <-- Done)    

