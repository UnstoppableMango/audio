[<Xunit.Trait("Category", "Unit")>]
module Safir.Audio.Tests.Flac.FlacStreamReaderReadTests

open System
open Safir.Audio.Flac
open Xunit

let expectLengthCases count : obj array seq =
    seq { for i in 1 .. (count - 1) -> [| Array.zeroCreate<byte> i |] }

let expectLengthThreeCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy |] |]; [| [| 0x00uy |] |] ]

let expectLengthFourCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0x00uy; 0x00uy |] |]
      [| [| 0x00uy |] |] ]

let expectGenericBytesCases count : obj array seq =
    [ [| Array.create count 0x00uy |]
      [| Array.create count 0xFFuy |]
      [| Array.create count 0x69uy |] ]

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

[<Theory>]
[<MemberData(nameof expectLengthThreeCases)>]
let ``Throws when buffer is too small for metadata block length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.MetadataBlockType }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let minimumBlockSizeCases: obj array seq =
    [ [| [| 0x00uy; 0x10uy |] |]
      [| [| 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof minimumBlockSizeCases)>]
let ``Reads minimum block size`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.StreamInfo
            Value = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MinimumBlockSize, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Fact>]
let ``Throws when buffer is too small for minimum block size`` () =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.StreamInfo
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

let invalidMinimumBlockSizeCases: obj array seq =
    [ [| [| 0x00uy; 0x0Fuy |] |]; [| [| 0x00uy; 0x00uy |] |] ]

[<Theory>]
[<MemberData(nameof invalidMinimumBlockSizeCases)>]
let ``Throws when minimum block size is invalid`` (data: byte array) =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.StreamInfo
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let maximumBlockSizeCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof maximumBlockSizeCases)>]
let ``Reads maximum block size`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.MinimumBlockSize }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MaximumBlockSize, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Fact>]
let ``Throws when buffer is too small for maximum block size`` () =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.MinimumBlockSize }
        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

let minimumFrameSizeCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof minimumFrameSizeCases)>]
let ``Reads minimum frame size`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.MaximumBlockSize }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MinimumFrameSize, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthThreeCases)>]
let ``Throws when buffer is too small for minimum frame size`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.MaximumBlockSize }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let maximumFrameSizeCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof maximumFrameSizeCases)>]
let ``Reads maximum frame size`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.MinimumFrameSize }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MaximumFrameSize, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthThreeCases)>]
let ``Throws when buffer is too small for maximum frame size`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.MinimumFrameSize }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let sampleRateCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x10uy |] |]
      [| [| 0x00uy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x00uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof sampleRateCases)>]
let ``Reads sample rate`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.MaximumFrameSize }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.StreamInfoSampleRate, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthThreeCases)>]
let ``Throws when buffer is too small for sample rate`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.MaximumFrameSize }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let invalidSampleRateCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0x9Fuy; 0xFFuy; 0x70uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy |] |] ]

[<Theory>]
[<MemberData(nameof invalidSampleRateCases)>]
let ``Throws when sample rate is invalid`` (data: byte array) =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.MaximumFrameSize }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let numberOfChannelsCases: obj array seq =
    [ [| [| 0x00uy |] |]; [| [| 0xFFuy |] |]; [| [| 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof numberOfChannelsCases)>]
let ``Reads number of channels`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.StreamInfoSampleRate }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.NumberOfChannels, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

let bitsPerSampleCases: obj array seq =
    [ [| [| 0x00uy; 0x30uy |] |]
      [| [| 0xFEuy; 0x3Fuy |] |]
      [| [| 0x01uy; 0xF0uy |] |]
      [| [| 0xFFuy; 0xFFuy |] |] ]

[<Theory>]
[<MemberData(nameof bitsPerSampleCases)>]
let ``Reads bits per sample`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.NumberOfChannels }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.BitsPerSample, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Fact>]
let ``Throws when buffer is too small for bits per sample`` () =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.NumberOfChannels }
        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

let invalidBitsPerSampleCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy |] |]; [| [| 0x00uy; 0x20uy |] |] ]

