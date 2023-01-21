namespace Safir.Audio.Benchmarks

#nowarn "3391"

open System
open System.IO
open BenchmarkDotNet.Attributes
open Safir.Audio

[<MemoryDiagnoser>]
type VorbisBench() =
    let bytes = File.ReadAllBytes("NEFFEX-Flirt.flac")

    [<Benchmark>]
    member this.VorbisCommentHeader() =
        let span: ReadOnlySpan<byte> = bytes
        Vorbis.readVorbisCommentHeader (span.Slice(338)) 294

    [<Benchmark>]
    member this.VorbisComment() =
        let span: ReadOnlySpan<byte> = bytes
        Vorbis.readVorbisComment (span.Slice(382)) 11
