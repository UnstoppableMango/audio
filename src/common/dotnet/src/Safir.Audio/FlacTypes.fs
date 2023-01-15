namespace Safir.Audio

type BlockType =
    | StreamInfo = 0
    | Padding = 1
    | Application = 2
    | SeekTable = 3
    | VorbisComment = 4
    | CueSheet = 5
    | Picture = 6
    | Invalid = 127

type MetadataBlockHeader =
    { LastBlock: bool
      BlockType: BlockType
      Length: int }

type MetadataBlockStreamInfo =
    { MinBlockSize: uint16
      MaxBlockSize: uint16
      MinFrameSize: uint
      MaxFrameSize: uint
      SampleRate: uint
      Channels: uint16
      BitsPerSample: uint16
      TotalSamples: uint64
      Md5Signature: string }

type MetadataBlockPadding =
    | MetadataBlockPadding of int

type MetadataBlockApplication =
    { ApplicationId: int
      ApplicationData: byte [] }

// TODO: How to define placeholder seek points?
type SeekPoint =
    { SampleNumber: uint64
      StreamOffset: uint64
      FrameSamples: uint16 }

type MetadataBlockSeekTable =
    | MetadataBlockSeekTable of SeekPoint list

type MetadataBlockVorbisComment =
    { VendorString: string
      UserComments: VorbisComment list }

type CueSheetTrackIndex = { Offset: uint64; IndexPoint: uint16 }

type CueSheetTrack =
    { Offset: uint64
      Number: uint16
      Isrc: string option
      IsAudio: bool
      PreEmphasis: bool
      IndexPoints: uint16
      TrackIndexPoints: CueSheetTrackIndex list }

type MetadataBlockCueSheet =
    { CatalogNumber: string
      LeadInSamples: uint64
      IsCompactDisc: bool
      TotalTracks: uint16
      Tracks: CueSheetTrack list }

type MetadataBlockData =
    | StreamInfo of MetadataBlockStreamInfo
    | Padding of MetadataBlockPadding
    | Application of MetadataBlockApplication
    | SeekTable of MetadataBlockSeekTable
    | VorbisComment of MetadataBlockVorbisComment

type MetadataBlock =
    { Header: MetadataBlockHeader
      Data: MetadataBlockData }

type FlacStream = { Metadata: MetadataBlock list }
