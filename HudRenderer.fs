module HudRenderer

open Fable.Import
open ModelsClient
open System
open Fable.Helpers.React.Props
open Fable.Core.JsInterop

module R = Fable.Helpers.React

// ====================================================================================================
// Components
// ====================================================================================================

type ButtonProps = {
    Click : unit -> unit
    Hover : (unit -> unit) option
}
type Button(initialProps) =
    inherit React.Component<ButtonProps,obj>(initialProps)
    member this.render() =
        R.div [ Style [ Border("1px solid black")
                        Padding("5px")
                        Margin("4px")
                        Float("left")
                        unbox ("cursor", "pointer") ]
                OnClick (fun _ -> this.props.Click())
                OnMouseEnter (fun _ -> match this.props.Hover with Some h -> h() | _ -> ignore()) ]
              [ (unbox this.props?children) ]

let button click hover text =
    R.com<Button, _, _> { Click = click
                          Hover = hover }
                        [ R.str text ]


// ====================================================================================================
// Main View
// ====================================================================================================

let view (model:Model) (dispatch : Message -> unit) =
    R.div [ ]
          [ R.div [] [ R.str ("FPS: " + model.Fps.ToString()) ]
            R.div [] [ button (fun _ -> AddItem "Penguin" |> dispatch) None "Add Penguin"
                       button (fun _ -> AddItem "Fable" |> dispatch) None "Add Fable" ]
            R.div [] (model.Items |> List.map(fun x -> button (fun _ -> RemoveItem (x.Name, x.Id) |> dispatch)
                                                              (Some(fun _ -> Highlight (x.X, x.Y) |> dispatch))
                                                              ("Remove " + x.Name + " " + x.Id.ToString()) )) ]



// ====================================================================================================
// Main React App
// ====================================================================================================

type App(initialProps) as this =
    //inherit React.Component<((Model -> unit)->unit)*Dispatch<Message>,Model option>(initialProps)
    inherit React.Component<obj,Model option>(obj(), None)
    let subscribe, dispatch = initialProps

    do
        subscribe <|
        let mutable last = unbox null
        fun model ->
        // Only update the hud when we are something we render changes
        if Object.ReferenceEquals(last, null) ||
            Object.ReferenceEquals(model.Items, last.Items) |> not ||
            model.Fps <> last.Fps then
            last <- model
            this.setState (Some(model))

    member this.render() =
        match this.state with
        | Some (model) -> view model dispatch
        | _ -> R.div [][]


let create subscribe dispatch holder = ReactDom.render( Fable.Helpers.React.com<App,_,_> (subscribe,dispatch) [], holder) |> ignore