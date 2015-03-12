// Adapted from: http://msdn.microsoft.com/en-us/library/ee370407.aspx
module GetReplies 
    open System
    type Message = string * AsyncReplyChannel<string>
    type Agent<'T> = MailboxProcessor<'T>

    let postAndReply = 

        let formatString = "Received message: {0}" 

        let agent = Agent<Message>.Start(fun inbox ->
            let rec loop () =
                async { let! (message, replyChannel) = inbox.Receive();
                        replyChannel.Reply(String.Format(formatString, message))
                        do! loop ()
                }
            loop ())

        printfn "Mailbox Processor Test"
        printfn "Type some text and press Enter to submit a message." 

        while true do
            printf "> " 
            let input = Console.ReadLine()

            //PostAndReply blocks
            let messageAsync = agent.PostAndAsyncReply(fun replyChannel -> 
                                                            input, replyChannel)

            Async.StartWithContinuations(messageAsync, 
                 (fun reply -> printfn "Reply received: %s" reply), //continuation
                 (fun _ -> ()), //exception
                 (fun _ -> ())) //cancellation
