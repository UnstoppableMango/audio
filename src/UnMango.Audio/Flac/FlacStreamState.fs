namespace UnMango.Audio.Flac

open System.Runtime.CompilerServices
open UnMango.Audio

// TODO: More fine-grained state functions
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
      Position: FlacValue }

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
          Position = FlacValue.None }

    static member StreamInfoHeader = { FlacStreamState.Empty with Position = FlacValue.Marker }

    static member After(position: FlacValue) =
        match position with // TODO: Throw on positions that require more state
        | FlacValue.DataBlockLength -> flacEx "More state is required. Use FlacStreamState.AfterBlockHeader"
        | FlacValue.Md5Signature
        | FlacValue.NumberOfSamples
        | FlacValue.UserComment
        | FlacValue.PictureData -> flacEx "More state is required. Use FlacStreamState.AfterBlockData"
        | _ -> { FlacStreamState.Empty with Position = position }

    static member AfterBlockHeader(lastBlock: bool, length: uint32, blockType: BlockType, state: FlacStreamState) =
        { state with
            Position = FlacValue.DataBlockLength
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
                Position = FlacValue.PictureData // TODO: More generic position
                SeekPointCount = ValueSome 0u // TODO: Will zero values here bite us?
                SeekPointOffset = ValueSome 0u
                UserCommentCount = ValueSome 0u
                UserCommentOffset = ValueSome 0u
                LastMetadataBlock = ValueSome lastBlock }

        FlacStreamState.AfterBlockData(lastBlock, state)

    static member AfterBlockData() = FlacStreamState.AfterBlockData(false)

    static member AfterLastBlock() = FlacStreamState.AfterBlockData(true)
