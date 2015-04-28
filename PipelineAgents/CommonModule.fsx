namespace Common

[<AutoOpenAttribute>]
module HelperModule =

    open System
    open System.Net
    open System.IO
    open System.Threading

    type Agent<'T> = MailboxProcessor<'T>

    let (<--) (m:Agent<_>) msg = m.Post msg
    let (<->) (m:Agent<_>) msg = m.PostAndReply(fun replyChannel -> msg replyChannel)
    let (<-!) (m: Agent<_>) msg = m.PostAndAsyncReply(fun replyChannel -> msg replyChannel)

[<AutoOpen>]
module ArrayUtils =

    open System
    open System.Drawing
    open System.Runtime.InteropServices

    //-----------------------------------------------------------------------------
    // Implements a fast unsafe conversion from 2D array to a bitmap 
      
    /// Converts array to a bitmap using the provided conversion function,
    /// which converts value from the array to a color value
    let toBitmap f (arr:_[,]) =
      // Create bitmap & lock it in the memory, so that we can access it directly
      let bmp = new Bitmap(arr.GetLength(0), arr.GetLength(1))
      let rect = new Rectangle(0, 0, bmp.Width, bmp.Height)
      let bmpData = 
        bmp.LockBits
          (rect, Imaging.ImageLockMode.ReadWrite, 
           Imaging.PixelFormat.Format32bppArgb)
       
      // Using pointer arithmethic to copy all the bits
      let ptr0 = bmpData.Scan0 
      let stride = bmpData.Stride
      for i = 0 to bmp.Width - 1 do
        for j = 0 to bmp.Height - 1 do
          let offset = i*4 + stride*j
          let clr = (f(arr.[i,j]) : Color).ToArgb()
          Marshal.WriteInt32(ptr0, offset, clr)
  
      bmp.UnlockBits(bmpData)
      bmp

module StartPhotoViewer = 
    let start() = System.Diagnostics.Process.Start(@"C:\Git\AkkaActorModel\src\Pipeline\PhotoViewer\bin\Release\PhotoViewer.exe")

    