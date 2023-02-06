namespace Safir.Audio.Flac

open System
open System.Buffers.Binary
open System.Runtime.CompilerServices
open System.Text
open Safir.Audio

// TODO: Learn to write performant low-level code
// TODO: Better delineate between "Read" and "Skip" functions

[<Struct; IsByRefLike>]
type FlacStreamReader =
    val private _buffer: ReadOnlySpan<byte>

    val mutable private _consumed: int
    val mutable private _value: ReadOnlySpan<byte>
    val mutable private _hasValue: bool
    val mutable private _valueType: FlacValue

    val mutable private _blockLength: ValueOption<uint32>
    val mutable private _blockType: ValueOption<BlockType>
    val mutable private _lastMetadataBlock: ValueOption<bool>
    val mutable private _seekPointCount: ValueOption<uint32>
    val mutable private _seekPointOffset: ValueOption<uint32>
    val mutable private _userCommentCount: ValueOption<uint32>
    val mutable private _userCommentOffset: ValueOption<uint32>
    val mutable private _cueSheetTrackCount: ValueOption<int>
    val mutable private _cueSheetTrackOffset: ValueOption<int>
    val mutable private _cueSheetTrackIndexCount: ValueOption<int>
    val mutable private _cueSheetTrackIndexOffset: ValueOption<int>

    new(buffer: ReadOnlySpan<byte>, state: FlacStreamState) =
        { _buffer = buffer
          _consumed = 0
          _value = ReadOnlySpan<byte>.Empty
          _hasValue = false
          _valueType = state.Value
          _blockLength = state.BlockLength
          _blockType = state.BlockType
          _lastMetadataBlock = state.LastMetadataBlock
          _seekPointCount = state.SeekPointCount
          _seekPointOffset = state.SeekPointOffset
          _userCommentCount = state.UserCommentCount
          _userCommentOffset = state.UserCommentOffset
          _cueSheetTrackCount = state.CueSheetTrackCount
          _cueSheetTrackOffset = state.CueSheetTrackOffset
          _cueSheetTrackIndexCount = state.CueSheetTrackIndexCount
          _cueSheetTrackIndexOffset = state.CueSheetTrackIndexOffset }

    new(buffer: ReadOnlySpan<byte>) = FlacStreamReader(buffer, FlacStreamState.Empty)

    member this.ValueType = if this._hasValue then this._valueType else FlacValue.None
    member this.Value = this._value

    // TODO: If we keep this, DRY the logic
    member this.NextValue =
        match this._valueType with
        | FlacValue.None -> FlacValue.Marker
        | FlacValue.Marker -> FlacValue.LastMetadataBlockFlag
        | FlacValue.LastMetadataBlockFlag -> FlacValue.MetadataBlockType
        | FlacValue.MetadataBlockType -> FlacValue.DataBlockLength
        | FlacValue.DataBlockLength ->
            match this._blockType with
            | ValueNone -> readerEx "Expected a value for BlockType"
            | ValueSome blockType ->
                match blockType with
                | BlockType.StreamInfo -> FlacValue.MinimumBlockSize
                | BlockType.Padding -> FlacValue.Padding
                | BlockType.Application -> FlacValue.ApplicationId
                | BlockType.SeekTable -> FlacValue.SeekPointSampleNumber
                | BlockType.VorbisComment -> FlacValue.VendorLength
                | BlockType.CueSheet -> FlacValue.MediaCatalogNumber
                | BlockType.Picture -> FlacValue.PictureType
                | _ -> readerEx "Unknown block type"
        | FlacValue.MinimumBlockSize -> FlacValue.MaximumBlockSize
        | FlacValue.MaximumBlockSize -> FlacValue.MinimumFrameSize
        | FlacValue.MinimumFrameSize -> FlacValue.MaximumFrameSize
        | FlacValue.MaximumFrameSize -> FlacValue.StreamInfoSampleRate
        | FlacValue.StreamInfoSampleRate -> FlacValue.NumberOfChannels
        | FlacValue.NumberOfChannels -> FlacValue.BitsPerSample
        | FlacValue.BitsPerSample -> FlacValue.TotalSamples
        | FlacValue.TotalSamples -> FlacValue.Md5Signature
        | FlacValue.Md5Signature ->
            match this._lastMetadataBlock with
            | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
            | ValueSome false -> FlacValue.LastMetadataBlockFlag
            | ValueSome true -> FlacValue.None // We currently don't support anything past metadata
        | FlacValue.Padding ->
            match this._lastMetadataBlock with
            | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
            | ValueSome false -> FlacValue.LastMetadataBlockFlag
            | ValueSome true -> FlacValue.None // We currently don't support anything past metadata
        | FlacValue.ApplicationId -> FlacValue.ApplicationData
        | FlacValue.ApplicationData ->
            match this._lastMetadataBlock with
            | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
            | ValueSome false -> FlacValue.LastMetadataBlockFlag
            | ValueSome true -> FlacValue.None // We currently don't support anything past metadata
        | FlacValue.SeekPointSampleNumber -> FlacValue.SeekPointOffset
        | FlacValue.SeekPointOffset -> FlacValue.NumberOfSamples
        | FlacValue.NumberOfSamples ->
            match this._seekPointCount, this._seekPointOffset with
            | ValueSome n, ValueSome i when i < n -> FlacValue.SeekPointSampleNumber
            | ValueSome n, ValueSome i when i = n ->
                match this._lastMetadataBlock with
                | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
                | ValueSome false -> FlacValue.LastMetadataBlockFlag
                | ValueSome true -> FlacValue.None // We currently don't support anything past metadata
            | _, _ -> readerEx "Expected values for SeekPointCount and SeekPointOffset"
        | FlacValue.VendorLength -> FlacValue.VendorString
        | FlacValue.VendorString -> FlacValue.UserCommentListLength
        | FlacValue.UserCommentListLength -> FlacValue.UserCommentLength
        | FlacValue.UserCommentLength -> FlacValue.UserComment
        | FlacValue.UserComment ->
            match this._userCommentCount, this._userCommentOffset with
            | ValueSome n, ValueSome i when i < n -> FlacValue.UserCommentLength
            | ValueSome n, ValueSome i when i = n ->
                match this._lastMetadataBlock with
                | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
                | ValueSome false -> FlacValue.LastMetadataBlockFlag
                | ValueSome true -> FlacValue.None // We currently don't support anything past metadata
            | _, _ -> readerEx "Expected values for UserCommentCount and UserCommentOffset"
        | FlacValue.MediaCatalogNumber -> FlacValue.NumberOfLeadInSamples
        | FlacValue.NumberOfLeadInSamples -> FlacValue.IsCueSheetCompactDisc
        | FlacValue.IsCueSheetCompactDisc -> FlacValue.CueSheetReserved
        | FlacValue.CueSheetReserved -> FlacValue.NumberOfTracks
        | FlacValue.NumberOfTracks -> FlacValue.TrackOffset
        | FlacValue.TrackOffset -> FlacValue.TrackNumber
        | FlacValue.TrackNumber -> FlacValue.TrackIsrc
        | FlacValue.TrackIsrc -> FlacValue.TrackType
        | FlacValue.TrackType -> FlacValue.PreEmphasis
        | FlacValue.PreEmphasis -> FlacValue.TrackReserved
        | FlacValue.TrackReserved -> FlacValue.NumberOfTrackIndexPoints
        | FlacValue.NumberOfTrackIndexPoints -> FlacValue.TrackIndexOffset
        | FlacValue.TrackIndexOffset -> FlacValue.IndexPointNumber
        | FlacValue.IndexPointNumber -> FlacValue.TrackIndexReserved
        | FlacValue.TrackIndexReserved ->
            match this._cueSheetTrackIndexCount, this._cueSheetTrackIndexOffset with
            | ValueSome n, ValueSome i when i < n -> FlacValue.TrackIndexOffset
            | ValueSome n, ValueSome i when i = n ->
                match this._cueSheetTrackCount, this._cueSheetTrackOffset with
                | ValueSome n, ValueSome i when i < n -> FlacValue.TrackOffset
                | ValueSome n, ValueSome i when i = n ->
                    match this._lastMetadataBlock with
                    | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
                    | ValueSome false -> FlacValue.LastMetadataBlockFlag
                    | ValueSome true -> FlacValue.None // We currently don't support anything past metadata
                | _, _ -> readerEx "Expected values for CueSheetTrackCount and CueSheetTrackOffset"
            | _, _ -> readerEx "Expected values for CueSheetTrackIndexCount and CueSheetTrackIndexOffset"
        | FlacValue.PictureType -> FlacValue.MimeTypeLength
        | FlacValue.MimeTypeLength -> FlacValue.MimeType
        | FlacValue.MimeType -> FlacValue.PictureDescriptionLength
        | FlacValue.PictureDescriptionLength -> FlacValue.PictureDescription
        | FlacValue.PictureDescription -> FlacValue.PictureWidth
        | FlacValue.PictureWidth -> FlacValue.PictureHeight
        | FlacValue.PictureHeight -> FlacValue.PictureColorDepth
        | FlacValue.PictureColorDepth -> FlacValue.PictureNumberOfColors
        | FlacValue.PictureNumberOfColors -> FlacValue.PictureDataLength
        | FlacValue.PictureDataLength -> FlacValue.PictureData
        | FlacValue.PictureData ->
            match this._lastMetadataBlock with
            | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
            | ValueSome false -> FlacValue.LastMetadataBlockFlag
            | ValueSome true -> FlacValue.None // We currently don't support anything past metadata
        | _ -> readerEx "Invalid stream position"

    member this.Read() =
        if this._buffer.Length = this._consumed then
            false
        else
            match this._valueType with
            | FlacValue.None -> this.Read(FlacValue.Marker, 4)
            | FlacValue.Marker -> this.ReadLastMetadataBlockFlag()
            | FlacValue.LastMetadataBlockFlag -> this.ReadMetadataBlockType()
            | FlacValue.MetadataBlockType -> this.ReadMetadataBlockLength()
            | FlacValue.DataBlockLength -> this.StartMetadataBlockData()
            | FlacValue.MinimumBlockSize -> this.Read(FlacValue.MaximumBlockSize, 2)
            | FlacValue.MaximumBlockSize -> this.Read(FlacValue.MinimumFrameSize, 3)
            | FlacValue.MinimumFrameSize -> this.Read(FlacValue.MaximumFrameSize, 3)
            | FlacValue.MaximumFrameSize -> this.ReadSampleRate()
            | FlacValue.StreamInfoSampleRate -> this.ReadNumberOfChannels()
            | FlacValue.NumberOfChannels -> this.ReadBitsPerSample()
            | FlacValue.BitsPerSample -> this.ReadTotalSamples()
            | FlacValue.TotalSamples -> this.Read(FlacValue.Md5Signature, 16)
            | FlacValue.Md5Signature -> this.EndMetadataBlockData()
            | FlacValue.Padding -> this.EndMetadataBlockData()
            | FlacValue.ApplicationId -> this.ReadApplicationData()
            | FlacValue.ApplicationData -> this.EndMetadataBlockData()
            | FlacValue.SeekPointSampleNumber -> this.Read(FlacValue.SeekPointOffset, 8)
            | FlacValue.SeekPointOffset -> this.Read(FlacValue.NumberOfSamples, 2)
            | FlacValue.NumberOfSamples -> this.EndSeekPoint()
            | FlacValue.VendorLength -> this.ReadVendorString()
            | FlacValue.VendorString -> this.Read(FlacValue.UserCommentListLength, 4)
            | FlacValue.UserCommentListLength -> this.StartUserCommentList()
            | FlacValue.UserCommentLength -> this.ReadUserComment()
            | FlacValue.UserComment -> this.EndUserComment()
            | FlacValue.MediaCatalogNumber -> this.ReadCueSheetLeadInSamplesNumber()
            | FlacValue.NumberOfLeadInSamples -> this.ReadIsCueSheetCompactDisc()
            | FlacValue.IsCueSheetCompactDisc -> this.ReadCueSheetReserved()
            | FlacValue.CueSheetReserved -> this.ReadCueSheetNumberOfTracks()
            | FlacValue.NumberOfTracks -> this.StartCueSheetTrack()
            | FlacValue.TrackOffset -> this.ReadCueSheetTrackNumber()
            | FlacValue.TrackNumber -> this.Read(FlacValue.TrackIsrc, 12)
            | FlacValue.TrackIsrc -> this.ReadCueSheetTrackType()
            | FlacValue.TrackType -> this.ReadCueSheetTrackPreEmphasis()
            | FlacValue.PreEmphasis -> this.ReadCueSheetTrackReserved()
            | FlacValue.TrackReserved -> this.ReadCueSheetNumberOfTrackIndexPoints()
            | FlacValue.NumberOfTrackIndexPoints -> this.StartCueSheetTrackIndexPoint()
            | FlacValue.TrackIndexOffset -> this.ReadCueSheetTrackIndexPointNumber()
            | FlacValue.IndexPointNumber -> this.ReadCueSheetTrackIndexReserved()
            | FlacValue.TrackIndexReserved -> this.EndCueSheetTrackIndexPoint()
            | FlacValue.PictureType -> this.Read(FlacValue.MimeTypeLength, 4)
            | FlacValue.MimeTypeLength -> this.ReadMimeType()
            | FlacValue.MimeType -> this.Read(FlacValue.PictureDescriptionLength, 4)
            | FlacValue.PictureDescriptionLength -> this.ReadPictureDescription()
            | FlacValue.PictureDescription -> this.Read(FlacValue.PictureWidth, 4)
            | FlacValue.PictureWidth -> this.Read(FlacValue.PictureHeight, 4)
            | FlacValue.PictureHeight -> this.Read(FlacValue.PictureColorDepth, 4)
            | FlacValue.PictureColorDepth -> this.Read(FlacValue.PictureNumberOfColors, 4)
            | FlacValue.PictureNumberOfColors -> this.Read(FlacValue.PictureDataLength, 4)
            | FlacValue.PictureDataLength -> this.ReadPictureData()
            | FlacValue.PictureData -> this.EndMetadataBlockData()
            | _ -> readerEx "Invalid stream position"

            this._hasValue <- true
            true

    member this.Skip() =
        // TODO: Optimize to skip everything but the necessary bits
        this.Read() |> ignore

    member this.SkipTo(value: FlacValue) =
        while this.NextValue <> value && this.Read() do
            () // We advance in the loop condition

    member private this.Read(value: FlacValue, length: int) =
        this._value <- this._buffer.Slice(this._consumed, length)
        this._consumed <- this._consumed + length
        this._valueType <- value

    member private this.ReadMagic() =
        let local = this._buffer.Slice(this._consumed, 4)

        if not <| local.SequenceEqual("fLaC"B) then
            readerEx "Invalid stream marker"

        this._value <- local
        this._consumed <- this._consumed + 4

    member private this.ReadLastMetadataBlockFlag() =
        let local = this._buffer[this._consumed] >>> 7 = 0x1uy // TODO: DRY

        this._value <- this._buffer.Slice(this._consumed, 1)
        this._lastMetadataBlock <- ValueSome local
        this._valueType <- FlacValue.LastMetadataBlockFlag

    member private this.ReadMetadataBlockType() =
        let temp = this._buffer[this._consumed]
        let local = temp &&& 0x7Fuy // TODO: DRY

        if local > 127uy then readerEx "Invalid metadata block type"

        this._value <- this._buffer.Slice(this._consumed, 1)
        this._consumed <- this._consumed + 1
        this._blockType <- ValueSome(enum<BlockType> (int local))
        this._valueType <- FlacValue.MetadataBlockType

    member private this.ReadMetadataBlockLength() =
        let local = this._buffer.Slice(this._consumed, 3)
        let length = readUInt32 local

        this._value <- local
        this._blockLength <- ValueSome length
        this._consumed <- this._consumed + 3
        this._valueType <- FlacValue.DataBlockLength

    member private this.StartMetadataBlockData() =
        match this._blockType with
        | ValueNone -> readerEx "Expected a value for BlockType"
        | ValueSome blockType ->
            match blockType with
            | BlockType.StreamInfo -> this.ReadMinimumBlockSize()
            | BlockType.Padding -> this.ReadMetadataBlockPadding()
            | BlockType.Application -> this.Read(FlacValue.ApplicationId, 4)
            | BlockType.SeekTable -> this.StartSeekTable()
            | BlockType.VorbisComment -> this.Read(FlacValue.VendorLength, 4)
            | BlockType.CueSheet -> this.ReadCueSheetCatalogNumber()
            | BlockType.Picture -> this.Read(FlacValue.PictureType, 4)
            | t when int t < 127 -> readerEx "TODO"
            | _ -> readerEx "Invalid block type"

    member private this.EndMetadataBlockData() =
        match this._lastMetadataBlock with
        | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
        | ValueSome false -> this.ReadLastMetadataBlockFlag()
        | ValueSome true -> // We currently don't support anything past metadata
            this._value <- ReadOnlySpan<byte>.Empty
            this._consumed <- this._buffer.Length
            this._valueType <- FlacValue.None

    member private this.ReadMinimumBlockSize() =
        let local = this._buffer.Slice(this._consumed, 2)
        let blockSize = BinaryPrimitives.ReadUInt16BigEndian(local)

        if blockSize <= 15us then
            readerEx "Invalid minimum block size"

        this._value <- local
        this._consumed <- this._consumed + 2
        this._valueType <- FlacValue.MinimumBlockSize

    member private this.ReadSampleRate() =
        let local = this._buffer.Slice(this._consumed, 3)
        let sampleRate = (readUInt32 local) >>> 4 // TODO: DRY

        if sampleRate = 0u || sampleRate > FlacConstants.MaxSampleRate then
            readerEx "Invalid sample rate"

        // TODO: How to represent this is offset by 4 bits?
        this._value <- local
        // We only fully consume the first two bytes
        this._consumed <- this._consumed + 2
        this._valueType <- FlacValue.StreamInfoSampleRate

    member private this.ReadNumberOfChannels() =
        // TODO: This is correct for the scope of this method, but incorrect elsewhere. Why?
        // let local = this._buffer[this._consumed] &&& 0x0Euy >>> 1
        // this._value <- ReadOnlySpan<byte>(&local)
        this._value <- this._buffer.Slice(this._consumed, 1)
        this._valueType <- FlacValue.NumberOfChannels

    member private this.ReadBitsPerSample() =
        let local = this._buffer.Slice(this._consumed, 2)

        // TODO: DRY
        let a = uint16 (local[0] &&& 0x01uy) <<< 13
        let b = uint16 (local[1]) >>> 4
        let bps = a + b + 1us

        if bps < 4us || bps > 32us then
            readerEx "Invalid bits per sample"

        this._value <- local
        // We only fully consume the first byte
        this._consumed <- this._consumed + 1
        this._valueType <- FlacValue.BitsPerSample

    // TODO: How to represent this first byte requires a mask?
    member private this.ReadTotalSamples() = this.Read(FlacValue.TotalSamples, 5)

    member private this.ReadMetadataBlockPadding() =
        match this._blockLength with
        | ValueNone -> readerEx "Expected a value for BlockLength"
        | ValueSome length ->
            let l = int length

            if l % 8 <> 0 then
                readerEx "Padding length must be a multiple of 8"

            let local = this._buffer.Slice(this._consumed, l)

            if local.IndexOfAnyExcept(0uy) <> -1 then
                readerEx "Padding contains invalid bytes"

            this._value <- local
            this._consumed <- this._consumed + l
            this._valueType <- FlacValue.Padding

    member private this.ReadApplicationData() =
        match this._blockLength with
        | ValueNone -> readerEx "Expected a value for BlockLength"
        | ValueSome length ->
            let l = int length - 4

            if l % 8 <> 0 then
                readerEx "Application data length must be a multiple of 8"

            this._value <- this._buffer.Slice(this._consumed, l)
            this._consumed <- this._consumed + l
            this._valueType <- FlacValue.ApplicationData

    member private this.StartSeekTable() =
        match this._blockLength with
        | ValueNone -> readerEx "Expected a value for BlockLength"
        | ValueSome length when length % 18u <> 0u -> readerEx "Invalid block length"
        | ValueSome length -> this._seekPointCount <- ValueSome(length / 18u)

        this._seekPointOffset <- ValueSome 0u
        this.StartSeekPoint()

    member private this.StartSeekPoint() =
        this.Read(FlacValue.SeekPointSampleNumber, 8)

        match this._seekPointOffset with
        | ValueNone -> readerEx "Expected a value for SeekPointOffset"
        | ValueSome i -> this._seekPointOffset <- ValueSome(i + 1u)

    member private this.EndSeekPoint() =
        match this._seekPointCount, this._seekPointOffset with
        | ValueSome n, ValueSome i when i < n -> this.StartSeekPoint()
        | ValueSome n, ValueSome i when i = n -> this.EndMetadataBlockData()
        | _, _ -> readerEx "Expected values for SeekPointCount and SeekPointOffset"

    member private this.ReadVendorString() =
        if this._value.Length < 4 then
            readerEx "Invalid VendorStringLength"

        // TODO: DRY
        let length = BinaryPrimitives.ReadUInt32LittleEndian(this._value) |> int
        let local = this._buffer.Slice(this._consumed, length)

        this._value <- local
        this._valueType <- FlacValue.VendorString
        this._consumed <- this._consumed + length

    member private this.StartUserCommentList() =
        if this._value.Length < 4 then
            readerEx "Invalid UserCommentListLength"

        let length = BinaryPrimitives.ReadUInt32LittleEndian(this._value)

        this._userCommentCount <- ValueSome length
        this._userCommentOffset <- ValueSome 0u
        this.Read(FlacValue.UserCommentLength, 4)

    member private this.ReadUserComment() =
        if this._value.Length < 4 then
            readerEx "Invalid UserCommentLength"

        let length = BinaryPrimitives.ReadUInt32LittleEndian(this._value) |> int
        let local = this._buffer.Slice(this._consumed, length)

        this._value <- local
        this._valueType <- FlacValue.UserComment
        this._consumed <- this._consumed + length

        match this._userCommentOffset with
        | ValueNone -> readerEx "Expected a value for UserCommentOffset"
        | ValueSome i -> this._userCommentOffset <- ValueSome(i + 1u)

    member private this.EndUserComment() =
        match this._userCommentCount, this._userCommentOffset with
        | ValueSome n, ValueSome i when i < n -> this.Read(FlacValue.UserCommentLength, 4)
        | ValueSome n, ValueSome i when i = n -> this.EndMetadataBlockData()
        | _, _ -> readerEx "Expected values for UserCommentCount and UserCommentOffset"

    // TODO: Validate for CD-DA; offset % 588 = 0
    member private this.ReadCueSheetTrackIndexOffset() =
        this.Read(FlacValue.TrackIndexOffset, 8)

    member private this.ReadCueSheetTrackIndexPointNumber() =
        let local = this._buffer[this._consumed]

        // TODO: Validate first index
        // TODO: Validate uniqueness

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._valueType <- FlacValue.IndexPointNumber

    member private this.ReadCueSheetTrackIndexReserved() =
        let local = this._buffer.Slice(this._consumed, 3)

        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        this._value <- local
        this._consumed <- this._consumed + 3
        this._valueType <- FlacValue.TrackIndexReserved

    // TODO: Validate for CD-DA; offset % 588 = 0
    member private this.ReadCueSheetTrackOffset() = this.Read(FlacValue.TrackOffset, 8)

    member private this.ReadCueSheetTrackNumber() =
        let local = this._buffer[this._consumed]

        // This may be a soft requirement...
        if local = 0uy then
            readerEx "Invalid cue sheet track number"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._valueType <- FlacValue.TrackNumber

    member private this.ReadCueSheetTrackType() =
        let local = this._buffer[this._consumed] &&& 0x80uy
        this._value <- ReadOnlySpan<byte>(&local)
        this._valueType <- FlacValue.TrackType

    member private this.ReadCueSheetTrackPreEmphasis() =
        let local = this._buffer[this._consumed] &&& 0x40uy
        this._value <- ReadOnlySpan<byte>(&local)
        this._valueType <- FlacValue.PreEmphasis

    member private this.ReadCueSheetTrackReserved() =
        let local = this._buffer.Slice(this._consumed, 14)

        // TODO: This doesn't account for the leading 6 bits
        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        // TODO: How to represent this is offset by two bits?
        this._value <- local
        this._consumed <- this._consumed + 14
        this._valueType <- FlacValue.TrackReserved

    // TODO: Validate ASCII characters
    // TODO: Validate CD-DA 13 digit number + 115 NUL bytes
    member private this.ReadCueSheetCatalogNumber() =
        this.Read(FlacValue.MediaCatalogNumber, 128)

    // TODO: Do we need any validation here?
    member private this.ReadCueSheetLeadInSamplesNumber() =
        this.Read(FlacValue.NumberOfLeadInSamples, 8)

    member private this.ReadIsCueSheetCompactDisc() =
        let local = this._buffer[this._consumed] >>> 7
        this._value <- ReadOnlySpan<byte>(&local)
        this._valueType <- FlacValue.IsCueSheetCompactDisc

    member private this.ReadCueSheetReserved() =
        let local = this._buffer.Slice(this._consumed, 259)

        // TODO: This doesn't account for the leading 7 bits
        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        // TODO: How to represent this is offset by one bit?
        this._value <- local
        this._consumed <- this._consumed + 259
        this._valueType <- FlacValue.CueSheetReserved

    member private this.ReadCueSheetNumberOfTracks() =
        let local = this._buffer[this._consumed]

        // TODO: Validate for CD-DA num < 100
        if local < 1uy then
            readerEx "Must have at least one cue sheet track"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._valueType <- FlacValue.NumberOfTracks
        this._cueSheetTrackCount <- ValueSome(int local)
        this._cueSheetTrackOffset <- ValueSome 0

    member private this.StartCueSheetTrack() =
        this.ReadCueSheetTrackOffset()

        match this._cueSheetTrackOffset with
        | ValueNone -> readerEx "Invalid reader state"
        | ValueSome i -> this._cueSheetTrackOffset <- ValueSome(i + 1)

    member private this.EndCueSheetTrack() =
        match this._cueSheetTrackCount, this._cueSheetTrackOffset with
        | ValueSome n, ValueSome i when i < n -> this.StartCueSheetTrack()
        | ValueSome n, ValueSome i when i = n -> this.EndMetadataBlockData()
        | _, _ -> readerEx "Expected values for CueSheetTrackCount and CueSheetTrackOffset"

    member private this.ReadCueSheetNumberOfTrackIndexPoints() =
        let local = this._buffer[this._consumed]

        // TODO: Validate for CD-DA num < 100
        if local < 1uy then
            readerEx "Must have at least one cue sheet track index"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._valueType <- FlacValue.NumberOfTrackIndexPoints
        this._cueSheetTrackIndexCount <- ValueSome(int local)

    member private this.StartCueSheetTrackIndexPoint() =
        this.ReadCueSheetTrackIndexOffset()

        match this._cueSheetTrackIndexOffset with
        | ValueNone -> readerEx "Expected a value for CueSheetTrackIndexOffset"
        | ValueSome i -> this._cueSheetTrackIndexOffset <- ValueSome(i + 1)

    member private this.EndCueSheetTrackIndexPoint() =
        match this._cueSheetTrackIndexCount, this._cueSheetTrackIndexOffset with
        | ValueSome n, ValueSome i when i < n -> this.StartCueSheetTrackIndexPoint()
        | ValueSome n, ValueSome i when i = n -> this.EndCueSheetTrack()
        | _, _ -> readerEx "Expected values for CueSheetTrackIndexCount and CueSheetTrackIndexOffset"

    member private this.ReadMimeType() =
        if this._value.Length < 4 then
            readerEx "Mime type length is too short"

        let length = BinaryPrimitives.ReadUInt32BigEndian(this._value) |> int
        let local = this._buffer.Slice(this._consumed, length)

        this._value <- local
        this._consumed <- this._consumed + length
        this._valueType <- FlacValue.MimeType

    member private this.ReadPictureDescription() =
        if this._value.Length < 4 then
            readerEx "Picture description length is too short"

        let length = BinaryPrimitives.ReadUInt32BigEndian(this._value) |> int

        let local = this._buffer.Slice(this._consumed, length)

        this._value <- local
        this._consumed <- this._consumed + length
        this._valueType <- FlacValue.PictureDescription

    member private this.ReadPictureData() =
        if this._value.Length < 4 then
            readerEx "Picture data length is too short"

        let length = BinaryPrimitives.ReadUInt32BigEndian(this._value) |> int

        let local = this._buffer.Slice(this._consumed, length)

        this._value <- local
        this._consumed <- this._consumed + length
        this._valueType <- FlacValue.PictureData

    member this.GetLastMetadataBlockFlag() =
        if this._valueType <> FlacValue.LastMetadataBlockFlag then
            readerEx "Expected reader to be positioned at LastMetadataBlockFlag"

        this._value[0] >>> 7 = 0x1uy

    member this.ReadAsLastMetadataBlockFlag() =
        this.Read() |> ignore
        this.GetLastMetadataBlockFlag()

    member this.GetBlockType() =
        if this._valueType <> FlacValue.MetadataBlockType then
            readerEx "Expected reader to be positioned at MetadataBlockType"

        let blockType = this._value[0] &&& 0x7Fuy |> int
        enum<BlockType> blockType

    member this.ReadAsBlockType() =
        this.Read() |> ignore
        this.GetBlockType()

    member this.GetDataBlockLength() =
        if this._valueType <> FlacValue.DataBlockLength then
            readerEx "Expected reader to be positioned at DataBlockLength"

        readUInt32 this._value

    member this.ReadAsDataBlockLength() =
        this.Read() |> ignore
        this.GetDataBlockLength()

    member this.GetMinimumBlockSize() =
        if this._valueType <> FlacValue.MinimumBlockSize then
            readerEx "Expected reader to be positioned at MinimumBlockSize"

        BinaryPrimitives.ReadUInt16BigEndian(this._value)

    member this.ReadAsMinimumBlockSize() =
        this.Read() |> ignore
        this.GetMinimumBlockSize()

    member this.GetMaximumBlockSize() =
        if this._valueType <> FlacValue.MaximumBlockSize then
            readerEx "Expected reader to be positioned at MaximumBlockSize"

        BinaryPrimitives.ReadUInt16BigEndian(this._value)

    member this.ReadAsMaximumBlockSize() =
        this.Read() |> ignore
        this.GetMaximumBlockSize()

    member this.GetMinimumFrameSize() =
        if this._valueType <> FlacValue.MinimumFrameSize then
            readerEx "Expected reader to be positioned at MinimumFrameSize"

        readUInt32 this._value

    member this.ReadAsMinimumFrameSize() =
        this.Read() |> ignore
        this.GetMinimumFrameSize()

    member this.GetMaximumFrameSize() =
        if this._valueType <> FlacValue.MaximumFrameSize then
            readerEx "Expected reader to be positioned at MaximumFrameSize"

        readUInt32 this._value

    member this.ReadAsMaximumFrameSize() =
        this.Read() |> ignore
        this.GetMaximumFrameSize()

    member this.GetSampleRate() =
        if this._valueType <> FlacValue.StreamInfoSampleRate then
            readerEx "Expected reader to be positioned at StreamInfoSampleRate"

        readUInt32 this._value >>> 4

    member this.ReadAsSampleRate() =
        this.Read() |> ignore
        this.GetSampleRate()

    member this.GetChannels() =
        if this._valueType <> FlacValue.NumberOfChannels then
            readerEx "Expected reader to be positioned at NumberOfChannels"

        uint16 (this._value[0] &&& 0x0Euy >>> 1) + 1us

    member this.ReadAsChannels() =
        this.Read() |> ignore
        this.GetChannels()

    member this.GetBitsPerSample() =
        if this._valueType <> FlacValue.BitsPerSample then
            readerEx "Expected reader to be positioned at BitsPerSample"

        let a = uint16 (this._value[0] &&& 0x01uy) <<< 13
        let b = uint16 (this._value[1]) >>> 4
        a + b + 1us

    member this.ReadAsBitsPerSample() =
        this.Read() |> ignore
        this.GetBitsPerSample()

    member this.GetTotalSamples() =
        if this._valueType <> FlacValue.TotalSamples then
            readerEx "Expected reader to be positioned at TotalSamples"

        let a = uint64 (this._value[0] &&& 0x0Fuy) <<< 8 * 4
        let b = uint64 this._value[1] <<< 8 * 3
        let c = uint64 this._value[2] <<< 8 * 2
        let d = uint64 this._value[3] <<< 8
        let e = uint64 this._value[4]
        a + b + c + d + e

    member this.ReadAsTotalSamples() =
        this.Read() |> ignore
        this.GetTotalSamples()

    member this.GetMd5Signature() =
        if this._valueType <> FlacValue.Md5Signature then
            readerEx "Expected reader to be positioned at Md5Signature"

        Convert.ToHexString(this._value)

    member this.ReadAsMd5Signature() =
        this.Read() |> ignore
        this.GetMd5Signature()

    member this.GetSeekPointSampleNumber() =
        if this._valueType <> FlacValue.SeekPointSampleNumber then
            readerEx "Expected reader to be positioned at SeekPointSampleNumber"

        BinaryPrimitives.ReadUInt64BigEndian(this._value)

    member this.ReadAsSeekPointSampleNumber() =
        this.Read() |> ignore
        this.GetSeekPointSampleNumber()

    member this.GetSeekPointOffset() =
        if this._valueType <> FlacValue.SeekPointOffset then
            readerEx "Expected reader to be positioned at SeekPointOffset"

        BinaryPrimitives.ReadUInt64BigEndian(this._value)

    member this.ReadAsSeekPointOffset() =
        this.Read() |> ignore
        this.GetSeekPointOffset()

    member this.GetSeekPointNumberOfSamples() =
        if this._valueType <> FlacValue.NumberOfSamples then
            readerEx "Expected reader to be positioned at NumberOfSamples"

        BinaryPrimitives.ReadUInt16BigEndian(this._value)

    member this.ReadAsSeekPointNumberOfSamples() =
        this.Read() |> ignore
        this.GetSeekPointNumberOfSamples()

    member this.GetVendorLength() =
        if this._valueType <> FlacValue.VendorLength then
            readerEx "Expected reader to be positioned at VendorLength"

        BinaryPrimitives.ReadUInt32LittleEndian(this._value)

    member this.ReadAsVendorLength() =
        this.Read() |> ignore
        this.GetVendorLength()

    member this.GetVendorString() =
        if this._valueType <> FlacValue.VendorString then
            readerEx "Expected reader to be positioned at VendorString"

        Encoding.UTF8.GetString(this._value)

    member this.ReadAsVendorString() =
        this.Read() |> ignore
        this.GetVendorString()

    member this.GetUserCommentListLength() =
        if this._valueType <> FlacValue.UserCommentListLength then
            readerEx "Expected reader to be positioned at UserCommentListLength"

        BinaryPrimitives.ReadUInt32LittleEndian(this._value)

    member this.ReadAsUserCommentListLength() =
        this.Read() |> ignore
        this.GetUserCommentListLength()

    member this.GetUserCommentLength() =
        if this._valueType <> FlacValue.UserCommentLength then
            readerEx "Expected reader to be positioned at UserCommentLength"

        BinaryPrimitives.ReadUInt32LittleEndian(this._value)

    member this.ReadAsUserCommentLength() =
        this.Read() |> ignore
        this.GetUserCommentLength()

    member this.GetUserComment() =
        if this._valueType <> FlacValue.UserComment then
            readerEx "Expected reader to be positioned at UserComment"

        Encoding.UTF8.GetString(this._value)

    member this.ReadAsUserComment() =
        this.Read() |> ignore
        this.GetUserComment()

    member this.GetPictureType() =
        if this._valueType <> FlacValue.PictureType then
            readerEx "Expected reader to be positioned at PictureType"

        let pictureType = BinaryPrimitives.ReadUInt32BigEndian(this._value)
        enum<PictureType> (int pictureType)

    member this.ReadAsPictureType() =
        this.Read() |> ignore
        this.GetPictureType()

    member this.GetMimeTypeLength() =
        if this._valueType <> FlacValue.MimeTypeLength then
            readerEx "Expected reader to be positioned at MimeTypeLength"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.ReadAsMimeTypeLength() =
        this.Read() |> ignore
        this.GetMimeTypeLength()

    member this.GetMimeType() =
        if this._valueType <> FlacValue.MimeType then
            readerEx "Expected reader to be positioned at MimeType"

        Encoding.UTF8.GetString(this._value)

    member this.ReadAsMimeType() =
        this.Read() |> ignore
        this.GetMimeType()

    member this.GetPictureDescriptionLength() =
        if this._valueType <> FlacValue.PictureDescriptionLength then
            readerEx "Expected reader to be positioned at PictureDescriptionLength"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.ReadAsPictureDescriptionLength() =
        this.Read() |> ignore
        this.GetPictureDescriptionLength()

    member this.GetPictureDescription() =
        if this._valueType <> FlacValue.PictureDescription then
            readerEx "Expected reader to be positioned at PictureDescription"

        Encoding.UTF8.GetString(this._value)

    member this.ReadAsPictureDescription() =
        this.Read() |> ignore
        this.GetPictureDescription()

    member this.GetPictureWidth() =
        if this._valueType <> FlacValue.PictureWidth then
            readerEx "Expected reader to be positioned at PictureWidth"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.ReadAsPictureWidth() =
        this.Read() |> ignore
        this.GetPictureWidth()

    member this.GetPictureHeight() =
        if this._valueType <> FlacValue.PictureHeight then
            readerEx "Expected reader to be positioned at PictureHeight"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.ReadAsPictureHeight() =
        this.Read() |> ignore
        this.GetPictureHeight()

    member this.GetPictureColorDepth() =
        if this._valueType <> FlacValue.PictureColorDepth then
            readerEx "Expected reader to be positioned at PictureColorDepth"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.ReadAsPictureColorDepth() =
        this.Read() |> ignore
        this.GetPictureColorDepth()

    member this.GetPictureNumberOfColors() =
        if this._valueType <> FlacValue.PictureNumberOfColors then
            readerEx "Expected reader to be positioned at PictureNumberOfColors"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.ReadAsPictureNumberOfColors() =
        this.Read() |> ignore
        this.GetPictureNumberOfColors()

    member this.GetPictureDataLength() =
        if this._valueType <> FlacValue.PictureDataLength then
            readerEx "Expected reader to be positioned at PictureDataLength"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.ReadAsPictureDataLength() =
        this.Read() |> ignore
        this.GetPictureDataLength()

    member this.GetPictureData() =
        if this._valueType <> FlacValue.PictureData then
            readerEx "Expected reader to be positioned at PictureData"

        this._value

    member this.ReadAsPictureData() =
        this.Read() |> ignore
        this.GetPictureData()
