using ClickableTransparentOverlay;
using ImGuiNET;
using System;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Veldrid.Sdl2;

namespace QRCodeScanner;

internal class WindowAbstraction : Overlay
{
	public Vector2 SizeMin { get; set; }
	public Vector2 SizeMax { get; set; }
	public Vector2 InitSize { get; set; }
	public Vector2 InitPosition { get; set; }

	public event Action OnExit;
	public readonly ImGuiWindowFlags windowState;

	private readonly string title;
	private Action[] drawFuncs;
	public Sdl2Window window;

	private readonly FieldInfo _windowFlags = typeof(Overlay).GetField("windowFlags", BindingFlags.NonPublic | BindingFlags.Instance);

	public WindowAbstraction(string title, ImGuiWindowFlags windowState = ImGuiWindowFlags.None) : base(title, true)
	{
		this.windowState = windowState;
		this.title = title;

		drawFuncs = new[] { () => { } };

		_windowFlags.SetValue(this, SDL_WindowFlags.Borderless);
	}

	public void SetDrawFunc(Action drawFunc)
	{
		drawFuncs[0] = drawFunc;
	}

	public void SetDrawFunc(params Action[] drawFuncs)
	{
		this.drawFuncs = drawFuncs;
	}

	protected override void PostStart()
	{
		base.PostStart();
		window = (Sdl2Window)typeof(Overlay).GetField("window", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
	}

	private string error = "";
	private bool errorOccured = false;
	private bool windowOpen = true;
	protected override Task Render()
	{
		if (!window.Focused) {
			return Task.CompletedTask;
		}

		ImGui.SetNextWindowSizeConstraints(SizeMin, SizeMax);
		ImGui.Begin(title, ref windowOpen, windowState);

		if (!windowOpen) {
			OnExit?.Invoke();
			Environment.Exit(0);
		}

		ImGui.SetWindowPos(title, new(InitPosition.X, InitPosition.Y), ImGuiCond.Once);
		ImGui.SetWindowSize(title, new(InitSize.X, InitSize.Y), ImGuiCond.Once);

		try {
			// call all draw funcs
			foreach (var func in drawFuncs) {
				func();
			}
		}
		catch (Exception ex) {
			error = ex.Message + "\n" + ex.StackTrace;
			errorOccured = true;
		}

		// open error window
		if (errorOccured) {
			ImGui.SetNextWindowSizeConstraints(new(300, 40), new(900, 700));
			ImGui.Begin("ERROR", ref errorOccured, ImGuiWindowFlags.AlwaysAutoResize);

			ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1)); // give red color
			ImGui.TextWrapped(error);
			ImGui.PopStyleColor();

			ImGui.End();
		}

		ImGui.End();

		return Task.CompletedTask;
	}
}

