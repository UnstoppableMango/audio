namespace Safir.Audio

type MetadataBlockPaddingCs = { Length: int }

type SeekPointCs =
    { IsPlaceHolder: bool
      SampleNumber: int64
      StreamOffset: int64
      FrameSamples: int }

type MetadataBlockSeekTableCs = { Points: SeekPointCs seq }

type MetadataBlockVorbisCommentCs =
    { Vendor: string
      UserComments: VorbisCommentCs seq }

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
