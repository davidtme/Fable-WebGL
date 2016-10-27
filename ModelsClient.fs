namespace ModelsClient

open System
open Fable.Import
open ModelsCommon

type Item =
    { X : int
      Y : int
      Name : string
      Id : int }

type Model =
    { Items : Item list
      Now : DateTime
      Scale : int
      FrameCount : int
      Fps : int
      NextFrameCountReset : DateTime
      Highlight : int * int }

type Message =
    | Advance
    | AddItem of string
    | RemoveItem of string * int
    | MouseMove of float * float
    | MouseDown
    | FromServer of ServerResponce
    | Highlight of int*int

type RenderMode =
    | Default
    | Ripple

// This can come from json somewhere
type SpriteLoadInfo = {
    Name : string
    Width : int
    Height : int
    Layers : (int * RenderMode * string) list
}