

let start exePath =
    System.Diagnostics.Process.Start(exePath:string) |> ignore


start @"C:\Git\AkkaActorModel\ChatServer\bin\Debug\ChatServer.exe"
[0..2] |> List.iter (fun _ -> start @"C:\Git\AkkaActorModel\ChatClient\bin\Debug\ChatClient.exe")



