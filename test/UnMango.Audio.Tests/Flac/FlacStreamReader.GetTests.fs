[<Xunit.Trait("Category", "Unit")>]
module UnMango.Audio.Tests.Flac.FlacStreamReaderGetTests

open System
open UnMango.Audio.Flac
open Xunit

let expectLengthCases count : obj array seq =
    seq { for i in 1 .. (count - 1) -> [| Array.zeroCreate<byte> i |] }

let expectGenericBytesCases count : obj array seq =
    [ [| Array.create count 0x00uy |]
      [| Array.create count 0xFFuy |]
      [| Array.create count 0x69uy |] ]

let valuesExcept (value: int) : obj array seq =
    Enum.GetValues<FlacValue>()
    |> Array.filter (fun x ->
        x
        <> (enum<FlacValue> value))
    |> Seq.map (fun x -> [| x |])

[<Theory>]
[<InlineData(0x80uy, true)>]
[<InlineData(0x00uy, false)>]
let ``Gets last metadata block flag`` (data: byte) (expected: bool) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.LastMetadataBlockFlag
            Value = [| data |] }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    let value = reader.GetLastMetadataBlockFlag()

    Assert.Equal(expected, value)

[<Theory>]
[<MemberData(nameof valuesExcept, FlacValue.LastMetadataBlockFlag)>]
let ``Throws when not positioned at last metadata block flag`` (value: FlacValue) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state = { FlacStreamState.Empty with Position = value }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

        reader.GetLastMetadataBlockFlag()
        |> ignore)

[<Theory>]
[<InlineData(0xFFuy, BlockType.Invalid)>]
[<InlineData(0x7Fuy, BlockType.Invalid)>]
[<InlineData(0x69uy, 105)>]
[<InlineData(0x00uy, BlockType.StreamInfo)>]
let ``Gets metadata block type`` (data: byte) (expected: BlockType) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.MetadataBlockType
            Value = [| data |] }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    let value = reader.GetBlockType()

    Assert.Equal(expected, value)

[<Theory>]
[<MemberData(nameof valuesExcept, FlacValue.MetadataBlockType)>]
let ``Throws when not positioned at last metadata block type`` (value: FlacValue) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state = { FlacStreamState.Empty with Position = value }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

        reader.GetBlockType()
        |> ignore)

let metadataBlockLengthCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy |]; 0 |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy |]
         16_777_215 |]
      [| [| 0x69uy; 0x69uy; 0x69uy |]
         6_908_265 |] ]

[<Theory>]
[<MemberData(nameof metadataBlockLengthCases)>]
let ``Gets metadata block length`` (data: byte array) (expected: uint32) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.DataBlockLength
            Value = data }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    let value = reader.GetDataBlockLength()

    Assert.Equal(expected, value)

[<Theory>]
[<MemberData(nameof valuesExcept, FlacValue.DataBlockLength)>]
let ``Throws when not positioned at data block length`` (value: FlacValue) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state = { FlacStreamState.Empty with Position = value }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

        reader.GetDataBlockLength()
        |> ignore)

let minMaxBlockSizeCases: obj array seq =
    [ [| [| 0x00uy; 0x10uy |]; 16 |]
      [| [| 0xFFuy; 0xFFuy |]; 65_535 |]
      [| [| 0x69uy; 0x69uy |]; 26_985 |] ]

[<Theory>]
[<MemberData(nameof minMaxBlockSizeCases)>]
let ``Gets minimum block size`` (data: byte array) (expected: uint16) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.MinimumBlockSize
            Value = data }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    let value = reader.GetMinimumBlockSize()

    Assert.Equal(expected, value)

[<Theory>]
[<MemberData(nameof valuesExcept, FlacValue.MinimumBlockSize)>]
let ``Throws when not positioned at minimum block size`` (value: FlacValue) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state = { FlacStreamState.Empty with Position = value }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

        reader.GetMinimumBlockSize()
        |> ignore)

[<Theory>]
[<MemberData(nameof minMaxBlockSizeCases)>]
let ``Gets maximum block size`` (data: byte array) (expected: uint16) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.MinimumBlockSize
            Value = data }

    let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

    let value = reader.GetMinimumBlockSize()

    Assert.Equal(expected, value)

