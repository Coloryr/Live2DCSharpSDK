using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL.Controls;
using Avalonia.OpenGL;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System;
using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Framework.Rendering.OpenGL;
using Avalonia.Rendering;
using System.Threading;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Controls.Documents;

namespace Live2DCSharpSDK.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Button1.Click += Button1_Click;
    }

    private void Button1_Click(object? sender, RoutedEventArgs e)
    {
        GL.RequestNextFrameRendering();
    }
}

public class OpenGlPageControl : OpenGlControlBase
{
    private LAppDelegate lapp;

    private string _info = string.Empty;
    private DateTime time;
    private bool render;

    public static readonly DirectProperty<OpenGlPageControl, string> InfoProperty =
        AvaloniaProperty.RegisterDirect<OpenGlPageControl, string>("Info", o => o.Info, (o, v) => o.Info = v);

    public string Info
    {
        get => _info;
        private set => SetAndRaise(InfoProperty, ref _info, value);
    }

    public OpenGlPageControl()
    {
        new Thread(() =>
        {
            while (true)
            {
                if (render)
                {
                    Dispatcher.UIThread.Invoke(RequestNextFrameRendering);
                }
                Thread.Sleep(15);
            }
        }).Start();
    }

    private static void CheckError(GlInterface gl)
    {
        int err;
        while ((err = gl.GetError()) != GlConsts.GL_NO_ERROR)
            Console.WriteLine(err);
    }

    private bool init = false;

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        if (init)
            return;
        CheckError(gl);

        Info = $"Renderer: {gl.GetString(GlConsts.GL_RENDERER)} Version: {gl.GetString(GlConsts.GL_VERSION)}";

        lapp = new(new AvaloniaApi(this, gl));
        //var model = lapp.Live2dManager.LoadModel("F:\\live2d\\koharu_haruto\\‚±‚Í‚é\\runtime\\", "koharu");
        var model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        CheckError(gl);
        init = true;
    }

    protected override void OnOpenGlDeinit(GlInterface GL)
    {
       
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        gl.Viewport(0, 0, (int)Bounds.Width, (int)Bounds.Height);
        render = true;
        var now = DateTime.Now;
        float span = 0;
        if (time.Ticks == 0)
        {
            time = now;
        }
        else
        {
            span = (float)(now - time).TotalSeconds;
            time = now;
        }
        lapp.Run(span);
        CheckError(gl);
    }
}