[<Theory>]
[<MemberData(nameof invalidBitsPerSampleCases)>]
let ``Throws when bits per sample is invalid`` (data: byte array) =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.NumberOfChannels }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let totalSamplesCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0x0Fuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof totalSamplesCases)>]
let ``Reads total samples`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.BitsPerSample }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.TotalSamples, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

let invalidTotalSamplesCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0x00uy; 0x00uy |] |]
      [| [| 0x00uy |] |] ]

[<Theory>]
[<MemberData(nameof invalidTotalSamplesCases)>]
let ``Throws when buffer is too small for total samples`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.BitsPerSample }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let md5SignatureCases: obj array seq =
    [ [| Array.create 16 0x00uy |]; [| Array.create 16 0xFFuy |] ]

[<Theory>]
[<MemberData(nameof md5SignatureCases)>]
let ``Reads MD5 signature`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.TotalSamples }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.Md5Signature, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

let invalidMd5SignatureCases: obj array seq =
    seq { for i in 1..15 -> [| Array.zeroCreate<byte> i |] }

[<Theory>]
[<MemberData(nameof invalidMd5SignatureCases)>]
let ``Throws when buffer is too small for MD5 signature`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.TotalSamples }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Fact>]
let ``Throws when at MD5 signature and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.Md5Signature }
        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at MD5 signature and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.Md5Signature
            LastMetadataBlock = ValueSome false }

    let mutable reader =
        FlacStreamReader(ReadOnlySpan<byte>.op_Implicit [| data |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual([| data |]))

[<Fact>]
let ``Reads to end when at MD5 signature and last block`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.Md5Signature
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

let paddingCases: obj array seq =
    [ [| [| 0x00uy |]; 1u |]; [| Array.zeroCreate<byte> 69; 69u |] ]

[<Theory>]
[<MemberData(nameof paddingCases)>]
let ``Reads padding`` (data: byte array) (length: uint) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.Padding
            BlockLength = ValueSome length
            Value = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.Padding, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Fact>]
let ``Throws when reading padding and no block length`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Padding
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

let invalidBufferPaddingCases: obj array seq =
    [ [| [| 0x00uy |]; 2u |]
      [| Array.zeroCreate<byte> 69; 70u |]
      [| [| 0x00uy |]; 69u |] ]

[<Theory>]
[<MemberData(nameof invalidBufferPaddingCases)>]
let ``Throws when buffer is too small for padding`` (data: byte array) (length: uint) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Padding
                BlockLength = ValueSome length
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let invalidPaddingCases: obj array seq =
    [ [| [| 0x0Fuy |]; 1u |]
      [| [| 0xFuy |]; 1u |]
      [| [| 0xF0uy; 0x00uy |]; 2u |]
      [| [| 0x00uy; 0xF0uy |]; 2u |] ]

[<Theory>]
[<MemberData(nameof invalidPaddingCases)>]
let ``Throws when padding is invalid`` (data: byte array) (length: uint) =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Padding
                BlockLength = ValueSome length
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Fact>]
let ``Throws when at padding and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.Padding }
        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at padding and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.Padding
            LastMetadataBlock = ValueSome false }

    let mutable reader =
        FlacStreamReader(ReadOnlySpan<byte>.op_Implicit [| data |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual([| data |]))

[<Fact>]
let ``Reads to end when at padding and last block`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.Padding
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

let applicationIdCases: obj array seq =
    [ [| [| 0x00uy; 0x10uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof applicationIdCases)>]
let ``Reads application id`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.Application
            Value = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.ApplicationId, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthFourCases)>]
let ``Throws when buffer is too small for applicationId`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Application
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let applicationDataCases: obj array seq =
    [ [| [| 0x00uy |]; 5u |]; [| Array.zeroCreate<byte> 69; 73u |] ]

[<Theory>]
[<MemberData(nameof applicationDataCases)>]
let ``Reads application data`` (data: byte array) (length: uint) =
    let state =
        { FlacStreamState.Empty with
            BlockLength = ValueSome length
            Value = FlacValue.ApplicationId }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.ApplicationData, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Fact>]
let ``Throws when reading application data and no block length`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.ApplicationId }
        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

let invalidBufferApplicationDataCases: obj array seq =
    [ [| [| 0x00uy |]; 6u |]
      [| Array.zeroCreate<byte> 69; 74u |]
      [| [| 0x00uy |]; 69u |] ]

[<Theory>]
[<MemberData(nameof invalidBufferApplicationDataCases)>]
let ``Throws when buffer is too small for application data`` (data: byte array) (length: uint) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockLength = ValueSome length
                Value = FlacValue.ApplicationId }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Fact>]