[<Theory>]
[<MemberData(nameof valuesExcept, FlacValue.MaximumBlockSize)>]
let ``Throws when not positioned at maximum block size`` (value: FlacValue) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state = { FlacStreamState.Empty with Position = value }
        let reader = FlacStreamReader(ReadOnlySpan<byte>.Empty, state)

        reader.GetMaximumBlockSize()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 3)>]
let ``Reads minimum frame size`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.MaximumBlockSize }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MinimumFrameSize, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 3)>]
let ``Throws when buffer is too small for minimum frame size`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.MaximumBlockSize }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 3)>]
let ``Reads maximum frame size`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.MinimumFrameSize }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MaximumFrameSize, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 3)>]
let ``Throws when buffer is too small for maximum frame size`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.MinimumFrameSize }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

let sampleRateCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x10uy |] |]
      [| [| 0x00uy; 0xFFuy; 0xFFuy |] |]
      [| [| 0x00uy; 0x69uy; 0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof sampleRateCases)>]
let ``Reads sample rate`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.MaximumFrameSize }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.StreamInfoSampleRate, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 3)>]
let ``Throws when buffer is too small for sample rate`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.MaximumFrameSize }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

let invalidSampleRateCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy; 0x00uy |] |]
      [| [| 0x9Fuy; 0xFFuy; 0x70uy |] |]
      [| [| 0xFFuy; 0xFFuy; 0xFFuy |] |] ]

[<Theory>]
[<MemberData(nameof invalidSampleRateCases)>]
let ``Throws when sample rate is invalid`` (data: byte array) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.MaximumFrameSize }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 1)>]
let ``Reads number of channels`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.StreamInfoSampleRate }

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
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.NumberOfChannels }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.BitsPerSample, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Fact>]
let ``Throws when buffer is too small for bits per sample`` () =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.NumberOfChannels }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

let invalidBitsPerSampleCases: obj array seq =
    [ [| [| 0x00uy; 0x00uy |] |]
      [| [| 0x00uy; 0x20uy |] |] ]

[<Theory>]
[<MemberData(nameof invalidBitsPerSampleCases)>]
let ``Throws when bits per sample is invalid`` (data: byte array) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.NumberOfChannels }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

let totalSamplesCases: obj array seq =
    [ [| [| 0x00uy
            0x00uy
            0x00uy
            0x00uy
            0x00uy |] |]
      [| [| 0x0Fuy
            0xFFuy
            0xFFuy
            0xFFuy
            0xFFuy |] |]
      [| [| 0xFFuy
            0xFFuy
            0xFFuy
            0xFFuy
            0xFFuy |] |]
      [| [| 0x69uy
            0x69uy
            0x69uy
            0x69uy
            0x69uy |] |] ]

[<Theory>]
[<MemberData(nameof totalSamplesCases)>]
let ``Reads total samples`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.BitsPerSample }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.TotalSamples, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 5)>]
let ``Throws when buffer is too small for total samples`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.BitsPerSample }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

let md5SignatureCases: obj array seq =
    [ [| Array.create 16 0x00uy |]
      [| Array.create 16 0xFFuy |] ]

[<Theory>]
[<MemberData(nameof md5SignatureCases)>]
let ``Reads MD5 signature`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.TotalSamples }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.Md5Signature, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 16)>]
let ``Throws when buffer is too small for MD5 signature`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.TotalSamples }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Fact>]
let ``Throws when at MD5 signature and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.Md5Signature }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at MD5 signature and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.Md5Signature
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
            Position = FlacValue.Md5Signature
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

let paddingCases: obj array seq =
    [ [| [| 0x00uy |]; 1u |]
      [| Array.zeroCreate<byte> 69; 69u |] ]

[<Theory>]
[<MemberData(nameof paddingCases)>]
let ``Reads padding`` (data: byte array) (length: uint) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.Padding
            BlockLength = ValueSome length
            Position = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.Padding, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Fact>]
let ``Throws when reading padding and no block length`` () =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Padding
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

let invalidBufferPaddingCases: obj array seq =
    [ [| [| 0x00uy |]; 2u |]
      [| Array.zeroCreate<byte> 69; 70u |]
      [| [| 0x00uy |]; 69u |] ]

[<Theory>]
[<MemberData(nameof invalidBufferPaddingCases)>]
let ``Throws when buffer is too small for padding`` (data: byte array) (length: uint) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Padding
                BlockLength = ValueSome length
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

let invalidPaddingCases: obj array seq =
    [ [| [| 0x0Fuy |]; 1u |]
      [| [| 0xFuy |]; 1u |]
      [| [| 0xF0uy; 0x00uy |]; 2u |]
      [| [| 0x00uy; 0xF0uy |]; 2u |] ]

[<Theory>]
[<MemberData(nameof invalidPaddingCases)>]
let ``Throws when padding is invalid`` (data: byte array) (length: uint) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Padding
                BlockLength = ValueSome length
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Fact>]
let ``Throws when at padding and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.Padding }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at padding and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.Padding
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
            Position = FlacValue.Padding
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads application id`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.Application
            Position = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.ApplicationId, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for applicationId`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Application
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

let applicationDataCases: obj array seq =
    [ [| [| 0x00uy |]; 5u |]
      [| Array.zeroCreate<byte> 69; 73u |] ]

[<Theory>]
[<MemberData(nameof applicationDataCases)>]
let ``Reads application data`` (data: byte array) (length: uint) =
    let state =
        { FlacStreamState.Empty with
            BlockLength = ValueSome length
            Position = FlacValue.ApplicationId }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.ApplicationData, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Fact>]
let ``Throws when reading application data and no block length`` () =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.ApplicationId }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

let invalidBufferApplicationDataCases: obj array seq =
    [ [| [| 0x00uy |]; 6u |]
      [| Array.zeroCreate<byte> 69; 74u |]
      [| [| 0x00uy |]; 69u |] ]

[<Theory>]
[<MemberData(nameof invalidBufferApplicationDataCases)>]
let ``Throws when buffer is too small for application data`` (data: byte array) (length: uint) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockLength = ValueSome length
                Position = FlacValue.ApplicationId }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Fact>]
let ``Throws when at application data and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.ApplicationData }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at application data and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.ApplicationData
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
            Position = FlacValue.ApplicationData
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 8)>]
let ``Reads seek point sample number`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.SeekTable
            BlockLength = ValueSome 18u
            Position = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.SeekPointSampleNumber, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 8)>]
