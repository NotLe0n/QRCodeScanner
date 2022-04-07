using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QRCodeScanner;

public static class Extensions
{
	public static SixLabors.ImageSharp.Image<Rgba32> ToImageSharpImage(this Bitmap bitmap)
	{
		using var memoryStream = new MemoryStream();

		bitmap.Save(memoryStream, ImageFormat.Bmp);
		memoryStream.Seek(0, SeekOrigin.Begin); // go back to beginning

		return SixLabors.ImageSharp.Image.Load<Rgba32>(memoryStream);
	}

	public static Bitmap GetBitmap(this BitmapSource source)
	{
		var bmp = new Bitmap(source.PixelWidth, source.PixelHeight, PixelFormat.Format32bppPArgb);

		BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
		source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
		bmp.UnlockBits(data);
		return bmp;
	}
}
