open System
open System.Threading

(*This is a simple implementation of an object pool using an agent (MailboxProcessor). 
The pool is created with an initial number of object using the specified generator. 
The ObjectPool has three functions: Put: An item can be 'Put' into the pool. 
Get: An item can be taken from the pool ToListAndClear: 
A list of all the items in the pool is returned and the pool is cleared.*)

//Agent alias for MailboxProcessor
type Agent<'T> = MailboxProcessor<'T>

///One of three messages for our Object Pool agent
type PoolMessage<'a> =
    | Get of AsyncReplyChannel<'a>
    | Put of 'a
    | Clear of AsyncReplyChannel<List<'a>>

/// Object pool representing a reusable pool of objects
type ObjectPool<'a>(generate: unit -> 'a, initialPoolCount) = 
    let initial = List.init initialPoolCount (fun (x) -> generate())
    let agent = Agent.Start(fun inbox ->
        let rec loop(x) = async {
            let! msg = inbox.Receive()
            match msg with
            | Get(reply) -> 
                let res = match x with
                          | a :: b -> 
                              reply.Reply(a);b
                          | [] as empty-> 
                              reply.Reply(generate());empty
                return! loop(res)
            | Put(value)-> 
                return! loop(value :: x) 
            | Clear(reply) -> 
                reply.Reply(x)
                return! loop(List.empty<'a>) }
        loop(initial))

    /// Clears the object pool, returning all of the data that was in the pool.
    member this.ToListAndClear() = 
        agent.PostAndAsyncReply(Clear)
    /// Puts an item into the pool
    member this.Put(item ) = 
        agent.Post(item)
    /// Gets an item from the pool or if there are none present use the generator
    member this.Get(item) = 
        agent.PostAndAsyncReply(Get)

type ObjGuid() =
    let guid = System.Guid.NewGuid()
    member x.GetId = guid

let agent = ObjectPool<ObjGuid>((fun _ -> ObjGuid()), 2)
agent.Get()