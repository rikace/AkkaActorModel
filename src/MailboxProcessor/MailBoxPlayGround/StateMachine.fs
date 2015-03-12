module StateMachine

// define the messages which can be used to change the state,
// using a Discriminated Union
type message =
    | HeatUp
    | CoolDown
 
// define the actor
let climateControl = MailboxProcessor.Start( fun inbox ->
 
    // the 'heating' state
    let rec heating() = async {
        printfn "Heating"
        let! msg = inbox.Receive()
        match msg with
        | CoolDown -> return! cooling()
        | _ -> return! heating()}
 
    // the 'cooling' state
    and cooling() = async {
        printfn "Cooling"
        let! msg = inbox.Receive()
        match msg with
        | HeatUp -> return! heating()
        | _ -> return! cooling()}
 
    // the initial state
    heating()
    )

climateControl.Post HeatUp
 
climateControl.Post CoolDown

