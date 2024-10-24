namespace FingerAuth

open WebSharper
open WebSharper.Capacitor

[<JavaScript;AutoOpen>]
module Logic =
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