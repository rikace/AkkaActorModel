


type States = 
    | State1
    | State2
    | State3

type StateMachine() = 
    let stateMachine = new MailboxProcessor<States>(fun inbox ->
                let rec state1 () = async {
                    printfn "current state is State1"
                    // <your operations>

                    //get another message and perform state transition
                    let! msg = inbox.Receive()
                    match msg with
                        | State1 -> return! (state1())
                        | State2 -> return! (state2())
                        | State3 -> return! (state3())
                    }
                and state2() = async {
                    printfn "current state is state2"
                    // <your operations>

                    //get another message and perform state transition
                    let! msg = inbox.Receive()
                    match msg with
                        | State1 -> return! (state1())
                        | State2 -> return! (state2())
                        | State3 -> return! (state3())
                    }
                and state3() = async {
                    printfn "current state is state3"
                    // <your operations>

                    //get another message and perform state transition
                    let! msg = inbox.Receive()
                    match msg with
                        | State1 -> return! (state1())
                        | State2 -> return! (state2())
                        | State3 -> return! (state3())
                    } 
                and state0 () = 
                    async {

                        //get initial message and perform state transition
                        let! msg = inbox.Receive()
                        match msg with
                            | State1 -> return! (state1())
                            | State2 -> return! (state2())
                            | State3 -> return! (state3())
                    }
                state0 ())

    //start the state machine and set it to state0
    do 
        stateMachine.Start()        

    member this.ChangeState(state) = stateMachine.Post(state)

let stateMachine = StateMachine()
stateMachine.ChangeState(States.State2)
stateMachine.ChangeState(States.State1)
