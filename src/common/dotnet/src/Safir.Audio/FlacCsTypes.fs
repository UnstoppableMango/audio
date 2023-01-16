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
        | MetadataBlockSeekTable p -> p |> List.toSeq

type MetadataBlockVorbisCommentCs(vorbisComment: MetadataBlockVorbisComment) =
    inherit MetadataBlockDataCs()
    member this.VendorString = vorbisComment.VendorString

    member this.UserComments =
        vorbisComment.UserComments |> List.map VorbisCs.toCsComment |> List.toSeq

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
        | Skipped x -> MetadataBlockSkippedCs(x)

type FlacStreamCs(stream: FlacStream) =
    member this.Metadata = stream.Metadata |> List.map MetadataBlockCs |> List.toSeq
