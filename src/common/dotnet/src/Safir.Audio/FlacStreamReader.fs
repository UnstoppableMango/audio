namespace Safir.Audio

open System
open System.Buffers.Binary
open System.Runtime.CompilerServices

// TODO: There are plenty of copies in here that while they don't
//       allocate are likely still hurting performance quite a bit
// TODO: Learn to write performant low-level code
// TODO: Better delineate between "Read" and "Skip" functions

[<Struct; IsByRefLike>]
type FlacStreamReader(buffer: ReadOnlySpan<byte>) =
    [<DefaultValue>]
    val mutable private _blockLength: ValueOption<uint32>

    [<DefaultValue>]
    val mutable private _blockType: ValueOption<BlockType>

    [<DefaultValue>]
    val mutable private _lastMetadataBlock: ValueOption<bool>

    [<DefaultValue>]
    val mutable private _numberOfSeekPoints: ValueOption<uint32>

    [<DefaultValue>]
    val mutable private _seekTableIndex: ValueOption<uint32>

    [<DefaultValue>]
    val mutable private _numberOfUserComments: ValueOption<uint32>

    [<DefaultValue>]
    val mutable private _userCommentIndex: ValueOption<uint32>

    [<DefaultValue>]
    val mutable private _numberOfCueSheetTracks: ValueOption<int>

    [<DefaultValue>]
    val mutable private _cueSheetTrackIndex: ValueOption<int>

    [<DefaultValue>]
    val mutable private _numberOfCueSheetTrackIndexPoints: ValueOption<int>

    // I hate naming things
    [<DefaultValue>]
    val mutable private _cueSheetTrackIndexPointIndex: ValueOption<int>

    [<DefaultValue>]
    val mutable private _position: StreamPosition

    [<DefaultValue>]
    val mutable private _consumed: int

    [<DefaultValue>]
    val mutable private _value: ReadOnlySpan<byte>

    member this.Position = this._position

    member this.Value = this._value

    member private this.Read(position: StreamPosition, length: int) =
        this._value <- buffer.Slice(this._consumed, length)
        this._consumed <- this._consumed + length
        this._position <- position

    member private this.StartMetadataBlockData() =
        match this._blockType with
        | ValueNone -> readerEx "Unknown block type"
        | ValueSome blockType ->
            match blockType with
            | BlockType.StreamInfo -> this.ReadMinimumBlockSize()
            | BlockType.Padding -> this.ReadMetadataBlockPadding()
            | BlockType.Application -> this.Read(StreamPosition.ApplicationId, 4)
            | BlockType.SeekTable -> this.StartSeekPoint()
            | BlockType.VorbisComment -> this.Read(StreamPosition.VendorLength, 4)
            | BlockType.CueSheet -> this.ReadCueSheetCatalogNumber()
            | BlockType.Picture -> this.Read(StreamPosition.PictureType, 4)
            | t when int t < 127 -> readerEx "TODO"
            | _ -> readerEx "Invalid block type"

    member private this.EndMetadataBlockData() =
        match this._lastMetadataBlock with
        | ValueNone -> readerEx "Unknown metadata position"
        | ValueSome false -> this.ReadLastMetadataBlockFlag()
        | ValueSome true -> () // Done. We don't currently support anything past metadata

    member private this.StartSeekPoint() =
        match this._blockLength with
        | ValueNone -> readerEx "Unknown block length"
        | ValueSome length when length % 18u <> 0u -> readerEx "Invalid block length"
        | ValueSome length -> this._numberOfSeekPoints <- ValueSome(length / 18u)

        match this._seekTableIndex with
        | ValueNone -> this._seekTableIndex <- ValueSome 0u
        | ValueSome i -> this._seekTableIndex <- ValueSome(i + 1u)

        this.Read(StreamPosition.SeekPointSampleNumber, 8)

    member private this.EndSeekPoint() =
        match this._numberOfSeekPoints, this._seekTableIndex with
        | ValueSome n, ValueSome i when i < n - 1u -> this.StartSeekPoint()
        | ValueSome n, ValueSome i when i = n - 1u -> this.EndMetadataBlockData()
        | _, _ -> readerEx "Invalid reader state"

    member private this.StartUserCommentList() =
        if this._value.Length < 4 then
            readerEx "Invalid reader state"

        let length = BinaryPrimitives.ReadUInt32LittleEndian(this._value)

        this._numberOfUserComments <- ValueSome length
        this._userCommentIndex <- ValueSome 0u

    member private this.EndUserComment() =
        match this._numberOfUserComments, this._userCommentIndex with
        | ValueSome n, ValueSome i when i < n - 1u -> this.Read(StreamPosition.UserCommentLength, 4)
        | ValueSome n, ValueSome i when i = n - 1u -> this.EndMetadataBlockData()
        | _, _ -> readerEx "Invalid reader state"

    member private this.StartCueSheetTrack() =
        if this._value.Length < 1 then
            readerEx "Invalid reader state"

        let length = int this._value[0]

        this._numberOfCueSheetTracks <- ValueSome length

        match this._cueSheetTrackIndex with
        | ValueNone -> this._cueSheetTrackIndex <- ValueSome 0
        | ValueSome i -> this._cueSheetTrackIndex <- ValueSome(i + 1)

        this.ReadCueSheetTrackOffset()

    member private this.EndCueSheetTrack() =
        match this._numberOfCueSheetTracks, this._cueSheetTrackIndex with
        | ValueSome n, ValueSome i when i < n - 1 -> this.StartCueSheetTrack()
        | ValueSome n, ValueSome i when i = n - 1 -> this.EndMetadataBlockData()
        | _, _ -> readerEx "Invalid reader state"

    member private this.StartCueSheetTrackIndexPoint() =
        if this._value.Length < 1 then
            readerEx "Invalid reader state"

        let length = int this._value[0]

        this._numberOfCueSheetTrackIndexPoints <- ValueSome length

        match this._cueSheetTrackIndexPointIndex with
        | ValueNone -> this._cueSheetTrackIndexPointIndex <- ValueSome 0
        | ValueSome i -> this._cueSheetTrackIndexPointIndex <- ValueSome(i + 1)

        this.ReadCueSheetTrackIndexOffset()

    member private this.EndCueSheetTrackIndexPoint() =
        match this._numberOfCueSheetTrackIndexPoints, this._cueSheetTrackIndexPointIndex with
        | ValueSome n, ValueSome i when i < n - 1 -> this.StartCueSheetTrackIndexPoint()
        | ValueSome n, ValueSome i when i = n - 1 -> this.EndCueSheetTrack()
        | _, _ -> readerEx "Invalid reader state"

    member this.Read() =
        match this._position with
        | StreamPosition.Start -> this.Read(StreamPosition.Marker, 4)
        | StreamPosition.Marker -> this.ReadLastMetadataBlockFlag()
        | StreamPosition.LastMetadataBlockFlag -> this.ReadMetadataBlockType()
        | StreamPosition.MetadataBlockType -> this.ReadMetadataBlockLength()
        | StreamPosition.DataBlockLength -> this.StartMetadataBlockData()
        | StreamPosition.MinimumBlockSize -> this.Read(StreamPosition.MaximumBlockSize, 2)
        | StreamPosition.MaximumBlockSize -> this.Read(StreamPosition.MinimumFrameSize, 2)
        | StreamPosition.MinimumFrameSize -> this.Read(StreamPosition.MaximumFrameSize, 3)
        | StreamPosition.MaximumFrameSize -> this.ReadSampleRate()
        | StreamPosition.StreamInfoSampleRate -> this.ReadNumberOfChannels()
        | StreamPosition.NumberOfChannels -> this.ReadBitsPerSample()
        | StreamPosition.BitsPerSample -> this.ReadTotalSamples()
        | StreamPosition.TotalSamples -> this.Read(StreamPosition.Md5Signature, 16)
        | StreamPosition.Md5Signature -> this.EndMetadataBlockData()
        | StreamPosition.Padding -> this.EndMetadataBlockData()
        | StreamPosition.ApplicationId -> this.ReadApplicationData()
        | StreamPosition.ApplicationData -> this.EndMetadataBlockData()
        | StreamPosition.SeekPointSampleNumber -> this.Read(StreamPosition.SeekPointOffset, 8)
        | StreamPosition.SeekPointOffset -> this.Read(StreamPosition.NumberOfSamples, 2)
        | StreamPosition.NumberOfSamples -> this.EndSeekPoint()
        | StreamPosition.VendorLength -> this.ReadVendorString()
        | StreamPosition.VendorString -> this.Read(StreamPosition.UserCommentListLength, 4)
        | StreamPosition.UserCommentListLength -> this.StartUserCommentList()
        | StreamPosition.UserCommentLength -> this.ReadUserComment()
        | StreamPosition.UserComment -> this.EndUserComment()
        | StreamPosition.MediaCatalogNumber -> this.ReadCueSheetLeadInSamplesNumber()
        | StreamPosition.NumberOfLeadInSamples -> this.ReadIsCueSheetCompactDisc()
        | StreamPosition.IsCueSheetCompactDisc -> this.ReadCueSheetReserved()
        | StreamPosition.CueSheetReserved -> this.ReadCueSheetNumberOfTracks()
        | StreamPosition.NumberOfTracks -> this.StartCueSheetTrack()
        | StreamPosition.TrackOffset -> this.ReadCueSheetTrackNumber()
        | StreamPosition.TrackNumber -> this.Read(StreamPosition.TrackIsrc, 12)
        | StreamPosition.TrackIsrc -> this.ReadCueSheetTrackType()
        | StreamPosition.TrackType -> this.ReadCueSheetTrackPreEmphasis()
        | StreamPosition.PreEmphasis -> this.ReadCueSheetTrackReserved()
        | StreamPosition.TrackReserved -> this.ReadCueSheetNumberOfTrackIndexPoints()
        | StreamPosition.NumberOfTrackIndexPoints -> this.StartCueSheetTrackIndexPoint()
        | StreamPosition.TrackIndexOffset -> this.ReadCueSheetTrackIndexPointNumber()
        | StreamPosition.IndexPointNumber -> this.ReadCueSheetTrackIndexReserved()
        | StreamPosition.TrackIndexReserved -> this.EndCueSheetTrackIndexPoint()
        | StreamPosition.PictureType -> this.Read(StreamPosition.MimeTypeLength, 4)
        | StreamPosition.MimeTypeLength -> this.ReadMimeType()
        | StreamPosition.MimeType -> this.Read(StreamPosition.PictureDescriptionLength, 4)
        | StreamPosition.PictureDescriptionLength -> this.ReadPictureDescription()
        | StreamPosition.PictureDescription -> this.Read(StreamPosition.PictureWidth, 4)
        | StreamPosition.PictureWidth -> this.Read(StreamPosition.PictureHeight, 4)
        | StreamPosition.PictureHeight -> this.Read(StreamPosition.PictureColorDepth, 4)
        | StreamPosition.PictureColorDepth -> this.Read(StreamPosition.PictureNumberOfColors, 4)
        | StreamPosition.PictureNumberOfColors -> this.Read(StreamPosition.PictureDataLength, 4)
        | StreamPosition.PictureDataLength -> this.ReadPictureData()
        | StreamPosition.PictureData -> this.EndMetadataBlockData()
        | _ -> readerEx "Invalid stream position"

    member this.ReadMagic() =
        let local = buffer.Slice(this._consumed, 4)

        if not <| local.SequenceEqual("fLaC"B) then
            readerEx "Invalid stream marker"

        this._value <- local
        this._consumed <- this._consumed + 4

    // Magic is more fun, despite how illogical it is
    member this.ReadMarker() = this.ReadMagic()

    member this.ReadLastMetadataBlockFlag() =
        let local = buffer[this._consumed] >>> 7

        this._value <- ReadOnlySpan<byte>(&local)
        this._position <- StreamPosition.LastMetadataBlockFlag

    member this.ReadMetadataBlockType() =
        let local = buffer[this._consumed] &&& 0x7Fuy

        if local > 127uy then readerEx "Invalid metadata block type"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._position <- StreamPosition.MetadataBlockType

    member this.ReadMetadataBlockLength() =
        let local = buffer.Slice(this._consumed, 3)
        let length = readUInt32 local

        this._value <- local
        this._blockLength <- ValueSome length
        this._consumed <- this._consumed + 3
        this._position <- StreamPosition.DataBlockLength

    member this.ReadMinimumBlockSize() =
        let local = buffer.Slice(this._consumed, 2)
        let blockSize = BinaryPrimitives.ReadUInt16BigEndian(local)

        if blockSize <= 15us then readerEx "Invalid block size"

        this._value <- local
        this._consumed <- this._consumed + 2
        this._position <- StreamPosition.MinimumBlockSize

    member this.ReadSampleRate() =
        let local = buffer.Slice(this._consumed, 3)
        let sampleRate = (readUInt32 local) >>> 4

        if sampleRate = 0u || sampleRate > FlacConstants.MaxSampleRate then
            readerEx "Invalid sample rate"

        // TODO: How to represent this is offset by 4 bits?
        this._value <- local
        // We only fully consume the first two bytes
        this._consumed <- this._consumed + 2
        this._position <- StreamPosition.StreamInfoSampleRate

    member this.ReadNumberOfChannels() =
        let local = buffer[this._consumed] &&& 0x0Euy >>> 1
        this._value <- ReadOnlySpan<byte>(&local)
        this._position <- StreamPosition.NumberOfChannels

    member this.ReadBitsPerSample() =
        let i = this._consumed
        let local = buffer.Slice(this._consumed, 2)

        let a = uint16 (local[i] &&& 0x01uy) <<< 13
        let b = uint16 (local[i + 1]) >>> 4
        let bps = a + b + 1us

        if bps < 4us || bps > 32us then
            readerEx "Invalid bits per sample"

        this._value <- local
        // We only fully consume the first byte
        this._consumed <- i + 1
        this._position <- StreamPosition.BitsPerSample

    // TODO: How to represent this first byte requires a mask?
    member this.ReadTotalSamples() =
        this.Read(StreamPosition.TotalSamples, 5)

    member this.ReadMetadataBlockPadding() =
        match this._blockLength with
        | ValueNone -> readerEx "Unknown block length"
        | ValueSome length ->
            let l = int length

            if l % 8 <> 0 then
                readerEx "Padding length must be a multiple of 8"

            let local = buffer.Slice(this._consumed, l)

            if local.IndexOfAnyExcept(0uy) <> -1 then
                readerEx "Padding contains invalid bytes"

            this._value <- local
            this._consumed <- this._consumed + l
            this._position <- StreamPosition.Padding

    member this.ReadApplicationData() =
        match this._blockLength with
        | ValueNone -> readerEx "Unknown block length"
        | ValueSome length ->
            let l = int length - 4

            if l % 8 <> 0 then
                readerEx "Application data length must be a multiple of 8"

            this._value <- buffer.Slice(this._consumed, l)
            this._consumed <- this._consumed + l
            this._position <- StreamPosition.ApplicationData

    member this.ReadVendorString() =
        if this._value.Length < 4 then
            readerEx "Invalid reader state"

        let length = BinaryPrimitives.ReadUInt32LittleEndian(this._value) |> int
        let local = this._value.Slice(this._consumed, length)

        this._value <- local
        this._position <- StreamPosition.VendorString
        this._consumed <- this._consumed + length

    member this.ReadUserComment() =
        if this._value.Length < 4 then
            readerEx "Invalid reader state"

        let newIndex =
            match this._userCommentIndex with
            | ValueNone -> readerEx "Invalid reader state"
            | ValueSome i -> i + 1u

        let length = BinaryPrimitives.ReadUInt32LittleEndian(this._value) |> int
        let local = this._value.Slice(this._consumed, length)

        this._value <- local
        this._position <- StreamPosition.UserComment
        this._userCommentIndex <- ValueSome newIndex
        this._consumed <- this._consumed + length

    // TODO: Validate for CD-DA; offset % 588 = 0
    member this.ReadCueSheetTrackIndexOffset() =
        this.Read(StreamPosition.TrackIndexOffset, 8)

    member this.ReadCueSheetTrackIndexPointNumber() =
        let local = buffer[this._consumed]

        // TODO: Validate first index
        // TODO: Validate uniqueness

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._position <- StreamPosition.IndexPointNumber

    member this.ReadCueSheetTrackIndexReserved() =
        let local = buffer.Slice(this._consumed, 3)

        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        this._value <- local
        this._consumed <- this._consumed + 3
        this._position <- StreamPosition.TrackIndexReserved

    // TODO: Validate for CD-DA; offset % 588 = 0
    member this.ReadCueSheetTrackOffset() =
        this.Read(StreamPosition.TrackOffset, 8)

    member this.ReadCueSheetTrackNumber() =
        let local = buffer[this._consumed]

        // This may be a soft requirement...
        if local = 0uy then
            readerEx "Invalid cue sheet track number"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._position <- StreamPosition.TrackNumber

    member this.ReadCueSheetTrackType() =
        let local = buffer[this._consumed] &&& 0x80uy
        this._value <- ReadOnlySpan<byte>(&local)
        this._position <- StreamPosition.TrackType

    member this.ReadCueSheetTrackPreEmphasis() =
        let local = buffer[this._consumed] &&& 0x40uy
        this._value <- ReadOnlySpan<byte>(&local)
        this._position <- StreamPosition.PreEmphasis

    member this.ReadCueSheetTrackReserved() =
        let local = buffer.Slice(this._consumed, 14)

        // TODO: This doesn't account for the leading 6 bits
        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        // TODO: How to represent this is offset by two bits?
        this._value <- local
        this._consumed <- this._consumed + 14
        this._position <- StreamPosition.TrackReserved

    // TODO: Validate ASCII characters
    // TODO: Validate CD-DA 13 digit number + 115 NUL bytes
    member this.ReadCueSheetCatalogNumber() =
        this.Read(StreamPosition.MediaCatalogNumber, 128)

    // TODO: Do we need any validation here?
    member this.ReadCueSheetLeadInSamplesNumber() =
        this.Read(StreamPosition.NumberOfLeadInSamples, 8)

    member this.ReadIsCueSheetCompactDisc() =
        let local = buffer[this._consumed] >>> 7
        this._value <- ReadOnlySpan<byte>(&local)
        this._position <- StreamPosition.IsCueSheetCompactDisc

    member this.ReadCueSheetReserved() =
        let local = buffer.Slice(this._consumed, 259)

        // TODO: This doesn't account for the leading 7 bits
        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        // TODO: How to represent this is offset by one bit?
        this._value <- local
        this._consumed <- this._consumed + 259
        this._position <- StreamPosition.CueSheetReserved

    member this.ReadCueSheetNumberOfTracks() =
        let local = buffer[this._consumed]

        // TODO: Validate for CD-DA num < 100
        if local < 1uy then
            readerEx "Must have at least one cue sheet track"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._position <- StreamPosition.NumberOfTracks

    member this.ReadCueSheetNumberOfTrackIndexPoints() =
        let local = buffer[this._consumed]

        // TODO: Validate for CD-DA num < 100
        if local < 1uy then
            readerEx "Must have at least one cue sheet track index"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._position <- StreamPosition.NumberOfTrackIndexPoints

    member this.ReadMimeType() =
        if this._value.Length < 4 then
            readerEx "Invalid reader state"

        let length = BinaryPrimitives.ReadUInt32BigEndian(this._value) |> int

        let local = buffer.Slice(this._consumed, length)

        this._value <- local
        this._consumed <- this._consumed + length
        this._position <- StreamPosition.MimeType

    member this.ReadPictureDescription() =
        if this._value.Length < 4 then
            readerEx "Invalid reader state"

        let length = BinaryPrimitives.ReadUInt32BigEndian(this._value) |> int

        let local = buffer.Slice(this._consumed, length)

        this._value <- local
        this._consumed <- this._consumed + length
        this._position <- StreamPosition.PictureDescription

    member this.ReadPictureData() =
        if this._value.Length < 4 then
            readerEx "Invalid reader state"

        let length = BinaryPrimitives.ReadUInt32BigEndian(this._value) |> int

        let local = buffer.Slice(this._consumed, length)

        this._value <- local
        this._consumed <- this._consumed + length
        this._position <- StreamPosition.PictureData
