namespace FingerAuth

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
type SafeRemotingProvider() =
    inherit Remoting.AjaxRemotingProvider()

    let url1 = "https://authapp.intellifactorylabs.com"
    let url2 = "https://localhost:5000"

    // Replace with the actual server URL
    override this.EndPoint = url1

    override this.AsyncBase(handle, data) =
        let def = base.AsyncBase(handle, data)
        async {
            try 
                return! def
            with e ->
                Console.Log("Remoting exception", handle, e)
                return box None
        }
