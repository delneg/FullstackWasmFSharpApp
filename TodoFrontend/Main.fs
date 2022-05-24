module TodoFrontend.Client.Main

open System
open System.Net.Http
open System.Net.Http.Json
open Microsoft.AspNetCore.Components
open Elmish
open Bolero
open Bolero.Html


/// Parses the template.html file and provides types to fill it with dynamic content.
type MasterTemplate = Template<"template.html">

/// Our application has three URL endpoints.
type EndPoint =
    | [<EndPoint "/">] All
    | [<EndPoint "/active">] Active
    | [<EndPoint "/completed">] Completed

/// This module defines the model, the update and the view for a single entry.
module Entry =

    /// The unique identifier of a Todo entry.
    type Key = int

    /// The model for a Todo entry.
    type Model =
        {
            Id : Key
            Task : string
            IsCompleted : bool
            Editing : option<string>
        }

    let New (key: Key) (task: string) =
        {
            Id = key
            Task = task
            IsCompleted = false
            Editing = None
        }

    type Message =
        | Remove
        | StartEdit
        | Edit of text: string
        | CommitEdit
        | CancelEdit
        | SetCompleted of completed: bool

    /// Defines how a given Todo entry is updated based on a message.
    /// Returns Some to update the entry, or None to delete it.
    let Update (msg: Message) (e: Model) : option<Model> =
        match msg with
        | Remove ->
            None
        | StartEdit ->
            Some { e with Editing = Some e.Task }
        | Edit value ->
            Some { e with Editing = e.Editing |> Option.map (fun _ -> value) }
        | CommitEdit ->
            Some { e with
                    Task = e.Editing |> Option.defaultValue e.Task
                    Editing = None }
        | CancelEdit ->
            Some { e with Editing = None }
        | SetCompleted value ->
            Some { e with IsCompleted = value }

    /// Render a given Todo entry.
    let Render (endpoint, entry) dispatch =
        MasterTemplate.Entry()
            .Label(text entry.Task)
            .CssAttrs(
                attr.``class`` (String.concat " " [
                    if entry.IsCompleted then "completed"
                    if entry.Editing.IsSome then "editing"
                    match endpoint, entry.IsCompleted with
                    | EndPoint.Completed, false
                    | EndPoint.Active, true -> "hidden"
                    | _ -> ()
                ])
            )
            .EditingTask(
                entry.Editing |> Option.defaultValue "",
                fun text -> dispatch (Message.Edit text)
            )
            .EditBlur(fun _ -> dispatch Message.CommitEdit)
            .EditKeyup(fun e ->
                match e.Key with
                | "Enter" -> dispatch Message.CommitEdit
                | "Escape" -> dispatch Message.CancelEdit
                | _ -> ()
            )
            .IsCompleted(
                entry.IsCompleted,
                fun x -> dispatch (Message.SetCompleted x)
            )
            .Remove(fun _ -> dispatch Message.Remove)
            .StartEdit(fun _ -> dispatch Message.StartEdit)
            .Elt()

    type Component() =
        inherit ElmishComponent<EndPoint * Model, Message>()

        override this.ShouldRender(oldModel, newModel) = oldModel <> newModel

        override this.View model dispatch = Render model dispatch

