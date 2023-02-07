[<Xunit.Trait("Category", "Unit")>]
module Safir.Audio.Tests.Flac.FlacStreamReaderNextValueTests

open System
open Safir.Audio.Flac
open Xunit

[<Fact>]
let ``When uninitialized is FlacValue.Marker`` () =
    let mutable reader = FlacStreamReader()

    Assert.Equal(FlacValue.Marker, reader.NextValue)

[<Fact>]
let ``When at FlacValue.None is FlacValue.Marker`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.None }
    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.Marker, reader.NextValue)

[<Fact>]
let ``When at FlacValue.Marker is FlacValue.LastMetadataBlockFlag`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.Marker }
    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.LastMetadataBlockFlag is FlacValue.MetadataBlockType`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.LastMetadataBlockFlag }
    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MetadataBlockType, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MetadataBlockType is FlacValue.DataBlockLength`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MetadataBlockType }
    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.DataBlockLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength but no block type throws exception`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.DataBlockLength }
        let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.StreamInfo is FlacValue.MinimumBlockSize`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.StreamInfo }

    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MinimumBlockSize, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.Padding is FlacValue.Padding`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.Padding }

    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.Padding, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.Application is FlacValue.ApplicationId`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.Application }

    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.ApplicationId, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.SeekTable is FlacValue.SeekPointSampleNumber`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.SeekTable }

    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.SeekPointSampleNumber, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.VorbisComment is FlacValue.VendorLength`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.VorbisComment }

    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.VendorLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.CueSheet is FlacValue.MediaCatalogNumber`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.CueSheet }

    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MediaCatalogNumber, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.Picture is FlacValue.PictureType`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.Picture }

    let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureType, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and unknown block type throws exception`` () =
    // TODO: This should probably reflect the skip logic
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.DataBlockLength
                BlockType = ValueSome(enum<BlockType> 69) }

        let mutable reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)
