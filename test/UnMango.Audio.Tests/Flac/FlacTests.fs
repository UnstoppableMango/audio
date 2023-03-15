[<Xunit.Trait("Category", "Unit")>]
module UnMango.Audio.Tests.Flac.FlacTests

open UnMango.Audio.Flac
open Swensen.Unquote
open Xunit

[<Fact>]
let ``Reads stream marker`` () =
    let mutable reader = FlacStreamReader("fLaC"B)

    let result = Flac.readMagic (&reader)

    test <@ "fLaC" = result @>

[<Fact>]
let ``readMagic throws when unable to read`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let mutable reader = FlacStreamReader()
        Flac.readMagic (&reader) |> ignore)

[<Fact>]
let ``readMagic throws when stream marker is invalid`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let mutable reader = FlacStreamReader("Mp69"B)
        Flac.readMagic (&reader) |> ignore)

[<Fact>]
let ``Reads metadata block header`` () =
    let data = [| 0x81uy; 0x00uy; 0x00uy; 0x45uy |]
    let state = { FlacStreamState.Empty with Position = FlacValue.Marker }
    let mutable reader = FlacStreamReader(data, state)

    let result = Flac.readMetadataBlockHeader (&reader)

    test
        <@ { LastBlock = true
             BlockType = BlockType.Padding
             Length = 69u } = result @>

[<Fact>]
let ``Reads metadata block header from start`` () =
    let data = Array.append "fLaC"B [| 0x00uy; 0x00uy; 0x00uy; 0x20uy |]
    let mutable reader = FlacStreamReader(data)

    // TODO: Do we like this API?
    let result = Flac.readMetadataBlockHeader (&reader)

    test
        <@ { LastBlock = false
             BlockType = BlockType.StreamInfo
             Length = 32u } = result @>
