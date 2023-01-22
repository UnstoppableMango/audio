using System.Text;

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

        Assert.Equal("fLaC", Encoding.ASCII.GetString(res));
    }

    [Fact]
    public void MagicNumber_Mp3File()
    {
        var file = File.ReadAllBytes($"{FileName}.mp3");

        Assert.Throws<InvalidOperationException>(() => FlacCs.ReadMagic(file));
    }

    [Fact]
    public void ReadMetadataBlockHeader_StreamInfo()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[4..]);

        Assert.Equal(34, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.StreamInfo, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockHeader_Padding()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[80_524..]);

        Assert.Equal(16384, res.Length);
        Assert.True(res.LastBlock);
        Assert.Equal(BlockType.Padding, res.BlockType);
    }

    [Fact(Skip = "No test files with application meta")]
    public void ReadMetadataBlockHeader_Application()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var application = FlacCs.ReadMetadataBlockApplication(file[69_420..], 69);

        Assert.Equal(69, application.Id);
        Assert.Equal(420, application.Data.Length);
    }

    [Fact]
    public void ReadMetadataBlockHeader_SeekTable()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[42..]);

        Assert.Equal(288, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.SeekTable, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockHeader_VorbisComment()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[334..]);

        Assert.Equal(294, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.VorbisComment, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockHeader_Picture()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockHeader(file[632..]);

        Assert.Equal(79_888, res.Length);
        Assert.False(res.LastBlock);
        Assert.Equal(BlockType.Picture, res.BlockType);
    }

    [Fact]
    public void ReadMetadataBlockStreamInfo()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var streamInfo = FlacCs.ReadMetadataBlockStreamInfo(file[8..]);

        Assert.Equal(4096u, streamInfo.MinBlockSize);
        Assert.Equal(4096u, streamInfo.MaxBlockSize);
        Assert.Equal(1781u, streamInfo.MinFrameSize);
        Assert.Equal(14163u, streamInfo.MaxFrameSize);
        Assert.Equal(44100u, streamInfo.SampleRate);
        Assert.Equal(2u, streamInfo.Channels);
        Assert.Equal(16u, streamInfo.BitsPerSample);
        Assert.Equal(7028438u, streamInfo.TotalSamples);

        var md5 = Flac.readMd5Signature(streamInfo.Md5Signature);

        Assert.Equal("3c16b5b7186537d6823c7be62fe8c661", md5, ignoreCase: true);
    }

    [Fact]
    public void ReadMetadataBlockPadding()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockPadding(file[80_528..], 16_384);

        Assert.NotNull(res);
        Assert.Equal(16384, res.Padding);
    }

    [Fact]
    public void ReadMetadataBlockSeekTable()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var res = FlacCs.ReadMetadataBlockSeekTable(file[46..], 288);

        Assert.Equal(16, res.Count);

        var first = Flac.readSeekPoint(res.SeekPoints[..]);
        Assert.Equal(0UL, first.SampleNumber);
        Assert.Equal(0UL, first.StreamOffset);
        Assert.Equal(4096, first.FrameSamples);

        var second = Flac.readSeekPoint(res.SeekPoints[18..]);
        Assert.Equal(438_272UL, second.SampleNumber);
        Assert.Equal(532_963UL, second.StreamOffset);
        Assert.Equal(4096, second.FrameSamples);

        var third = Flac.readSeekPoint(res.SeekPoints[36..]);
        Assert.Equal(880_640UL, third.SampleNumber);
        Assert.Equal(1_732_918UL, third.StreamOffset);
        Assert.Equal(4096, third.FrameSamples);

        var last = Flac.readSeekPoint(res.SeekPoints[(15 * 18)..]);
        Assert.Equal(6_610_944UL, last.SampleNumber);
        Assert.Equal(1_8894_059UL, last.StreamOffset);
        Assert.Equal(4096, last.FrameSamples);
    }

    [Fact]
    public void ReadMetadataBlockVorbisComment()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var vorbisComment = FlacCs.ReadMetadataBlockVorbisComment(file[338..], 294);

        Assert.True("reference libFLAC 1.3.2 20170101"u8.SequenceEqual(vorbisComment.VendorString));
        Assert.Equal(14u, vorbisComment.UserCommentListLength);

        var title = comments[0];
        Assert.Equal("TITLE"u8.ToArray(), title.Name);
        Assert.Equal("Flirt"u8.ToArray(), title.Value);

        var artist = comments[1];
        Assert.Equal("ARTIST"u8.ToArray(), artist.Name);
        Assert.Equal("NEFFEX"u8.ToArray(), artist.Value);

        var album = comments[2];
        Assert.Equal("ALBUM"u8.ToArray(), album.Name);
        Assert.Equal("Flirt"u8.ToArray(), album.Value);

        var albumArtist = comments[3];
        Assert.Equal("ALBUMARTIST"u8.ToArray(), albumArtist.Name);
        Assert.Equal("NEFFEX"u8.ToArray(), albumArtist.Value);

        var trackNumber = comments[4];
        Assert.Equal("TRACKNUMBER"u8.ToArray(), trackNumber.Name);
        Assert.Equal("1"u8.ToArray(), trackNumber.Value);

        var discNumber = comments[5];
        Assert.Equal("DISCNUMBER"u8.ToArray(), discNumber.Name);
        Assert.Equal("1"u8.ToArray(), discNumber.Value);

        var genre1 = comments[6];
        Assert.Equal("GENRE"u8.ToArray(), genre1.Name);
        Assert.Equal("Electro"u8.ToArray(), genre1.Value);

        var genre2 = comments[7];
        Assert.Equal("GENRE"u8.ToArray(), genre2.Name);
        Assert.Equal("Dance"u8.ToArray(), genre2.Value);

        var genre3 = comments[8];
        Assert.Equal("GENRE"u8.ToArray(), genre3.Name);
        Assert.Equal("Pop"u8.ToArray(), genre3.Value);

        var date = comments[9];
        Assert.Equal("DATE"u8.ToArray(), date.Name);
        Assert.Equal("2017-10-11"u8.ToArray(), date.Value);

        var lengthComment = comments[10];
        Assert.Equal("LENGTH"u8.ToArray(), lengthComment.Name);
        Assert.Equal("159000"u8.ToArray(), lengthComment.Value);

        var publisher = comments[11];
        Assert.Equal("PUBLISHER"u8.ToArray(), publisher.Name);
        Assert.Equal("Burning Boat"u8.ToArray(), publisher.Value);

        var isrc = comments[12];
        Assert.Equal("ISRC"u8.ToArray(), isrc.Name);
        Assert.Equal("TCADH1750644"u8.ToArray(), isrc.Value);

        var barcode = comments[13];
        Assert.Equal("BARCODE"u8.ToArray(), barcode.Name);
        Assert.Equal("859723487007"u8.ToArray(), barcode.Value);
    }

    [Fact]
    public void ReadMetadataBlockPicture()
    {
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

        var picture = FlacCs.ReadMetadataBlockPicture(file[636..], 79_888);

        Assert.Equal(PictureType.FrontCover, picture.Type);
        Assert.True("image/jpeg"u8.SequenceEqual(picture.MimeType));
        Assert.Equal(0, picture.Description.Length);
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
        ReadOnlySpan<byte> file = File.ReadAllBytes($"{FileName}.flac");

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
