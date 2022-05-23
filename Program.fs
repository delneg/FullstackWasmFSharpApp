open System
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
//open Giraffe
//open Giraffe.EndpointRouting
//open System
//open Microsoft.AspNetCore.Builder
//open Microsoft.Extensions.Hosting
//
//let handler1 : HttpHandler =
//    fun (_ : HttpFunc) (ctx : HttpContext) ->
//        ctx.WriteTextAsync "Hello World"
//
//let handler2 (firstName : string, age : int) : HttpHandler =
//    fun (_ : HttpFunc) (ctx : HttpContext) ->
//        sprintf "Hello %s, you are %i years old." firstName age
//        |> ctx.WriteTextAsync
//
//let handler3 (a : string, b : string, c : string, d : int) : HttpHandler =
//    fun (_ : HttpFunc) (ctx : HttpContext) ->
//        sprintf "Hello %s %s %s %i" a b c d
//        |> ctx.WriteTextAsync
//
//let endpoints =
//    [
//        subRoute "/foo" [
//            GET [
//                route "/bar" (text "Aloha!")
//            ]
//        ]
//        GET [
//            route  "/" (text "Hello World")
//            routef "/%s/%i" handler2
//            routef "/%s/%s/%s/%i" handler3
//        ]
//        GET_HEAD [
//            route "/foo" (text "Bar")
//            route "/x"   (text "y")
//            route "/abc" (text "def")
//        ]
//        // Not specifying a http verb means it will listen to all verbs
//        subRoute "/sub" [
//            route "/test" handler1
//        ]
//    ]
//
//let notFoundHandler =
//    "Not Found"
//    |> text
//    |> RequestErrors.notFound
//
//let configureApp (appBuilder : IApplicationBuilder) =
//    appBuilder
//        .UseRouting()
//        .UseGiraffe(endpoints)
//        .UseGiraffe(notFoundHandler)
//
//let configureServices (services : IServiceCollection) =
//    services
//        .AddRouting()
//        .AddGiraffe()
//    |> ignore
//
////
//[<EntryPoint>]
//let main args =
//    let builder =  WebHostBuilder()
//    let configBuilder = ConfigurationBuilder()
//    
//    builder.UseConfiguration(configBuilder.AddCommandLine(args).Build()) |> ignore
//    
//    builder
//        .ConfigureKestrel(Action<_>(fun (options:KestrelServerOptions) ->
//            options.ListenAnyIP(8080, Action<_>(fun listenOptions ->
//                listenOptions.Protocols <-  HttpProtocols.Http1AndHttp2
//                listenOptions.UseHttps() |> ignore
//                ()
//                ))
//            ()))
//        .Configure(configureApp)
//        .ConfigureServices(configureServices)
//        .Build()
//        .Start()
//    let builder = WebApplication.CreateBuilder(args).UseWasiConnectionListener()
////    configureServices builder.Services
////
////    let app = builder.Build()
////
////    if app.Environment.IsDevelopment() then
////        app.UseDeveloperExceptionPage() |> ignore
////    
////    configureApp app
////    app.Run()
//
//    0

[<EntryPoint>]
let main args =
    
    
//    let host = WebHost.CreateDefaultBuilder()
    let builder = WebApplication.CreateBuilder(args).UseWasiConnectionListener()
//    let a:Action<WebHostBuilderContext,IConfigurationBuilder> = Action<_>(fun  context options  -> ()
//    builder.WebHost.ConfigureAppConfiguration(a)
    let app = builder.Build()

    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore

    app.Start()

    0 // Exit code

