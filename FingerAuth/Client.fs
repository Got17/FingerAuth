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
open System

type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

type EndPoint = 
    | [<EndPoint "/">] Home
    | [<EndPoint "/picdraw">] PicDraw

type Users = {
    Username: string
    Password: string
    PenColor: string
}    


[<JavaScript;AutoOpen>]
module Logic =
    let canvas() = As<HTMLCanvasElement>(JS.Document.GetElementById("annotationCanvas"))
    let getContext (e: Dom.EventTarget) = As<HTMLCanvasElement>(e).GetContext("2d")
    let toPicDrawPage = Var.Create ""

    let showToast(text) = 
        Capacitor.Toast.Show(Toast.ShowOptions(
            text = text,
            Duration = "short"
        ))    

    let showAlert(title, message) =
        Capacitor.Dialog.Alert(Dialog.AlertOptions(
            Title = title,
            Message = message
        ))
 
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

    let USERNAME_KEY = "stored_username"
    let PASSWORD_KEY = "stored_password"

    let username = Var.Create ""
    let password = Var.Create ""

    let saveCredentials (username: string, password: string) = promise {
        Capacitor.Preferences.Set(Preferences.SetOptions(
            key = USERNAME_KEY,
            value = username
        )) |> ignore

        Capacitor.Preferences.Set(Preferences.SetOptions(
            key = PASSWORD_KEY,
            value = password
        )) |> ignore
    }

    let loadCredentials() = promise {
        let! savedUsername = Capacitor.Preferences.Get(Preferences.GetOptions(key = USERNAME_KEY))
        let! savedPassword = Capacitor.Preferences.Get(Preferences.GetOptions(key = PASSWORD_KEY))

        if not (String.IsNullOrEmpty(username.Value)) && not (String.IsNullOrEmpty(password.Value)) then
            username := savedUsername.Value.Value1
            password := savedPassword.Value.Value1
            showToast("Credentials loaded") |> ignore
    }

    let login() = promise {
        printfn("Logging in with username and password")

        if not (String.IsNullOrWhiteSpace(username.Value)) && not (String.IsNullOrWhiteSpace(password.Value)) then
            saveCredentials(username.Value, password.Value) |> ignore
            toPicDrawPage := "/#/picdraw"
            JS.Window.Location.Replace(toPicDrawPage.Value) |> ignore 
        else
            showAlert("Alert", "Username and Password can not be left empty.") |> ignore
            

        printfn("User logged in successfully!")
    }

    let authenticateUser() = promise {
        try
            let! checkBioResult = Capacitor.BiometricAuth.CheckBiometry();
            if (checkBioResult.IsAvailable = false) then
                printfn("Biometric authentication not available on this device.");

            (*else*)      //
                Capacitor.BiometricAuth.Authenticate(BiometricAuth.AuthenticateOptions(
                    Reason = "Please authenticate to use PicDrawApp",
                    AndroidTitle = "Biometric Authentication",
                    AndroidSubtitle = "Use your fingerprint to access the app",
                    AllowDeviceCredential = true
                )) |> ignore

                loadCredentials()|>ignore

                toPicDrawPage := "/#/picdraw"
                JS.Window.Location.Replace(toPicDrawPage.Value) |> ignore                   
                

        with ex ->
            (*let error = ex |> As<BiometricAuth.BiometryError>*) 
            printfn($"Authentication failed: {ex}")
            showAlert("Alert", $"{ex}") |> ignore            
    }
        

[<JavaScript;AutoOpen>]
module Pages = 
    let isDrawing = Var.Create false
    let lastX, lastY = Var.Create 0.0, Var.Create 0.0   
    let colorStroke = Var.Create ""

    let draw (e: Dom.EventTarget, offsetX, offsetY) =
            let ctx = getContext e
            ctx.StrokeStyle <- colorStroke.Value 
            ctx.LineWidth <- 2.0 
            ctx.BeginPath()
            ctx.MoveTo(lastX.Value, lastY.Value)
            ctx.LineTo(offsetX, offsetY)
            ctx.Stroke()
            lastX := offsetX
            lastY := offsetY

    let HomePage() = 
        IndexTemplate.Home()
            .Username(username.V)
            .Password(password.V)
            .LogIn(fun _ -> 
                async {
                    return! login().AsAsync()
                }
                |> Async.StartImmediate
            )
            .BiometricAuthenticate(fun e -> 
                e.Event.PreventDefault()
                username := e.Vars.Username.Value
                password := e.Vars.Password.Value
                async {
                    return! authenticateUser().AsAsync()
                }
                |> Async.StartImmediate
            )      
            .ToPicDrawPage(toPicDrawPage.V)
            .Doc()

    let PicDrawPage() = 
        if username.Value = "Got" then
            colorStroke := "#FF0000" // red
        else
            colorStroke :=  "#0000FF" // blue

        showToast("Log in Successfully") |> ignore

        IndexTemplate.PicDraw()
            .UsernamePicDraw(username.V)
            .PasswordPicDraw(password.V)
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
             
