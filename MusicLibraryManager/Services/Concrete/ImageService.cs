using System.Runtime.InteropServices;
using SkiaSharp;

namespace MusicLibraryManager.Services.Concrete;

public class ImageService : IImageService
{
    public void CopyToClipboard(byte[] imageData)
    {
        using var skBitmap = SKBitmap.Decode(imageData);
        if (skBitmap == null) return;

        // Convert to BGRA format for Windows clipboard (DIB format)
        using var bgraBitmap = new SKBitmap(skBitmap.Width, skBitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bgraBitmap);
        canvas.DrawBitmap(skBitmap, 0, 0);

        var width = bgraBitmap.Width;
        var height = bgraBitmap.Height;
        var stride = width * 4;
        var pixels = bgraBitmap.GetPixelSpan().ToArray();

        // Create DIB (Device Independent Bitmap) for clipboard
        var bitmapInfoSize = 40; // BITMAPINFOHEADER size
        var pixelDataSize = stride * height;
        var dibSize = bitmapInfoSize + pixelDataSize;

        var dibData = new byte[dibSize];

        // BITMAPINFOHEADER
        BitConverter.GetBytes(40).CopyTo(dibData, 0);  // biSize
        BitConverter.GetBytes(width).CopyTo(dibData, 4);  // biWidth
        BitConverter.GetBytes(height).CopyTo(dibData, 8);  // biHeight (positive = bottom-up)
        BitConverter.GetBytes((short)1).CopyTo(dibData, 12);  // biPlanes
        BitConverter.GetBytes((short)32).CopyTo(dibData, 14);  // biBitCount
        BitConverter.GetBytes(0).CopyTo(dibData, 16);  // biCompression (BI_RGB)
        BitConverter.GetBytes(pixelDataSize).CopyTo(dibData, 20);  // biSizeImage

        // Copy pixel data (flip vertically for bottom-up DIB format)
        for (int y = 0; y < height; y++)
        {
            var srcRow = y * stride;
            var dstRow = (height - 1 - y) * stride;
            Array.Copy(pixels, srcRow, dibData, bitmapInfoSize + dstRow, stride);
        }

        CopyDibToClipboard(dibData);
    }

    private static void CopyDibToClipboard(byte[] dibData)
    {
        const uint CF_DIB = 8;
        const uint GMEM_MOVEABLE = 0x0002;

        if (!OpenClipboard(IntPtr.Zero))
            return;

        try
        {
            EmptyClipboard();

            var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)dibData.Length);
            if (hGlobal == IntPtr.Zero)
                return;

            var pGlobal = GlobalLock(hGlobal);
            if (pGlobal == IntPtr.Zero)
            {
                GlobalFree(hGlobal);
                return;
            }

            try
            {
                Marshal.Copy(dibData, 0, pGlobal, dibData.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (SetClipboardData(CF_DIB, hGlobal) == IntPtr.Zero)
                GlobalFree(hGlobal);
        }
        finally
        {
            CloseClipboard();
        }
    }

    public byte[]? GetAlbumCoverData(Track track)
    {
        var pictures = track?.Tag.Pictures;
        if (pictures == null || pictures.Length == 0)
            return null;

        return pictures[0].Data.Data;
    }

    public bool HasImageInClipboard()
    {
        const uint CF_DIB = 8;
        return IsClipboardFormatAvailable(CF_DIB);
    }

    public (byte[]? Data, string? MimeType) GetImageFromClipboard()
    {
        const uint CF_DIB = 8;

        if (!OpenClipboard(IntPtr.Zero))
            return (null, null);

        try
        {
            var hData = GetClipboardData(CF_DIB);
            if (hData == IntPtr.Zero)
                return (null, null);

            var pData = GlobalLock(hData);
            if (pData == IntPtr.Zero)
                return (null, null);

            try
            {
                // Read BITMAPINFOHEADER
                var biSize = Marshal.ReadInt32(pData, 0);
                var width = Marshal.ReadInt32(pData, 4);
                var height = Marshal.ReadInt32(pData, 8);
                var biBitCount = Marshal.ReadInt16(pData, 14);
                var biCompression = Marshal.ReadInt32(pData, 16);

                // Only handle uncompressed 32-bit or 24-bit bitmaps
                if (biCompression != 0 || (biBitCount != 32 && biBitCount != 24))
                    return (null, null);

                var absHeight = Math.Abs(height);
                var bytesPerPixel = biBitCount / 8;
                var srcStride = ((width * bytesPerPixel + 3) / 4) * 4; // DIB rows are 4-byte aligned
                var pixelDataOffset = biSize;
                var isBottomUp = height > 0;

                // Create SKBitmap and copy pixel data
                using var skBitmap = new SKBitmap(width, absHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                var dstPixels = skBitmap.GetPixels();

                for (int y = 0; y < absHeight; y++)
                {
                    var srcY = isBottomUp ? (absHeight - 1 - y) : y;
                    var srcOffset = pixelDataOffset + srcY * srcStride;

                    for (int x = 0; x < width; x++)
                    {
                        var srcPixelOffset = srcOffset + x * bytesPerPixel;
                        var dstPixelOffset = (y * width + x) * 4;

                        byte b = Marshal.ReadByte(pData, srcPixelOffset);
                        byte g = Marshal.ReadByte(pData, srcPixelOffset + 1);
                        byte r = Marshal.ReadByte(pData, srcPixelOffset + 2);
                        byte a = biBitCount == 32 ? Marshal.ReadByte(pData, srcPixelOffset + 3) : (byte)255;

                        Marshal.WriteByte(dstPixels + dstPixelOffset, b);
                        Marshal.WriteByte(dstPixels + dstPixelOffset + 1, g);
                        Marshal.WriteByte(dstPixels + dstPixelOffset + 2, r);
                        Marshal.WriteByte(dstPixels + dstPixelOffset + 3, a);
                    }
                }

                // Encode to PNG
                using var image = SKImage.FromBitmap(skBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return (data.ToArray(), "image/png");
            }
            finally
            {
                GlobalUnlock(hData);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
