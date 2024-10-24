namespace FingerAuth

open WebSharper
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.Sitelets

type EndPoint = 
    | [<EndPoint "/">] LogIn
    | [<EndPoint "/signup">] SignUp
    | [<EndPoint "/picdraw">] PicDraw

[<JavaScript>]
module Client =   
    let router = Router.Infer<EndPoint>()
    // Install our client-side router and track the current page
    let currentPage = Router.InstallHash LogIn router

    [<SPAEntryPoint>]
    let Main () =   
        let renderInnerPage (currentPage: Var<EndPoint>) =
            currentPage.View.Map (fun endpoint ->
                match endpoint with        
                | LogIn -> LoginPage()
                | SignUp -> SignUpPage()
                | PicDraw -> PicDrawPage()

            )
            |> Doc.EmbedView

        IndexTemplate()
            .PageContent(renderInnerPage currentPage)
            .Bind()
             
