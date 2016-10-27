module GameWorldRenderer

open System
open Fable.Import
open Fable.Core.JsInterop
open WebGLHelper
open ModelsClient
open Fable.Core

type private Sprite = {
    Width : int
    Height : int
    Layers : (int * RenderMode * Browser.WebGLTexture) list
}

let create spriteLoadInfos (holder : Browser.Element) =

    let canvas = Browser.document.createElement_canvas()
    let width = 640
    let height = 256

    canvas.width <- float width
    canvas.height <- float height
    canvas.style.width <- width.ToString() + "px"
    canvas.style.height <- height.ToString() + "px"



    holder.appendChild(canvas) |> ignore

    let context =
        // TODO: Change to a tryPick and handle if missing.
        ["webgl"; "experimental-webgl"; "webkit-3d"; "moz-webgl"]
        |> List.pick(fun n ->
            try
                let c : Browser.WebGLRenderingContext = canvas.getContext(n) |> unbox
                if c <> null then Some c
                else None
            with _ ->
                None
        )

    let positionBuffer = createSpritePositionBuffer context
    let textureBuffer = createSpriteTextureBuffer context
    let defaultSpriteUniformSetter = createSpriteUniformSetter context positionBuffer textureBuffer

    let defaultRender =
        let program = createShaderProgram context defaultVertex defaultFragment
        context.useProgram(program)

        let spriteUniformSetter = defaultSpriteUniformSetter program

        fun spriteUniform ->
            context.useProgram(program)
            spriteUniformSetter spriteUniform
            drawSprite context

    let highlightRender =
        let fragment = """
            precision mediump float;
            varying vec2 vTextureCoord;
            uniform sampler2D uSampler;
            uniform float now;

            void main(void) {
                gl_FragColor = texture2D(uSampler, vec2(vTextureCoord.x, vTextureCoord.y))
                    * vec4(abs(sin(now)), 0.5, 0.5, 1.0);
            }
            """

        let program = createShaderProgram context defaultVertex fragment
        context.useProgram(program)

        let spriteUniformSetter = defaultSpriteUniformSetter program
        let nowUniformLocation = context.getUniformLocation (program, "now")

        fun spriteUniform now ->
            context.useProgram(program)
            spriteUniformSetter spriteUniform
            context.uniform1f(nowUniformLocation, now)
            drawSprite context

    let rippleRender =
        let fragment = """
            precision mediump float;
            varying vec2 vTextureCoord;
            uniform sampler2D uSampler;
            uniform float now;

            vec2 params = vec2(2.5, 10.0);

            // Simple circular wave function
            float wave(vec2 pos, float t, float freq, float numWaves, vec2 center) {
                float d = length(pos - center);
                d = log(1.0 + exp(d));
                return 1.0/(1.0+20.0*d*d) *
                       sin(2.0*3.1415*(-numWaves*d + t*freq));
            }

            // This height map combines a couple of waves
            float height(vec2 pos, float t) {
                float w;
                w =  wave(pos, t, params.x, params.y, vec2(0.5, -0.5));
                w += wave(pos, t, params.x, params.y, -vec2(0.5, -0.5));
                return w;
            }

            // Discrete differentiation
            vec2 normal(vec2 pos, float t) {
                return 	vec2(height(pos - vec2(0.01, 0), t) - height(pos, t),
                             height(pos - vec2(0, 0.01), t) - height(pos, t));
            }

            void main(void) {

                vec2 uv = vTextureCoord.xy;
                vec2 uvn = uv - vec2(1.0);

                uv += normal(uvn, now);

                gl_FragColor = texture2D(uSampler, vec2(1.0+uv.x, uv.y));
            }
            """

        let program = createShaderProgram context defaultVertex fragment
        context.useProgram(program)

        let spriteUniformSetter = defaultSpriteUniformSetter program
        let nowUniformLocation = context.getUniformLocation (program, "now")

        fun spriteUniform now ->
            context.useProgram(program)
            spriteUniformSetter spriteUniform
            context.uniform1f(nowUniformLocation, now)
            drawSprite context


    let clear = clear context
    let createTexture = createTexture context

    // Try not to use "context" after this point, bind a function above.

    let mutable sprites = Map.empty

    let imageLoadCanvas = Browser.document.createElement_canvas()
    let imageLoadCanvasContext = imageLoadCanvas.getContext_2d()

    let getSprite name =
        match sprites |> Map.tryFind name with
        | Some (_, (Some sprite)) ->
            Some sprite

        | Some (_, None) ->
            // This texture has been requested, do nothing
            None

        | None ->
            sprites <- sprites |> Map.add name (true, None)

            match spriteLoadInfos |> Map.tryFind name with
            | Some (info : SpriteLoadInfo) ->
                let result = Array.init (List.length info.Layers) (fun _ -> None)
                let loaded () =
                    if result |> Array.forall(function Some _ -> true | _ -> false) then
                        let sprite =
                            { Width = info.Width
                              Height = info.Height
                              Layers =
                                    result
                                    |> Array.map (fun x -> x.Value)
                                    |> Array.toList }

                        sprites <- sprites |> Map.add name (true, (Some sprite))

                info.Layers
                |> List.iteri(fun index (level, mode, url) ->
                    let image = Browser.document.createElement_img()
                    image.onload <- fun _ ->
                        imageLoadCanvas.width <- float info.Width
                        imageLoadCanvas.height <- float info.Height
                        imageLoadCanvasContext.clearRect(0., 0., float info.Width, float info.Height)
                        imageLoadCanvasContext.drawImage(U3.Case1(image), 0., 0., float info.Width, float info.Height)

                        let d = imageLoadCanvasContext.getImageData(0., 0., float info.Width, float info.Height)

                        Array.set result index (Some (level, mode, createTexture d))
                        loaded()

                        obj()

                    image.src <- url)

            | _ ->
                ignore()

            None

    let created = DateTime.Now;
    let mutable last = DateTime.Now
    let mutable lastDispatch = fun _ -> ignore()

    fun model dispatch ->

    if Object.ReferenceEquals(lastDispatch, dispatch) then
        lastDispatch <- dispatch
        canvas.onmousemove <- Func<_,_>(fun e ->
            let rect = canvas.getBoundingClientRect()
            MouseMove(e.clientX - rect.left, e.clientY - rect.top) |> dispatch
            obj())


    match model with
    | model when model.Now <> last ->
        last <- model.Now

        let scale = float model.Scale
        let now = (created - model.Now).TotalSeconds
        let resolution = canvas.width, canvas.height

        clear (fst resolution) (snd resolution)

        model.Items
        |> List.map(fun item ->
            match getSprite item.Name with
            | Some sprite ->
                sprite.Layers
                |> List.map(fun (level, mode, texture) ->
                    item, (sprite.Width, sprite.Height), level, mode, texture
                )
            | _ ->
                [])
        |> List.concat
        |> List.sortWith(fun (aItem, _, aLevel, _, _) (bItem, _, bLevel, _, _) ->
            if aItem.Y > bItem.Y then 1
            elif aItem.Y < bItem.Y then -1
            else
                if aItem.X > bItem.X then 1
                elif aItem.X < bItem.X then -1
                else
                    if aLevel > bLevel then 1
                    elif aLevel < bLevel then -1
                    else 0
        )
        |> List.iter(fun (item, (width, height), _, mode, texture) ->
            let size = float width, float height
            let position = float item.X * scale, float item.Y * scale
            let spriteUniformValues = resolution, position, size, texture

            if item.X = fst model.Highlight && item.Y = snd model.Highlight then
                highlightRender spriteUniformValues now
            else
                match mode with
                | RenderMode.Default ->
                    defaultRender spriteUniformValues

                | RenderMode.Ripple ->
                    rippleRender spriteUniformValues now

        )

    | _ -> ignore()