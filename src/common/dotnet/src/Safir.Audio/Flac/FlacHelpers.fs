[<AutoOpen>]
module Safir.Audio.Flac.FlacHelpers

let readerEx m = raise (FlacStreamReaderException(m))
