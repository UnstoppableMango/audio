[<Xunit.Trait("Category", "Unit")>]
module Safir.Audio.Tests.Flac.FlacStreamReaderNextValueTests

open System
open Safir.Audio.Flac
open Xunit

[<Fact>]
let ``When uninitialized is FlacValue.Marker`` () =
    let reader = FlacStreamReader()

    Assert.Equal(FlacValue.Marker, reader.NextValue)

[<Fact>]
let ``When at FlacValue.None is FlacValue.Marker`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.None }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.Marker, reader.NextValue)

[<Fact>]
let ``When at FlacValue.Marker is FlacValue.LastMetadataBlockFlag`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.Marker }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.LastMetadataBlockFlag is FlacValue.MetadataBlockType`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.LastMetadataBlockFlag }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MetadataBlockType, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MetadataBlockType is FlacValue.DataBlockLength`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MetadataBlockType }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.DataBlockLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength but no block type throws exception`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.DataBlockLength }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.StreamInfo is FlacValue.MinimumBlockSize`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.StreamInfo }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MinimumBlockSize, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.Padding is FlacValue.Padding`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.Padding }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.Padding, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.Application is FlacValue.ApplicationId`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.Application }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.ApplicationId, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.SeekTable is FlacValue.SeekPointSampleNumber`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.SeekTable }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.SeekPointSampleNumber, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.VorbisComment is FlacValue.VendorLength`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.VorbisComment }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.VendorLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.CueSheet is FlacValue.MediaCatalogNumber`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.CueSheet }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MediaCatalogNumber, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and BlockType.Picture is FlacValue.PictureType`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome BlockType.Picture }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureType, reader.NextValue)

[<Fact>]
let ``When at FlacValue.DataBlockLength and unknown block type is FlacValue.MetadataBlockData`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.DataBlockLength
            BlockType = ValueSome(enum<BlockType> 69) }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MetadataBlockData, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MinimumBlockSize is FlacValue.MaximumBlockSize`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MinimumBlockSize }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MaximumBlockSize, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MaximumBlockSize is FlacValue.MinimumFrameSize`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MaximumBlockSize }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MinimumFrameSize, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MinimumFrameSize is FlacValue.MaximumFrameSize`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MinimumFrameSize }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MaximumFrameSize, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MaximumFrameSize is FlacValue.StreamInfoSampleRate`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MaximumFrameSize }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.StreamInfoSampleRate, reader.NextValue)

[<Fact>]
let ``When at FlacValue.StreamInfoSampleRate is FlacValue.NumberOfChannels`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.StreamInfoSampleRate }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.NumberOfChannels, reader.NextValue)

[<Fact>]
let ``When at FlacValue.NumberOfChannels is FlacValue.BitsPerSample`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.NumberOfChannels }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.BitsPerSample, reader.NextValue)

[<Fact>]
let ``When at FlacValue.BitsPerSample is FlacValue.TotalSamples`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.BitsPerSample }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TotalSamples, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TotalSamples is FlacValue.Md5Signature`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.TotalSamples }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.Md5Signature, reader.NextValue)

[<Fact>]
let ``When at FlacValue.Md5Signature and unknown last metadata block flag throws exception`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.Md5Signature }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.Md5Signature and not last metadata block is FlacValue.LastMetadataBlockFlag`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.Md5Signature
            LastMetadataBlock = ValueSome false }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.Md5Signature and last metadata block is FlacValue.None`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.Md5Signature
            LastMetadataBlock = ValueSome true }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.None, reader.NextValue)

[<Fact>]
let ``When at FlacValue.Padding and unknown last metadata block flag throws exception`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.Padding }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.Padding and not last metadata block is FlacValue.LastMetadataBlockFlag`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.Padding
            LastMetadataBlock = ValueSome false }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.Padding and last metadata block is FlacValue.None`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.Padding
            LastMetadataBlock = ValueSome true }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.None, reader.NextValue)

[<Fact>]
let ``When at FlacValue.ApplicationId is FlacValue.ApplicationData`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.ApplicationId }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.ApplicationData, reader.NextValue)

