namespace FingerAuth

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Templating
open WebSharper.TouchEvents
open WebSharper.UI.Notation

type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument, ServerLoad.WhenChanged>

[<JavaScript;AutoOpen>]
module Pages = 
    let isDrawing = Var.Create false
    let lastX, lastY = Var.Create 0.0, Var.Create 0.0   
    let colorStroke = Var.Create "#ff0000"

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

    let LoginPage() = 
        async {
            return! resumeAuthentication().AsAsync()
        }
        |> Async.StartImmediate

        IndexTemplate.Login()
            .Username(username.V)
            .Password(password.V)
            .LogIn(fun e -> 
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

    let SignUpPage() = 
        IndexTemplate.SignUp()
            .Username(username.V)
            .Password(password.V)
            .SignUp(fun _ ->
                async {
                    return! signUp().AsAsync()
                }
                |> Async.StartImmediate
            )
            .Doc()

    let PicDrawPage() = 
        showToast("Login Successfully") |> ignore

        IndexTemplate.PicDraw()
            .PenColor(colorStroke)
            .ColorLabel(colorStroke.View)
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

