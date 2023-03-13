# UnMango .NET Audio Metadata Library

A .NET library for parsing audio metadata. Written in F#, but intended to be used by either F# or C#.

The primary package is `UnMango.Audio` which will contain all of the metadata functionality.

## Project goals

- Read metadata
  - Flac
  - Vorbis
  - Mp3
  - Wav
- Write metadata
  - Flac
  - Vorbis
  - Mp3
  - Wav
- Read from file
- Read from `Stream`
- Read from `ReadOnlySpan<byte>`
- Low allocation
- Low and high level read/write APIs
- DI friendly API
- Idiomatic F# and C# APIs

## Possible features

- Additional audio formats
- Read partial `Stream`
- Read stream data

## Contributing

Check out the documentation in the [Contributing](./docs/Contributing.md) file.
