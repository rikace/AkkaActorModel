module ActorWithControl

    open System
    open System.Threading
    open System.IO
    open Microsoft.FSharp.Control.WebExtensions

    type Agent<'T> = MailboxProcessor<'T>

    type SingleAgent<'T> =
    | Set of 'T
    | Get of AsyncReplyChannel<List<'T>>

    type Identifier = 
    | RoomId of Guid // Room is a container (e.g. a game)
    | UserId of Guid // User (e.g. a player)
    
    type ControlMethods<'T> =
    | Create of Identifier * Agent<'T>
    | Read of Identifier * AsyncReplyChannel<Option<Agent<'T>>>
    | Delete of Identifier
    | ReadAllIds of AsyncReplyChannel<List<Identifier>>
    | ReadAllOf of (Identifier -> bool)*AsyncReplyChannel<List<Identifier * Agent<'T>>>
    
    let internal notifyError = new Event<_>()
    let public OnError (watch:Action<_>) =
        notifyError.Publish |> Observable.add(fun e -> watch.Invoke(e))

    // Error queue agent
    let internal supervisor = 
        Agent<System.Exception>.Start(fun inbox ->
            async { while true do 
                    let! err = inbox.Receive()
                    notifyError.Trigger(err)
                    printfn "an error occurred in an agent: %A" err })

    // Agent for storing other agents
    let internal control = new Agent<ControlMethods<SingleAgent<obj>>>(fun msg ->
        let rec msgPassing all =
            async { 
                let! c = msg.Receive()
                match c with
                | Create(id,agent) ->
                    return! msgPassing((id,agent)::all)
                | Read(id,reply) ->
                    let response = 
                        all 
                        |> List.filter (fun i -> (fst i) = id) 
                    match response with
                    | [] -> reply.Reply(None)
                    | h::t -> reply.Reply(Some(snd h))
                    return! msgPassing(all)
                | Delete(id) -> 
                    let removed = 
                        all 
                        |> List.filter (fun i -> (fst i) <> id) 
                    return! msgPassing(removed)
                | ReadAllIds(reply) -> 
                    let agents = all |> List.map fst
                    reply.Reply(agents)
                    return! msgPassing(all)
                | ReadAllOf(myfilter, reply) -> 
                    let agents = 
                        all
                        |> List.filter(fun i -> myfilter(fst i)) 
                    reply.Reply(agents)
                    return! msgPassing(all)
            }
        msgPassing [])
    control.Error.Add(fun error -> supervisor.Post error)
    control.Start()

    /// Create a new actor (like room or user)
    let public CreateNewItem id initialState = 
        let agent = new Agent<_>(fun msg ->
            let rec msgPassing all =
                async { 
                    let! r = msg.Receive()
                    match r with
                    | Set(i) ->
                        //printf "%s" r
                        //let r = f(c)
                        return! msgPassing(i::all)
                    | Get(reply) ->
                        reply.Reply(all)
                        return! msgPassing(all)
                }
            msgPassing [])
        (id, agent) |> Create |> control.Post
        agent.Error.Add(fun error -> supervisor.Post error)
        agent.Post(Set(initialState))
        agent.Start()
        id

    /// Fetch agent form the control agent
    let internal fetchAgent id = control.PostAndReply(fun a -> Read(id, a))

    /// Insert item state
    let public AddAction id msg =
        match fetchAgent id with
        | Some(agent) -> 
            agent.Post(Set(msg))
            true
        | _ -> false

    /// Get item state
    let public ShowItemState id =
        let result = 
            match fetchAgent id with
            | Some(agent) -> agent.PostAndReply(fun msg -> Get(msg))
            | _ -> []
        result |> List.toSeq

    /// This just removes the reference
    let public Delete id =
        Delete(id) |> control.Post

    /// Return all states
    let public ReturnAllOf(i) =
        let rec fetch (r:list<Identifier*Agent<_>>) (acc:list<Identifier*seq<_>>) =
            match r with
            | [] -> acc
            | iagent::t -> 
                let agent = snd iagent
                let one = (fst iagent), agent.PostAndReply(fun msg -> Get(msg)) |> List.toSeq
                fetch t (one :: acc) 

        let result = control.PostAndReply(fun a -> ReadAllOf(i, a)) 
        fetch result []
        |> List.toSeq

    /// Return all states
    let public ReturnAll() = ReturnAllOf(fun _ -> true)

    // --------------------------------------------------------------------------------

    ///Some helper functions for domain "game"
    let internal isRoom = function | RoomId(_) -> true | _ -> false
    let internal isUser = function | UserId(_) -> true | _ -> false

    let public ReturnAllGames() =
        control.PostAndReply(fun a -> ReadAllIds(a)) 
        |> List.filter(isRoom)
        |> List.toSeq

    let public ReturnUserData() = ReturnAllOf(isUser)
    let public ReturnGameData() = ReturnAllOf(isRoom)

    let NewUser info = 
        let id = Guid.NewGuid() |> UserId
        CreateNewItem id info

    type Actions =
    | PlayerJoin of Identifier
    | VisitorJoin of Identifier
    | PlayerMakeMove of Identifier*obj
    | SendMsgToAll of Identifier*string
    | Leave of Identifier

    type UserActions =
    | PlayedGame of Identifier*int//opponent, result
    | RegisterAsUser of string*int*int*obj //id, name, games, scores, ...

    let NewGameRoom player = 
        let a = player |> Actions.PlayerJoin
        let id = Guid.NewGuid() |> RoomId
        CreateNewItem id a 

    // --------------------------------------------------------------------------------

(*
    //Add a player
    let player1 = ("tuomas", 0, 0, obj()) |> UserActions.RegisterAsUser |> NewUser

    //Create new game
    let game1 = player1 |> NewGameRoom

    //Add another player    
    let player2 = ("toka", 0, 0, obj()) |> UserActions.RegisterAsUser |> NewUser

    //AddAction will add any object to any actor.
    //There is no limit for objects, but it is easier to follow if 
    //objects are custom types like Actions or UserActions here

    //Adding info to player1:
    UserActions.PlayedGame(player2, 1) |> AddAction player1 

    //Adding info to game1:
    Actions.SendMsgToAll(player1, "hello") |> AddAction game1 
    Actions.PlayerMakeMove(player1, (8, 3)) |> AddAction game1 
    player2 |> Actions.PlayerJoin |> AddAction game1 

    //Show item history/state:
    ShowItemState game1
    ShowItemState player1


    let anotherGame = 
        let newPlayer = ("simppa", 0, 0, obj()) |> UserActions.RegisterAsUser |> NewUser
        let game2 = newPlayer |> NewGameRoom
        Actions.SendMsgToAll(newPlayer, "hello") |> AddAction game2 
    ReturnAll()

    Delete game1
*)