[<Fact>]
let ``When at FlacValue.ApplicationData and unknown last metadata block flag throws exception`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.ApplicationData }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.ApplicationData and not last metadata block is FlacValue.LastMetadataBlockFlag`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.ApplicationData
            LastMetadataBlock = ValueSome false }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.ApplicationData and last metadata block is FlacValue.None`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.ApplicationData
            LastMetadataBlock = ValueSome true }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.None, reader.NextValue)

[<Fact>]
let ``When at FlacValue.SeekPointSampleNumber is FlacValue.SeekPointOffset`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.SeekPointSampleNumber }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.SeekPointOffset, reader.NextValue)

[<Fact>]
let ``When at FlacValue.SeekPointOffset is FlacValue.NumberOfSamples`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.SeekPointOffset }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.NumberOfSamples, reader.NextValue)

let numberOfSamplesInvalidStateTestData: obj array seq =
    seq {
        [| ValueNone; ValueNone |]
        [| ValueSome 69u; ValueNone |]
        [| ValueNone; ValueSome 69u |]
        [| ValueSome 69u; ValueSome 420u |]
    }
    |> Seq.map (Array.map box)

[<Theory>]
[<MemberData(nameof numberOfSamplesInvalidStateTestData)>]
let ``When at FlacValue.NumberOfSamples and invalid state throws exception`` count offset =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.NumberOfSamples
                SeekPointCount = count
                SeekPointOffset = offset }

        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.NumberOfSamples and offset is less than count is FlacValue.SeekPointSampleNumber`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.NumberOfSamples
            SeekPointCount = ValueSome 420u
            SeekPointOffset = ValueSome 69u }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.SeekPointSampleNumber, reader.NextValue)

[<Fact>]
let ``When at FlacValue.NumberOfSamples and offset is equal to count and unknown last metadata block flag throws exception``
    ()
    =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.NumberOfSamples
                SeekPointCount = ValueSome 69u
                SeekPointOffset = ValueSome 69u }

        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.NumberOfSamples and offset is equal to count and not last metadata block is FlacValue.LastMetadataBlockFlag``
    ()
    =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.NumberOfSamples
            SeekPointCount = ValueSome 69u
            SeekPointOffset = ValueSome 69u
            LastMetadataBlock = ValueSome false }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.NumberOfSamples and offset is equal to count and last metadata block is FlacValue.None`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.NumberOfSamples
            SeekPointCount = ValueSome 69u
            SeekPointOffset = ValueSome 69u
            LastMetadataBlock = ValueSome true }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.None, reader.NextValue)

[<Fact>]
let ``When at FlacValue.VendorLength is FlacValue.VendorString`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.VendorLength }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.VendorString, reader.NextValue)

[<Fact>]
let ``When at FlacValue.VendorString is FlacValue.UserCommentListLength`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.VendorString }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.UserCommentListLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.UserCommentListLength is FlacValue.UserCommentLength`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.UserCommentListLength }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.UserCommentLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.UserCommentLength is FlacValue.UserComment`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.UserCommentLength }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.UserComment, reader.NextValue)

let userCommentInvalidStateTestData: obj array seq =
    seq {
        [| ValueNone; ValueNone |]
        [| ValueSome 69u; ValueNone |]
        [| ValueNone; ValueSome 69u |]
        [| ValueSome 69u; ValueSome 420u |]
    }
    |> Seq.map (Array.map box)

[<Theory>]
[<MemberData(nameof userCommentInvalidStateTestData)>]
let ``When at FlacValue.UserComment and invalid state throws exception`` count offset =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.UserComment
                UserCommentCount = count
                UserCommentOffset = offset }

        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.UserComment and offset is less than count is FlacValue.SeekPointSampleNumber`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.UserComment
            UserCommentCount = ValueSome 420u
            UserCommentOffset = ValueSome 69u }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.UserCommentLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.UserComment and offset is equal to count and unknown last metadata block flag throws exception``
    ()
    =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.UserComment
                UserCommentCount = ValueSome 69u
                UserCommentOffset = ValueSome 69u }

        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.UserComment and offset is equal to count and not last metadata block is FlacValue.LastMetadataBlockFlag``
    ()
    =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.UserComment
            UserCommentCount = ValueSome 69u
            UserCommentOffset = ValueSome 69u
            LastMetadataBlock = ValueSome false }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.UserComment and offset is equal to count and last metadata block is FlacValue.None`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.UserComment
            UserCommentCount = ValueSome 69u
            UserCommentOffset = ValueSome 69u
            LastMetadataBlock = ValueSome true }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.None, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MediaCatalogNumber is FlacValue.NumberOfLeadInSamples`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MediaCatalogNumber }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.NumberOfLeadInSamples, reader.NextValue)

