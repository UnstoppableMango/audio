module Safir.Audio.Flac

open System
open System.Buffers.Binary

let private magic = "fLaC"B

let private throw m : unit = invalidOp m

let private readInt3 (bytes: ReadOnlySpan<byte>) =
    let a = int bytes[0] <<< 16
    let b = int bytes[1] <<< 8
    let c = int bytes[2]
    a + b + c

let private readUInt3 (bytes: ReadOnlySpan<byte>) =
    let a = uint bytes[0] <<< 16
    let b = uint bytes[1] <<< 8
    let c = uint bytes[2]
    a + b + c

let readMagic (f: ReadOnlySpan<byte>) =
    let marker = f.Slice(0, 4)

    if not <| marker.SequenceEqual(magic) then
        throw "Invalid stream marker"

    marker

let readMetadataBlockHeader (f: ReadOnlySpan<byte>) =
    let last = (f[0] &&& 0x80uy) <> 0uy
    let t = int (f[0] &&& 0x7Fuy)

    if t > 127 then throw "Invalid block type"

    let bt = enum<BlockType> t
    let length = readInt3 (f.Slice(1, 3))

    { LastBlock = last
      BlockType = bt
      Length = length }

let readMd5Signature (f: ReadOnlySpan<byte>) = Convert.ToHexString(f)

let readMetadataBlockStreamInfo (f: ReadOnlySpan<byte>) : MetadataBlockStreamInfoValue =
    if f.Length < 34 then
        throw "Invalid stream info block length"

    let minBlockSize = BinaryPrimitives.ReadUInt16BigEndian(f.Slice(0, 2))
    let maxBlockSize = BinaryPrimitives.ReadUInt16BigEndian(f.Slice(2, 2))

    let minFrameSize = readUInt3 (f.Slice(4, 3))
    let maxFrameSize = readUInt3 (f.Slice(7, 3))
    let sampleRate = (readUInt3 (f.Slice(10, 3))) >>> 4
    let channels = uint16 (f[12] &&& 0x0Euy >>> 1) + 1us
    let bitsPerSample = (uint16 (f[12] &&& 0x01uy) <<< 13) + (uint16 f[13] >>> 4) + 1us

    let a = uint64 (f[13] &&& 0x0Fuy) <<< 8 * 4
    let b = uint64 f[14] <<< 8 * 3
    let c = uint64 f[15] <<< 8 * 2
    let d = uint64 f[16] <<< 8
    let e = uint64 f[17]
    let samples = a + b + c + d + e

    let md5 = f.Slice(18, 16)

    { MinBlockSize = minBlockSize
      MaxBlockSize = maxBlockSize
      MinFrameSize = minFrameSize
      MaxFrameSize = maxFrameSize
      Channels = channels
      BitsPerSample = bitsPerSample
      SampleRate = sampleRate
      TotalSamples = samples
      Md5Signature = md5 }

let readMetadataBlockPadding (f: ReadOnlySpan<byte>) (length: int) =
    if f.Length < length then
        throw "Not enough bytes to read padding"

    if length % 8 <> 0 then
        throw "Padding length must be a multiple of 8"

    if f.Slice(0, length).IndexOfAnyExcept(0uy) <> -1 then
        throw "Padding contains invalid bytes"

    MetadataBlockPaddingValue length

let readMetadataBlockApplication (f: ReadOnlySpan<byte>) (length: int) : MetadataBlockApplicationValue =
    let dataLength = length - 4

    if dataLength % 8 <> 0 then
        throw "Application block length must be a multiple of 8"

    let id = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(0, 4))
    let data = f.Slice(4, dataLength)

    { ApplicationId = id
      ApplicationData = data }

let readSeekPoint (f: ReadOnlySpan<byte>) : SeekPointValue =
    if f.Length < 18 then throw "Invalid seek point length"

    { SampleNumber = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))
      StreamOffset = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(8, 8))
      FrameSamples = BinaryPrimitives.ReadUInt16BigEndian(f.Slice(16, 2)) }

let readMetadataBlockSeekTable (f: ReadOnlySpan<byte>) (length: int) =
    if length % 18 <> 0 then
        throw "Seek table length must be a multiple of 18"

    // TODO: Where to put this logic
    // let n = length / 18
    // let points = Array.zeroCreate n
    //
    // for i = 0 to n - 1 do
    //     points[i] <- readSeekPoint (f.Slice(i * 18, 18))

    { Count = length / 18
      SeekPoints = f.Slice(0, length) }

let readMetadataBlockVorbisComment (f: ReadOnlySpan<byte>) (length: int) =
    if int64 length >= (pown 2L 24) - 1L then
        throw "Invalid vorbis comment block length"

    Vorbis.readCommentHeader f length

let readCueSheetTrackIndex (f: ReadOnlySpan<byte>) : CueSheetTrackIndexValue =
    let offset = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))
    let indexPoint = uint16 f[8]

    if f.Slice(9, 3 * 8).IndexOfAnyExcept(0uy) <> -1 then
        throw "Non-zero bit found in reserved block"

    { Offset = offset
      IndexPoint = indexPoint }

