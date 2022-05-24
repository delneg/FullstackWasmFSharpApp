open System.Collections
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting

type Key = int
type Todo =
  { Id: Key
    Task: string
    IsCompleted: bool
  }
  
type TodoSave = Todo -> Todo

type TodoCriteria =
  | All
  | Single of Key

type TodoFind = TodoCriteria -> Todo[]
type TodoDelete = Key -> Todo option
type TodoUpdate = Todo -> Todo option

module TodoInMemory = 
    let find (inMemory : Hashtable) (criteria : TodoCriteria) : Todo[] =
      match criteria with
      | All -> inMemory.Values |> Seq.cast |> Array.ofSeq
      | Single id -> if inMemory.ContainsKey id then (inMemory.Item id :?> Todo |> Array.singleton) else [||]
    let save (inMemory : Hashtable) (todo : Todo) : Todo =
      inMemory.Add(todo.Id, todo)
      todo
    
    let delete (inMemory: Hashtable) (key: Key) : Todo option =
      if inMemory.ContainsKey key then
          let todo = inMemory.Item key :?> Todo
          inMemory.Remove key
          Some todo
      else None
    
    let update (inMemory: Hashtable) (todo: Todo) : Todo option =
      if inMemory.ContainsKey todo.Id then
          inMemory[todo.Id] <- todo
          Some todo
      else
          None
    

type IServiceCollection with
  member this.AddTodoInMemory (inMemory : Hashtable) =
    this.AddSingleton<TodoFind>(TodoInMemory.find inMemory) |> ignore
    this.AddSingleton<TodoSave>(TodoInMemory.save inMemory) |> ignore
    this.AddSingleton<TodoDelete>(TodoInMemory.delete inMemory) |> ignore
    this.AddSingleton<TodoUpdate>(TodoInMemory.update inMemory) |> ignore
  
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
            return! json (save todo) next ctx
          }
          
    let updateTodo =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let upd = ctx.GetService<TodoUpdate>()
                let! todo = ctx.BindJsonAsync<Todo>()
                match upd todo with
                | Some todo -> return! json todo next ctx
                | None ->
                    return! RequestErrors.NOT_FOUND "Not found" next ctx 
            }
            
    let deleteTodo =
        fun (id: int) (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let del = ctx.GetService<TodoDelete>()
                match del id with
                | Some todo -> return! json todo next ctx
                | None ->
                    return! RequestErrors.NOT_FOUND "Not found" next ctx 
            }
            
    let handlers =
        [ 
          //OPTIONS [route "/" (text "ok" )]
          GET [ route "/" getAllTodos ]
          POST [ route "/" createNewTodo ]
          PUT [ route "/" updateTodo ]
          DELETE [ routef "/%i" deleteTodo ]
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
    //appBuilder.UseCors(Action<_>(fun (b: Infrastructure.CorsPolicyBuilder) -> b.AllowAnyHeader() |> ignore; b.AllowAnyMethod() |> ignore)) |> ignore
    appBuilder.UseCors(fun builder ->
                             builder.WithOrigins("http://localhost:5000").AllowAnyMethod().AllowAnyHeader() |> ignore
                         ) |> ignore
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
    services.AddSingleton<TodoUpdate>(TodoInMemory.update inMemory) |> ignore
    services.AddSingleton<TodoDelete>(TodoInMemory.delete inMemory) |> ignore
    services.AddCors() |> ignore


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
