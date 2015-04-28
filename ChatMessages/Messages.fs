module ChatMessages

open System


type ConnectRequest(username:string) =
    member x.UserName = username

type ConnectResponse(message:string) =
    member x.Message = message

type NickRequest(oldUsername:string, newUsername:string) = 
    member x.OldUsername = oldUsername
    member x.NewUSername = newUsername

type NickResponse(oldUsername:string, newUsername:string) =
    member x.OldUsername = oldUsername
    member x.NewUSername = newUsername

type SayRequest(username:string, text:string) = 
    member x.Username = username
    member x.Text = text

type SayResponse(username:string, text:string) =
    member x.Username = username
    member x.Text = text



//
//type ChatMessage = 
//    | ConnectRequest of username:string
//    | ConnectResponse of message:string
//    | NickRequest of oldUsername:string * newUsername:string
//    | NickResponse of oldUsername:string * newUsername:string    
//    | SayRequest of username:string * text:string
//    | SayResponse of username:string * text:string
//    | Disconnect

