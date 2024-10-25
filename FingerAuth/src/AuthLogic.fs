namespace FingerAuth

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.Capacitor
open WebSharper.UI.Notation
open System

[<JavaScript;AutoOpen>]
module AuthLogic = 
    let toPicDrawPage = Var.Create ""

    let USERNAME_KEY = "stored_username"
    let PASSWORD_KEY = "stored_password"

    let username = Var.Create ""
    let password = Var.Create ""

    let mutable appListener: PluginListenerHandle = Unchecked.defaultof<_>

    let preferencesSet(key, value) = 
        Capacitor.Preferences.Set(Preferences.SetOptions(key = key, value = value))

    let preferencesGet(key) = 
        Capacitor.Preferences.Get(Preferences.GetOptions(key = key))

    let saveCredentials (username: string, password: string) = promise {
        preferencesSet(USERNAME_KEY, username) |> ignore
        preferencesSet(PASSWORD_KEY, password) |> ignore
    }

    let loadCredentials() = promise {
        let! savedUsername = preferencesGet(USERNAME_KEY)
        let! savedPassword = preferencesGet(PASSWORD_KEY)

        username := savedUsername.Value.Value1
        password := savedPassword.Value.Value1
        //showToast("Credentials loaded") |> ignore
    }

    let ToPicDrawPage() = 
        toPicDrawPage := "/#/picdraw"
        JS.Window.Location.Replace(toPicDrawPage.Value) |> ignore 

    let login () = promise {
        if not (String.IsNullOrWhiteSpace(username.Value)) || not (String.IsNullOrWhiteSpace(password.Value)) then
            try 
                printfn $"Attempting login with Username: '{username.Value}' Password: '{password.Value}'"
                let! isValid = Server.verifyUser(username.Value, password.Value)
                if isValid then
                    saveCredentials(username.Value, password.Value) |> ignore
                    ToPicDrawPage()
                else
                    showAlert("Alert", "Invalid username or password.") |> ignore
            with ex -> 
                printfn($"Error during login: {ex.Message}")
                showAlert("Error", $"An error occurred during login: {ex.Message}") |> ignore
        else
            showAlert("Alert", "Username and password cannot be left empty.") |> ignore
    }

    let signUp() = promise {
        if String.IsNullOrWhiteSpace(username.Value) || String.IsNullOrWhiteSpace(password.Value) then
            showAlert("Alert", "Username and password cannot be empty.") |> ignore
        else
            let! saveUserSuccessful = Server.saveUser(username.Value, password.Value)
            printfn($"{saveUserSuccessful}")

            if saveUserSuccessful = "Registered successfully" then
                printfn("true")
                JS.Window.Location.Replace("/#") |> ignore 

            showToast($"{saveUserSuccessful}") |> ignore  
    }

    let authenticateUser() = promise {
        try
            let! checkBioResult = Capacitor.BiometricAuth.CheckBiometry()
            if not(checkBioResult.IsAvailable) then
                printfn("Biometric authentication not available on this device.");

            //else
                Capacitor.BiometricAuth.Authenticate(BiometricAuth.AuthenticateOptions(
                    Reason = "Please authenticate to use PicDrawApp",
                    AndroidTitle = "Biometric Authentication",
                    AndroidSubtitle = "Use your fingerprint to access the app",
                    AllowDeviceCredential = true
                )) |> ignore

                ToPicDrawPage()

        with 
        | exn ->
            let error = exn |> As<BiometricAuth.BiometryError>
            printfn($"Unexpected error: {exn.Message}")
            showAlert($"{error.Message}", $"{error.Code}") |> ignore     
    }     

    let updateBiometryInfo(info: BiometricAuth.CheckBiometryResult): unit =
        printfn($"updateBiometryInfo {info}")

    let resumeAuthentication() = promise {
        let! checkBioResult = Capacitor.BiometricAuth.CheckBiometry()
        updateBiometryInfo(checkBioResult)

        try 
            let! resumeListener = Capacitor.BiometricAuth.AddResumeListener(updateBiometryInfo)
            appListener <- resumeListener
        with
        | exn ->
            let error = exn |> As<BiometricAuth.BiometryError>
            printfn($"Unexpected error: {exn.Message}")
            showAlert($"{error.Message}", $"{error.Code}") |> ignore  
    }

    (*let resumeEventListenner() = promise {
        let resume() = authenticateUser()
        Capacitor.App.AddListener("resume", )
    }*)

