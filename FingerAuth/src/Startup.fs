open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open WebSharper.AspNetCore
open FingerAuth

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    // Add services to the container.
    builder.Services.AddWebSharper()
        .AddAuthentication("WebSharper")
        .AddCookie("WebSharper", fun options -> ())
    |> ignore

    builder.Services.AddCors(fun options ->
        options.AddPolicy("CorsPolicy", fun policy ->
            policy
                .WithOrigins("https://localhost:5173", "https://authapp.intellifactorylabs.com")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                //.SetIsOriginAllowed(fun _ -> true) // Allow all origins - for testing purposes only
                |> ignore
        )
    ) |> ignore

    let app = builder.Build()

    // Configure the HTTP request pipeline.
    if not (app.Environment.IsDevelopment()) then
        app.UseExceptionHandler("/Error")
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            .UseHsts()
        |> ignore
    
    app.UseHttpsRedirection()
        .UseCors("CorsPolicy")
        .UseDefaultFiles()
        .UseStaticFiles()
        //Enable if you want to make RPC calls to server
        .UseWebSharperRemoting(fun config -> ())
    |> ignore 
       
    app.Run()

    0 // Exit code
