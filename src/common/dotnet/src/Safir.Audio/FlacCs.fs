module Safir.Audio.FlacCs

let ReadMagic f =
    Flac.readMagic f |> Option.defaultValue null

let ReadMetadataBlockHeader f =
    Flac.readMetadataBlockHeader f |> Option.get

let ReadMetadataBlockStreamInfo f =
    Flac.readMetadataBlockStreamInfo f
    |> Option.map MetadataBlockStreamInfoCs
    |> Option.get

let ReadMetadataBlockPadding f l =
    Flac.readMetadataBlockPadding f l
    |> Option.map MetadataBlockPaddingCs
    |> Option.get

let ReadMetadataBlockApplication f l =
    Flac.readMetadataBlockApplication f l
    |> Option.map MetadataBlockApplicationCs
    |> Option.get

let ReadMetadataBlockSeekTable f l =
    Flac.readMetadataBlockSeekTable f l
    |> Option.map MetadataBlockSeekTableCs
    |> Option.get

let ReadMetadataBlockVorbisComment f l =
    Flac.readMetadataBlockVorbisComment f l
    |> Option.map MetadataBlockVorbisCommentCs
    |> Option.get

let ReadMetadataBlockPicture f l =
    Flac.readMetadataBlockPicture f l
    |> Option.map MetadataBlockPictureCs
    |> Option.get

let ReadFlacStream f =
    Flac.readFlacStream f |> Option.map FlacStreamCs |> Option.get
