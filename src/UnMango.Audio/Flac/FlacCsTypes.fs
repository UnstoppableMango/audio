namespace UnMango.Audio.Flac

open UnMango.Audio.Vorbis

type SeekPointCs =
    { IsPlaceHolder: bool
      SampleNumber: int64
      StreamOffset: int64
      FrameSamples: int }

type CueSheetTrackCs
    (offset: int64, number: int16, isrc: string, isAudio: bool, preEmphasis: bool, indexPoints: CueSheetTrackIndex seq)
    =
    member this.Offset = offset
    member this.Number = number
    member this.Isrc = isrc
    member this.IsAudio = isAudio
    member this.PreEmphasis = preEmphasis
    member this.IndexPoints = indexPoints

[<AbstractClass>]
type MetadataBlockDataCs() =
    class
    end

type MetadataBlockStreamInfoCs
    (
        minBlockSize: int,
        maxBlockSize: int,
        minFrameSize: int,
        maxFrameSize: int,
        sampleRate: int,
        channels: int,
        bitsPerSample: int,
        totalSamples: int64,
        md5Signature: string
    ) =
    inherit MetadataBlockDataCs()
    member this.MinBlockSize = minBlockSize
    member this.MaxBlockSize = maxBlockSize
    member this.MinFrameSize = minFrameSize
    member this.MaxFrameSize = maxFrameSize
    member this.SampleRate = sampleRate
    member this.Channels = channels
    member this.BitsPerSample = bitsPerSample
    member this.TotalSamples = totalSamples
    member this.Md5Signature = md5Signature

type MetadataBlockPaddingCs(length: int) =
    inherit MetadataBlockDataCs()
    member this.Length = length

type MetadataBlockApplicationCs(applicationId: int, applicationData: byte array) =
    inherit MetadataBlockDataCs()
    member this.ApplicationId = applicationId
    member this.ApplicationData = applicationData

type MetadataBlockSeekTableCs(points: SeekPointCs seq) =
    inherit MetadataBlockDataCs()
    member this.Points = points

type MetadataBlockVorbisCommentCs(vendor: string, userComments: VorbisCommentCs seq) =
    inherit MetadataBlockDataCs()
    member this.Vendor = vendor
    member this.UserComments = userComments

type MetadataBlockCueSheetCs
    (catalogNumber: string, leadInSamples: int64, isCompactDisc: bool, totalTracks: int, tracks: CueSheetTrackCs seq) =
    inherit MetadataBlockDataCs()
    member this.CatalogNumber = catalogNumber
    member this.LeadInSamples = leadInSamples
    member this.IsCompactDisc = isCompactDisc
    member this.TotalTracks = totalTracks
    member this.Tracks = tracks

type MetadataBlockPictureCs
    (
        pictureType: PictureType,
        mimeType: string,
        description: string,
        width: int,
        height: int,
        depth: int,
        colors: int,
        dataLength: int,
        data: byte array
    ) =
    inherit MetadataBlockDataCs()
    member this.Type = pictureType
    member this.MimeType = mimeType
    member this.Description = description
    member this.Width = width
    member this.Height = height
    member this.Depth = depth
    member this.Colors = colors
    member this.DataLength = dataLength
    member this.Data = data

type MetadataBlockSkippedCs(data: byte array) =
    inherit MetadataBlockDataCs()
    member this.Data = data

type MetadataBlockCs(header: MetadataBlockHeader, data: MetadataBlockDataCs) =
    member this.Header = header
    member this.Data = data

type FlacStreamCs(metadata: MetadataBlockCs seq) =
    member this.Metadata = metadata
