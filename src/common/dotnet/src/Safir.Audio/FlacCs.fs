module Safir.Audio.FlacCs

let ReadMagic f = Flac.readMagic f

let ReadMetadataBlockHeader f = Flac.readMetadataBlockHeader f

let ReadMetadataBlockStreamInfo f =
    Flac.readMetadataBlockStreamInfo f |> MetadataBlockStreamInfoCs

let ReadMetadataBlockPadding f l =
    Flac.readMetadataBlockPadding f l |> MetadataBlockPaddingCs

let ReadMetadataBlockApplication f l =
    Flac.readMetadataBlockApplication f l |> MetadataBlockApplicationCs

let ReadMetadataBlockSeekTable f l =
    Flac.readMetadataBlockSeekTable f l |> MetadataBlockSeekTableCs

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
