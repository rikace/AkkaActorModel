
(*“Be sure to remove an event handler when an object no longer needs to subscribe to an event. 
Otherwise, the event raiser will still have a reference to the object and therefore will 
prevent the subscriber from being garbage collected. This is a very common way to introduce 
memory leaks in .NET applications.
If the Event is a result of Event-Combinators, the RemoveHanlder leaves some hanlders!
Better to use Observable*)

type SetAction = Added | Removed

type SetOperationEventArgs<'a>(value : 'a, action : SetAction) =
    inherit System.EventArgs()

    member this.Action = action
    member this.Value = value

type SetOperationDelegate<'a> = delegate of obj * SetOperationEventArgs<'a> -> unit

type NoisySet<'a when 'a : comparison>() =
    let mutable m_set = Set.empty : Set<'a>

    let m_itemAdded =
        new Event<SetOperationDelegate<'a>, SetOperationEventArgs<'a>>()

    let m_itemRemoved =
        new Event<SetOperationDelegate<'a>, SetOperationEventArgs<'a>>()

    member this.Add(x) =
        m_set <- m_set.Add(x)        
        m_itemAdded.Trigger(this, new SetOperationEventArgs<_>(x, Added))

    member this.Remove(x) =
        m_set <- m_set.Remove(x)
        // Fire the 'Remove' event
        m_itemRemoved.Trigger(this, new SetOperationEventArgs<_>(x, Removed))

    // Publish the events so others can subscribe to them
    member this.ItemAddedEvent   = m_itemAdded.Publish
    member this.ItemRemovedEvent = m_itemRemoved.Publish


let s = new NoisySet<int>()

let setOperationHandler =
    new SetOperationDelegate<int>(
        fun sender args ->
            printfn "%d was %A" args.Value args.Action
    )

s.ItemAddedEvent.AddHandler(setOperationHandler)
s.ItemRemovedEvent.AddHandler(setOperationHandler)


(*If your events don’t follow the pattern (Object, Delegate) wherre the first parameter is the sender object,
 then you should use the DelegateEvent<'del> type instead. It is used the same way, 
 except that its arguments are passed in as an obj array.*)

open System
open System.Threading
type ClockUpdateDelegate = delegate of int * int * int -> unit

type Clock() =
    let m_event = new DelegateEvent<ClockUpdateDelegate>()

    member this.Start() =
        printfn "Started..."
        while true do
            Threading.Thread.Sleep(1500)

            let hour   = DateTime.Now.Hour
            let minute = DateTime.Now.Minute
            let second = DateTime.Now.Second

            m_event.Trigger( [| box hour; box minute; box second |] )

    member this.ClockUpdate = m_event.Publish

let c = new Clock()
c.ClockUpdate.AddHandler(new ClockUpdateDelegate(fun h m s -> printfn "[%d:%d:%d]" h m s))
c.Start()


