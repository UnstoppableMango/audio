namespace Safir.Audio.Flac

open System.Runtime.CompilerServices
open Safir.Audio
open Safir.Audio.Vorbis

type StreamPosition =
    | Start = 0
    | Marker = 1
    | LastMetadataBlockFlag = 2
    | MetadataBlockType = 3
    | DataBlockLength = 4
    | MinimumBlockSize = 5
    | MaximumBlockSize = 6
    | MinimumFrameSize = 7
    | MaximumFrameSize = 8
    | StreamInfoSampleRate = 9
    | NumberOfChannels = 10
    | BitsPerSample = 11
    | TotalSamples = 12
    | Md5Signature = 13
    | Padding = 14
    | ApplicationId = 15
    | ApplicationData = 16
    | SeekPointSampleNumber = 17
    | SeekPointOffset = 18
    | NumberOfSamples = 19
    | VendorLength = 20
    | VendorString = 21
    | UserCommentListLength = 22
    | UserCommentLength = 23
    | UserComment = 24
    | MediaCatalogNumber = 25
    | NumberOfLeadInSamples = 26
    | IsCueSheetCompactDisc = 27
    | CueSheetReserved = 28
    | NumberOfTracks = 29
    | TrackOffset = 30
    | TrackNumber = 31
    | TrackIsrc = 32
    | TrackType = 33
    | PreEmphasis = 34
    | TrackReserved = 35
    | NumberOfTrackIndexPoints = 36
    | TrackIndexOffset = 37
    | IndexPointNumber = 38
    | TrackIndexReserved = 39
    | PictureType = 40
    | MimeTypeLength = 41
    | MimeType = 42
    | PictureDescriptionLength = 43
    | PictureDescription = 44
    | PictureWidth = 45
    | PictureHeight = 46
    | PictureColorDepth = 47
    | PictureNumberOfColors = 48
    | PictureDataLength = 49
    | PictureData = 50
    | End = 420

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
type FlacStreamState =
    { BlockLength: ValueOption<uint32>
      BlockType: ValueOption<BlockType>
      LastMetadataBlock: ValueOption<bool>
      SeekPointCount: ValueOption<uint32>
      SeekPointOffset: ValueOption<uint32>
      UserCommentCount: ValueOption<uint32>
      UserCommentOffset: ValueOption<uint32>
      CueSheetTrackCount: ValueOption<int>
      CueSheetTrackOffset: ValueOption<int>
      CueSheetTrackIndexCount: ValueOption<int>
      CueSheetTrackIndexOffset: ValueOption<int>
      Position: StreamPosition }

    static member Empty =
        { BlockLength = ValueNone
          BlockType = ValueNone
          LastMetadataBlock = ValueNone
          SeekPointCount = ValueNone
          SeekPointOffset = ValueNone
          UserCommentCount = ValueNone
          UserCommentOffset = ValueNone
          CueSheetTrackCount = ValueNone
          CueSheetTrackOffset = ValueNone
          CueSheetTrackIndexCount = ValueNone
          CueSheetTrackIndexOffset = ValueNone
          Position = StreamPosition.Start }

    static member StreamInfoHeader = { FlacStreamState.Empty with Position = StreamPosition.Marker }

    static member After(position: StreamPosition) =
        match position with // TODO: Throw on positions that require more state
        | StreamPosition.DataBlockLength -> readerEx "More state is required. Use FlacStreamState.AfterBlockHeader"
        | StreamPosition.Md5Signature
        | StreamPosition.NumberOfSamples
        | StreamPosition.UserComment
        | StreamPosition.PictureData -> readerEx "More state is required. Use FlacStreamState.AfterBlockData"
        | _ -> { FlacStreamState.Empty with Position = position }

    static member AfterBlockHeader(lastBlock: bool, length: uint32, blockType: BlockType, state: FlacStreamState) =
        { state with
            Position = StreamPosition.DataBlockLength
            LastMetadataBlock = ValueSome lastBlock
            BlockLength = ValueSome length
            BlockType = ValueSome blockType }

    static member AfterBlockHeader(lastBlock: bool, length: uint32, blockType: BlockType) =
        FlacStreamState.AfterBlockHeader(lastBlock, length, blockType, FlacStreamState.Empty)

    static member AfterBlockData(lastBlock: bool, state: FlacStreamState) =
        { state with LastMetadataBlock = ValueSome lastBlock }

    static member AfterBlockData(lastBlock: bool) =
        let state =
            { FlacStreamState.Empty with
                Position = StreamPosition.PictureData // TODO: More generic position
                SeekPointCount = ValueSome 0u // TODO: Will zero values here bite us?
                SeekPointOffset = ValueSome 0u
                UserCommentCount = ValueSome 0u
                UserCommentOffset = ValueSome 0u
                LastMetadataBlock = ValueSome lastBlock }

        FlacStreamState.AfterBlockData(lastBlock, state)

    static member AfterBlockData() = FlacStreamState.AfterBlockData(false)

    static member AfterLastBlock() = FlacStreamState.AfterBlockData(true)

type MetadataBlockHeader = { BlockType: BlockType }

type MetadataBlockStreamInfo =
    { MinBlockSize: int
      MaxBlockSize: int
      MinFrameSize: int
      MaxFrameSize: int
      SampleRate: int
      Channels: int
      BitsPerSample: int
      TotalSamples: int64
      Md5Signature: string }

type MetadataBlockPadding = MetadataBlockPadding of int

type MetadataBlockApplication =
    { ApplicationId: int
      ApplicationData: byte array }

type SeekPoint =
    | Placeholder
    | SeekPoint of
        {| SampleNumber: int64
           StreamOffset: int64
           FrameSamples: int |}

type MetadataBlockSeekTable = { Points: SeekPoint list }

type MetadataBlockVorbisComment = MetadataBlockVorbisComment of VorbisCommentHeader

type CueSheetTrackIndex = { Offset: int64; IndexPoint: int }

type CueSheetTrack =
    { Offset: int64
      Number: int16
      Isrc: string option
      IsAudio: bool
      PreEmphasis: bool
      IndexPoints: CueSheetTrackIndex list }

// TODO: Discriminate between CD-DA cue-sheets
type MetadataBlockCueSheet =
    { CatalogNumber: string
      LeadInSamples: int64
      IsCompactDisc: bool
      TotalTracks: int
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
      MimeType: string
      Description: string
      Width: int
      Height: int
      Depth: int
      Colors: int
      DataLength: int
      Data: byte array }

type MetadataBlockData =
    | StreamInfo of MetadataBlockStreamInfo
    | Padding of MetadataBlockPadding
    | Application of MetadataBlockApplication
    | SeekTable of MetadataBlockSeekTable
    | VorbisComment of MetadataBlockVorbisComment
    | CueSheet of MetadataBlockCueSheet
    | Picture of MetadataBlockPicture
    | Skipped

type MetadataBlock =
    { Header: MetadataBlockHeader
      Data: MetadataBlockData }

type FlacStream = { Metadata: MetadataBlock list }
