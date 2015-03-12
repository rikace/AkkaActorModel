module UpgradeAgent

// Note: static typing, this agent can't upgrade its state data type (so better to use obj or custom interface)...
// Also this runs only in localhost...
// So this is more a technical demo than something useful
// See slide 37: http://www.infoq.com/presentations/Message-Passing-Concurrency

open System

type Methods<'state, 'x, 'reply> = 
| Upgrade of 
    ('x*'state -> 'x*'state) // function what to do
    *('state -> 'state) // just for state conversion
| From of AsyncReplyChannel<'reply> * 'x

type UpgradableAgent<'state, 'x>() =
    let gen_server = MailboxProcessor.Start(fun msg ->
        let rec loop (state, f) =
            async { 
                let! receive = msg.Receive()
                match receive with
                | Upgrade(f1,f2) ->
                    let state1 = f2(state)
                    return! loop(state1, f1)
                | From(from, x) ->
                    let (reply, state1) = f(x,state)
                    from.Reply(reply)
                    return! loop(state1, f)
            }
        let initDoit (x,state) = (x, state)
        let initState = Unchecked.defaultof<'state> //None
        loop(initState, initDoit))
       
    member this.DoIt (item:'x) =  
        // could use also PostAndAsyncReply
        gen_server.PostAndReply(fun rep -> From(rep, item))

    member this.Upgrade functionality =  
        functionality |> Upgrade |> gen_server.Post
// [/snippet]
// [snippet:Some tests]
let test1 =
    let server = new UpgradableAgent<int, int>()
    let myfunc = fun (x,state) -> (x+state, state) 
    let myStateConvert = fun initstate -> 5
    server.Upgrade(myfunc, myStateConvert)
    Console.WriteLine(server.DoIt(7)); // 12
    Console.WriteLine(server.DoIt(7)); // 12

    let myfunc2 = fun (x,state) -> (x+state, x+state) 
    let myStateConvert2 = fun initstate -> 5
    server.Upgrade(myfunc2, myStateConvert2)
    Console.WriteLine(server.DoIt(7)); // 12
    Console.WriteLine(server.DoIt(7)); // 19
    Console.WriteLine(server.DoIt(7)); // 26
    
let test3 =
    let server3 = new UpgradableAgent<obj, string>()
    let myfunc3 ((x:string), (state:obj)) = (x+unbox(state), box(x+unbox(state))) 
    let myStateConvert3 = fun initstate -> box(" world!")
    server3.Upgrade(myfunc3, myStateConvert3)
    Console.WriteLine(server3.DoIt("hello")); // "hello world!"
    Console.WriteLine(server3.DoIt("hello ")); // "hello hello world!"