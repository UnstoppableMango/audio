namespace Safir.Audio

open System
open System.Buffers.Binary
open System.Runtime.CompilerServices
open System.Text

// TODO: There are plenty of copies in here that while they don't
//       allocate are likely still hurting performance quite a bit
// TODO: Learn to write performant low-level code
// TODO: Better delineate between "Read" and "Skip" functions

[<Struct; IsByRefLike>]
type FlacStreamReader =
    val private _buffer: ReadOnlySpan<byte>

    val mutable private _position: StreamPosition
    val mutable private _consumed: int
    val mutable private _value: ReadOnlySpan<byte>

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
          _position = state.Position
          _consumed = 0
          _value = ReadOnlySpan<byte>.Empty
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

    member this.Position = this._position

    member this.Value = this._value

    member this.Read() =
        match this._position with
        | StreamPosition.Start -> this.Read(StreamPosition.Marker, 4)
        | StreamPosition.Marker -> this.ReadLastMetadataBlockFlag()
        | StreamPosition.LastMetadataBlockFlag -> this.ReadMetadataBlockType()
        | StreamPosition.MetadataBlockType -> this.ReadMetadataBlockLength()
        | StreamPosition.DataBlockLength -> this.StartMetadataBlockData()
        | StreamPosition.MinimumBlockSize -> this.Read(StreamPosition.MaximumBlockSize, 2)
        | StreamPosition.MaximumBlockSize -> this.Read(StreamPosition.MinimumFrameSize, 3)
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

    member private this.Read(position: StreamPosition, length: int) =
        this._value <- this._buffer.Slice(this._consumed, length)
        this._consumed <- this._consumed + length
        this._position <- position

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
        this._position <- StreamPosition.LastMetadataBlockFlag

    member private this.ReadMetadataBlockType() =
        let temp = this._buffer[this._consumed]
        let local = temp &&& 0x7Fuy // TODO: DRY

        if local > 127uy then readerEx "Invalid metadata block type"

        this._value <- this._buffer.Slice(this._consumed, 1)
        this._consumed <- this._consumed + 1
        this._blockType <- ValueSome(enum<BlockType> (int local))
        this._position <- StreamPosition.MetadataBlockType

    member private this.ReadMetadataBlockLength() =
        let local = this._buffer.Slice(this._consumed, 3)
        let length = readUInt32 local

        this._value <- local
        this._blockLength <- ValueSome length
        this._consumed <- this._consumed + 3
        this._position <- StreamPosition.DataBlockLength

    member private this.StartMetadataBlockData() =
        match this._blockType with
        | ValueNone -> readerEx "Expected a value for BlockType"
        | ValueSome blockType ->
            match blockType with
            | BlockType.StreamInfo -> this.ReadMinimumBlockSize()
            | BlockType.Padding -> this.ReadMetadataBlockPadding()
            | BlockType.Application -> this.Read(StreamPosition.ApplicationId, 4)
            | BlockType.SeekTable -> this.StartSeekTable()
            | BlockType.VorbisComment -> this.Read(StreamPosition.VendorLength, 4)
            | BlockType.CueSheet -> this.ReadCueSheetCatalogNumber()
            | BlockType.Picture -> this.Read(StreamPosition.PictureType, 4)
            | t when int t < 127 -> readerEx "TODO"
            | _ -> readerEx "Invalid block type"

    member private this.EndMetadataBlockData() =
        match this._lastMetadataBlock with
        | ValueNone -> readerEx "Expected a value for LastMetadataBlock"
        | ValueSome false -> this.ReadLastMetadataBlockFlag()
        | ValueSome true -> // We currently don't support anything past metadata
            this._value <- ReadOnlySpan<byte>.Empty
            this._consumed <- this._buffer.Length
            this._position <- StreamPosition.End

    member private this.ReadMinimumBlockSize() =
        let local = this._buffer.Slice(this._consumed, 2)
        let blockSize = BinaryPrimitives.ReadUInt16BigEndian(local)

        if blockSize <= 15us then
            readerEx "Invalid minimum block size"

        this._value <- local
        this._consumed <- this._consumed + 2
        this._position <- StreamPosition.MinimumBlockSize

    member private this.ReadSampleRate() =
        let local = this._buffer.Slice(this._consumed, 3)
        let sampleRate = (readUInt32 local) >>> 4 // TODO: DRY

        if sampleRate = 0u || sampleRate > FlacConstants.MaxSampleRate then
            readerEx "Invalid sample rate"

        // TODO: How to represent this is offset by 4 bits?
        this._value <- local
        // We only fully consume the first two bytes
        this._consumed <- this._consumed + 2
        this._position <- StreamPosition.StreamInfoSampleRate

    member private this.ReadNumberOfChannels() =
        // TODO: This is correct for the scope of this method, but incorrect elsewhere. Why?
        // let local = this._buffer[this._consumed] &&& 0x0Euy >>> 1
        // this._value <- ReadOnlySpan<byte>(&local)
        this._value <- this._buffer.Slice(this._consumed, 1)
        this._position <- StreamPosition.NumberOfChannels

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
        this._position <- StreamPosition.BitsPerSample

    // TODO: How to represent this first byte requires a mask?
    member private this.ReadTotalSamples() =
        this.Read(StreamPosition.TotalSamples, 5)

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
            this._position <- StreamPosition.Padding

    member private this.ReadApplicationData() =
        match this._blockLength with
        | ValueNone -> readerEx "Expected a value for BlockLength"
        | ValueSome length ->
            let l = int length - 4

            if l % 8 <> 0 then
                readerEx "Application data length must be a multiple of 8"

            this._value <- this._buffer.Slice(this._consumed, l)
            this._consumed <- this._consumed + l
            this._position <- StreamPosition.ApplicationData

    member private this.StartSeekTable() =
        match this._blockLength with
        | ValueNone -> readerEx "Expected a value for BlockLength"
        | ValueSome length when length % 18u <> 0u -> readerEx "Invalid block length"
        | ValueSome length -> this._seekPointCount <- ValueSome(length / 18u)

        this._seekPointOffset <- ValueSome 0u
        this.StartSeekPoint()

    member private this.StartSeekPoint() =
        this.Read(StreamPosition.SeekPointSampleNumber, 8)

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
        this._position <- StreamPosition.VendorString
        this._consumed <- this._consumed + length

    member private this.StartUserCommentList() =
        if this._value.Length < 4 then
            readerEx "Invalid UserCommentListLength"

        let length = BinaryPrimitives.ReadUInt32LittleEndian(this._value)

        this._userCommentCount <- ValueSome length
        this._userCommentOffset <- ValueSome 0u
        this.Read(StreamPosition.UserCommentLength, 4)

    member private this.ReadUserComment() =
        if this._value.Length < 4 then
            readerEx "Invalid UserCommentLength"

        let length = BinaryPrimitives.ReadUInt32LittleEndian(this._value) |> int
        let local = this._buffer.Slice(this._consumed, length)

        this._value <- local
        this._position <- StreamPosition.UserComment
        this._consumed <- this._consumed + length

        match this._userCommentOffset with
        | ValueNone -> readerEx "Expected a value for UserCommentOffset"
        | ValueSome i -> this._userCommentOffset <- ValueSome(i + 1u)

    member private this.EndUserComment() =
        match this._userCommentCount, this._userCommentOffset with
        | ValueSome n, ValueSome i when i < n -> this.Read(StreamPosition.UserCommentLength, 4)
        | ValueSome n, ValueSome i when i = n -> this.EndMetadataBlockData()
        | _, _ -> readerEx "Expected values for UserCommentCount and UserCommentOffset"

    // TODO: Validate for CD-DA; offset % 588 = 0
    member private this.ReadCueSheetTrackIndexOffset() =
        this.Read(StreamPosition.TrackIndexOffset, 8)

    member private this.ReadCueSheetTrackIndexPointNumber() =
        let local = this._buffer[this._consumed]

        // TODO: Validate first index
        // TODO: Validate uniqueness

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._position <- StreamPosition.IndexPointNumber

    member private this.ReadCueSheetTrackIndexReserved() =
        let local = this._buffer.Slice(this._consumed, 3)

        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        this._value <- local
        this._consumed <- this._consumed + 3
        this._position <- StreamPosition.TrackIndexReserved

    // TODO: Validate for CD-DA; offset % 588 = 0
    member private this.ReadCueSheetTrackOffset() =
        this.Read(StreamPosition.TrackOffset, 8)

    member private this.ReadCueSheetTrackNumber() =
        let local = this._buffer[this._consumed]

        // This may be a soft requirement...
        if local = 0uy then
            readerEx "Invalid cue sheet track number"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._position <- StreamPosition.TrackNumber

    member private this.ReadCueSheetTrackType() =
        let local = this._buffer[this._consumed] &&& 0x80uy
        this._value <- ReadOnlySpan<byte>(&local)
        this._position <- StreamPosition.TrackType

    member private this.ReadCueSheetTrackPreEmphasis() =
        let local = this._buffer[this._consumed] &&& 0x40uy
        this._value <- ReadOnlySpan<byte>(&local)
        this._position <- StreamPosition.PreEmphasis

    member private this.ReadCueSheetTrackReserved() =
        let local = this._buffer.Slice(this._consumed, 14)

        // TODO: This doesn't account for the leading 6 bits
        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        // TODO: How to represent this is offset by two bits?
        this._value <- local
        this._consumed <- this._consumed + 14
        this._position <- StreamPosition.TrackReserved

    // TODO: Validate ASCII characters
    // TODO: Validate CD-DA 13 digit number + 115 NUL bytes
    member private this.ReadCueSheetCatalogNumber() =
        this.Read(StreamPosition.MediaCatalogNumber, 128)

    // TODO: Do we need any validation here?
    member private this.ReadCueSheetLeadInSamplesNumber() =
        this.Read(StreamPosition.NumberOfLeadInSamples, 8)

    member private this.ReadIsCueSheetCompactDisc() =
        let local = this._buffer[this._consumed] >>> 7
        this._value <- ReadOnlySpan<byte>(&local)
        this._position <- StreamPosition.IsCueSheetCompactDisc

    member private this.ReadCueSheetReserved() =
        let local = this._buffer.Slice(this._consumed, 259)

        // TODO: This doesn't account for the leading 7 bits
        if local.Slice(1).IndexOfAnyExcept(0uy) <> -1 then
            readerEx "Reserved block contains invalid bytes"

        // TODO: How to represent this is offset by one bit?
        this._value <- local
        this._consumed <- this._consumed + 259
        this._position <- StreamPosition.CueSheetReserved

    member private this.ReadCueSheetNumberOfTracks() =
        let local = this._buffer[this._consumed]

        // TODO: Validate for CD-DA num < 100
        if local < 1uy then
            readerEx "Must have at least one cue sheet track"

        this._value <- ReadOnlySpan<byte>(&local)
        this._consumed <- this._consumed + 1
        this._position <- StreamPosition.NumberOfTracks
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
        this._position <- StreamPosition.NumberOfTrackIndexPoints
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
        this._position <- StreamPosition.MimeType

    member private this.ReadPictureDescription() =
        if this._value.Length < 4 then
            readerEx "Picture description length is too short"

        let length = BinaryPrimitives.ReadUInt32BigEndian(this._value) |> int

        let local = this._buffer.Slice(this._consumed, length)

        this._value <- local
        this._consumed <- this._consumed + length
        this._position <- StreamPosition.PictureDescription

    member private this.ReadPictureData() =
        if this._value.Length < 4 then
            readerEx "Picture data length is too short"

        let length = BinaryPrimitives.ReadUInt32BigEndian(this._value) |> int

        let local = this._buffer.Slice(this._consumed, length)

        this._value <- local
        this._consumed <- this._consumed + length
        this._position <- StreamPosition.PictureData

    member this.GetLastMetadataBlockFlag() =
        if this._position <> StreamPosition.LastMetadataBlockFlag then
            readerEx "Expected reader to be positioned at LastMetadataBlockFlag"

        this._value[0] >>> 7 = 0x1uy

    member this.GetMetadataBlockType() =
        if this._position <> StreamPosition.MetadataBlockType then
            readerEx "Expected reader to be positioned at MetadataBlockType"

        let blockType = this._value[0] &&& 0x7Fuy |> int
        enum<BlockType> blockType

    member this.GetDataBlockLength() =
        if this._position <> StreamPosition.DataBlockLength then
            readerEx "Expected reader to be positioned at DataBlockLength"

        readUInt32 this._value

    member this.GetMinimumBlockSize() =
        if this._position <> StreamPosition.MinimumBlockSize then
            readerEx "Expected reader to be positioned at MinimumBlockSize"

        BinaryPrimitives.ReadUInt16BigEndian(this._value)

    member this.GetMaximumBlockSize() =
        if this._position <> StreamPosition.MaximumBlockSize then
            readerEx "Expected reader to be positioned at MaximumBlockSize"

        BinaryPrimitives.ReadUInt16BigEndian(this._value)

    member this.GetMinimumFrameSize() =
        if this._position <> StreamPosition.MinimumFrameSize then
            readerEx "Expected reader to be positioned at MinimumFrameSize"

        readUInt32 this._value

    member this.GetMaximumFrameSize() =
        if this._position <> StreamPosition.MaximumFrameSize then
            readerEx "Expected reader to be positioned at MaximumFrameSize"

        readUInt32 this._value

    member this.GetSampleRate() =
        if this._position <> StreamPosition.StreamInfoSampleRate then
            readerEx "Expected reader to be positioned at StreamInfoSampleRate"

        readUInt32 this._value >>> 4

    member this.GetChannels() =
        if this._position <> StreamPosition.NumberOfChannels then
            readerEx "Expected reader to be positioned at NumberOfChannels"

        uint16 (this._value[0] &&& 0x0Euy >>> 1) + 1us

    member this.GetBitsPerSample() =
        if this._position <> StreamPosition.BitsPerSample then
            readerEx "Expected reader to be positioned at BitsPerSample"

        let a = uint16 (this._value[0] &&& 0x01uy) <<< 13
        let b = uint16 (this._value[1]) >>> 4
        a + b + 1us

    member this.GetTotalSamples() =
        if this._position <> StreamPosition.TotalSamples then
            readerEx "Expected reader to be positioned at TotalSamples"

        let a = uint64 (this._value[0] &&& 0x0Fuy) <<< 8 * 4
        let b = uint64 this._value[1] <<< 8 * 3
        let c = uint64 this._value[2] <<< 8 * 2
        let d = uint64 this._value[3] <<< 8
        let e = uint64 this._value[4]
        a + b + c + d + e

    member this.GetMd5Signature() =
        if this._position <> StreamPosition.Md5Signature then
            readerEx "Expected reader to be positioned at Md5Signature"

        Convert.ToHexString(this._value)

    member this.GetSeekPointSampleNumber() =
        if this._position <> StreamPosition.SeekPointSampleNumber then
            readerEx "Expected reader to be positioned at SeekPointSampleNumber"

        BinaryPrimitives.ReadUInt64BigEndian(this._value)

    member this.GetSeekPointOffset() =
        if this._position <> StreamPosition.SeekPointOffset then
            readerEx "Expected reader to be positioned at SeekPointOffset"

        BinaryPrimitives.ReadUInt64BigEndian(this._value)

    member this.GetSeekPointNumberOfSamples() =
        if this._position <> StreamPosition.NumberOfSamples then
            readerEx "Expected reader to be positioned at NumberOfSamples"

        BinaryPrimitives.ReadUInt16BigEndian(this._value)

    member this.GetVendorLength() =
        if this._position <> StreamPosition.VendorLength then
            readerEx "Expected reader to be positioned at VendorLength"

        BinaryPrimitives.ReadUInt32LittleEndian(this._value)

    member this.GetVendorString() =
        if this._position <> StreamPosition.VendorString then
            readerEx "Expected reader to be positioned at VendorString"

        Encoding.UTF8.GetString(this._value)

    member this.GetUserCommentListLength() =
        if this._position <> StreamPosition.UserCommentListLength then
            readerEx "Expected reader to be positioned at UserCommentListLength"

        BinaryPrimitives.ReadUInt32LittleEndian(this._value)

    member this.GetUserCommentLength() =
        if this._position <> StreamPosition.UserCommentLength then
            readerEx "Expected reader to be positioned at UserCommentLength"

        BinaryPrimitives.ReadUInt32LittleEndian(this._value)

    member this.GetUserComment() =
        if this._position <> StreamPosition.UserComment then
            readerEx "Expected reader to be positioned at UserComment"

        Encoding.UTF8.GetString(this._value)

    member this.GetPictureType() =
        if this._position <> StreamPosition.PictureType then
            readerEx "Expected reader to be positioned at PictureType"

        let pictureType = BinaryPrimitives.ReadUInt32BigEndian(this._value)
        enum<PictureType> (int pictureType)

    member this.GetMimeTypeLength() =
        if this._position <> StreamPosition.MimeTypeLength then
            readerEx "Expected reader to be positioned at MimeTypeLength"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.GetMimeType() =
        if this._position <> StreamPosition.MimeType then
            readerEx "Expected reader to be positioned at MimeType"

        Encoding.UTF8.GetString(this._value)

    member this.GetPictureDescriptionLength() =
        if this._position <> StreamPosition.PictureDescriptionLength then
            readerEx "Expected reader to be positioned at PictureDescriptionLength"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.GetPictureDescription() =
        if this._position <> StreamPosition.PictureDescription then
            readerEx "Expected reader to be positioned at PictureDescription"

        Encoding.UTF8.GetString(this._value)

    member this.GetPictureWidth() =
        if this._position <> StreamPosition.PictureWidth then
            readerEx "Expected reader to be positioned at PictureWidth"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.GetPictureHeight() =
        if this._position <> StreamPosition.PictureHeight then
            readerEx "Expected reader to be positioned at PictureHeight"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.GetPictureColorDepth() =
        if this._position <> StreamPosition.PictureColorDepth then
            readerEx "Expected reader to be positioned at PictureColorDepth"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.GetPictureNumberOfColors() =
        if this._position <> StreamPosition.PictureNumberOfColors then
            readerEx "Expected reader to be positioned at PictureNumberOfColors"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.GetPictureDataLength() =
        if this._position <> StreamPosition.PictureDataLength then
            readerEx "Expected reader to be positioned at PictureDataLength"

        BinaryPrimitives.ReadUInt32BigEndian(this._value)

    member this.GetPictureData() =
        if this._position <> StreamPosition.PictureData then
            readerEx "Expected reader to be positioned at PictureData"

        this._value
