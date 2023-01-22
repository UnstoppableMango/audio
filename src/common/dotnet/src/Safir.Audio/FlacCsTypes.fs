namespace Safir.Audio

open System.Runtime.CompilerServices

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockStreamInfoCs(streamInfo: MetadataBlockStreamInfo) =
    member this.MinBlockSize = streamInfo.MinBlockSize
    member this.MaxBlockSize = streamInfo.MaxBlockSize
    member this.MinFrameSize = streamInfo.MinFrameSize
    member this.MaxFrameSize = streamInfo.MaxFrameSize
    member this.SampleRate = streamInfo.SampleRate
    member this.Channels = streamInfo.Channels
    member this.BitsPerSample = streamInfo.BitsPerSample
    member this.TotalSamples = streamInfo.TotalSamples
    member this.Md5Signature = streamInfo.Md5Signature

type MetadataBlockPaddingCs(padding: MetadataBlockPadding) =
    member this.Padding =
        match padding with
        | MetadataBlockPadding p -> p

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockApplicationCs(application: MetadataBlockApplication) =
    member this.Id = application.ApplicationId
    member this.Data = application.ApplicationData

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockSeekTableCs(seekTable: MetadataBlockSeekTable) =
    member this.Count = seekTable.Count
    member this.SeekPoints = seekTable.SeekPoints

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockVorbisCommentCs(vorbisComment: MetadataBlockVorbisComment) =
    member this.VendorLength = vorbisComment.VendorLength
    member this.VendorString = vorbisComment.VendorString
    member this.UserCommentListLength = vorbisComment.UserCommentListLength
    member this.UserComments = vorbisComment.UserComments

type MetadataBlockCueSheetCs(cueSheet: MetadataBlockCueSheet) = class end

[<Struct; IsReadOnly; IsByRefLike>]
type MetadataBlockPictureCs(picture: MetadataBlockPicture) =
    member this.Type = picture.Type
    member this.MimeLength = picture.MimeLength
    member this.MimeType = picture.MimeType
    member this.DescriptionLength = picture.DescriptionLength
    member this.Description = picture.Description
    member this.Width = picture.Width
    member this.Height = picture.Height
    member this.Depth = picture.Depth
    member this.Colors = picture.Colors
    member this.DataLength = picture.DataLength
    member this.Data = picture.Data

type MetadataBlockSkippedCs(block: byte []) =
    member this.Block = block

// type MetadataBlockCs(block: MetadataBlock) =
//     member this.Header = block.Header
//
//     member this.Data =
//         match block.Data with
//         | StreamInfo x -> MetadataBlockStreamInfoCs(x) :> MetadataBlockDataCs
//         | Padding x -> MetadataBlockPaddingCs(x)
//         | Application x -> MetadataBlockApplicationCs(x)
//         | SeekTable x -> MetadataBlockSeekTableCs(x)
//         | VorbisComment x -> MetadataBlockVorbisCommentCs(x)
//         | CueSheet x -> MetadataBlockCueSheetCs(x)
//         | Picture x -> MetadataBlockPictureCs(x)
//         | Skipped x -> MetadataBlockSkippedCs(x.ToArray())
//
// type FlacStreamCs(stream: FlacStream) =
//     member this.Metadata = stream.Metadata |> List.toSeq
