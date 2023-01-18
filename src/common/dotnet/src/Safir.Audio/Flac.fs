module Safir.Audio.Flac

open System
open System.Buffers.Binary
open System.Text

let private magic = [| 0x66uy; 0x4Cuy; 0x61uy; 0x43uy |]

// TODO: Use these min/max block sizes for validation/whatever
// https://xiph.org/flac/format.html#metadata_block_streaminfo
let private minBlockSize = 15
let private maxBlockSize = 65535

let readMagic (f: ReadOnlySpan<byte>) =
    let n = f.Slice(0, 4)

    if n.SequenceEqual(magic) then Some "fLaC" else None

let readMetadataBlockHeader (f: ReadOnlySpan<byte>) =
    let last = (f[0] &&& 0x80uy) <> 0uy
    let t = int (f[0] &&& 0x7Fuy)

    if t > 127 then
        None
    else
        let bt = enum<BlockType> t

        let la = [| 0uy; f[1]; f[2]; f[3] |]
        let length = BinaryPrimitives.ReadInt32BigEndian(la)

        { LastBlock = last
          BlockType = bt
          Length = length }
        |> Some

let readMetadataBlockStreamInfo (f: ReadOnlySpan<byte>) =
    if f.Length < 34 then
        None
    else
        let mnb = BinaryPrimitives.ReadUInt16BigEndian(f.Slice(0, 2))
        let mxb = BinaryPrimitives.ReadUInt16BigEndian(f.Slice(2, 2))

        let mnfa = [| 0uy; f[4]; f[5]; f[6] |]
        let mnf = BinaryPrimitives.ReadUInt32BigEndian(mnfa)

        let mxfa = [| 0uy; f[7]; f[8]; f[9] |]
        let mxf = BinaryPrimitives.ReadUInt32BigEndian(mxfa)

        let sra = [| 0uy; f[10]; f[11]; f[12] |]
        let sr = BinaryPrimitives.ReadUInt32BigEndian(sra) >>> 4

        let c = uint16 (f[12] &&& 0x0Euy >>> 1) + 1us

        let bsa = [| f[12] &&& 0x01uy <<< 5; f[13] >>> 4 |]
        let bs = BinaryPrimitives.ReadUInt16BigEndian(bsa) + 1us

        let sa = [| 0uy; 0uy; 0uy; f[13] &&& 0x0Fuy; f[14]; f[15]; f[16]; f[17] |]
        let s = BinaryPrimitives.ReadUInt64BigEndian(sa)

        let md5 =
            f.Slice(18, 16).ToArray()
            |> Array.map (fun x -> String.Format("{0:x2}", x))
            |> (fun x -> String.Join(String.Empty, x))

        { MinBlockSize = mnb
          MaxBlockSize = mxb
          MinFrameSize = mnf
          MaxFrameSize = mxf
          Channels = c
          BitsPerSample = bs
          SampleRate = sr
          TotalSamples = s
          Md5Signature = md5 }
        |> Some

let readMetadataBlockPadding (f: ReadOnlySpan<byte>) (length: int) =
    if length % 8 = 0 && f.Slice(0, length).IndexOfAnyExcept(0uy) = -1 then
        MetadataBlockPadding length |> Some
    else
        None

let readMetadataBlockApplication (f: ReadOnlySpan<byte>) (length: int) =
    let dataLength = length - 4

    if dataLength % 8 = 0 then
        let id = BinaryPrimitives.ReadInt32BigEndian(f.Slice(0, 4))
        let d = f.Slice(4, dataLength).ToArray()

        { ApplicationId = id
          ApplicationData = d }
        |> Some
    else
        None

let readSeekPoint (f: ReadOnlySpan<byte>) =
    if f.Length < 18 then
        None
    else
        { SampleNumber = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))
          StreamOffset = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(8, 8))
          FrameSamples = BinaryPrimitives.ReadUInt16BigEndian(f.Slice(16, 2)) }
        |> Some

let readMetadataBlockSeekTable (f: ReadOnlySpan<byte>) (length: int) =
    if length % 18 <> 0 then
        None
    else
        let mutable ts = List.Empty

        for i = (length / 18) - 1 downto 0 do
            ts <- readSeekPoint (f.Slice(i * 18, 18)) :: ts

        ts |> List.sequenceOptionM |> Option.map MetadataBlockSeekTable

let readMetadataBlockVorbisComment (f: ReadOnlySpan<byte>) (length: int) =
    if int64 length >= (pown 2L 24) - 1L then
        None
    else
        Vorbis.pVorbisCommentHeader f length
        |> Option.map (fun x ->
            { VendorString = x.VendorString
              UserComments = x.UserComments })

let readCueSheetTrackIndex (f: ReadOnlySpan<byte>) =
    let o = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))

    let ipna = [| 0uy; f[8] |]
    let ipn = BinaryPrimitives.ReadUInt16BigEndian(ipna)

    if f.Slice(9, 3 * 8).IndexOfAnyExcept(0uy) <> -1 then
        None
    else
        Some { Offset = o; IndexPoint = ipn }

