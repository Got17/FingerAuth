namespace FingerAuth

open WebSharper
open WebSharper.Web
open System.IO
open System.Security.Cryptography
open System.Text
open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

type User = {
    Username: string
    PasswordHash: string
}

module Server = 
    let hashPassword (password: string) =
        use sha256 = SHA256.Create()
        let bytes = Encoding.UTF8.GetBytes(password)
        let hash = sha256.ComputeHash(bytes)
        
        BitConverter.ToString(hash).Replace("-", "").ToLower()

    let usersFilePath = "users.txt"

    [<Rpc>]
    let saveUser (username: string, password: string) =
        async {
            let passwordHash = password
            let user = { Username = username; PasswordHash = passwordHash }
            let userString = $"{user.Username},{user.PasswordHash}"
        
            // Append the new user to the users file
            File.AppendAllLines(usersFilePath, [ userString ])
            return "User registered successfully"
        }

    [<Rpc>]
    let testEndpoint () =
        async {
            return "Test endpoint is working!"
        }

    [<Rpc>]
    let verifyUser (username: string, password: string) =
        async {
            try
                let passwordHash = password
                let users = File.ReadAllLines(usersFilePath)

                let matchingUser = 
                    users 
                    |> Seq.tryFind (fun line -> 
                        let parts = line.Split(',')
                        parts.[0] = username && parts.[1] = passwordHash
                    )

                match matchingUser with
                | Some _ -> return true
                | None -> return false
            with ex ->
                //Log the error to the server console
                printfn $"Error during user verification: {ex.Message}"
                return false
        }

