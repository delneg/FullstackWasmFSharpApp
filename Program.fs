open System
open System.Collections
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting
open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
type Todo =
  { Id: string
    Text: string
    Done: bool
  }
  
type TodoSave = Todo -> Todo

type TodoCriteria =
  | All

type TodoFind = TodoCriteria -> Todo[]

module TodoInMemory = 
    let find (inMemory : Hashtable) (criteria : TodoCriteria) : Todo[] =
      match criteria with
      | All -> inMemory.Values |> Seq.cast |> Array.ofSeq
      
    let save (inMemory : Hashtable) (todo : Todo) : Todo =
      inMemory.Add(todo.Id, todo)
      todo
      

type IServiceCollection with
  member this.AddTodoInMemory (inMemory : Hashtable) =
    this.AddSingleton<TodoFind>(TodoInMemory.find inMemory) |> ignore
    this.AddSingleton<TodoSave>(TodoInMemory.save inMemory) |> ignore
  
module TodoHttp =
    let getAllTodos: HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
          let find = ctx.GetService<TodoFind>()
          let todos = find TodoCriteria.All
          json todos next ctx
    
    let createNewTodo: HttpHandler =
         fun (next: HttpFunc) (ctx: HttpContext) ->
          task {
            let save = ctx.GetService<TodoSave>()
            let! todo = ctx.BindJsonAsync<Todo>()
            let todo = { todo with Id = ShortGuid.fromGuid(Guid.NewGuid()) }
            return! json (save todo) next ctx
          }
          
    let updateTodo id =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            text ("Update " + id) next ctx
            
    let deleteTodo id =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            text ("Delete " + id) next ctx
    let handlers =
        [ GET [ route "/" getAllTodos ]
          POST [ route "/" createNewTodo ]
          PUT [ routef "/%s" updateTodo ]
          DELETE [ routef "/%s" deleteTodo ]
        ]

    

let endpoints =
    [ subRoute "/todos" TodoHttp.handlers
      subRoute "/foo" [ GET [ route "/bar" (text "Aloha!") ] ]
      GET [ route "/" (text "Hello World") ]

      ]


let notFoundHandler =
    "Not Found"
    |> text
    |> RequestErrors.notFound

let configureApp (appBuilder : IApplicationBuilder) =
    appBuilder
        .UseRouting()
        .UseGiraffe(endpoints)
        .UseGiraffe(notFoundHandler)

let configureServices (services : IServiceCollection) =
    let inMemory = Hashtable()
    services
        .AddRouting()
        .AddGiraffe()
        .AddTodoInMemory(Hashtable())
    services.AddSingleton<TodoFind>(TodoInMemory.find inMemory) |> ignore
    services.AddSingleton<TodoSave>(TodoInMemory.save inMemory) |> ignore

//
[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args).UseWasiConnectionListener()
    configureServices builder.Services

    let app = builder.Build()

    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore
    
    configureApp app
    app.Run()

    0
