module PingPong

type Agent<'a> = MailboxProcessor<'a>

let pong1 = Agent<string * (string -> unit)>.Start(fun inbox ->
  let rec loop (state: System.Collections.Generic.List<string>) = async {
    let! msg = inbox.Receive()
    match msg with
    // Build up state and return it at the end.
    | "Stop", _ ->
        for m in state do System.Console.WriteLine m
    | m, f ->
        state.Add m
        if state.Count > 5 then
          f "Bail!"
          for m in state do System.Console.WriteLine m
        else
          f "Pong"
          return! loop state
  }
  loop (new System.Collections.Generic.List<string>()) )

let pong2 = Agent<string * (string -> unit)>.Start(fun inbox ->
  let rec loop (state: System.Collections.Generic.List<string>) = async {
    let! msg = inbox.Receive()
    match msg with
    // Build up state and return it at the end.
    | "Stop", _ ->
        for m in state do System.Console.WriteLine m
    | m, f ->
        state.Add m
        f "Pong"
        return! loop state
  }
  loop (new System.Collections.Generic.List<string>()) )

let rec ping (target1: Agent<_>) (target2: Agent<_>) = Agent<string>.Start(fun inbox ->
  async {
    let target = ref target1
    for x=1 to 10 do
      (!target).Post("Ping", inbox.Post)
      let! msg = inbox.Receive()
      if msg = "Bail!" then
        target := target2
      System.Console.WriteLine msg
    (!target).Post("Stop", inbox.Post)
  })