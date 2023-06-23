using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Live2DCSharpSDK.App;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        lapp = new(new AvaloniaApi(this, gl), Console.WriteLine);
        //var model = lapp.Live2dManager.LoadModel("F:\\live2d\\koharu_haruto\\‚±‚Í‚é\\runtime\\", "koharu");
        var model = lapp.Live2dManager.LoadModel("F:\\live2d\\Resources\\Haru\\", "Haru");
        CheckError(gl);
        init = true;
    }

    protected override void OnOpenGlDeinit(GlInterface GL)
    {
        render = false;
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

        Task.Run(() =>
        {
            Thread.Sleep(15);
            if (render)
            {
                Dispatcher.UIThread.Invoke(RequestNextFrameRendering);
            }
        });
    }
}