let ``Throws when buffer is too small for seek point sample number`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.SeekTable
                BlockLength = ValueSome 18u
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Fact>]
let ``Throws when reading seek point sample number and no block length`` () =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.SeekTable
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Theory>]
[<InlineData(1)>]
[<InlineData(17)>]
[<InlineData(69)>]
let ``Throws when reading seek point sample number and invalid block length`` (length: uint) =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.SeekTable
                BlockLength = ValueSome length
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 8)>]
let ``Reads seek point offset`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.SeekPointSampleNumber }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.SeekPointOffset, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 8)>]
let ``Throws when buffer is too small for seek point offset`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.SeekPointSampleNumber }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 2)>]
let ``Reads number of samples`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.SeekPointOffset }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.NumberOfSamples, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 2)>]
let ``Throws when buffer is too small for number of samples`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.SeekPointOffset }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 8)>]
let ``Reads seek point sample number when at number of samples and offset is less than count`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.NumberOfSamples
            SeekPointCount = ValueSome 2u
            SeekPointOffset = ValueSome 1u }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.SeekPointSampleNumber, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 8)>]
let ``Throws when at number of samples and buffer is too small for seek point sample number`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.NumberOfSamples
                SeekPointCount = ValueSome 2u
                SeekPointOffset = ValueSome 1u }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

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
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.NumberOfSamples
                SeekPointCount = count
                SeekPointOffset = offset }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Fact>]
let ``Throws when at number of samples, count equals offset, and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.NumberOfSamples
                SeekPointCount = ValueSome 69u
                SeekPointOffset = ValueSome 69u }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at number of samples, count equals offset, and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.NumberOfSamples
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
            Position = FlacValue.NumberOfSamples
            SeekPointCount = ValueSome 69u
            SeekPointOffset = ValueSome 69u
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads vendor length`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.VorbisComment
            Position = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.VendorLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for vendor length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.VorbisComment
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory(Skip = "We currently can't start in the middle of the vendor string")>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads vendor string`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.VendorLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.VendorString, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of the vendor string")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for vendor string`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.VendorLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads user comment list length`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.VendorString }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.UserCommentListLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for user comment list length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.VendorString }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads user comment length`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.UserCommentListLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.UserCommentLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for user comment length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.UserCommentListLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory(Skip = "We currently can't start in the middle of a user comment")>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads user comment`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.UserCommentLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.UserComment, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of a user comment")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for user comment`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.UserCommentLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads user comment length when at user comment and offset is less than count`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.UserComment
            UserCommentCount = ValueSome 2u
            UserCommentOffset = ValueSome 1u }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.UserCommentLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when at user comment and buffer is too small for user comment length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.UserComment
                UserCommentCount = ValueSome 2u
                UserCommentOffset = ValueSome 1u }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

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
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.UserComment
                UserCommentCount = count
                UserCommentOffset = offset }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Fact>]
let ``Throws when at user comment, count equals offset, and unknown last metadata block`` () =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.UserComment
                UserCommentCount = ValueSome 69u
                UserCommentOffset = ValueSome 69u }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Theory>]
[<InlineData(0x80uy)>]
[<InlineData(0x00uy)>]
let ``Reads last metadata block flag when at user comment, count equals offset, and not last block`` (data: byte) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.UserComment
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
            Position = FlacValue.UserComment
            UserCommentCount = ValueSome 69u
            UserCommentOffset = ValueSome 69u
            LastMetadataBlock = ValueSome true }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)

// TODO: Cue sheet tests

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture type`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome BlockType.Picture
            Position = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureType, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture type`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome BlockType.Picture
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads mime type length`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.PictureType }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MimeTypeLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for mime type length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.PictureType }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory(Skip = "We currently can't start in the middle of the mime type")>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads mime type`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.MimeTypeLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MimeType, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of the mime type")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for mime type`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.MimeTypeLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture description length`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.MimeType }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureDescriptionLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture description length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.MimeType }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory(Skip = "We currently can't start in the middle of the picture description")>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture description`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.PictureDescriptionLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureDescription, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of the picture description")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture description`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.PictureDescriptionLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture width`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.PictureDescription }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureWidth, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture width`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.PictureDescription }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture height`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.PictureWidth }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureHeight, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture height`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.PictureWidth }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture color depth`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.PictureHeight }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureColorDepth, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture color depth`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.PictureHeight }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture number of colors`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.PictureColorDepth }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureNumberOfColors, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture number of colors`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.PictureColorDepth }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<MemberData(nameof expectGenericBytesCases, 4)>]
let ``Reads picture data length`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.PictureNumberOfColors }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureDataLength, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture data length`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.PictureNumberOfColors }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory(Skip = "We currently can't start in the middle of the picture data")>]
[<MemberData(nameof expectGenericBytesCases, 69)>]
let ``Reads picture data`` (data: byte array) =
    let state =
        { FlacStreamState.Empty with
            Position = FlacValue.PictureDataLength }

    let mutable reader = FlacStreamReader(data, state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.PictureData, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual(data))

