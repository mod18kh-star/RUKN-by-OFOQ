using System.Buffers.Binary;
using OFOQ.Market.Api.Security.Uploads;

namespace OFOQ.Market.Api.Tests.Security;

public sealed class PublicImageUploadValidatorTests
{
    [Fact]
    public void RejectsSvgEvenWhenDeclaredAsImage()
    {
        var content =
            "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8
                .ToArray();

        var valid =
            PublicImageUploadValidator.TryValidate(
                "logo.svg",
                "image/svg+xml",
                content,
                out _,
                out _);

        Assert.False(
            valid);
    }

    [Fact]
    public void RejectsSpoofedPng()
    {
        var content =
            "<html><script>alert(1)</script></html>"u8
                .ToArray();

        var valid =
            PublicImageUploadValidator.TryValidate(
                "image.png",
                "image/png",
                content,
                out _,
                out _);

        Assert.False(
            valid);
    }

    [Fact]
    public void AcceptsSafePngHeader()
    {
        var content =
            CreatePng(
                width: 1200,
                height: 800);

        var valid =
            PublicImageUploadValidator.TryValidate(
                "image.png",
                "image/png",
                content,
                out var extension,
                out var contentType);

        Assert.True(
            valid);

        Assert.Equal(
            ".png",
            extension);

        Assert.Equal(
            "image/png",
            contentType);
    }

    [Fact]
    public void RejectsExcessivePngPixelCount()
    {
        var content =
            CreatePng(
                width: 8192,
                height: 8192);

        var valid =
            PublicImageUploadValidator.TryValidate(
                "image.png",
                "image/png",
                content,
                out _,
                out _);

        Assert.False(
            valid);
    }

    [Fact]
    public void AcceptsSafeJpegHeader()
    {
        var content =
            CreateJpeg(
                width: 1600,
                height: 900);

        var valid =
            PublicImageUploadValidator.TryValidate(
                "image.jpeg",
                "image/jpeg",
                content,
                out var extension,
                out var contentType);

        Assert.True(
            valid);

        Assert.Equal(
            ".jpg",
            extension);

        Assert.Equal(
            "image/jpeg",
            contentType);
    }

    [Fact]
    public void AcceptsSafeWebPVp8XHeader()
    {
        var content =
            CreateWebPVp8X(
                width: 1920,
                height: 1080);

        var valid =
            PublicImageUploadValidator.TryValidate(
                "image.webp",
                "image/webp",
                content,
                out var extension,
                out var contentType);

        Assert.True(
            valid);

        Assert.Equal(
            ".webp",
            extension);

        Assert.Equal(
            "image/webp",
            contentType);
    }

    private static byte[] CreatePng(
        uint width,
        uint height)
    {
        var bytes =
            new byte[24];

        new byte[]
        {
            137, 80, 78, 71, 13, 10, 26, 10
        }.CopyTo(
            bytes,
            0);

        "IHDR"u8.CopyTo(
            bytes.AsSpan(
                12,
                4));

        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(
                16,
                4),
            width);

        BinaryPrimitives.WriteUInt32BigEndian(
            bytes.AsSpan(
                20,
                4),
            height);

        return bytes;
    }

    private static byte[] CreateJpeg(
        ushort width,
        ushort height)
    {
        var bytes =
            new byte[41];

        bytes[0] = 0xFF;
        bytes[1] = 0xD8;

        bytes[2] = 0xFF;
        bytes[3] = 0xE0;
        bytes[4] = 0x00;
        bytes[5] = 0x10;

        bytes[20] = 0xFF;
        bytes[21] = 0xC0;
        bytes[22] = 0x00;
        bytes[23] = 0x11;
        bytes[24] = 0x08;

        BinaryPrimitives.WriteUInt16BigEndian(
            bytes.AsSpan(
                25,
                2),
            height);

        BinaryPrimitives.WriteUInt16BigEndian(
            bytes.AsSpan(
                27,
                2),
            width);

        bytes[^2] = 0xFF;
        bytes[^1] = 0xD9;

        return bytes;
    }

    private static byte[] CreateWebPVp8X(
        uint width,
        uint height)
    {
        var bytes =
            new byte[30];

        "RIFF"u8.CopyTo(
            bytes.AsSpan(
                0,
                4));

        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(
                4,
                4),
            22);

        "WEBP"u8.CopyTo(
            bytes.AsSpan(
                8,
                4));

        "VP8X"u8.CopyTo(
            bytes.AsSpan(
                12,
                4));

        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(
                16,
                4),
            10);

        WriteUInt24LittleEndian(
            bytes.AsSpan(
                24,
                3),
            width - 1);

        WriteUInt24LittleEndian(
            bytes.AsSpan(
                27,
                3),
            height - 1);

        return bytes;
    }

    private static void WriteUInt24LittleEndian(
        Span<byte> destination,
        uint value)
    {
        destination[0] =
            (byte)value;

        destination[1] =
            (byte)(
                value >> 8);

        destination[2] =
            (byte)(
                value >> 16);
    }
}