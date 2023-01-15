module Safir.Audio.FlacCs

let ParseMagic f =
    Flac.pMagic f |> Option.defaultValue null

let ParseMetadataBlockHeader f =
    Flac.pMetadataBlockHeader f |> Option.get

let ParseMetadataBlockStreamInfo f =
    Flac.pMetadataBlockStreamInfo f
    |> Option.map MetadataBlockStreamInfoCs
    |> Option.get

let ParseMetadataBlockPadding f l =
    Flac.pMetadataBlockPadding f l
    |> Option.map MetadataBlockPaddingCs
    |> Option.get

let ParseMetadataBlockApplication f l =
    Flac.pMetadataBlockApplication f l
    |> Option.map MetadataBlockApplicationCs
    |> Option.get

let ParseMetadataBlockSeekTable f l =
    Flac.pMetadataBlockSeekTable f l
    |> Option.map MetadataBlockSeekTableCs
    |> Option.get

let ParseMetadataBlockVorbisComment f l =
    Flac.pMetadataBlockVorbisComment f l
    |> Option.map MetadataBlockVorbisCommentCs
    |> Option.get

let ParseFlacStream f =
    Flac.pFlacStream f |> Option.map FlacStreamCs |> Option.get
