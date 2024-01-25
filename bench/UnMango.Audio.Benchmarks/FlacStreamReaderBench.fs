namespace UnMango.Audio.Benchmarks

open System.IO
open BenchmarkDotNet.Attributes
open UnMango.Audio.Flac

[<MemoryDiagnoser>]
type FlacStreamReaderBench() =
    let bytes = File.ReadAllBytes("NEFFEX-Flirt.flac")

    [<Benchmark>]
    member this.ReadMetadata() =
        let reader = FlacStreamReader(bytes)

        for i = 0 to 117 do
            reader.Read()
            |> ignore
