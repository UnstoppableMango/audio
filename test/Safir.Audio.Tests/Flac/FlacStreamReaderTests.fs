[<Xunit.Trait("Category", "Unit")>]
module Safir.Audio.Tests.Flac.FlacStreamReaderTests

open Safir.Audio.Flac
open Xunit

[<Fact>]
let ``Initializes with no value`` () =
    let reader = FlacStreamReader()

    Assert.Equal(FlacValue.None, reader.ValueType)
    Assert.Equal(0, reader.Value.Length)
