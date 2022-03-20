using System.Drawing.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using SixLabors.ImageSharp;
using System.Text;

namespace QRCodeScanner;

public static class Extensions
{
	public static Image<TPixel> ToImageSharpImage<TPixel>(this System.Drawing.Bitmap bitmap) where TPixel : unmanaged, IPixel<TPixel>
	{
		using var memoryStream = new MemoryStream();

		bitmap.Save(memoryStream, ImageFormat.Png);
		memoryStream.Seek(0, SeekOrigin.Begin);

		return Image.Load<TPixel>(memoryStream);
	}

	public static byte[] ToByteArray(this System.Drawing.Bitmap bitmap)
	{
		using var stream = new MemoryStream();

		bitmap.Save(stream, ImageFormat.Png);
		return stream.ToArray();
	}

	public static string ByteArrayToStr(this byte[] data)
	{
		var decoder = Encoding.UTF8.GetDecoder();
		int charCount = decoder.GetCharCount(data, 0, data.Length);

		char[] charArray = new char[charCount];
		decoder.GetChars(data, 0, data.Length, charArray, 0);
		return new string(charArray);
	}
}
