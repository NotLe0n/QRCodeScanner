using System.Drawing.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using SixLabors.ImageSharp;
using System.Text;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using System;

namespace QRCodeScanner;

public static class Extensions
{
	public static Image<TPixel> ToImageSharpImage<TPixel>(this Bitmap bitmap) where TPixel : unmanaged, IPixel<TPixel>
	{
		using var memoryStream = new MemoryStream();

		bitmap.Save(memoryStream, ImageFormat.Png);
		memoryStream.Seek(0, SeekOrigin.Begin);

		return SixLabors.ImageSharp.Image.Load<TPixel>(memoryStream);
	}

	public static byte[] ToByteArray(this Bitmap bitmap)
	{
		using var stream = new MemoryStream();

		bitmap.Save(stream, ImageFormat.Png);
		return stream.ToArray();
	}

	public static string ToHexString(this byte[] data)
	{
		StringBuilder hex = new StringBuilder(data.Length * 2);
		foreach (byte b in data)
			hex.AppendFormat("{0:X2} ", b);

		return hex.ToString();
	}

	public static Bitmap GetBitmap(this BitmapSource source)
	{
		var bmp = new Bitmap(source.PixelWidth, source.PixelHeight, PixelFormat.Format32bppPArgb);

		BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
		source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
		bmp.UnlockBits(data);
		return bmp;
	}
}
