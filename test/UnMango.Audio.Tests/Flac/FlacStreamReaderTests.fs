[<Xunit.Trait("Category", "Unit")>]
module UnMango.Audio.Tests.Flac.FlacStreamReaderTests

open UnMango.Audio.Flac
open Xunit

[<Fact>]
let ``Initializes with no value`` () =
    let reader = FlacStreamReader()

    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)