let readCueSheetTrack (f: ReadOnlySpan<byte>) =
    let trackOffset = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))
    let trackNumber = uint16 f[8]
    let isrc = f.Slice(9, 12 * 8)

    let mutable offset = 9 + (12 * 8)

    let apb = f[offset]
    let isAudio = (apb &&& 0x80uy) = 0uy
    let preEmphasis = (apb &&& 0x40uy) <> 0uy

    // Skip 6 + 13 * 8
    if f.Slice(offset, 13 * 8).IndexOfAnyExcept(0uy) <> -1 then
        throw "Non-zero bit found in reserved block"

    offset <- offset + (13 * 8)

    let n = uint16 f[offset]

    // TODO
    let ctis =
        if trackNumber = 170us || trackNumber = 255us then
            List.empty
        else
            List.empty

    { Offset = trackOffset
      Number = trackNumber
      Isrc = isrc
      IsAudio = isAudio
      PreEmphasis = preEmphasis
      IndexPoints = n
      TrackIndexPoints = ReadOnlySpan<byte>.Empty }

// TODO
let readMetadataBlockCueSheet (f: ReadOnlySpan<byte>) (length: int) : MetadataBlockCueSheetValue =
    let nope =
        raise (NotImplementedException())
        ()

    nope

    { Tracks = ReadOnlySpan<byte>.Empty
      CatalogNumber = ReadOnlySpan<byte>.Empty
      TotalTracks = 0us
      IsCompactDisc = false
      LeadInSamples = 0UL }

let readMetadataBlockPicture (f: ReadOnlySpan<byte>) (length: int) =
    if f.Length < length then
        throw "Buffer is smaller than specified picture size"

    let mutable o = 0

    let typeInt = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(0, 4)) |> int
    let pictureType = enum<PictureType> typeInt
    o <- o + 4

    let mimeLength = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
    o <- o + 4

    let mimeLengthInt = mimeLength |> int
    let mimeType = f.Slice(o, mimeLengthInt)
    o <- o + mimeLengthInt

    let descriptionLength = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
    o <- o + 4

    let descriptionLenghtInt = descriptionLength |> int
    let description = f.Slice(o, descriptionLenghtInt)
    o <- o + descriptionLenghtInt

    let width = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
    o <- o + 4

    let height = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
    o <- o + 4

    let depth = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
    o <- o + 4

    let colors = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
    o <- o + 4

    let dataLength = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))

    let dataLengthInt = dataLength |> int
    let data = f.Slice(o, dataLengthInt)

    { Type = pictureType
      MimeLength = mimeLength
      MimeType = mimeType
      DescriptionLength = descriptionLength
      Description = description
      Width = width
      Height = height
      Depth = depth
      Colors = colors
      DataLength = dataLength
      Data = data }

let readMetadataBlockData (f: ReadOnlySpan<byte>) (length: int) =
    function
    | BlockType.StreamInfo -> MetadataBlockDataValue.StreamInfo(readMetadataBlockStreamInfo f)
    | BlockType.Padding -> MetadataBlockDataValue.Padding(readMetadataBlockPadding f length)
    | BlockType.SeekTable -> MetadataBlockDataValue.SeekTable(readMetadataBlockSeekTable f length)
    | BlockType.VorbisComment -> MetadataBlockDataValue.VorbisComment(readMetadataBlockVorbisComment f length)
    | BlockType.CueSheet -> MetadataBlockDataValue.CueSheet(readMetadataBlockCueSheet f length)
    | BlockType.Picture -> MetadataBlockDataValue.Picture(readMetadataBlockPicture f length)
    | BlockType.Invalid ->
        let nope =
            invalidOp "Invalid metadata block type"
            ()

        nope

        MetadataBlockDataValue.Skipped(f.Slice(0))
    | _ -> MetadataBlockDataValue.Skipped(f.Slice(0, length))

let readMetadataBlock (f: ReadOnlySpan<byte>) : MetadataBlockValue =
    let header = readMetadataBlockHeader f
    let data = readMetadataBlockData (f.Slice(4)) header.Length header.BlockType

    { Header = header; Data = data }

let readMetadataBlocks (f: ReadOnlySpan<byte>) =
    let mutable blocks = List.empty
    let mutable cont = true
    let mutable offset = 0

    // while cont do
    //     let block = readMetadataBlock (f.Slice(offset))
    //
    //     blocks <- block :: blocks
    //     offset <- offset + block.Header.Length + 4
    //     cont <- not block.Header.LastBlock

    blocks |> List.rev

let readStream (f: ReadOnlySpan<byte>) =
    let magic = readMagic f
    let streamInfo = readMetadataBlock (f.Slice(4))

    if streamInfo.Header.BlockType <> BlockType.StreamInfo then
        throw "Stream info must be the first block"

    let blocks = readMetadataBlocks (f.Slice(streamInfo.Header.Length + 8))

    { Metadata = List.Empty }
