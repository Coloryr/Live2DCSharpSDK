using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Live2DCSharpSDK.App;
using System;

namespace Live2DCSharpSDK.Avalonia;

public partial class MainWindow : Window
{
    private FpsTimer _renderTimer;

    public MainWindow()
    {
        InitializeComponent();

        Button1.Click += Button1_Click;

        Closing += MainWindow_Closing;

        _renderTimer = new(GL);
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        _renderTimer.Close();
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
        lapp.BGColor = new(0, 1, 0, 1);
        var model = lapp.Live2dManager.LoadModel("F:\\live2d\\Resources", "Mao");
        //var model = lapp.Live2dManager.LoadModel("F:\\live2d\\girl07\\l2d00.u\\", "l2d00.u");
        CheckError(gl);
        init = true;
    }

    protected override void OnOpenGlDeinit(GlInterface GL)
    {

    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        gl.Viewport(0, 0, (int)Bounds.Width, (int)Bounds.Height);
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