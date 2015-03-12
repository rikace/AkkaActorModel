//The basic structure I use is:
//
//map supervisor which queues up all the work to do in its state and receives work request from map workers
//reduce supervisor does the same thing as map supervisor for reduce work
//a bunch of map and reduce workers that map and reduce, if one fails its work it sends it back to the respective supervisr to be reprocessed.
//The questions I wonder about is:
//
//does this make any sense compared to a more traditional (yet very nice) map reduce like (http://tomasp.net/blog/fsharp-parallel-aggregate.aspx) that uses PSeq ?
//the way I implemented the map and reduce workers seems ugly is there a better way ?
//it seems like I can create a 1000 000 map workers and 1000 0000 reduce workers lol, how should I choose these numbers, the more the better ?
//Thanks a lot,

#load "..\CommonModule.fsx"
//#load "Utilities\show-wpf40.fsx"
#load "..\3.async\8.BarrierAsync.fsx"

open System
open System.IO
open System.Threading
open Microsoft.FSharp.Control
open Common
open AsyncHelper

type Agent<'T> = MailboxProcessor<'T>

//This is the response the supervisor
//gives to the worker request for work
type 'work SupervisorResponse =
| Work of 'work //a piece of work
| NoWork//no work left to do 

//This is the message to the supervisor
type 'work WorkMsg = 
| ToDo of 'work //piles up work in the Supervisor queue
| WorkReq of   AsyncReplyChannel<SupervisorResponse<'work>> //' 

//The supervisor agent can be interacted with
type AgentOperation = 
| Stop //stop the agent
| Status //yield the current status of supervisor

type 'work SupervisorMsg = 
| WorkRel of 'work WorkMsg
| Operation of AgentOperation 

