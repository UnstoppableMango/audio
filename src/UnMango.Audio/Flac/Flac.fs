module UnMango.Audio.Flac.Flac

open System
open System.Buffers
open System.Buffers.Binary
open System.Text
open UnMango.Audio
open UnMango.Audio.Vorbis

let readMagic (reader: byref<FlacStreamReader>) =
    if
        reader.Read()
        && reader.Value.SequenceEqual("fLaC"B)
    then
        Encoding.ASCII.GetString(reader.Value)
    else
        flacEx "Invalid Flac stream"

let readMetadataBlockHeader (reader: byref<FlacStreamReader>) =
    reader.SkipTo(FlacValue.LastMetadataBlockFlag)

    { LastBlock = reader.ReadAsLastMetadataBlockFlag()
      BlockType = reader.ReadAsBlockType()
      Length = reader.ReadAsDataBlockLength() }

let readMetadataBlockStreamInfo (reader: byref<FlacStreamReader>) =
    { MinBlockSize =
        int
        <| reader.ReadAsMinimumBlockSize()
      MaxBlockSize =
        int
        <| reader.ReadAsMaximumBlockSize()
      MinFrameSize =
        int
        <| reader.ReadAsMinimumFrameSize()
      MaxFrameSize =
        int
        <| reader.ReadAsMaximumFrameSize()
      SampleRate =
        int
        <| reader.ReadAsSampleRate()
      Channels =
        int
        <| reader.ReadAsChannels()
      BitsPerSample =
        int
        <| reader.ReadAsBitsPerSample()
      TotalSamples =
        int64
        <| reader.ReadAsTotalSamples()
      Md5Signature = reader.ReadAsMd5Signature() }

let readMetadataBlockPadding (reader: byref<FlacStreamReader>) =
    reader.Read()
    |> ignore

    MetadataBlockPadding reader.Value.Length

let readMetadataBlockApplication (reader: byref<FlacStreamReader>) =
    { ApplicationId = flacEx "TODO"
      ApplicationData = flacEx "TODO" }

let readSeekPoint (reader: byref<FlacStreamReader>) =
    {| SampleNumber =
        int64
        <| reader.ReadAsSeekPointSampleNumber()
       StreamOffset =
        int64
        <| reader.ReadAsSeekPointOffset()
       FrameSamples =
        int
        <| reader.ReadAsSeekPointNumberOfSamples() |}
    |> SeekPoint // TODO: Placeholder seek point

let readMetadataBlockSeekTable (reader: byref<FlacStreamReader>) =
    let mutable seekPoints = List.empty

    while reader.NextValue
          <> FlacValue.LastMetadataBlockFlag do
        seekPoints <-
            (readSeekPoint &reader)
            :: seekPoints

    // TODO: Can we not cons/rev?
    { Points =
        seekPoints
        |> List.rev }

let readVorbisComment (reader: byref<FlacStreamReader>) =
    reader.ReadAsUserCommentLength()
    |> ignore

    let comment = reader.ReadAsUserComment()

    match Vorbis.toComment comment with
    | Some comment -> comment
    | None -> flacEx "Invalid user comment"

let readMetadataBlockVorbisComment (reader: byref<FlacStreamReader>) =
    let mutable comments = List.empty

    reader.ReadAsVendorLength()
    |> ignore

    let vendor = reader.ReadAsVendorString()

    reader.ReadAsUserCommentListLength()
    |> ignore

    while reader.NextValue
          <> FlacValue.LastMetadataBlockFlag do
        comments <-
            (readVorbisComment &reader)
            :: comments

    // TODO: Can we not cons/rev?
    { Vendor = vendor
      UserComments = comments |> List.rev }
    |> MetadataBlockVorbisComment

// TODO
let readCueSheetTrackIndex (f: ReadOnlySpan<byte>) =
    let offset = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))
    let indexPoint = uint16 f[8]

    if
        f
            .Slice(9, 3 * 8)
            .IndexOfAnyExcept(0uy)
        <> -1
    then
        flacEx "Non-zero bit found in reserved block"

    { Offset = int64 offset
      IndexPoint = int indexPoint }