let readCueSheetTrack (f: ReadOnlySpan<byte>) =
    let o = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))

    let ta = [| 0uy; f[8] |]
    let t = BinaryPrimitives.ReadUInt16BigEndian(ta)

    let isrcS = f.Slice(9, 12 * 8)

    let isrc =
        if isrcS.IndexOfAnyExcept(0uy) <> -1 then
            Some(Encoding.ASCII.GetString(isrcS))
        else
            None

    let apb = f[105]
    let a = (apb &&& 0x80uy) = 0uy
    let p = (apb &&& 0x40uy) <> 0uy

    // Skip 6 + 13 * 8
    if f.Slice(105, 13 * 8).IndexOfAnyExcept(0uy) <> -1 then
        None
    else
        let ia = [| 0uy; f[209] |]
        let i = BinaryPrimitives.ReadUInt16BigEndian(ia)

        // TODO
        let ctis = if t = 170us || t = 255us then List.empty else List.empty

        { Offset = o
          Number = t
          Isrc = isrc
          IsAudio = a
          PreEmphasis = p
          IndexPoints = i
          TrackIndexPoints = ctis }
        |> Some

// TODO
let readMetadataBlockCueSheet (f: ReadOnlySpan<byte>) (length: int) = None

let readMetadataBlockPicture (f: ReadOnlySpan<byte>) (length: int) =
    if f.Length < length then
        None
    else
        let mutable o = 0

        let tb = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(0, 4)) |> int
        let t = enum<PictureType> tb
        o <- o + 4

        let ml = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
        o <- o + 4

        let mli = ml |> int
        let mt = Encoding.ASCII.GetString(f.Slice(o, mli))
        o <- o + mli

        let dl = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
        o <- o + 4

        let dli = dl |> int
        let ds = Encoding.UTF8.GetString(f.Slice(o, dli))
        o <- o + dli

        let w = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
        o <- o + 4

        let h = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
        o <- o + 4

        let d = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
        o <- o + 4

        let c = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))
        o <- o + 4

        let pdl = BinaryPrimitives.ReadUInt32BigEndian(f.Slice(o, 4))

        let pdli = pdl |> int
        let pd = f.Slice(o, pdli).ToArray()

        { Type = t
          MimeLength = ml
          MimeType = mt
          DescriptionLength = dl
          Description = ds
          Width = w
          Height = h
          Depth = d
          Colors = c
          DataLength = pdl
          Data = pd }
        |> Some

let readMetadataBlockData (f: ReadOnlySpan<byte>) (length: int) =
    function
    | BlockType.StreamInfo -> Option.map StreamInfo (readMetadataBlockStreamInfo f)
    | BlockType.Padding -> Option.map Padding (readMetadataBlockPadding f length)
    | BlockType.SeekTable -> Option.map SeekTable (readMetadataBlockSeekTable f length)
    | BlockType.VorbisComment -> Option.map VorbisComment (readMetadataBlockVorbisComment f length)
    | BlockType.CueSheet -> Option.map CueSheet (readMetadataBlockCueSheet f length)
    | BlockType.Picture -> Option.map Picture (readMetadataBlockPicture f length)
    | BlockType.Invalid -> None
    | _ -> Some(Skipped(f.Slice(0, length).ToArray()))

let readMetadataBlock (f: ReadOnlySpan<byte>) =
    match readMetadataBlockHeader f with
    | Some header ->
        readMetadataBlockData (f.Slice(4)) header.Length header.BlockType
        |> Option.defaultValue (Skipped(f.Slice(4, header.Length).ToArray()))
        |> (fun d -> Some { Header = header; Data = d })
    | None -> None

let readMetadataBlocks (f: ReadOnlySpan<byte>) =
    let mutable blocks = List.empty
    let mutable cont = true
    let mutable offset = 0

    while cont do
        let block = readMetadataBlock (f.Slice(offset))

        blocks <- block :: blocks

        block |> Option.iter (fun x -> offset <- offset + x.Header.Length + 4)

        cont <-
            block
            |> Option.map (fun x -> not x.Header.LastBlock)
            |> Option.defaultValue false

    blocks |> List.rev |> List.sequenceOptionM

let readFlacStream (f: ReadOnlySpan<byte>) =
    let magic = readMagic f

    let isStreamInfo x =
        x.Header.BlockType = BlockType.StreamInfo

    let streamInfo =
        match magic with
        | Some _ -> readMetadataBlock (f.Slice(4))
        | None -> None
        |> Option.filter isStreamInfo

    let blocks =
        match streamInfo with
        | Some x -> readMetadataBlocks (f.Slice(x.Header.Length + 8))
        | None -> None

    blocks
    |> Option.map2 (fun s b -> s :: b) streamInfo
    |> Option.map (fun x -> { Metadata = x })
