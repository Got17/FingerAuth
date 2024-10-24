namespace FingerAuth

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Capacitor
open WebSharper.TouchEvents
open WebSharper.UI.Notation

[<JavaScript;AutoOpen>]
module DrawingLogic = 
    let canvas() = As<HTMLCanvasElement>(JS.Document.GetElementById("annotationCanvas"))
    let getContext (e: Dom.EventTarget) = As<HTMLCanvasElement>(e).GetContext("2d")
    

    let loadImageOnCanvas (imagePath: string) =
        let img = 
            Elt.img [
                on.load (fun img e ->
                    let canvas = canvas()
                    let ctx = canvas.GetContext "2d"
                    ctx.ClearRect(0.0, 0.0, canvas.Width |> float, canvas.Height |> float)
                    ctx.DrawImage(img, 0.0, 0.0, canvas.Width |> float, canvas.Height |> float)
                )
            ] []

        img.Dom.SetAttribute("src", imagePath)

    let takePicture() = promise {
        let! image = Capacitor.Camera.GetPhoto(Camera.ImageOptions(
            resultType = Camera.CameraResultType.Uri,
            Source = Camera.CameraSource.PROMPT,
            Quality = 90
        ))
        image.WebPath |> loadImageOnCanvas
    } 

    let MouseUpAndOutAction (isDrawing: Var<bool>) = 
        isDrawing := false            

    let saveAndShareImage () = promise {
        let date = new Date()
        let canvas = canvas()
        let fileName = $"{date.GetTime()}_image.png"
        let imageData = canvas.ToDataURL("image/png")
        let! savedImage = Capacitor.Filesystem.WriteFile(Filesystem.WriteFileOptions(
            Path = fileName,
            Data = imageData,                                
            Directory = Filesystem.Directory.DOCUMENTS

        ))

        Capacitor.Share.Share(Share.ShareOptions(
            Title = "Check out my annotated picture!",
            Text = "Here is an image I created using PicDraw!",
            Url = savedImage.Uri,
            DialogTitle = "Share your creation"
        )) |> ignore

        return savedImage
    }   

    let intTofloat(value:int) = System.Convert.ToDouble(value)

    let setOffsetX (touch: Touch) = 
        let canvas = canvas()
        let rect = canvas.GetBoundingClientRect()

        let scaleX = intTofloat(canvas.Width) / rect.Width

        let offsetX = (touch.ClientX - rect.Left) * scaleX

        offsetX

    let setOffsetY (touch: Touch) = 
        let canvas = canvas()
        let rect = canvas.GetBoundingClientRect()

        let scaleY = intTofloat(canvas.Height) / rect.Height
        let offsetY = (touch.ClientY - rect.Top) * scaleY
    
        offsetY   