// TODO
let readCueSheetTrack (f: ReadOnlySpan<byte>) =
    let trackOffset = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))
    let trackNumber = uint16 f[8]
    let isrc = f.Slice(9, 12 * 8)

    let mutable offset = 9 + (12 * 8)

    let apb = f[offset]
    let isAudio = (apb &&& 0x80uy) = 0uy

    let preEmphasis =
        (apb &&& 0x40uy)
        <> 0uy

    // Skip 6 + 13 * 8
    if
        f
            .Slice(offset, 13 * 8)
            .IndexOfAnyExcept(0uy)
        <> -1
    then
        flacEx "Non-zero bit found in reserved block"

    offset <- offset + (13 * 8)

    let n = uint16 f[offset]

    // TODO
    let ctis =
        if
            trackNumber = 170us
            || trackNumber = 255us
        then
            List.empty
        else
            List.empty

    { Offset = int64 trackOffset
      Number = int16 trackNumber
      Isrc = None
      IsAudio = isAudio
      PreEmphasis = preEmphasis
      IndexPoints = List.Empty }

// TODO
let readMetadataBlockCueSheet (reader: byref<FlacStreamReader>) =
    raise (NotImplementedException())

    { Tracks = List.empty
      CatalogNumber = String.Empty
      TotalTracks = 0
      IsCompactDisc = false
      LeadInSamples = 0 }

let readMetadataBlockPicture (reader: byref<FlacStreamReader>) =
    let pictureType = reader.ReadAsPictureType()

    reader.ReadAsMimeTypeLength()
    |> ignore

    let mimeType = reader.ReadAsMimeType()

    reader.ReadAsPictureDescriptionLength()
    |> ignore

    let description = reader.ReadAsPictureDescription()

    let width =
        int
        <| reader.ReadAsPictureWidth()

    let height =
        int
        <| reader.ReadAsPictureHeight()

    let depth =
        int
        <| reader.ReadAsPictureColorDepth()

    let colors =
        int
        <| reader.ReadAsPictureNumberOfColors()

    let dataLength =
        int
        <| reader.ReadAsPictureDataLength()

    let data =
        reader
            .ReadAsPictureData()
            .ToArray()

    { Type = pictureType
      MimeType = mimeType
      Description = description
      Width = width
      Height = height
      Depth = depth
      Colors = colors
      DataLength = dataLength
      Data = data }

let readMetadataBlockData (reader: byref<FlacStreamReader>) =
    function
    | BlockType.StreamInfo -> StreamInfo(readMetadataBlockStreamInfo &reader)
    | BlockType.Padding -> Padding(readMetadataBlockPadding &reader)
    | BlockType.SeekTable -> SeekTable(readMetadataBlockSeekTable &reader)
    | BlockType.VorbisComment -> VorbisComment(readMetadataBlockVorbisComment &reader)
    | BlockType.CueSheet -> CueSheet(readMetadataBlockCueSheet &reader)
    | BlockType.Picture -> Picture(readMetadataBlockPicture &reader)
    | t when t < BlockType.Invalid -> Skipped(reader.Value.ToArray())
    | _ -> flacEx "Invalid block type"

let readMetadataBlock (reader: byref<FlacStreamReader>) =
    let header = readMetadataBlockHeader &reader
    let data = readMetadataBlockData &reader header.BlockType

    { Header = header; Data = data }

let readStream (reader: byref<FlacStreamReader>) =
    readMagic &reader
    |> ignore

    let streamInfo = readMetadataBlock &reader

    if
        streamInfo.Header.BlockType
        <> BlockType.StreamInfo
    then
        flacEx "Stream info must be the first block"

    let mutable metadata = [ streamInfo ]

    while reader.NextValue
          <> FlacValue.None do
        metadata <-
            (readMetadataBlock &reader)
            :: metadata

    { Metadata = metadata |> List.rev }

let addComment (stream: Span<byte>) (key: string) (value: string) =
    // TODO: Everything
    let buffer = ArrayBufferWriter(stream.Length)
    let writer = FlacStreamWriter(buffer)
    writer.WriteComment(key, value)
