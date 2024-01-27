module Tests

open System
open System.Buffers
open System.IO
open System.IO.Pipelines
open UnMango.Audio.Flac
open Xunit
open FsCheck.Xunit

[<Property>]
let ``Should parse magic string`` () =
    let buffer = ReadOnlySequence<byte>("fLaC"B)
    let struct (_, actual, _) = Flac.parse buffer Initial
    FlacValue.Magic = actual

[<Fact>]
let ``Acceptance test`` () = async {
    use stream = File.OpenRead("NEFFEX-Flirt.flac")
    let reader = PipeReader.Create(stream)
    let handle v = async { printfn $"Test %A{v}" }
    do! Flac.Read(reader, handle) |> Async.Ignore
}