let ``Throws when at application data and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.ApplicationData }
        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at application data and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.ApplicationData
            LastMetadataBlock = ValueSome false }

    let mutable reader =
        FlacStreamReader(ReadOnlySpan<byte>.op_Implicit [| data |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual([| data |]))

[<Fact>]
let ``Reads to end when at application data and last block`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.ApplicationData
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

let seekPointSampleNumberCases: obj array seq =
    [ [| [| 0x00uy; 0x10uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof seekPointSampleNumberCases)>]
let ``Reads seek point sample number`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.SeekTable
            BlockLength = ValueSome 18u
            Value = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.SeekPointSampleNumber, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 8)>]
let ``Throws when buffer is too small for seek point sample number`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.SeekTable
                BlockLength = ValueSome 18u
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Fact>]
let ``Throws when reading seek point sample number and no block length`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.SeekTable
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

[<Theory>]
[<InlineData(1)>]
[<InlineData(17)>]
[<InlineData(69)>]
let ``Throws when reading seek point sample number and invalid block length`` (length: uint) =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.SeekTable
                BlockLength = ValueSome length
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

let seekPointOffsetCases: obj array seq =
    [ [| [| 0x00uy; 0x10uy; 0x00uy; 0x00uy; 0x00uy; 0x10uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof seekPointOffsetCases)>]
let ``Reads seek point offset`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.SeekPointSampleNumber }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.SeekPointOffset, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 8)>]
let ``Throws when buffer is too small for seek point offset`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.SeekPointSampleNumber }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let numberOfSamplesCases: obj array seq =
    [ [| [| 0x00uy; 0x10uy |] |]
      [| [| 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof numberOfSamplesCases)>]
let ``Reads number of samples`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.SeekPointOffset }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.NumberOfSamples, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 2)>]
let ``Throws when buffer is too small for number of samples`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.SeekPointOffset }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Theory>]
[<MemberData(nameof seekPointSampleNumberCases)>]
let ``Reads seek point sample number when at number of samples and offset is less than count`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.NumberOfSamples
            SeekPointCount = ValueSome 2u
            SeekPointOffset = ValueSome 1u }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.SeekPointSampleNumber, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 8)>]
let ``Throws when at number of samples and buffer is too small for seek point sample number`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.NumberOfSamples
                SeekPointCount = ValueSome 2u
                SeekPointOffset = ValueSome 1u }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let numberOfSamplesInvalidStateCases: obj array seq =
    seq {
        [| ValueNone; ValueNone |]
        [| ValueSome 69u; ValueNone |]
        [| ValueNone; ValueSome 69u |]
        [| ValueSome 69u; ValueSome 420u |]
    }
    |> Seq.map (Array.map box)

[<Theory>]
[<MemberData(nameof numberOfSamplesInvalidStateCases)>]
let ``Throws when at number of samples and state is invalid`` (count: uint voption) (offset: uint voption) =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.NumberOfSamples
                SeekPointCount = count
                SeekPointOffset = offset }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

