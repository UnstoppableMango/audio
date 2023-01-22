module Safir.Audio.FlacCs

let ReadMagic f = Flac.readMagic f

let ReadMetadataBlockHeader f = Flac.readMetadataBlockHeader f

let ReadMetadataBlockStreamInfo f =
    Flac.readMetadataBlockStreamInfo f |> MetadataBlockStreamInfoValueCs

let ReadMetadataBlockPadding f l =
    Flac.readMetadataBlockPadding f l |> MetadataBlockPaddingValueCs

let ReadMetadataBlockApplication f l =
    Flac.readMetadataBlockApplication f l |> MetadataBlockApplicationValueCs

let ReadMetadataBlockSeekTable f l =
    Flac.readMetadataBlockSeekTable f l |> MetadataBlockSeekTableValueCs

let ReadMetadataBlockVorbisComment f l =
    Flac.readMetadataBlockVorbisComment f l |> MetadataBlockVorbisCommentValueCs

let ReadMetadataBlockPicture f l =
    Flac.readMetadataBlockPicture f l |> MetadataBlockPictureValueCs

let ReadFlacStream f = Flac.readFlacStream f |> FlacStreamCs
