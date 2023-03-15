[<Xunit.Trait("Category", "Unit")>]
module UnMango.Audio.Tests.Flac.FLacStreamWriterTests

open System.Buffers
open UnMango.Audio.Flac
open Xunit

[<Fact>]
let ``WriteComment writes comment`` () =
    let output = ArrayBufferWriter(13)
    let writer = FlacStreamWriter(output)

    let result = writer.WriteComment("TITLE", "Yee")

    Assert.True(result)
    Assert.Equal(13, output.WrittenCount)

    let state = { FlacStreamState.Empty with Position = FlacValue.UserCommentListLength }
    let mutable reader = FlacStreamReader(output.WrittenSpan, state)

    Assert.Equal(9u, reader.ReadAsUserCommentLength())
    Assert.Equal("TITLE=Yee", reader.ReadAsUserComment())
