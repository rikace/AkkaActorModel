#load "AgentSystem.fs"
#load "MapReduceSystem.fs"
#load "..\Utilities\show.fs"
open System
open System.IO
open System.Collections.Generic
open System.Threading
open Microsoft.FSharp.Control
open AgentSystem
open MapReduceSystem.MapReduce

module MapReduce_Simple = 
    let data = [| 1..100 |]

    let reduceFunction (list:seq<int>) = list |> Seq.sum
    let mapData () =
        [0..9] 
        |> Seq.map (fun i -> i * 10,(i + 1) * 10 - 1)
        |> Seq.map (fun (a,b) -> async { let! sum = async { return reduceFunction (data.[a..b]) } 
                                                    |> Async.StartChild 
                                         return! sum })
    let reduceData (seq:seq<Async<int>>) = seq |> Seq.sumBy Async.RunSynchronously

    let mapReduceF = mapData() |> reduceData

    printfn "final result = %A" mapReduceF


    // Given a key and some buckets, it picks one of the buckets. Its type is: ‘a –> int –> int
    // First a simple partition function can be defined as:
    let partitionF = fun key M -> abs(key.GetHashCode()) % M 

    // agent that just prints out the reduced values:
    let printer:AsyncAgent<ReceiverMessage<string, seq<int>>, unit> = spawnWorker (fun msg ->   match msg with
                                                                                                | Reduced(key, value)   -> showA (sprintf "%A %A" key value)
                                                                                                                           printfn "%A %A" key value
                                                                                                | MapReduceDone         -> showA "All done!!"
                                                                                                                           printfn "All done!!")

    // The agent gets notified whenever a new key is reduced or the algorithm ends. 
    // Agents force you to think explicitly about the parallelism in your app. 
    // The mapping function simply split the content of a file into words and adds a word/1 pair to the list. 
    let map = fun (fileName:string) (fileContent:string) ->
                let l = new List<string * int>()
                let wordDelims = [|' ';',';';';'.';':';'?';'!';'(';')';'\n';'\t';'\f';'\r';'\b'|]
                fileContent.Split(wordDelims) |> Seq.iter (fun word -> l.Add((word, 1)))
                l :> seq<string * int>

    //The reducer function simply sums the various word statistics sent by the mappers
    let reduce = fun key (values:seq<int>) -> [values |> Seq.sum] |> seq<int>

    //Now we can create some fake input to check that it works:
    let testInput = ["File1", "I was going to the airport when I saw someone crossing";
                                   "File2", "I was going home when I saw you coming toward me"]   


    mapReduce testInput map reduce printer 2 2 partitionF


(************* MAP REDUCE WORDS COUNT **************************************)
module MapReduce_WordCount =
    // The agent gets notified whenever a new key is reduced or the algorithm ends. 
    // Agents force you to think explicitly about the parallelism in your app. 
    // The mapping function simply split the content of a file into words and adds a word/1 pair to the list. 
    let map = fun (fileName:string) (fileContent:string) ->
                let l = new List<string * int>()
                let wordDelims = [|' ';',';';';'.';':';'?';'!';'(';')';'\n';'\t';'\f';'\r';'\b'|]
                fileContent.Split(wordDelims) |> Seq.iter (fun word -> l.Add((word, 1)))
                l :> seq<string * int>

    // Given a key and some buckets, it picks one of the buckets. Its type is: ‘a –> int –> int
    // First a simple partition function can be defined as:
    let partitionF = fun key M -> abs(key.GetHashCode()) % M 

    //The reducer function simply sums the various word statistics sent by the mappers
    let reduce = fun key (values:seq<int>) -> [values |> Seq.sum] |> seq<int>

    let printF (msg:string) = printfn "%s" msg; showA (sprintf "%s%s" msg Environment.NewLine)

    // use of mapReduce to found the frequency of words in several text files
    let gathererF = fun msg (data:List<string * int>, counter, step) ->
                        match msg with
                        | Reduced(key, value)   ->
                            if counter % step = 0 then
                                printF (sprintf "Processed %i words. Now processing %s" counter key)
                            data.Add((key, value |> Seq.head))
                            data, counter + 1, step
                        | MapReduceDone         ->
                            data
                            |> Seq.distinctBy (fun (key, _) -> key.ToLower())
                            |> Seq.filter (fun (key, _) -> not(key = "" || key = "\"" || (fst (Double.TryParse(key)))))
                            |> Seq.toArray
                            |> Array.sortBy snd
                            |> Array.rev
                            |> Seq.take 20
                            |> Seq.iter (fun (key, value) ->  printF (sprintf "%A\t\t%A" key value))
                            printF (sprintf "All done!!")
                            data, counter, step                           
                    

    // Every time a new word is reduced, a message is printed out and the result is added to a running list.
    // We want to maximize the number of processors to use, so let’s split the books in chunks so that they can be operated in parallel
    let gatherer:AsyncAgent<ReceiverMessage<string,seq<int>>,(List<string * int> * int * int)> = 
                    spawnAgent gathererF (new List<string * int>(), 0, 1000)

    let splitBook howManyBlocks fileName =
        let buffers = Array.init howManyBlocks (fun _ -> new System.Text.StringBuilder())
        fileName
        |> File.ReadAllLines
        |> Array.iteri (fun i line -> buffers.[i % (howManyBlocks)].Append(line) |> ignore)
        buffers

    let getFilePath fileName = Path.Combine(Path.GetDirectoryName(__SOURCE_DIRECTORY__ ),"Data", fileName)

    let blocks1 = getFilePath  "DocTest.txt" |> splitBook 100
    let blocks2 = getFilePath  "DocTest2.txt" |> splitBook 100
    let blocks3 = getFilePath  "DocTest3.txt" |> splitBook 100


    // 
    let input =
        blocks1 |> Array.append blocks2 |> Array.append blocks3 
        |> Array.mapi (fun i b -> i.ToString(), b.ToString())
        
    mapReduce input map reduce gatherer 20 20 partitionF



