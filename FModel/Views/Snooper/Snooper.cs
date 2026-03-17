using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using CUE4Parse.UE4.Assets.Exports;
using FModel.Views.Snooper.Buffers;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using Avalonia.Platform;
using Avalonia.Threading;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace FModel.Views.Snooper;

public class Snooper : GameWindow
{
    public readonly FramebufferObject Framebuffer;
    public readonly Renderer Renderer;

    private readonly SnimGui _gui;

    private bool _init;

    public Snooper(GameWindowSettings gwSettings, NativeWindowSettings nwSettings) : base(gwSettings, nwSettings)
    {
        Framebuffer = new FramebufferObject(ClientSize);
        Renderer = new Renderer(ClientSize.X, ClientSize.Y);

        _gui = new SnimGui(ClientSize.X, ClientSize.Y);
        _init = false;
    }

    public bool TryLoadExport(CancellationToken cancellationToken, UObject dummy, Lazy<UObject> export)
    {
        Renderer.Load(cancellationToken, dummy, export);
        return Renderer.Options.Models.Count > 0;
    }

    public unsafe void WindowShouldClose(bool value, bool clear)
    {
        if (clear)
        {
            Renderer.CameraOp.Speed = 1f;
            Renderer.Save();
        }

        // GLFW.SetWindowShouldClose is thread-safe; IsVisible (glfwShowWindow/
        // glfwHideWindow) must be called from the GLFW main thread (UI thread).
        GLFW.SetWindowShouldClose(WindowPtr, value); // start / stop game loop
        Dispatcher.UIThread.Post(() => IsVisible = !value);
    }

    public unsafe void WindowShouldFreeze(bool value)
    {
        GLFW.SetWindowShouldClose(WindowPtr, value); // start / stop game loop
        Dispatcher.UIThread.Post(() => IsVisible = true);
    }

    public override void Run()
    {
        Renderer.Options.SwapMaterial(false);
        Renderer.Options.AnimateMesh(false);

        // GLFW.SetWindowShouldClose is documented as callable from any thread.
        unsafe
        { GLFW.SetWindowShouldClose(WindowPtr, false); }

        // glfwShowWindow must be called from the GLFW main thread (the thread that
        // created the window, which is the Avalonia UI thread).
        Dispatcher.UIThread.Post(() => IsVisible = true);

        // Run the blocking GLFW game loop on a dedicated background thread so that
        // neither the calling thread nor the Avalonia UI event loop is blocked for
        // the lifetime of the 3D viewer window. OpenTK transfers the GL context to
        // this thread via Context.MakeCurrent() at the start of base.Run().
        new Thread(() => base.Run()) { IsBackground = true, Name = "Snooper-GameLoop" }.Start();
    }

    private unsafe void LoadWindowIcon()
    {
        using var stream = AssetLoader.Open(new Uri("avares://FModel/Resources/engine.png"));
        using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
        var memoryGroup = img.GetPixelMemoryGroup();
        Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
        var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);
        foreach (var memory in memoryGroup)
        {
            memory.Span.CopyTo(block);
            block = block[memory.Length..];
        }

        Icon = new WindowIcon(new OpenTK.Windowing.Common.Input.Image(img.Width, img.Height, array.ToArray()));
    }

    protected override void OnLoad()
    {
        if (_init)
        {
            Renderer.Options.SetupModelsAndLights();
            return;
        }

        base.OnLoad();
        CenterWindow();
        LoadWindowIcon();

        GL.ClearColor(OpenTK.Mathematics.Color4.Black);
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.VertexProgramPointSize);
        GL.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        Framebuffer.Setup();
        Renderer.Setup();
        _init = true;
    }

    private void ClearWhatHasBeenDrawn()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        if (!IsVisible)
            return;

        ClearWhatHasBeenDrawn(); // clear window background
        Framebuffer.Bind(); // switch to viewport background
        ClearWhatHasBeenDrawn(); // clear viewport background

        Renderer.Render(); // render everything

        Framebuffer.BindMsaa();
        Framebuffer.Bind(0); // switch to window background

        _gui.Render(this); // render UI
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        if (!IsVisible)
            return;

        var delta = (float) e.Time;

        _gui.Controller.Update(this, delta);
        Renderer.Update(this, delta);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (!IsVisible)
            return;

        _gui.Controller.PressChar((char) e.Unicode);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

        Framebuffer.WindowResized(e.Width, e.Height);
        Renderer.WindowResized(e.Width, e.Height);

        _gui.Controller.WindowResized(e.Width, e.Height);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        WindowShouldClose(true, true);
    }

    public static unsafe int GetMaxRefreshFrequency()
    {
        if (!GLFW.Init())
            return 60;

        var monitor = GLFW.GetPrimaryMonitor();
        if (monitor != null)
        {
            var mode = GLFW.GetVideoMode(monitor);
            if (mode != null && mode->RefreshRate > 0)
                return mode->RefreshRate;
        }

        return 60;
    }
}
