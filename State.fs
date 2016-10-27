module State

open System
open ModelsClient
open ModelsCommon
open Elmish
open Fable.PowerPack
open Fable.Core
open Fable.Import

let init () =
    let now = DateTime.Now

    let r = { Items = []
              Now = now
              FrameCount = 0
              NextFrameCountReset = now
              Fps = 0
              Highlight = -1, -1
              Scale = 64 }
    r, []

let rand = System.Random()

let update serverRequest message model =
    match message with
    | Advance ->

        let now = DateTime.Now
        let frameCount, nextFrameCountReset, fps =

            if now > model.NextFrameCountReset then
                0, now.AddSeconds(1.), model.FrameCount
            else
                model.FrameCount + 1, model.NextFrameCountReset, model.Fps

        { model with
            Now = now
            Fps = fps
            FrameCount = frameCount
            NextFrameCountReset = nextFrameCountReset }, Cmd.ofAnimationFrame ignore (fun _ -> Advance) (fun _ -> invalidOp "Can't happen")

    | AddItem name ->
        let id =
            match model.Items |> List.filter(fun x -> x.Name = name) with
            | [] -> 1
            | items -> (items |> List.maxBy(fun x -> x.Id)).Id + 1

        { model with Items =
                         model.Items @ [ { Name = name
                                           X = rand.Next(0, 10)
                                           Y = rand.Next(0, 4)
                                           Id = id } ]
                         |> List.sortBy(fun x -> x.Name, x.Id) }, []

    | RemoveItem (name, id) ->
        { model with Items = model.Items |> List.filter(fun x -> not(x.Name = name && x.Id = id)) }, []


    | Highlight (x, y)->
         { model with  Highlight = x,y }, []

    | MouseMove (x, y) ->
        let mapX = (x / float model.Scale) |> floor |> int
        let mapY = (y / float model.Scale) |> floor |> int

        model, Cmd.ofMsg(Highlight(mapX, mapY))

    | MouseDown ->
        printfn "Down"
        model, []

    | FromServer m ->
        model, []