/// This module defines the model, the update and the view for a full todo list.
module TodoList =    

    /// The model for the full TodoList application.
    type Model =
        {
            EndPoint : EndPoint
            NewTask : string
            Entries : Entry.Model array
            NextKey : Entry.Key
            error: string option
        }

        static member Empty =
            {
                EndPoint = All
                NewTask = ""
                Entries = [||]
                NextKey = 0
                error = None
            }

    type Message =
        | GetEntries
        | GotEntries of Entry.Model array
        | EditNewTask of text: string
        | AddEntry
        | EntryAdded of Entry.Model
        | EntryDeleted of Entry.Key
        | EntryUpdated of Entry.Model
        | ClearCompleted
        | SetAllCompleted of completed: bool
        | EntryMessage of key: Entry.Key * message: Entry.Message
        | SetEndPoint of EndPoint
        | Error of exn

    let Router = Router.infer SetEndPoint (fun m -> m.EndPoint)

    /// Defines how the Todo list is updated based on a message.
    let Update  (http: HttpClient) (msg: Message) (model: Model) =
        match msg with
        | GetEntries ->
            let getEntries() = http.GetFromJsonAsync<Entry.Model[]>("/todos")
            let cmd = Cmd.OfTask.either getEntries () GotEntries Error
            { model with Entries = [||] }, cmd
        | GotEntries entries ->
            {model with Entries = entries |> Array.sortBy (fun x -> x.Id)}, Cmd.none
        | EditNewTask value ->
            { model with NewTask = value }, Cmd.none
        | AddEntry ->
            let newEntry = Entry.New model.NextKey model.NewTask
            let saveEntry() =
                task {
                    let! res = http.PostAsJsonAsync("/todos",newEntry)
                    return! res.Content.ReadFromJsonAsync<Entry.Model>()
                }
            let cmd = Cmd.OfTask.either saveEntry () EntryAdded Error
            { model with
                NewTask = ""
                Entries = model.Entries
                NextKey = model.NextKey + 1 }, cmd
        | EntryAdded entry ->
            {model with Entries = Array.append model.Entries [|entry|]}, Cmd.none
        | ClearCompleted ->
            { model with Entries = Array.filter (fun e -> not e.IsCompleted) model.Entries }, Cmd.none
        | SetAllCompleted c ->
            { model with Entries = Array.map (fun e -> { e with IsCompleted = c }) model.Entries }, Cmd.none
        | EntryMessage (key, msg) ->
            let updated = model.Entries
                          |> Array.tryFind (fun (x: Entry.Model) -> x.Id = key) 
                          |> Option.bind (Entry.Update msg)
            match updated with
            | None ->
                let deleteEntry () =
                    task {
                        let! res = http.DeleteAsync($"/todos/{key}")
                        let! deser = res.Content.ReadFromJsonAsync<Entry.Model>()
                        return deser.Id
                    }
                let cmd = Cmd.OfTask.either deleteEntry () EntryDeleted Error
                model, cmd
            | Some e ->
                let updateEntry () =
                    task {
                        let! res = http.PutAsJsonAsync("/todos", e)
//                        let! cont = res.Content.ReadAsStringAsync()
//                        Console.WriteLine(cont)
                        return! res.Content.ReadFromJsonAsync<Entry.Model>()
                    }
                let cmd = Cmd.OfTask.either updateEntry () EntryUpdated Error
                model, cmd
        | EntryDeleted key ->
            {model with Entries = Array.filter (fun x -> x.Id <> key) model.Entries}, Cmd.none
        | EntryUpdated entry ->
            let newEntries = Array.map (fun (x: Entry.Model) -> if x.Id = entry.Id then entry else x) model.Entries
            { model with Entries = newEntries}, Cmd.none
        | SetEndPoint ep ->
            { model with EndPoint = ep }, Cmd.none
        | Error exn ->
            Console.WriteLine(exn.Message)
            { model with error = Some exn.Message }, Cmd.none

    /// Render the whole application.
    let Render (state: Model) (dispatch: Dispatch<Message>) =
        let countNotCompleted =
            state.Entries
            |> Array.filter (fun e -> not e.IsCompleted)
            |> Array.length
        MasterTemplate()
            .HiddenIfNoEntries(if Array.isEmpty state.Entries then "hidden" else "")
            .Entries(concat {
                for entry in state.Entries do
                    let entryDispatch msg = dispatch (EntryMessage (entry.Id, msg))
                    ecomp<Entry.Component,_,_> (state.EndPoint, entry) entryDispatch
            })
//            .ClearCompleted(fun _ -> dispatch Message.ClearCompleted)
//            .IsCompleted(
//                (countNotCompleted = 0),
//                fun c -> dispatch (Message.SetAllCompleted c)
//            )
            .Task(
                state.NewTask,
                fun text -> dispatch (Message.EditNewTask text)
            )
            .Edit(fun e ->
                if e.Key = "Enter" && state.NewTask <> "" then
                    dispatch Message.AddEntry
            )
            .ItemsLeft(
                match countNotCompleted with
                | 1 -> "1 item left"
                | n -> string n + " items left"
            )
            .CssFilterAll(attr.``class`` (if state.EndPoint = EndPoint.All then "selected" else null))
            .CssFilterActive(attr.``class`` (if state.EndPoint = EndPoint.Active then "selected" else null))
            .CssFilterCompleted(attr.``class`` (if state.EndPoint = EndPoint.Completed then "selected" else null))
            .Elt()

    /// The entry point of our application, called on page load (see Startup.fs).
    type Component() =
        inherit ProgramComponent<Model, Message>()
        [<Inject>]
        member val HttpClient = Unchecked.defaultof<HttpClient> with get, set

        override this.Program =
            let update = Update this.HttpClient
            Program.mkProgram (fun _ -> Model.Empty, Cmd.ofMsg GetEntries) update Render
            |> Program.withRouter Router