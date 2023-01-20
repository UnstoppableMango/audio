namespace Safir.Audio

open System.Runtime.CompilerServices

type BlockType =
    | StreamInfo = 0
    | Padding = 1
    | Application = 2
    | SeekTable = 3
    | VorbisComment = 4
    | CueSheet = 5
    | Picture = 6
    | Invalid = 127

[<Struct; IsReadOnly>]
type MetadataBlockHeader =
    { LastBlock: bool
      BlockType: BlockType
      Length: int }

[<Struct; IsReadOnly>]
type MetadataBlockStreamInfo =
    { MinBlockSize: uint16
      MaxBlockSize: uint16
      MinFrameSize: uint
      MaxFrameSize: uint
      SampleRate: uint
      Channels: uint16
      BitsPerSample: uint16
      TotalSamples: uint64
      Md5Signature: byte [] }

[<Struct; IsReadOnly>]
type MetadataBlockPadding = MetadataBlockPadding of int

[<Struct; IsReadOnly>]
type MetadataBlockApplication =
    { ApplicationId: int
      ApplicationData: byte [] }

// TODO: How to define placeholder seek points?
[<Struct; IsReadOnly>]
type SeekPoint =
    { SampleNumber: uint64
      StreamOffset: uint64
      FrameSamples: uint16 }

[<Struct; IsReadOnly>]
type MetadataBlockSeekTable = MetadataBlockSeekTable of SeekPoint array

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

type PictureType =
    | Other = 0
    | StandardFileIcon = 1
    | OtherFileIcon = 2
    | FrontCover = 3
    | BackCover = 4
    | LeafletPage = 5
    | Media = 6
    | LeadArtist = 7
    | Artist = 8
    | Conductor = 9
    | Band = 10
    | Composer = 11
    | Lyricist = 12
    | RecordingLocation = 13
    | DuringRecording = 14
    | DuringPerformance = 15
    | VideoScreenCapture = 16
    | ABrightColouredFish = 17
    | Illustration = 18
    | ArtistLogoType = 19
    | PublisherLogoType = 20

type MetadataBlockPicture =
    { Type: PictureType
      MimeLength: uint32
      MimeType: string
      DescriptionLength: uint32
      Description: string
      Width: uint32
      Height: uint32
      Depth: uint32
      Colors: uint32
      DataLength: uint32
      Data: byte [] }

type MetadataBlockData =
    | StreamInfo of MetadataBlockStreamInfo
    | Padding of MetadataBlockPadding
    | Application of MetadataBlockApplication
    | SeekTable of MetadataBlockSeekTable
    | VorbisComment of MetadataBlockVorbisComment
    | CueSheet of MetadataBlockCueSheet
    | Picture of MetadataBlockPicture
    | Skipped of byte []

type MetadataBlock =
    { Header: MetadataBlockHeader
      Data: MetadataBlockData }

type FlacStream = { Metadata: MetadataBlock list }