[<Fact>]
let ``When at FlacValue.NumberOfLeadInSamples is FlacValue.IsCueSheetCompactDisc`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.NumberOfLeadInSamples }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.IsCueSheetCompactDisc, reader.NextValue)

[<Fact>]
let ``When at FlacValue.IsCueSheetCompactDisc is FlacValue.CueSheetReserved`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.IsCueSheetCompactDisc }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.CueSheetReserved, reader.NextValue)

[<Fact>]
let ``When at FlacValue.CueSheetReserved is FlacValue.NumberOfTracks`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.CueSheetReserved }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.NumberOfTracks, reader.NextValue)

[<Fact>]
let ``When at FlacValue.NumberOfTracks is FlacValue.TrackOffset`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.NumberOfTracks }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackOffset, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackOffset is FlacValue.TrackNumber`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.TrackOffset }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackNumber, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackNumber is FlacValue.TrackIsrc`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.TrackNumber }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackIsrc, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackIsrc is FlacValue.TrackType`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.TrackIsrc }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackType, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackType is FlacValue.PreEmphasis`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.TrackType }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PreEmphasis, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PreEmphasis is FlacValue.TrackReserved`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PreEmphasis }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackReserved, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackReserved is FlacValue.NumberOfTrackIndexPoints`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.TrackReserved }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.NumberOfTrackIndexPoints, reader.NextValue)

[<Fact>]
let ``When at FlacValue.NumberOfTrackIndexPoints is FlacValue.TrackIndexOffset`` () =
    let state =
        { FlacStreamState.Empty with Value = FlacValue.NumberOfTrackIndexPoints }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackIndexOffset, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackIndexOffset is FlacValue.IndexPointNumber`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.TrackIndexOffset }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.IndexPointNumber, reader.NextValue)

[<Fact>]
let ``When at FlacValue.IndexPointNumber is FlacValue.TrackIndexReserved`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.IndexPointNumber }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackIndexReserved, reader.NextValue)

let trackIndexReservedInvalidStateTestData: obj array seq =
    seq {
        [| ValueNone; ValueNone; ValueNone; ValueNone |]
        [| ValueSome 69; ValueNone; ValueNone; ValueNone |]
        [| ValueNone; ValueSome 69; ValueNone; ValueNone |]
        [| ValueSome 69; ValueSome 420; ValueNone; ValueNone |]
        [| ValueSome 69; ValueSome 69; ValueNone; ValueNone |]
        [| ValueSome 69; ValueSome 69; ValueSome 69; ValueNone |]
        [| ValueSome 69; ValueSome 69; ValueNone; ValueSome 69 |]
        [| ValueSome 69; ValueSome 69; ValueSome 69; ValueSome 420 |]
    }
    |> Seq.map (Array.map box)

