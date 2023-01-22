module Safir.Audio.FlacCs

let ReadMagic f = Flac.readMagic f

let ReadMetadataBlockHeader f = Flac.readMetadataBlockHeader f

let ReadMetadataBlockStreamInfo f =
    let result = Flac.readMetadataBlockStreamInfo f
    MetadataBlockStreamInfoValueCs(result)

let ReadMetadataBlockPadding f l =
    let result = Flac.readMetadataBlockPadding f l
    MetadataBlockPaddingValueCs(result)

let ReadMetadataBlockApplication f l =
    let result = Flac.readMetadataBlockApplication f l
    MetadataBlockApplicationValueCs(result)

let ReadMetadataBlockSeekTable f l =
    let result = Flac.readMetadataBlockSeekTable f l
    MetadataBlockSeekTableValueCs(result)

let ReadMetadataBlockVorbisComment f l =
    let result = Flac.readMetadataBlockVorbisComment f l
    MetadataBlockVorbisCommentValueCs(result)

let ReadMetadataBlockPicture f l =
    let result = Flac.readMetadataBlockPicture f l
    MetadataBlockPictureValueCs(result)

// let ReadFlacStream f = Flac.readFlacStream f |> FlacStreamCs
