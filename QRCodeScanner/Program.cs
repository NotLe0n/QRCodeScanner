using ImGuiNET;
using System.Threading.Tasks;
using System.Drawing;
using AForge.Video;
using AForge.Video.DirectShow;
using SixLabors.ImageSharp.PixelFormats;
using QRCodeDecoderLibrary;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace QRCodeScanner;

public class Program
{
	private static WindowAbstraction window;
	private static VideoCaptureDevice captureDevice;
	private static Bitmap image;
	private static readonly FilterInfoCollection filterInfoCollection = new(FilterCategory.VideoInputDevice);

	public static async Task Main()
	{
		window = new WindowAbstraction("QR-Code scanner", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)
		{
			SizeMin = new(200, 200),
			SizeMax = new(700, 500)
		};
		window.SetDrawFunc(Draw);

		await window.Run();
	}

	private static bool webCamOn;
	private static int selectedItem;
	private static string result = string.Empty;
	private static void Draw()
	{
		string[] names = new string[filterInfoCollection.Count];
		for (int i = 0; i < filterInfoCollection.Count; i++)
		{
			names[i] = filterInfoCollection[i].Name;
		}

		ImGui.Text("Video Gerät auswählen");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(200);

		ImGui.Combo("", ref selectedItem, names, filterInfoCollection.Count);

		ImGui.SameLine();

		// if button was pressed
		if (ImGui.Button($"Webcam {(webCamOn ? "aus" : "an")}schalten", new(130, 20)))
		{
			webCamOn = !webCamOn;

			if (webCamOn) {
				StartVideoDevice();
			}
			else {
				StopVideoDevice();
			}
		}

		//if (ImGui.Button("Bild Auswählen"))
		//{
			
		//}

		if (!webCamOn || image is null)
			return;

		ImGui.Separator();

		ImGui.Text("Webcam Bild:");
		window.RemoveImage("Webcam Image"); // remove image, or do nothing if it doesn't exist yet
		window.AddOrGetImagePointer("Webcam Image", image.ToImageSharpImage<Rgba32>(), false, true, out var handle, out var w, out var h); // add/refresh image

		// scale image
		float ratioX =  window.SizeMin.X / w;
		float ratioY = window.SizeMin.Y / h;
		float ratio = ratioX < ratioY ? ratioX : ratioY;

		ImGui.Image(handle, new(w * ratio, h * ratio));

		// scan QR-Code and save contents in "result" in different thread
		new Task(ScanQRCode).Start();

		ImGui.SameLine();

		ImGui.BeginChild("Inhalt_Child", new(200, 100));
		ImGui.Text("Inhalt:\n" + result); // qr code inhalt anzeigen

		// open link button should appear if the QR-Code has a link
		if (IsValidURL(result))
		{
			if (ImGui.Button("Link Öffnen"))
			{
				var psi = new ProcessStartInfo()
				{
					UseShellExecute = true,
					FileName = result
				};
				Process.Start(psi);
			}
		}

		ImGui.EndChild();	
	}

	private static void StartVideoDevice()
	{
		captureDevice = new VideoCaptureDevice(filterInfoCollection[selectedItem].MonikerString);
		captureDevice.NewFrame += CaptureDevice_NewFrame;
		captureDevice.Start();
	}

	private static void StopVideoDevice()
	{
		captureDevice.NewFrame -= CaptureDevice_NewFrame;
		captureDevice.SignalToStop();
		captureDevice = null;
	}

	private static void CaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
	{
		image = eventArgs.Frame.Clone() as Bitmap;
	}

	private static void ScanQRCode()
	{
		QRDecoder decoder = new();
		// call image decoder method with file name
		byte[][] qrCodes = decoder.ImageDecoder(image);
		if (qrCodes is null)
			return;

		foreach (var qrCode in qrCodes)
		{
			result = qrCode.ByteArrayToStr();
		}
	}

	private static bool IsValidURL(string URL)
	{
		string pattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";
		var rgx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
		return rgx.IsMatch(URL);
	}
}