[<Fact>]
let ``Throws when at number of samples, count equals offset, and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.NumberOfSamples
                SeekPointCount = ValueSome 69u
                SeekPointOffset = ValueSome 69u }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at number of samples, count equals offset, and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.NumberOfSamples
            SeekPointCount = ValueSome 69u
            SeekPointOffset = ValueSome 69u
            LastMetadataBlock = ValueSome false }

    let mutable reader =
        FlacStreamReader(ReadOnlySpan<byte>.op_Implicit [| data |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual([| data |]))

[<Fact>]
let ``Reads to end when at number of samples, count equals offset, and last block`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.NumberOfSamples
            SeekPointCount = ValueSome 69u
            SeekPointOffset = ValueSome 69u
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

let vendorLengthCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof vendorLengthCases)>]
let ``Reads vendor length`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.VorbisComment
            Value = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.VendorLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for vendor length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.VorbisComment
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let vendorStringCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory(Skip = "We currently can't start in the middle of the vendor string")>]
[<MemberData(nameof vendorStringCases)>]
let ``Reads vendor string`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.VendorLength }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.VendorString, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of the vendor string")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for vendor string`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.VendorLength }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let userCommentListLengthCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof userCommentListLengthCases)>]
let ``Reads user comment list length`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.VendorString }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.UserCommentListLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for user comment list length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.VendorString }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let userCommentLengthCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof userCommentLengthCases)>]
let ``Reads user comment length`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.UserCommentListLength }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.UserCommentLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for user comment length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.UserCommentListLength }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let userCommentCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory(Skip = "We currently can't start in the middle of a user comment")>]
[<MemberData(nameof userCommentCases)>]
let ``Reads user comment`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.UserCommentLength }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.UserComment, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of a user comment")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for user comment`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.UserCommentLength }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Theory>]
[<MemberData(nameof userCommentLengthCases)>]
let ``Reads user comment length when at user comment and offset is less than count`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.UserComment
            UserCommentCount = ValueSome 2u
            UserCommentOffset = ValueSome 1u }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.UserCommentLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when at user comment and buffer is too small for user comment length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.UserComment
                UserCommentCount = ValueSome 2u
                UserCommentOffset = ValueSome 1u }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let userCommentInvalidStateCases: obj array seq =
    seq {
        [| ValueNone; ValueNone |]
        [| ValueSome 69u; ValueNone |]
        [| ValueNone; ValueSome 69u |]
        [| ValueSome 69u; ValueSome 420u |]
    }
    |> Seq.map (Array.map box)

[<Theory>]
[<MemberData(nameof userCommentInvalidStateCases)>]
let ``Throws when at user comment and state is invalid`` (count: uint voption) (offset: uint voption) =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.UserComment
                UserCommentCount = count
                UserCommentOffset = offset }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

[<Fact>]
let ``Throws when at user comment, count equals offset, and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                Value = FlacValue.UserComment
                UserCommentCount = ValueSome 69u
                UserCommentOffset = ValueSome 69u }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)
        reader.Read() |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at user comment, count equals offset, and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.UserComment
            UserCommentCount = ValueSome 69u
            UserCommentOffset = ValueSome 69u
            LastMetadataBlock = ValueSome false }

    let mutable reader =
        FlacStreamReader(ReadOnlySpan<byte>.op_Implicit [| data |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.LastMetadataBlockFlag, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual([| data |]))

[<Fact>]
let ``Reads to end when at user comment, count equals offset, and last block`` () =
    let state =
        { FlacStreamState.Empty with
            Value = FlacValue.UserComment
            UserCommentCount = ValueSome 69u
            UserCommentOffset = ValueSome 69u
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

// TODO: Cue sheet tests

let pictureTypeCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof pictureTypeCases)>]
let ``Reads picture type`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.Picture
            Value = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureType, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture type`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Picture
                Value = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let mimeTypeLengthCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof mimeTypeLengthCases)>]
let ``Reads mime type length`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureType }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MimeTypeLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for mime type length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.PictureType }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let mimeTypeCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory(Skip = "We currently can't start in the middle of the mime type")>]
[<MemberData(nameof mimeTypeCases)>]
let ``Reads mime type`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.MimeTypeLength }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MimeType, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of the mime type")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for mime type`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.MimeTypeLength }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let pictureDescriptionLengthCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof pictureDescriptionLengthCases)>]
let ``Reads picture description length`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.MimeType }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureDescriptionLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture description length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.MimeType }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

let pictureDescriptionCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x69uy; 0x69uy; 0x69uy; 0x69uy |] |] ]

[<Theory(Skip = "We currently can't start in the middle of the picture description")>]
[<MemberData(nameof pictureDescriptionCases)>]
let ``Reads picture description`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with Value = FlacValue.PictureDescriptionLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureDescription, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of the picture description")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture description`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state =
            { FlacStreamState.Empty with Value = FlacValue.PictureDescriptionLength }

        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture width`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureDescription }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureWidth, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture width`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.PictureDescription }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture height`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureWidth }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureHeight, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture height`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.PictureWidth }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture color depth`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureHeight }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureColorDepth, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture color depth`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.PictureHeight }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture number of colors`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureColorDepth }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureNumberOfColors, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture number of colors`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.PictureColorDepth }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture data length`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureNumberOfColors }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureDataLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture data length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.PictureNumberOfColors }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)

[<Theory(Skip = "We currently can't start in the middle of the picture data")>]
[<MemberData(nameof expectGenericBytesCases, 69)>]
let ``Reads picture data`` (data: byte array) =
    let state = { FlacStreamState.Empty with Value = FlacValue.PictureDataLength }
    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureData, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of the picture data")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture data`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException> (fun () ->
        let state = { FlacStreamState.Empty with Value = FlacValue.PictureDataLength }
        let mutable reader = FlacStreamReader(data, state)
        reader.Read() |> ignore)
