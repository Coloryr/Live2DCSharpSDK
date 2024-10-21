using System.Windows;
using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;

namespace Live2DCSharpSDK.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    GLWpfControl GLControl;
    private LAppDelegate lapp;

    public MainWindow()
    {
        InitializeComponent();
        GLControl = new();

        GLControl.SizeChanged += GLControl_Resized;
        GLControl.Render += GLControl_Render;
        BorderOpenTK.Child = GLControl;
        //CompositionTarget.Rendering += CompositionTarget_Rendering;

        var cubismAllocator = new LAppAllocator();
        var cubismOption = new CubismOption()
        {
            LogFunction = Console.WriteLine,
            LoggingLevel = LAppDefine.CubismLoggingLevel
        };
        CubismFramework.StartUp(cubismAllocator, cubismOption);

        var settings = new GLWpfControlSettings
        {
            MajorVersion = 3,
            MinorVersion = 3
        };
        GLControl.Start(settings);
        lapp = new LAppDelegateOpenGL(new OpenTKWPFApi(GLControl))
        {
            BGColor = new(0, 1, 0, 1)
        };
        var model = lapp.Live2dManager.LoadModel("F:\\live2d\\Resources\\Mao", "Mao");
        //model = lapp.Live2dManager.LoadModel("F:\\Downloads\\haru_greeter_pro_jp\\haru_greeter_pro_jp\\runtime", "haru_greeter_t05");
    }

    private void GLControl_Render(TimeSpan obj)
    {
        GL.ClearColor(Color4.Blue);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        lapp.Run((float)obj.TotalSeconds);
    }

    private void GLControl_Resized(object sender, SizeChangedEventArgs e)
    {
        if (lapp == null)
            return;
        lapp.Resize();
        GL.Viewport(0, 0, (int)GLControl.ActualWidth, (int)GLControl.ActualHeight);
    }
}