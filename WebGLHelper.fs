module WebGLHelper

open Fable.Core.JsInterop
open Fable.Import

// Shorthand
type GL = Browser.WebGLRenderingContext

let createTexture (gl:GL) (image : Browser.ImageData)  =
    let texture = gl.createTexture()

    gl.bindTexture(gl.TEXTURE_2D, texture);

    gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, 1.);
    gl.texImage2D(gl.TEXTURE_2D, 0., gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    //gl.bindTexture(gl.TEXTURE_2D, null);

    texture

let createShaderProgram (gl:GL) vertex fragment =
    let vertexShader = gl.createShader(gl.VERTEX_SHADER);
    gl.shaderSource(vertexShader, vertex);
    gl.compileShader(vertexShader);

    let fragShader = gl.createShader(gl.FRAGMENT_SHADER);
    gl.shaderSource(fragShader, fragment);
    gl.compileShader(fragShader);

    let program = gl.createProgram()
    gl.attachShader(program, vertexShader);
    gl.attachShader(program, fragShader);
    gl.linkProgram(program);

    program

let createAttributeLocation (gl : GL) program name =
    let attributeLocation = gl.getAttribLocation(program, name)
    gl.enableVertexAttribArray(attributeLocation)

    attributeLocation

let createSpriteUniformSetter (gl:GL) positionBuffer textureBuffer program =
    let vertexPositionAttribute = createAttributeLocation gl program "aVertexPosition"
    let textureCoordAttribute = createAttributeLocation gl program "aTextureCoord"

    let resolutionUniformLocation = gl.getUniformLocation (program, "resolution")
    let positionUniformLocation = gl.getUniformLocation (program, "position")
    let sizeUniformLocation = gl.getUniformLocation (program, "size")
    let textureUniformLocation = gl.getUniformLocation (program, "uSampler")

    fun ((resolutionX, resolutionY), (x, y), (width, height), texture) ->
        gl.bindBuffer(gl.ARRAY_BUFFER, positionBuffer);
        gl.vertexAttribPointer(vertexPositionAttribute, 3., gl.FLOAT, false, 0., 0.)

        gl.bindBuffer(gl.ARRAY_BUFFER, textureBuffer);
        gl.vertexAttribPointer(textureCoordAttribute, 2., gl.FLOAT, false, 0., 0.)

        gl.uniform2f (resolutionUniformLocation, resolutionX, resolutionY)
        gl.uniform2f (positionUniformLocation, x, y)
        gl.uniform2f (sizeUniformLocation, width, height)

        gl.activeTexture (gl.TEXTURE0)
        gl.bindTexture (gl.TEXTURE_2D, texture)
        gl.uniform1i (textureUniformLocation, 0.)


let createBuffer (items : float[]) (gl:GL) =
    let buffer = gl.createBuffer();

    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    gl.bufferData(gl.ARRAY_BUFFER, (createNew JS.Float32Array items) |> unbox, gl.STATIC_DRAW)

    buffer

let createSpritePositionBuffer gl =
    createBuffer [| 1.; 1.; 0.;
                    0.; 1.; 0.;
                    1.; 0.; 0.;
                    0.; 0.; 0. |] gl

let createSpriteTextureBuffer gl =
    createBuffer [| 1.; 1.;
                    0.; 1.;
                    1.; 0.;
                    0.; 0. |] gl

let clear (gl:GL) width height =
    gl.clearColor(0., 0., 0., 1.);

    gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
    //gl.enable(gl.DEPTH_TEST);
    gl.enable(gl.BLEND);

    gl.viewport(0., 0., width, height);
    gl.clear(float (int gl.COLOR_BUFFER_BIT ||| int gl.DEPTH_BUFFER_BIT));

let drawSprite (gl:GL) =
    gl.drawArrays (gl.TRIANGLE_STRIP, 0., 4.)

let defaultVertex = """
    attribute vec3 aVertexPosition;
    attribute vec2 aTextureCoord;

    uniform vec2 resolution;
    uniform vec2 position;
    uniform vec2 size;

    varying vec2 vTextureCoord;

    void main(void) {
        gl_Position = vec4(-1. + (((position.x * 2.) + (aVertexPosition.x * (size.x * 2.))) / resolution.x),
                            1. - (((position.y * 2.) + (aVertexPosition.y * (size.y * 2.))) / resolution.y),
                            aVertexPosition.z,
                            1.0);

        vTextureCoord = aTextureCoord;
    }
"""

let defaultFragment = """
    precision mediump float;

    varying vec2 vTextureCoord;

    uniform sampler2D uSampler;

    void main(void) {
        gl_FragColor = texture2D(uSampler, vec2(vTextureCoord.x, vTextureCoord.y));
    }
"""