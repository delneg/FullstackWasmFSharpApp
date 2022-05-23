namespace TodoFrontend.Client

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open System
open System.Net.Http

module Program =

    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<Main.TodoList.Component>(".todoapp")
        let backendUri = Uri builder.HostEnvironment.BaseAddress
        printfn $"Building with backendAddr {backendUri}"
        builder.Services.AddScoped<HttpClient>(fun _ ->
            new HttpClient(BaseAddress = backendUri)) |> ignore
        builder.Build().RunAsync() |> ignore
        0
