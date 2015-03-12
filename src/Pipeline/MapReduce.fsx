#load "..\CommonModule.fsx"
//#load "Utilities\show-wpf40.fsx"
#load "..\3.async\8.BarrierAsync.fsx"

open System
open System.IO
open System.Collections.Generic
open System.Threading
open Microsoft.FSharp.Control
open Common
open AsyncHelper

(*  Async is a very versatile structure, which has been used to compose CPU/IO bound computations. 
    So it is very tempting to implement a MapReduce function based on Async 
    and borrowing ideas from the theory of list homomorphisms.*)

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

let getFilePath fileName = Path.Combine(Path.GetDirectoryName(__SOURCE_DIRECTORY__ ),"Data", fileName)

let filesToProcess = ( [| "DocTest.txt"; "DocTest2.txt" ;"DocTest3.txt"|] |> Array.map getFilePath)

let mapReduce (mapF : 'T -> Async<'R>) (reduceF : 'R -> 'R -> Async<'R>) (input : 'T []) : Async<'R> = 
    let rec mapReduce' s e =
        async { 
            if s + 1 >= e then return! mapF input.[s]
            else 
                let m = (s + e) / 2
                let! (left, right) =  mapReduce' s m <||> mapReduce' m e
                return! reduceF left right
        }
    mapReduce' 0 input.Length




let (<||>) first second = async { 
    let! results = Async.Parallel([|first; second|]) 
    return (results.[0], results.[1]) }

let readFile filePath = async {
    use fileStream = File.OpenRead(filePath)
    use reader = new StreamReader(fileStream)
    let! text = reader.ReadToEndAsync() |> Async.AwaitTask
    return text }


let mapF filePath =
    async {
        let! text = readFile filePath
        let punctuation = [| ' '; '.'; ','|]
        let words = text.Split(punctuation, StringSplitOptions.RemoveEmptyEntries)
        return 
            words 
            |> Seq.map (fun word -> word.ToUpper())
            |> Seq.filter (fun word -> not (noiseWords |> Seq.exists (fun noiseWord -> noiseWord.ToUpper() = word)) && Seq.length word > 3)
            |> Seq.groupBy id 
            |> Seq.map (fun (key, values) -> (key, values |> Seq.length)) |> Seq.toList
    }

let reduceF (left : (string * int) list) (right : (string * int) list) = 
    async {
        return 
            left @ right 
            |> Seq.groupBy fst 
            |> Seq.map (fun (key, values) -> (key, values |> Seq.sumBy snd)) 
            |> Seq.toList
    }

mapReduce mapF reduceF filesToProcess
|> Async.RunSynchronously
|> List.sortBy (fun (_, count) -> -count) 
|> List.head


////////////// Map Phase   (MSDN) 
let inputFile = @"web.log"
let mapLogFileIpAddr logFile =
  let fileReader logFile = 
    seq { use fileReader = new StreamReader(File.OpenRead(logFile))
      while not fileReader.EndOfStream do
        yield fileReader.ReadLine() }    

  // Takes lines and extracts IP Address Out, 
  // filter invalid lines out first
  let cutIp = 
    let line = fileReader inputFile 
    line
    |> Seq.filter (fun line -> not (line.StartsWith("#")))
    |> Seq.map (fun line -> line.Split [|' '|])
    |> Seq.map (fun line -> line.[8],1)
    |> Seq.toArray
  cutIp

// Reduce Phase
let ipMatches = mapLogFileIpAddr inputFile
let reduceFileIpAddr = 
  Array.fold
    (fun (acc : Map<string, int>) ((ipAddr, num) : string * int) ->
      if Map.containsKey ipAddr acc then
        let ipFreq = acc.[ipAddr]
        Map.add ipAddr (ipFreq + num) acc
      else
        Map.add ipAddr 1 acc)
    Map.empty
    ipMatches

// Display Top 10 Ip Addresses
let topIpAddressOutput reduceOutput = 
  let sortedResults = 
    reduceFileIpAddr
    |> Map.toSeq
    |> Seq.sortBy (fun (ip, ipFreq) -> -ipFreq) 
    |> Seq.take 10
  sortedResults
  |> Seq.iter(fun (ip, ipFreq) ->
    printfn "%s, %d" ip ipFreq);;

reduceFileIpAddr |> topIpAddressOutput