[<Theory>]
[<MemberData(nameof trackIndexReservedInvalidStateTestData)>]
let ``When at FlacValue.TrackIndexReserved and invalid state throws exception`` indexCount indexOffset count offset =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.TrackIndexReserved
                CueSheetTrackIndexCount = indexCount
                CueSheetTrackIndexOffset = indexOffset
                CueSheetTrackCount = count
                CueSheetTrackOffset = offset }

        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.TrackIndexReserved and offset is less than count is FlacValue.TrackIndexOffset`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.TrackIndexReserved
            CueSheetTrackIndexCount = ValueSome 420
            CueSheetTrackIndexOffset = ValueSome 69 }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackIndexOffset, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackIndexReserved and index offset is equal to index count and offset is less than count is FlacValue.TrackOffset``
    ()
    =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.TrackIndexReserved
            CueSheetTrackIndexCount = ValueSome 69
            CueSheetTrackIndexOffset = ValueSome 69
            CueSheetTrackCount = ValueSome 420
            CueSheetTrackOffset = ValueSome 69 }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.TrackOffset, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackIndexReserved and index offset is equal to index count and offset is equal to count and unknown last metadata block flag throws exception``
    ()
    =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.TrackIndexReserved
                CueSheetTrackIndexCount = ValueSome 69
                CueSheetTrackIndexOffset = ValueSome 69
                CueSheetTrackCount = ValueSome 69
                CueSheetTrackOffset = ValueSome 69 }

        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.TrackIndexReserved and index offset is equal to index count and offset equal to count and not last block is FlacValue.LastMetadataBlockFlag``
    ()
    =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.TrackIndexReserved
            CueSheetTrackIndexCount = ValueSome 69
            CueSheetTrackIndexOffset = ValueSome 69
            CueSheetTrackCount = ValueSome 69
            CueSheetTrackOffset = ValueSome 69
            LastMetadataBlock = ValueSome false }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.TrackIndexReserved and index offset is equal to index count and offset equal to count and last block is FlacValue.None``
    ()
    =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.TrackIndexReserved
            CueSheetTrackIndexCount = ValueSome 69
            CueSheetTrackIndexOffset = ValueSome 69
            CueSheetTrackCount = ValueSome 69
            CueSheetTrackOffset = ValueSome 69
            LastMetadataBlock = ValueSome true }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.None, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureType is FlacValue.MimeTypeLength`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureType }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MimeTypeLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MimeTypeLength is FlacValue.MimeType`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MimeTypeLength }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.MimeType, reader.NextValue)

[<Fact>]
let ``When at FlacValue.MimeType is FlacValue.PictureDescriptionLength`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.MimeType }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureDescriptionLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureDescriptionLength is FlacValue.PictureDescription`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureDescriptionLength }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureDescription, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureDescription is FlacValue.PictureWidth`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureDescription }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureWidth, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureWidth is FlacValue.PictureHeight`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureWidth }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureHeight, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureHeight is FlacValue.PictureColorDepth`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureHeight }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureColorDepth, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureColorDepth is FlacValue.PictureNumberOfColors`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureColorDepth }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureNumberOfColors, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureNumberOfColors is FlacValue.PictureDataLength`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureNumberOfColors }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureDataLength, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureDataLength is FlacValue.PictureData`` () =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureDataLength }
    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.PictureData, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureData and unknown last metadata block flag throws exception`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.PictureData }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)

[<Fact>]
let ``When at FlacValue.PictureData and not last metadata block is FlacValue.LastMetadataBlockFlag`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.PictureData
            LastMetadataBlock = ValueSome false }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.NextValue)

[<Fact>]
let ``When at FlacValue.PictureData and last metadata block is FlacValue.None`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.PictureData
            LastMetadataBlock = ValueSome true }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    Assert.Equal(FlacValue.None, reader.NextValue)

[<Fact>]
let ``When at unknown FlacValue throws exception`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = (enum<FlacValue> 420) }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)
        reader.NextValue |> ignore)
