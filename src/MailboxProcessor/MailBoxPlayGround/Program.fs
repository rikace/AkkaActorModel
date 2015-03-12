// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

type Message =
    | Increment
    | Print

[<EntryPoint>]
let main argv = 
    
    let mailbox = MailboxProcessor.Start(
        fun mailbox ->
            let rec loop state =
                async {
                    let! msg = mailbox.Receive()
                    match msg with
                    | Increment -> return! loop (state + 1)
                    | Print -> printfn "%i" state
                               return! loop state
                }
            loop 0)

    mailbox.Post(Print)
    mailbox.Post(Increment)
    mailbox.Post(Increment)
    mailbox.Post(Print)

    System.Console.ReadLine () |> ignore
    0 // return an integer exit code
