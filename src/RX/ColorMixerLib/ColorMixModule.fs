namespace ColorMixerLib

open System
open System.Windows 
open System.Windows.Media
open System.Windows.Controls

type ColorEventArgs(color:Color) =
    inherit System.EventArgs()

    member this.Color = color    
 
type ColorDelegate = delegate of obj * ColorEventArgs -> unit

type ColorUpdate = 
  | Red of byte
  | Green of byte
  | Blue of byte

type Selector(SliderRed:Slider, SliderGreen:Slider, SliderBlue:Slider) as this =
  
  let sliderR : Slider = SliderRed
  let sliderG : Slider = SliderGreen
  let sliderB : Slider = SliderBlue

  let r = sliderR.ValueChanged |> Event.map (fun r -> Red(byte r.NewValue))
  let g = sliderG.ValueChanged |> Event.map (fun r -> Green(byte r.NewValue))
  let b = sliderB.ValueChanged |> Event.map (fun r -> Blue(byte r.NewValue))
      
  let colorChanged = 
    Event.merge (Event.merge r g) b
    |> Event.scan (fun (r, g, b) update ->
        match update with
        | Red nr -> (nr, g, b)
        | Green ng -> (r, ng, b)
        | Blue nb -> (r, g, nb) ) (0uy, 0uy, 0uy)
    |> Event.map (fun (r, g, b) -> 
       new SolidColorBrush(Color.FromArgb(255uy, r, g, b))) 

  let getColor = new Event<Color>()

  member x.GetColor() =
    getColor.Trigger(Color.FromArgb(255uy, byte sliderR.Value, byte sliderG.Value, byte sliderB.Value))

  [<CLIEvent>]
  member x.ColorChanged = colorChanged

  [<CLIEvent>]
  member x.CurrentColor = getColor.Publish

