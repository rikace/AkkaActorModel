
let start (filePath:string) =
    System.Diagnostics.Process.Start(filePath) |> ignore

let node2 = __SOURCE_DIRECTORY__ + "/../Node2/bin/Debug/Node2.exe"
let node1 = __SOURCE_DIRECTORY__ + "/../Node1/bin/Debug/Node1.exe"

node2 |> start
node1 |> start


