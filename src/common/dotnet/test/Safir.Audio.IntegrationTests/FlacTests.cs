namespace Safir.Audio.IntegrationTests;

[Trait("Category", "Unit")]
public class FlacTests
{
    private const string FileName = "NEFFEX-Flirt";

    [Fact]
    public void MagicNumber_FlacFile()
    {
        var file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMagic(file);

        Assert.Equal("fLaC", res);
    }

    [Fact]
    public void MagicNumber_Mp3File()
    {
        var file = File.ReadAllBytes($"{FileName}.mp3");

        var res = FlacCs.ReadMagic(file);

        Assert.Null(res);
    }

    [Fact]
    public void ReadMetadataBlockHeader_StreamInfo()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[4..]);

        Assert.Equal(34, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.StreamInfo, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockHeader_Padding()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[80_524..]);

        Assert.Equal(16384, res.Length);
        Assert.True(res.LastBlock);
        Assert.Equal(BlockType.Padding, res.BlockType);
    }

    [Fact(Skip = "No test files with application meta")]
    public void ReadMetadataBlockHeader_Application()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var application = FlacCs.ReadMetadataBlockApplication(file[69_420..], 69);

        Assert.Equal(69, application.Id);
        Assert.Equal(420, application.Data.Length);
    }

    [Fact]
    public void ReadMetadataBlockHeader_VorbisComment()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[334..]);

        Assert.Equal(294, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.VorbisComment, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockHeader_Picture()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[632..]);

        Assert.Equal(79_888, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.Picture, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockStreamInfo()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var streamInfo = FlacCs.ReadMetadataBlockStreamInfo(file[8..]);

        Assert.Equal(4096u, streamInfo.MinBlockSize);
        Assert.Equal(4096u, streamInfo.MaxBlockSize);
        Assert.Equal(1781u, streamInfo.MinFrameSize);
        Assert.Equal(14163u, streamInfo.MaxFrameSize);
        Assert.Equal(44100u, streamInfo.SampleRate);
        Assert.Equal(2u, streamInfo.Channels);
        Assert.Equal(16u, streamInfo.BitsPerSample);
        Assert.Equal(7028438u, streamInfo.TotalSamples);
        Assert.Equal("3c16b5b7186537d6823c7be62fe8c661", streamInfo.Md5Signature);
    }

    [Fact]
    public void ReadMetadataBlockPadding()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockPadding(file[80_528..], 16384);

        Assert.NotNull(res);
        Assert.Equal(16384, res.Padding);
    }

    [Fact]
    public void ReadMetadataBlockSeekTable()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockSeekTable(file[46..], 288);

        var list = res.SeekPoints.ToList();
        Assert.Equal(16, list.Count);

        var first = list[0];
        Assert.Equal(0UL, first.SampleNumber);
        Assert.Equal(0UL, first.StreamOffset);
        Assert.Equal(4096, first.FrameSamples);

        var second = list[1];
        Assert.Equal(438_272UL, second.SampleNumber);
        Assert.Equal(532_963UL, second.StreamOffset);
        Assert.Equal(4096, second.FrameSamples);

        var third = list[2];
        Assert.Equal(880_640UL, third.SampleNumber);
        Assert.Equal(1_732_918UL, third.StreamOffset);
        Assert.Equal(4096, third.FrameSamples);

        var last = list.Last();
        Assert.Equal(6_610_944UL, last.SampleNumber);
        Assert.Equal(1_8894_059UL, last.StreamOffset);
        Assert.Equal(4096, last.FrameSamples);
    }

    [Fact]
    public void ReadMetadataBlockVorbisComment()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var vorbisComment = FlacCs.ReadMetadataBlockVorbisComment(file[338..], 294);

        Assert.NotNull(vorbisComment);

        var comments = vorbisComment.UserComments.ToList();
        Assert.Equal(14, comments.Count);

        var title = Assert.IsType<TitleComment>(comments[0]);
        Assert.Equal("Flirt", title.Value);

        var artist = Assert.IsType<ArtistComment>(comments[1]);
        Assert.Equal("NEFFEX", artist.Value);

        var album = Assert.IsType<AlbumComment>(comments[2]);
        Assert.Equal("Flirt", album.Value);

        var albumArtist = Assert.IsType<OtherComment>(comments[3]);
        Assert.Equal("ALBUMARTIST", albumArtist.Name);
        Assert.Equal("NEFFEX", albumArtist.Value);

        var trackNumber = Assert.IsType<TrackNumberComment>(comments[4]);
        Assert.Equal("1", trackNumber.Value);

        var discNumber = Assert.IsType<OtherComment>(comments[5]);
        Assert.Equal("DISCNUMBER", discNumber.Name);
        Assert.Equal("1", discNumber.Value);

        var genre1 = Assert.IsType<GenreComment>(comments[6]);
        Assert.Equal("Electro", genre1.Value);

        var genre2 = Assert.IsType<GenreComment>(comments[7]);
        Assert.Equal("Dance", genre2.Value);

        var genre3 = Assert.IsType<GenreComment>(comments[8]);
        Assert.Equal("Pop", genre3.Value);

        var date = Assert.IsType<DateComment>(comments[9]);
        Assert.Equal("2017-10-11", date.Value);

        var lengthComment = Assert.IsType<OtherComment>(comments[10]);
        Assert.Equal("LENGTH", lengthComment.Name);
        Assert.Equal("159000", lengthComment.Value);

        var publisher = Assert.IsType<OtherComment>(comments[11]);
        Assert.Equal("PUBLISHER", publisher.Name);
        Assert.Equal("Burning Boat", publisher.Value);

        var isrc = Assert.IsType<IsrcComment>(comments[12]);
        Assert.Equal("TCADH1750644", isrc.Value);

        var barcode = Assert.IsType<OtherComment>(comments[13]);
        Assert.Equal("BARCODE", barcode.Name);
        Assert.Equal("859723487007", barcode.Value);
    }

    [Fact]
    public void ReadMetadataBlockPicture()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var picture = FlacCs.ReadMetadataBlockPicture(file[636..], 79_888);

        Assert.Equal(PictureType.FrontCover, picture.Type);
        Assert.Equal("image/jpeg", picture.MimeType);
        Assert.Equal(string.Empty, picture.Description);
        Assert.Equal(800u, picture.Width);
        Assert.Equal(800u, picture.Height);
        Assert.Equal(24u, picture.Depth);
        Assert.Equal(0u, picture.Colors);
        Assert.Equal(79_846u, picture.DataLength);
        Assert.Equal(79_846, picture.Data.Length);
    }

    [Fact]
    public void ReadFlacStream_FlacFile()
    {
        Span<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadFlacStream(file);

        Assert.NotEmpty(res.Metadata);

        var metadata = res.Metadata.ToList();
        Assert.Equal(5, metadata.Count);

        Assert.IsType<MetadataBlockStreamInfoCs>(metadata[0].Data);
        Assert.IsType<MetadataBlockSeekTableCs>(metadata[1].Data);
        Assert.IsType<MetadataBlockVorbisCommentCs>(metadata[2].Data);
        Assert.IsType<MetadataBlockPictureCs>(metadata[3].Data);
        Assert.IsType<MetadataBlockPaddingCs>(metadata[4].Data);
    }
}
