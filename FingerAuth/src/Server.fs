﻿namespace FingerAuth

open WebSharper
open WebSharper.Remoting
open System.IO
open System.Security.Cryptography
open System.Text
open System

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

    let usersFilePath = "src/users.txt"

    [<Rpc; RemotingProvider(typeof<SafeRemotingProvider>)>]
    let saveUser (username: string, password: string) =
        async {
            try
                let passwordHash = password
                let user = { Username = username; PasswordHash = passwordHash }
                let userString = $"{user.Username},{user.PasswordHash}"

                // Read the existing users
                let users = File.ReadAllLines(usersFilePath)

                // Check if the username already exists
                let usernameExists = 
                    users 
                    |> Seq.exists (fun line -> 
                        let parts = line.Split(',')
                        if parts.Length >= 1 then 
                            parts.[0].Trim() = username.Trim()
                        else 
                            false
                    )

                if usernameExists then
                    return "Username already exists"
                else
                    // Append the new user to the users file
                    File.AppendAllLines(usersFilePath, [ userString ])
                    return "Registered successfully"
            with ex ->
                printfn $"Error during user registration: {ex.Message}"
                return "Error during registration"
        }
    
    [<Rpc; RemotingProvider(typeof<SafeRemotingProvider>)>]
    //[<Rpc>]
    let verifyUser (username: string, password: string) =
        async {
            try
                let passwordHash = password
                let users = File.ReadAllLines(usersFilePath)

                // Debugging logs
                printfn $"Incoming verifyUser request with Username: {username}, Password: {password}"
                printfn $"Read users from file: {users}"

                let matchingUser = 
                    users 
                    |> Seq.tryFind (fun line -> 
                        let parts = line.Split(',')
                        printfn $"Processing line: '{line}'"
                        if parts.Length >= 2 then
                            printfn($"username: {parts.[0]}, password: {parts.[1]}")
                            parts.[0] = username && parts.[1] = passwordHash
                        else
                            printfn "Invalid line format or empty line."
                            false
                    )

                match matchingUser with
                | Some _ -> 
                    printfn "User found, login successful!"
                    return true
                | None -> 
                    printfn "No matching user found."
                    return false
            with ex ->
                // Log the error to the server console
                printfn $"Error during user verification: {ex.Message}"
                return false
        }



