[<AutoOpenAttribute>]
module PrintWithColor

open System

let cprintfWith endl c fmt = 
    Printf.kprintf 
        (fun s -> 
            let old = System.Console.ForegroundColor 
            try 
              System.Console.ForegroundColor <- c;
              System.Console.Write (s + endl)
            finally
              System.Console.ForegroundColor <- old) 
        fmt

let cprintf c fmt = cprintfWith "" c fmt
let cprintfn c fmt = cprintfWith "\n" c fmt


//cprintfn ConsoleColor.Red "Ciao"