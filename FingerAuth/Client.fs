namespace FingerAuth

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.Capacitor
open WebSharper.TouchEvents
open WebSharper.Sitelets
open WebSharper.UI.Notation

type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

type EndPoint = 
    | [<EndPoint "/">] Home
    | [<EndPoint "/picdraw">] PicDraw


[<JavaScript; AutoOpen>]
module Logic =
    let canvas() = As<HTMLCanvasElement>(JS.Document.GetElementById("annotationCanvas"))
    let getContext (e: Dom.EventTarget) = As<HTMLCanvasElement>(e).GetContext("2d")
    let isAuthenticated = Var.Create false
    let toPicDrawPage = Var.Create ""

    let authenticateToast() = 
        Capacitor.Toast.Show(Toast.ShowOptions(
            text = "Authenticate Successfully",
            Duration = "short"
        ))

    let authenticateUser() = promise {
        try
            let! checkBioResult = Capacitor.BiometricAuth.CheckBiometry();
            if(checkBioResult.IsAvailable = false) then
                printfn("Biometric authentication not available on this device.");

            else      
                let! _ = Capacitor.BiometricAuth.Authenticate(BiometricAuth.AuthenticateOptions(
                    Reason = "Please authenticate to use PicDrawApp",
                    AndroidTitle = "Biometric Authentication",
                    AndroidSubtitle = "Use your fingerprint to access the app",
                    AllowDeviceCredential = true
                ))

                toPicDrawPage := "/#/picdraw"
                JS.Window.Location.Replace(toPicDrawPage.Value) |> ignore   
                
                isAuthenticated := true
                authenticateToast() |> ignore

        with ex ->
            let error = ex |> As<BiometricAuth.BiometryErrorType> 
            printfn($"Authentication failed: {error}")
            Capacitor.Dialog.Alert(Dialog.AlertOptions(
                Title = "Alert",
                Message = $"{error}"
            ))|> ignore            
    }
 
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

    let MouseUpAndOutAction (isDrawing) = 
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

[<JavaScript; AutoOpen>]
module Pages = 
    let isDrawing = Var.Create false
    let lastX, lastY = Var.Create 0.0, Var.Create 0.0   

    let draw (e: Dom.EventTarget, offsetX, offsetY) =
            let ctx = getContext e
            ctx.StrokeStyle <- "#FF0000" 
            ctx.LineWidth <- 2.0 
            ctx.BeginPath()
            ctx.MoveTo(lastX.Value, lastY.Value)
            ctx.LineTo(offsetX, offsetY)
            ctx.Stroke()
            lastX := offsetX
            lastY := offsetY

    let PicDrawPage() = 
        IndexTemplate.PicDraw()
            .CaptureBtn(fun _ -> 
                async {
                    return! takePicture().Then(fun _ -> printfn "Succesfully take or choose a picture").AsAsync()
                }
                |> Async.Start
            )
            .canvasMouseDown(fun e ->
                isDrawing := true
                lastX := (e.Event.OffsetX)
                lastY := (e.Event.OffsetY)
            )
            .canvasMouseUp(fun _ -> 
                MouseUpAndOutAction(isDrawing)
            )
            .canvasMouseOut(fun _ ->
                MouseUpAndOutAction(isDrawing)
            )
            .canvasMouseMove(fun e -> 
                let offsetX = e.Event.OffsetX
                let offsetY = e.Event.OffsetY

                if isDrawing.Value then
                    draw (e.Target, offsetX, offsetY)
            )
            .SaveShareBtn(fun _ -> 
                async {
                    return! saveAndShareImage().Then(fun image -> printfn $"Saved Image URL: {image.Uri}").AsAsync()
                }
                |> Async.Start
            )
            .canvasInit(fun () ->
                let canvas = canvas()
                canvas.AddEventListener("touchstart", fun (e: Dom.Event) -> 
                    let touchEvent = e |> As<TouchEvent>
                    touchEvent.PreventDefault()

                    isDrawing := true

                    let touch = touchEvent.Touches[0]

                    let offsetX = setOffsetX(touch)
                    let offsetY = setOffsetY(touch)

                    lastX := offsetX
                    lastY := offsetY
                )

                canvas.AddEventListener("touchmove", fun (e: Dom.Event) -> 
                    let touchEvent = e |> As<TouchEvent>
                    touchEvent.PreventDefault()

                    let touch = touchEvent.Touches[0]

                    let offsetX = setOffsetX(touch)
                    let offsetY = setOffsetY(touch)

                    if isDrawing.Value then
                        draw(e.Target, offsetX, offsetY)
                )

                canvas.AddEventListener("touchend", fun (e: Dom.Event) -> 
                    let touchEvent = e |> As<TouchEvent>
                    touchEvent.PreventDefault()
                    isDrawing := false
                )
            )
            .Doc()    

    let HomePage() = 
        IndexTemplate.textAuth()
            .authenticate(fun e -> 
                e.Event.PreventDefault()
                async {
                    return! authenticateUser().AsAsync()
                }
                |> Async.StartImmediate
            )      
            .ToPicDrawPage(toPicDrawPage.V)
            .Doc()

[<JavaScript>]
module Client =   
    let router = Router.Infer<EndPoint>()
    // Install our client-side router and track the current page
    let currentPage = Router.InstallHash Home router

    [<SPAEntryPoint>]
    let Main () =        
        let renderInnerPage (currentPage: Var<EndPoint>) =
            currentPage.View.Map (fun endpoint ->
                match endpoint with        
                | Home -> HomePage()
                | PicDraw -> PicDrawPage()

            )
            |> Doc.EmbedView

        IndexTemplate()
            .PageContent(renderInnerPage currentPage)
            .Bind()
             
