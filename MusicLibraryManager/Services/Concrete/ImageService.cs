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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
