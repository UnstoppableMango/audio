[<Xunit.Trait("Category", "Unit")>]
module Safir.Audio.Tests.Flac.FlacStreamReaderReadTests

open System
open Safir.Audio.Flac
open Xunit

[<Fact>]
let ``Returns false when there is no more data to read`` () =
    let mutable reader = FlacStreamReader()

    Assert.False(reader.Read())

[<Fact>]
let ``Reads magic number marker`` () =
    let mutable reader = FlacStreamReader("fLaC"B)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.Marker, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual("fLaC"B))

[<Fact>]
let ``Reads invalid magic number marker with 4 bytes`` () =
    let mutable reader = FlacStreamReader("Mp69"B)

    // Review: Is this how we want this API to work?
    Assert.True(reader.Read())
    Assert.Equal(FlacValue.Marker, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual("Mp69"B))

[<Fact>]
let ``Throws on invalid magic number marker`` () =
    // Review: Is this how we want this API to work?
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let mutable reader = FlacStreamReader("mP3"B)
        reader.Read() |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag`` (data: byte) =
    let state = { FlacStreamState.Empty with Value = FlacValue.Marker }

    let mutable reader =
        FlacStreamReader(ReadOnlySpan<byte>.op_Implicit [| data |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual([| data |]))

[<Theory>]
[<InlineData(0xFFuy)>]
[<InlineData(0x7Fuy)>]
[<InlineData(0x69uy)>]
[<InlineData(0x00uy)>]
let ``Reads metadata block type`` (data: byte) =
    let state = { FlacStreamState.Empty with Value = FlacValue.LastMetadataBlockFlag }

    let mutable reader =
        FlacStreamReader(ReadOnlySpan<byte>.op_Implicit [| data |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MetadataBlockType, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual([| data |]))

let metadataBlockLengthCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof metadataBlockLengthCases)>]
let ``Reads metadata block length`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.MetadataBlockType }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.DataBlockLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))
