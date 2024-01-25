namespace UnMango.Audio.Benchmarks

#nowarn "3391"

open System
open System.IO
open BenchmarkDotNet.Attributes
open UnMango.Audio
open UnMango.Audio.Flac

[<MemoryDiagnoser>]
type FlacBench() =
    let bytes = File.ReadAllBytes("NEFFEX-Flirt.flac")

    [<Benchmark>]
    member this.Magic() =
        let mutable reader = FlacStreamReader bytes
        Flac.readMagic &reader

    [<Benchmark>]
    member this.StreamInfoHeader() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(4), FlacStreamState.StreamInfoHeader)

        Flac.readMetadataBlockHeader &reader

    [<Benchmark>]
    member this.StreamInfo() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(8), FlacStreamState.AfterBlockHeader(false, 34u, BlockType.StreamInfo))

        Flac.readMetadataBlockStreamInfo &reader

    [<Benchmark>]
    member this.SeekTableHeader() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(42), FlacStreamState.AfterBlockData())

        Flac.readMetadataBlockHeader &reader

    [<Benchmark>]
    member this.SeekTable() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(46), FlacStreamState.AfterBlockHeader(false, 288u, BlockType.SeekTable))

        Flac.readMetadataBlockSeekTable &reader

    [<Benchmark>]
    member this.VorbisCommentHeader() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(334), FlacStreamState.AfterBlockData())

        Flac.readMetadataBlockHeader &reader

    [<Benchmark>]
    member this.VorbisComment() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(338), FlacStreamState.AfterBlockHeader(false, 294u, BlockType.VorbisComment))

        Flac.readMetadataBlockVorbisComment &reader

    [<Benchmark>]
    member this.PictureHeader() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(632), FlacStreamState.AfterBlockData())

        Flac.readMetadataBlockHeader &reader

    [<Benchmark>]
    member this.Picture() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(636), FlacStreamState.AfterBlockHeader(false, 79_888u, BlockType.Picture))

        Flac.readMetadataBlockPicture &reader

    [<Benchmark>]
    member this.PaddingHeader() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(80_524), FlacStreamState.AfterBlockData())

        Flac.readMetadataBlockHeader &reader

    [<Benchmark>]
    member this.Padding() =
        let span: ReadOnlySpan<byte> = bytes

        let mutable reader =
            FlacStreamReader(span.Slice(80_528), FlacStreamState.AfterBlockHeader(true, 16_384u, BlockType.Padding))

        Flac.readMetadataBlockPadding &reader

    [<Benchmark>]
    member this.FlacStream() =
        let mutable reader = FlacStreamReader bytes
        Flac.readStream &reader
