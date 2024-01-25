module UnMango.Audio.Flac.FlacCs

open UnMango.Audio
open UnMango.Audio.Vorbis

let ToMetadataBlockStreamInfoCs block =
    MetadataBlockStreamInfoCs(
        block.MinBlockSize,
        block.MaxBlockSize,
        block.MinFrameSize,
        block.MaxFrameSize,
        block.SampleRate,
        block.Channels,
        block.BitsPerSample,
        block.TotalSamples,
        block.Md5Signature
    )

let ToMetadataBlockPaddingCs block =
    let (MetadataBlockPadding length) = block
    MetadataBlockPaddingCs(length)

let ToMetadataBlockApplicationCs block =
    MetadataBlockApplicationCs(block.ApplicationId, block.ApplicationData)

let ToMetadataBlockSeekTableCs block =
    let points =
        block.Points
        |> List.map (function
            | Placeholder ->
                { IsPlaceHolder = true
                  FrameSamples = 0
                  SampleNumber = 0
                  StreamOffset = 0 }
            | SeekPoint x ->
                { IsPlaceHolder = false
                  FrameSamples = x.FrameSamples
                  SampleNumber = x.SampleNumber
                  StreamOffset = x.StreamOffset })
        |> List.toSeq

    MetadataBlockSeekTableCs(points)

let ToMetadataBlockVorbisCommentCs block =
    let (MetadataBlockVorbisComment result) = block

    let comments =
        result.UserComments
        |> List.map VorbisCs.toComment
        |> List.toSeq

    MetadataBlockVorbisCommentCs(result.Vendor, comments)

let ToCueSheetTrackCs track =
    let isrc =
        match track.Isrc with
        | Some x -> x
        | None -> null

    CueSheetTrackCs(track.Offset, track.Number, isrc, track.IsAudio, track.PreEmphasis, track.IndexPoints)

let ToMetadataBlockCueSheetCs block =
    MetadataBlockCueSheetCs(
        block.CatalogNumber,
        block.LeadInSamples,
        block.IsCompactDisc,
        block.TotalTracks,
        block.Tracks
        |> List.map ToCueSheetTrackCs
    )

let ToMetadataBlockPictureCs block =
    MetadataBlockPictureCs(
        block.Type,
        block.MimeType,
        block.Description,
        block.Width,
        block.Height,
        block.Depth,
        block.Colors,
        block.DataLength,
        block.Data
    )

let ToMetadataBlockDataCs =
    function
    | StreamInfo x -> ToMetadataBlockStreamInfoCs x :> MetadataBlockDataCs
    | Padding x -> ToMetadataBlockPaddingCs x
    | Application x -> ToMetadataBlockApplicationCs x
    | SeekTable x -> ToMetadataBlockSeekTableCs x
    | VorbisComment x -> ToMetadataBlockVorbisCommentCs x
    | CueSheet x -> ToMetadataBlockCueSheetCs x
    | Picture x -> ToMetadataBlockPictureCs x
    | Skipped x -> MetadataBlockSkippedCs(x)

let ToMetadataBlockCs block =
    MetadataBlockCs(
        block.Header,
        block.Data
        |> ToMetadataBlockDataCs
    )

let ToFlacStreamCs stream =
    FlacStreamCs(
        stream.Metadata
        |> List.map ToMetadataBlockCs
    )

let ReadMagic (r: byref<FlacStreamReader>) = Flac.readMagic &r

let ReadMetadataBlockHeader (r: byref<FlacStreamReader>) = Flac.readMetadataBlockHeader &r

let ReadMetadataBlockStreamInfo (r: byref<FlacStreamReader>) =
    Flac.readMetadataBlockStreamInfo &r
    |> ToMetadataBlockStreamInfoCs

let ReadMetadataBlockPadding (r: byref<FlacStreamReader>) =
    Flac.readMetadataBlockPadding &r
    |> ToMetadataBlockPaddingCs

let ReadMetadataBlockApplication (r: byref<FlacStreamReader>) =
    Flac.readMetadataBlockApplication &r
    |> ToMetadataBlockApplicationCs

let ReadMetadataBlockSeekTable (r: byref<FlacStreamReader>) =
    Flac.readMetadataBlockSeekTable &r
    |> ToMetadataBlockSeekTableCs

let ReadMetadataBlockVorbisComment (r: byref<FlacStreamReader>) =
    Flac.readMetadataBlockVorbisComment &r
    |> ToMetadataBlockVorbisCommentCs

let ReadMetadataBlockCueSheet (r: byref<FlacStreamReader>) =
    Flac.readMetadataBlockCueSheet &r
    |> ToMetadataBlockCueSheetCs

let ReadMetadataBlockPicture (r: byref<FlacStreamReader>) =
    Flac.readMetadataBlockPicture &r
    |> ToMetadataBlockPictureCs

let ReadMetadataBlockData (r: byref<FlacStreamReader>) (blockType: BlockType) =
    Flac.readMetadataBlockData &r blockType
    |> ToMetadataBlockDataCs

let ReadMetadataBlock (r: byref<FlacStreamReader>) =
    Flac.readMetadataBlock &r
    |> ToMetadataBlockCs

let ReadStream (r: byref<FlacStreamReader>) =
    Flac.readStream &r
    |> ToFlacStreamCs
