open System
open System.Threading
open System.IO

//~~~~~~~~~~~~~~~~ OBSERVABLE JUKEBOX   ~~~~~~~~~~~~~~~~~~~~~~~~


[<Measure>]
type minute

[<Measure>]
type bpm = 1/minute

type MusicGenre = Classical | Pop | HipHop | Rock | Latin | Country

type Song = { Title : string; Genre : MusicGenre; BPM : int<bpm> }

type SongChangeArgs(title : string, genre : MusicGenre, bpm : int<bpm>) =
    inherit System.EventArgs()

    member this.Title = title
    member this.Genre = genre
    member this.BeatsPerMinute = bpm

type SongChangeDelegate = delegate of obj * SongChangeArgs -> unit



type JukeBox() =
    let m_songStartedEvent = new Event<SongChangeDelegate, SongChangeArgs>()

    member this.PlaySong(song) =
        m_songStartedEvent.Trigger(this,
                new SongChangeArgs(song.Title, song.Genre, song.BPM))

    [<CLIEvent>]
    member this.SongStartedEvent = m_songStartedEvent.Publish


let jb = JukeBox()
let fastSongEvent, slowSongEvent =
    jb.SongStartedEvent
    // Filter event to just dance music
    |> Observable.filter(fun songArgs ->
            match songArgs.Genre with
            | Pop | HipHop | Latin | Country -> true
            | _ -> false)
    // Split the event into 'fast song' and 'slow song'
    |> Observable.partition(fun songChangeArgs ->
            songChangeArgs.BeatsPerMinute >= 120<bpm>);;

slowSongEvent.Add(fun args -> printfn"You hear '%s' and start to dance slowly..."
                                  args.Title)

fastSongEvent.Add(fun args -> printfn "You hear '%s' and start to dance fast!" args.Title);;

jb.PlaySong( { Title = "Burnin Love"; Genre = Pop; BPM = 120<bpm> } );;

jb.PlaySong( { Title = "Country Song"; Genre = Country; BPM = 58<bpm> } );;

