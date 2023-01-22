namespace Safir.Audio.Benchmarks

#nowarn "3391"

open System
open System.IO
open BenchmarkDotNet.Attributes
open Safir.Audio

[<MemoryDiagnoser>]
type FlacBench() =
    let bytes = File.ReadAllBytes("NEFFEX-Flirt.flac")

    [<Benchmark>]
    member this.Magic() = Flac.readMagic bytes

    [<Benchmark>]
    member this.StreamInfoHeader() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockHeader (span.Slice(4))

    [<Benchmark>]
    member this.StreamInfo() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockStreamInfo (span.Slice(8))

    [<Benchmark>]
    member this.SeekTableHeader() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockHeader (span.Slice(42))

    [<Benchmark>]
    member this.SeekTable() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockSeekTable (span.Slice(46)) 288

    [<Benchmark>]
    member this.VorbisCommentHeader() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockHeader (span.Slice(334))

    [<Benchmark>]
    member this.VorbisComment() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockVorbisComment (span.Slice(338)) 294

    [<Benchmark>]
    member this.PictureHeader() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockHeader (span.Slice(632))

    [<Benchmark>]
    member this.Picture() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockPicture (span.Slice(636)) 79_888

    [<Benchmark>]
    member this.PaddingHeader() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockHeader (span.Slice(80_524))

    [<Benchmark>]
    member this.Padding() =
        let span: ReadOnlySpan<byte> = bytes
        Flac.readMetadataBlockPadding (span.Slice(80_528)) 16_384

    [<Benchmark>]
    member this.FlacStream() = Flac.readStream bytes
