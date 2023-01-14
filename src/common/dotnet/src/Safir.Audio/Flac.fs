module Safir.Audio.Flac

open System
open System.Buffers.Binary
open System.Text

let private magic = [| 0x66uy; 0x4Cuy; 0x61uy; 0x43uy |]

let pMagic (f: ReadOnlySpan<byte>) =
    let n = f.Slice(0, 4)

    if n.SequenceEqual(magic) then
        { Remaining = f.Slice(4)
          Result = Some "fLaC" }
    else
        { Remaining = f; Result = None }

let pMetadataBlockHeader (f: ReadOnlySpan<byte>) =
    let last = (f[0] &&& 0x80uy) <> 0uy
    let t = int (f[0] &&& 0x7Fuy)
    let bt = enum<BlockType> t

    let la = [| 0uy; f[1]; f[2]; f[3] |]
    let length = BinaryPrimitives.ReadInt32BigEndian(la)

    { Remaining = f.Slice(4)
      Result =
        { LastBlock = last
          BlockType = bt
          Length = length }
        |> Some }

let pMetadataBlockStreamInfo (f: ReadOnlySpan<byte>) (length: int) =
    if length = 34 then
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

        { Remaining = f.Slice(34)
          Result =
            { MinBlockSize = mnb
              MaxBlockSize = mxb
              MinFrameSize = mnf
              MaxFrameSize = mxf
              Channels = c
              BitsPerSample = bs
              SampleRate = sr
              TotalSamples = s
              Md5Signature = md5 }
            |> Some }
    else
        { Remaining = f; Result = None }


let pMetadataBlockPadding (f: ReadOnlySpan<byte>) (length: int) =
    if length % 8 = 0 && f.Slice(0, length).IndexOfAnyExcept(0uy) = -1 then
        { Remaining = f.Slice(length)
          Result = MetadataBlockPadding length |> Some }
    else
        { Remaining = f; Result = None }

let pMetadataBlockApplication (f: ReadOnlySpan<byte>) (length: int) =
    let dataLength = length - 4

    if dataLength % 8 = 0 then
        let id = BinaryPrimitives.ReadInt32BigEndian(f.Slice(0, 4))
        let d = f.Slice(4, dataLength).ToArray()

        { Remaining = f.Slice(length)
          Result =
            { ApplicationId = id
              ApplicationData = d }
            |> Some }
    else
        { Remaining = f; Result = None }

let pSeekPoint (f: ReadOnlySpan<byte>) =
    { SampleNumber = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))
      StreamOffset = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(8, 8))
      FrameSamples = BinaryPrimitives.ReadUInt16BigEndian(f.Slice(16, 2)) }

let pMetadataBlockSeekTable (f: ReadOnlySpan<byte>) (length: int) =
    if length % 18 = 0 then
        let mutable ts = List.Empty

        for i = (length / 18) - 1 downto 0 do
            ts <- pSeekPoint (f.Slice(i * 18, 18)) :: ts

        { Remaining = f.Slice(length)
          Result = MetadataBlockSeekTable ts |> Some }
    else
        { Remaining = f; Result = None }

let pMetadataBlockVorbisComment (f: ReadOnlySpan<byte>) (length: int) =
    if length < (pown 2 24) - 1 then
        let res = Vorbis.pVorbisCommentHeader f length

        Parse.map
            (fun (x: VorbisCommentHeader) ->
                { VendorString = x.VendorString
                  UserComments = x.UserComments })
            res
    else
        { Remaining = f; Result = None }

let pCueSheetTrackIndex (f: ReadOnlySpan<byte>) (length: int) =
    let o = BinaryPrimitives.ReadUInt64BigEndian(f.Slice(0, 8))

    let ipna = [| 0uy; f[8] |]
    let ipn = BinaryPrimitives.ReadUInt16BigEndian(ipna)

    if f.Slice(9, 3 * 8).IndexOfAnyExcept(0uy) = -1 then
        { Remaining = f.Slice(length)
          Result = Some { Offset = o; IndexPoint = ipn } }
    else
        { Remaining = f; Result = None }

let pCueSheetTrack (f: ReadOnlySpan<byte>) (length: int) =
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
        { Remaining = f; Result = None }
    else
        let ia = [| 0uy; f[209] |]
        let i = BinaryPrimitives.ReadUInt16BigEndian(ia)

        // TODO
        let ctis = if t = 170us || t = 255us then List.empty else List.empty

        { Remaining = f.Slice(length)
          Result =
            { Offset = o
              Number = t
              Isrc = isrc
              IsAudio = a
              PreEmphasis = p
              IndexPoints = i
              TrackIndexPoints = ctis }
            |> Some }

let pMetadataBlockCueSheet (f: ReadOnlySpan<byte>) (length: int) = { Remaining = f; Result = None }

let pMetadataBlockData (f: ReadOnlySpan<byte>) (length: int) =
    function
    | BlockType.StreamInfo -> Parse.map StreamInfo (pMetadataBlockStreamInfo f length)
    | BlockType.Padding -> Parse.map Padding (pMetadataBlockPadding f length)
    | BlockType.SeekTable -> Parse.map SeekTable (pMetadataBlockSeekTable f length)
    | BlockType.VorbisComment -> Parse.map VorbisComment (pMetadataBlockVorbisComment f length)
    | _ -> { Remaining = f; Result = None }
