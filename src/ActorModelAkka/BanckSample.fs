module BanckSample


#if INTERACTIVE
#r @"..\..\bin\Akka.dll"
#r @"..\..\bin\Akka.FSharp.dll"
#r @"..\..\bin\Akka.Remote.dll"
#r @"..\..\bin\FSharp.PowerPack.dll"
#endif

open System
open Akka.FSharp
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

type Withdraw = { amount:decimal }
    
type BankAccountActor() =
    inherit TypedActor()

    let mutable balance = 0m

    interface IHandle<Withdraw> with 
        member this.Handle(withdraw:Withdraw) = 
             if balance < withdraw.amount then
                printfn "You don't have enaough money"
                this.Sender <! "fail"
             else
                balance <- balance - withdraw.amount
                printfn "The the new balance is %A" balance
                this.Sender <! "Success"


type BankAccountActorTest() =
    inherit TypedActor()

    let mutable balance = 0

    interface IHandle<string> with 
        member this.Handle(withdraw:string) = 
                printfn "You don't have enaough money"                
        
                


type BacnkAccount() =
    
    let _lock = new Object()
    let mutable balance = 0m

    // this will work fine in a single threaded environment 
    // where only one thread is accessing the above code
    member x.Withdraw(amount:decimal) =
        
        if balance < amount then // ?? no thread safe
            raise (ArgumentException("amount may not be larger than account balance"))

        balance <- balance - amount
        
        // ...more code here



    member x.WithdrawThreadSafe(amount:decimal) =
        
        lock(_lock)(fun () ->
            if balance < amount then // ?? no thread safe
                raise (ArgumentException("amount may not be larger than account balance"))

            balance <- balance - amount
        
        // ...more code here

        )


let system = ActorSystem.Create("bank-system")
let bankAccount = system.ActorOf<BankAccountActor>()

bankAccount <! "10"

bankAccount <! {amount = 10m}

let (balance:string) = bankAccount <? {amount = 10m} |> Async.RunSynchronously
