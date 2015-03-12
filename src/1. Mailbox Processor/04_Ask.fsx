module Ask

#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open Akka.FSharp
open Akka.Actor
open System
open System.IO


// Actor example with the ask function
// In the actor we use 'sender <!' to return the value.


let filePath = __SOURCE_DIRECTORY__ + @"\..\..\src\Data\words.txt"


let system = ActorSystem.Create("Ask-System")

let readFile (filePath:string) = async {
    use fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 0x100, true)
    use stream = new AsyncStreamReader(fileStream)
    let! response = stream.ReadToEnd()
    return response }

let echoServer = 
    spawn system "ReadFileServer"
    <| fun mailbox ->
        let rec loop() =
            actor {
                let! message = mailbox.Receive()
                let sender = mailbox.Sender()
                match box message with
                | :? string as filePath -> 
                        async {
                            let! response = readFile filePath
                            printfn "actor: done!"
                            sender <! response } |> Async.Start 
                        return! loop() 
                | _ ->  failwith "unknown message"
            } 
        loop()


let task = (echoServer <? filePath)

let response = Async.RunSynchronously (task)
let fileSize = string(response) |> String.length

printfn "File size %d" fileSize

system.Shutdown()

