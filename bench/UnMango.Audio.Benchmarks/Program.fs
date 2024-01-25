open BenchmarkDotNet.Running

type Marker =
    class
    end

[<EntryPoint>]
let main argv =
    BenchmarkSwitcher
        .FromAssembly(typeof<Marker>.Assembly)
        .Run(argv)
    |> ignore

    0
