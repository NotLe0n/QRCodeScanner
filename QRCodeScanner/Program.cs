using AForge.Video;
using AForge.Video.DirectShow;
using ImGuiNET;
using NativeFileDialogSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clowd.Clipboard;
using ZXing;
using ZXing.Common;
using System.Threading;

namespace QRCodeScanner;

public class Program
{
	private static WindowAbstraction window;
	private static VideoCaptureDevice captureDevice;
	private static readonly FilterInfoCollection filterInfoCollection = new(FilterCategory.VideoInputDevice);

	public static async Task Main()
	{
		window = new WindowAbstraction("QR-Code scanner", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize) {
			InitPosition = new(400, 400),
			SizeMin = new(500, 200),
			SizeMax = new(1400, 700)
		};

		window.SetDrawFunc(Draw);
		await window.Run();
	}

	private static bool webCamOn;
	private static Bitmap webcamImage;
	private static int selectedItem;
	private static string result;
	private static bool byteMode;

	private static void Draw()
	{
		string[] names = new string[filterInfoCollection.Count];
		for (int i = 0; i < filterInfoCollection.Count; i++) {
			names[i] = filterInfoCollection[i].Name;
		}

		ImGui.SetWindowFontScale(2f);
		ImGui.Text("Video Gerät auswählen");

		ImGui.SameLine();
		// video device selections
		ImGui.SetNextItemWidth(400);
		ImGui.Combo("", ref selectedItem, names, filterInfoCollection.Count);

		ImGui.SameLine();
		// if button was pressed
		if (ImGui.Button($"Webcam {(webCamOn ? "aus" : "an")}schalten", new(260, 40))) {
			webCamOn = !webCamOn;

			if (webCamOn) {
				StartVideoDevice();
			}
			else {
				StopVideoDevice();
			}

			// scan QR-Code and save contents in "result" in different thread
			var task = new Task(ScanWebcam);
			task.Start();
		}

		if (ImGui.Button("Bild Auswählen")) {
			var result = Dialog.FileOpen(defaultPath: Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
			if (result.IsOk) {
				using var file = File.OpenRead(result.Path);
				var image = new Bitmap(file);

				ScanQRCode(image);
			}
		}

		ImGui.SameLine();

		if (ImGui.Button("Bild Ausschneiden")) {
			// open snip & sketch
			Process.Start(new ProcessStartInfo("cmd.exe", "/c explorer ms-screenclip:") {
				CreateNoWindow = true
			});

			Process[] processes = Array.Empty<Process>();
			do {
				processes = Process.GetProcessesByName("ScreenClippingHost");
			}
			while (processes.Length == 0);

			processes[0].EnableRaisingEvents = true;
			processes[0].Exited += (_, __) =>
			{
				using var handle = new ClipboardHandle();
				handle.Open();

				Bitmap image = handle.GetImage()?.GetBitmap();
				ScanQRCode(image);
			};
		}

		ImGui.SameLine();

		ImGui.Checkbox("Bytes anzeigen", ref byteMode);

		ImGui.Separator();

		if (webCamOn && webcamImage is not null) {
			DrawWebcamImage();
		}

		if (result is not null) {
			DrawResults();
		}
	}

	private static void DrawResults()
	{
		ImGui.SameLine();
		ImGui.BeginChild("Inhalt_Child", new(600, 200), false);
	
		ImGui.TextWrapped($"QR-Code Inhalt:\n{result}"); // qr code inhalt anzeigen

		ImGui.Separator();

		// open link button should appear if the QR-Code has a link
		if (IsValidURL(result) && ImGui.Button("Link Öffnen")) {
			OpenLink(result);
		}

		ImGui.SameLine();

		if (ImGui.Button("Kopieren")) {
			using var clipboard = new ClipboardHandle();
			clipboard.Open();
			clipboard.SetText(result);
		}

		ImGui.SameLine();

		// button to clear the results list
		if (ImGui.Button("Entfernen")) {
			result = null;
		}

		ImGui.EndChild();
	}

	private static void DrawWebcamImage()
	{
		ImGui.Text("Webcam Bild:");
		window.RemoveImage("Webcam Image"); // remove image, or do nothing if it doesn't exist yet

		Bitmap clone;
		lock (webcamImage) {
			clone = (Bitmap)webcamImage.Clone();
		}
		window.AddOrGetImagePointer("Webcam Image", clone.ToImageSharpImage<Rgba32>(), false, true, out var handle, out var w, out var h); // add/refresh image

		// scale image
		float ratioX = window.SizeMin.X / w;
		float ratioY = window.SizeMin.Y / h;
		float ratio = ratioX < ratioY ? ratioX : ratioY;

		ImGui.Image(handle, new(w * ratio, h * ratio));
	}

	private static void ScanWebcam()
	{
		while (webCamOn) {
			if (webcamImage is null)
				continue;

			Bitmap clone;
			lock (webcamImage) {
				clone = (Bitmap)webcamImage.Clone();
			}

			ScanQRCode(clone);

			Thread.Sleep(1000);
		}
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
		webcamImage = eventArgs.Frame.Clone() as Bitmap;
	}

	private static readonly DecodingOptions decodingOptions = new() { TryInverted = true, TryHarder = true };
	private static void ScanQRCode(Bitmap bitmap)
	{
		if (result is not null)
			return;

		if (bitmap is null)
			return;

		BarcodeReader<Bitmap> reader = new(bmp => new BitmapLuminanceSource(bmp)) { AutoRotate = true };
		reader.Options = decodingOptions;
		Result decoded = reader.Decode(bitmap);

		if (decoded is null) {
			return;
		}

		result = byteMode ? decoded.RawBytes.ToHexString() : decoded.Text;
	}

	private static void OpenLink(string result)
	{
		var psi = new ProcessStartInfo() {
			UseShellExecute = true,
			FileName = result
		};
		Process.Start(psi);
	}

	private static bool IsValidURL(string URL)
	{
		string pattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";
		var rgx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
		return rgx.IsMatch(URL);
	}
}