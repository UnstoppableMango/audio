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
let ``Reads invalid magic number marker`` () =
    // Review: Is this how we want this API to work?
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let mutable reader = FlacStreamReader("mP3"B)
        reader.Read() |> ignore)