[<Theory(Skip = "We currently can't start in the middle of the picture data")>]
[<MemberData(nameof expectLengthCases, 4)>]
let ``Throws when buffer is too small for picture data`` (data: byte array) =
    Assert.Throws<ArgumentOutOfRangeException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.PictureDataLength }

        let mutable reader = FlacStreamReader(data, state)

        reader.Read()
        |> ignore)

[<Theory>]
[<InlineData(127)>]
[<InlineData(420)>]
let ``Throws when block type is invalid`` blockType =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome(enum<BlockType> blockType)
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Theory>]
[<InlineData(7)>]
[<InlineData(69)>]
[<InlineData(126)>]
let ``Reads metadata block data when block type is unrecognized`` blockType =
    let state =
        { FlacStreamState.Empty with
            BlockType = ValueSome(enum<BlockType> blockType)
            BlockLength = ValueSome 1u
            Position = FlacValue.DataBlockLength }

    let mutable reader = FlacStreamReader([| 0x69uy |], state)

    Assert.True(reader.Read())
    Assert.Equal(FlacValue.MetadataBlockData, reader.ValueType)
    Assert.True(reader.Value.SequenceEqual([| 0x69uy |]))

[<Theory>]
[<InlineData(7)>]
[<InlineData(69)>]
[<InlineData(126)>]
let ``Throws when block type is unrecognized and unknown block length`` blockType =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                BlockType = ValueSome(enum<BlockType> blockType)
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)

[<Fact>]
let ``Throws when block type is unknown`` =
    Assert.Throws<FlacStreamReaderException>(fun () ->
        let state =
            { FlacStreamState.Empty with
                Position = FlacValue.DataBlockLength }

        let mutable reader = FlacStreamReader([| 0x69uy |], state)

        reader.Read()
        |> ignore)
