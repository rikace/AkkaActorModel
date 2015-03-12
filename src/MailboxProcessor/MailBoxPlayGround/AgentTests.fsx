//#load "..\Utilities\show-wpf40.fsx"
//#load "..\Utilities\show.fs"

open System
open System.IO
open System.Threading
open Microsoft.FSharp.Control
open System.Collections.Generic


/// The internal type of messages for the agent
type Message = Word of string | Fetch of AsyncReplyChannel<Map<string,int>> | Stop
 
type WordCountingAgent() =
    let counter = MailboxProcessor.Start(fun inbox ->
             // The states of the message processing state machine...
             let rec loop(words : Map<string,int>) =
                async { let! msg = inbox.Receive()
                        match msg with
                        | Word word ->
                            if words.ContainsKey word then
                                let count = words.[word]
                                let words = words.Remove word
                                return! loop(words.Add (word, (count + 1)) )
                            else
                                // do printfn "New word: %s" word
                                return! loop(words.Add (word, 1) )
                               
                        | Stop ->
                            // exit
                            return ()
                        | Fetch  replyChannel  ->
                            // post response to reply channel and continue
                            do replyChannel.Reply(words)
                            return! loop(words) }
 
             // The initial state of the message processing state machine...
             loop(Map.empty))
 
    member a.AddWord(n) = counter.Post(Word(n))
    member a.Stop() = counter.Post(Stop)
    member a.Fetch() = counter.PostAndAsyncReply(fun replyChannel -> Fetch(replyChannel))



let counter = new WordCountingAgent()
 
let readLines file =
  seq { use r = new StreamReader( File.OpenRead file )
        while not r.EndOfStream do yield r.ReadLine() }
 
let processFile file = async {
    let lines = readLines file
    for line in lines do
        let punctuation = [| ' '; '.'; '"'; ''';
          ','; ';'; ':'; '!'; '?'; '-'; '('; ')'; |]
        let words = line.Split(punctuation)
        for word in words do
            if word.Length > 0 then
                counter.AddWord word 
    return! counter.Fetch()  }
 
let start() =
    let autoResetEvent = new AutoResetEvent(false)
    let files = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(__SOURCE_DIRECTORY__) ,"Data"), "*.txt")
    
    files 
    |> Array.map processFile
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore

    Async.StartWithContinuations(counter.Fetch(),
        (fun res -> printfn "Finished Words: %i" res.Count),
        (fun ex->()),
        (fun cnl -> ()))
   