//Supervises Map and Reduce workers
module AgentSupervisor= 
    let getNew (name:string) = 
        new Agent<SupervisorMsg<'work>>(fun inbox -> //'
            let rec loop state  = async {
                let! msg = inbox.Receive()
                match msg with 
                | WorkRel(m) -> 
                    match m with 
                    | ToDo(work) -> 
                        let newState = work::state 
                        return! loop newState
                    | WorkReq(replyChannel) ->  
                        match state with 
                        | [] -> 
                            replyChannel.Reply(NoWork)
                            return! loop []
                        | [item] -> 
                            replyChannel.Reply(Work(item))
                            return! loop []
                        | (item::remaining) -> 
                            replyChannel.Reply(Work(item))
                            return! loop remaining
                | Operation(op) -> 
                    match op with 
                    | Status -> 
                        return! loop state
                    | Stop ->  return()
            }
            loop [] )
    let stop (agent:Agent<SupervisorMsg<'work>>) = agent.Post(Operation(Stop))
    let status (agent:Agent<SupervisorMsg<'work>>) =agent.Post(Operation(Status))

//Code for the workers
type 'success WorkOutcome = 
| Success of 'success
| Fail

type WorkerMsg = 
| Start
| Stop
| Continue

module AgentWorker = 
    type WorkerSupervisors<'reduce,'work> = 
        { Map:Agent<SupervisorMsg<'work>> ; Reduce:Agent<SupervisorMsg<'reduce>> }

    let stop (agent:Agent<WorkerMsg>) = agent.Post(Stop)
    let start (agent:Agent<WorkerMsg>) = agent.Start()
                                         agent.Post(Start)

    let getNewMapWorker( map, supervisors:WorkerSupervisors<'reduce,'work>  ) = 
        new Agent<WorkerMsg>(fun inbox ->  
            let rec loop ()  = async {
                let! msg = inbox.Receive()
                match msg with 
                | Start -> inbox.Post(Continue)
                           return! loop ()
                | Continue ->   
                    let! supervisorOrder = supervisors.Map.PostAndAsyncReply(fun replyChannel -> WorkRel(WorkReq(replyChannel)))
                    match supervisorOrder with 
                    | Work(work) -> 
                        let! res = map work 
                        match res with
                        | Success(toReduce) -> 
                            supervisors.Reduce
                                .Post(WorkRel(ToDo(toReduce)))
                        | Fail -> 
                            Console.WriteLine("Map Fail")
                            supervisors.Map
                                .Post(WorkRel(ToDo(work)))
                            inbox.Post(Continue)
                    | NoWork -> 
                            inbox.Post(Continue)
                            return! loop ()
                | Stop -> return ()
                }
            loop ()  )


    let getNewReduceWorker(reduce,reduceSupervisor:Agent<SupervisorMsg<'work>>)=
        new Agent<WorkerMsg>(fun inbox ->  
            let rec loop ()  = async {
                let! msg = inbox.Receive()
                match msg with
                | Start -> inbox.Post(Continue)
                           return! loop()
                | Continue ->   
                    let! supervisorOrder = 
                        reduceSupervisor.PostAndAsyncReply(fun replyChannel -> 
                            WorkRel(WorkReq(replyChannel)))
                    match supervisorOrder with 
                    | Work(work) -> 
                        let! res = reduce work 
                        match res with 
                        | Success(toReduce) -> inbox.Post(Continue)
                        | Fail -> 
                            Console.WriteLine("ReduceFail")
                            reduceSupervisor.Post(WorkRel(ToDo(work)))
                            inbox.Post(Continue)
                    | NoWork -> inbox.Post(Continue)
                    return! loop()
                |Stop ->Console.WriteLine("Reduce worker stopped"); return () 
                }
            loop() )

open AgentWorker

type MapReduce<'work,'reduce>( numberMap:int , 
                               numberReduce: int, 
                               toProcess:'work list,  
                               map:'work->Async<'reduce WorkOutcome>,
                               reduce:'reduce-> Async<unit WorkOutcome>) = 

    let mapSupervisor= AgentSupervisor.getNew("MapSupervisor")  
    let reduceSupervisor  = AgentSupervisor.getNew("ReduceSupervisor")

    let workerSupervisors = {Map = mapSupervisor ; Reduce = reduceSupervisor }

    let mapWorkers = 
        [for i in 1..numberMap -> 
            AgentWorker.getNewMapWorker(map,workerSupervisors) ]
    let reduceWorkers = 
        [for i in 1..numberReduce -> 
            AgentWorker.getNewReduceWorker(reduce,workerSupervisors.Reduce) ] 

    member this.Start() = 
        //Post work to do
        toProcess
        |>List.iter(fun elem -> mapSupervisor.Post( WorkRel(ToDo(elem))))
        //Start supervisors
        mapSupervisor.Start()
        reduceSupervisor.Start()
        //start workers 
        List.iter( fun mapper -> mapper |>start) mapWorkers 
        List.iter( fun reducer ->reducer|>start) reduceWorkers

    member this.Status() =  (mapSupervisor|>AgentSupervisor.status)
                            (reduceSupervisor|>AgentSupervisor.status)
    member this.Stop() = 
        List.map2(fun mapper reducer -> 
            mapper |>stop; reducer|>stop) mapWorkers reduceWorkers


let noiseWords = [|"a"; "about"; "above"; "all"; "along"; "also"; "although"; "am"; "an"; "any"; "are"; "aren't"; "as"; "at";
            "be"; "because"; "been"; "but"; "by"; "can"; "cannot"; "could"; "couldn't";
            "did"; "didn't"; "do"; "does"; "doesn't"; "e.g."; "either"; "etc"; "etc."; "even"; "ever";
            "for"; "from"; "further"; "get"; "gets"; "got"; "had"; "hardly"; "has"; "hasn't"; "having"; "he"; 
            "hence"; "her"; "here"; "hereby"; "herein"; "hereof"; "hereon"; "hereto"; "herewith"; "him"; 
            "his"; "how"; "however"; "I"; "i.e."; "if"; "into"; "it"; "it's"; "its"; "me"; "more"; "most"; "mr"; "my";
            "near"; "nor"; "now"; "of"; "onto"; "other"; "our"; "out"; "over"; "really"; "said"; "same"; "she"; "should"; 
            "shouldn't"; "since"; "so"; "some"; "such"; "than"; "that"; "the"; "their"; "them"; "then"; "there"; "thereby"; 
            "therefore"; "therefrom"; "therein"; "thereof"; "thereon"; "thereto"; "therewith"; "these"; "they"; "this"; 
            "those"; "through"; "thus"; "to"; "too"; "under"; "until"; "unto"; "upon"; "us"; "very"; "viz"; "was"; "wasn't";
            "we"; "were"; "what"; "when"; "where"; "whereby"; "wherein"; "whether"; "which"; "while"; "who"; "whom"; "whose";
            "why"; "with"; "without"; "would"; "you"; "your" ; "have"; "thou"; "will"; "shall"|]
let getFilePath fileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(__SOURCE_DIRECTORY__ ),"Data", fileName)
let filesToProcess = ( [| "DocTest.txt"; "DocTest2.txt" ;"DocTest3.txt"|] |> Array.map getFilePath)

let readFile filePath = async {
    use fileStream = System.IO.File.OpenRead(filePath)
    use reader = new System.IO.StreamReader(fileStream)
    let! text = reader.ReadToEndAsync() |> Async.AwaitTask
    return text }


let mapF filePath =
    async {
        let! text = readFile filePath
        let punctuation = [| ' '; '.'; ','|]
        let words = text.Split(punctuation, StringSplitOptions.RemoveEmptyEntries)
        let res =  
            words 
            |> Seq.map (fun word -> word.ToUpper())
            |> Seq.filter (fun word -> not (noiseWords |> Seq.exists (fun noiseWord -> noiseWord.ToUpper() = word)) && Seq.length word > 3)
            |> Seq.groupBy id 
            |> Seq.map (fun (key, values) -> (key, values |> Seq.length)) |> Seq.toList 
        return Success(res) }
            


//Run some tests
let map = fun (n:string) -> mapF n 
let reduceF (data : (string * int) list) =
    async {
        let res = 
            data
            |> Seq.groupBy fst 
            |> Seq.map (fun (key, values) -> (key, values |> Seq.sumBy snd)) 
            |> Seq.toList
        printfn "Total %d" res.Length
        return Success() }

let reduce = function (toto: int64) -> async{ return Success() }

let mp = MapReduce<string,(string * int) list>(3, 1, filesToProcess |> Array.toList, map, reduceF)

mp.Start()
mp.Status()
mp.Stop()



