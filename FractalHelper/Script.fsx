

let fractalRemote = __SOURCE_DIRECTORY__ + "/../Fractal/Fractal.Remote/bin/Debug/Fractal.Remote.exe"       
let fractal = __SOURCE_DIRECTORY__ + "/../Fractal/Fractal/bin/Debug/Fractal.exe"     

let start (filePath:string) =
    System.Diagnostics.Process.Start(filePath) |> ignore


start fractalRemote
start fractal 