namespace Safir.Audio

[<AbstractClass>]
type MetadataBlockDataCs() =
    class
    end

type MetadataBlockStreamInfoCs(streamInfo: MetadataBlockStreamInfo) =
    inherit MetadataBlockDataCs()
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
    inherit MetadataBlockDataCs()

    member this.Padding =
        match padding with
        | MetadataBlockPadding p -> p

type MetadataBlockApplicationCs(application: MetadataBlockApplication) =
    inherit MetadataBlockDataCs()
    member this.Id = application.ApplicationId
    member this.Data = application.ApplicationData

type MetadataBlockSeekTableCs(seekTable: MetadataBlockSeekTable) =
    inherit MetadataBlockDataCs()

    member this.SeekPoints =
        match seekTable with
        | MetadataBlockSeekTable p -> p |> Array.toSeq

type MetadataBlockVorbisCommentCs(vorbisComment: MetadataBlockVorbisComment) =
    inherit MetadataBlockDataCs()
    member this.VendorString = vorbisComment.VendorString

    member this.UserComments =
        vorbisComment.UserComments |> List.map VorbisCs.toCsComment |> List.toSeq

type MetadataBlockCueSheetCs(cueSheet: MetadataBlockCueSheet) =
    inherit MetadataBlockDataCs()

type MetadataBlockPictureCs(picture: MetadataBlockPicture) =
    inherit MetadataBlockDataCs()
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
    inherit MetadataBlockDataCs()
    member this.Block = block

type MetadataBlockCs(block: MetadataBlock) =
    member this.Header = block.Header

    member this.Data =
        match block.Data with
        | StreamInfo x -> MetadataBlockStreamInfoCs(x) :> MetadataBlockDataCs
        | Padding x -> MetadataBlockPaddingCs(x)
        | Application x -> MetadataBlockApplicationCs(x)
        | SeekTable x -> MetadataBlockSeekTableCs(x)
        | VorbisComment x -> MetadataBlockVorbisCommentCs(x)
        | CueSheet x -> MetadataBlockCueSheetCs(x)
        | Picture x -> MetadataBlockPictureCs(x)
        | Skipped x -> MetadataBlockSkippedCs(x)

type FlacStreamCs(stream: FlacStream) =
    member this.Metadata = stream.Metadata |> List.map MetadataBlockCs |> List.toSeq
