

let chatServer = __SOURCE_DIRECTORY__ + "/../ChatServer/bin/Debug/ChatServer.exe"       
let chatClient = __SOURCE_DIRECTORY__ + "/../ChatClient/bin/Debug/ChatClient.exe"       

let start (filePath:string) =
    System.Diagnostics.Process.Start(filePath) |> ignore


start chatServer

[0..2] |> Seq.iter(fun _ -> start chatClient)