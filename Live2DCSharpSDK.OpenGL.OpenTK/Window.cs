using Live2DCSharpSDK.App;
using Live2DCSharpSDK.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

namespace Live2DCSharpSDK.OpenTK;

public class Window : GameWindow
{
    private LAppDelegate lapp;
    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
    {
        var version = GL.GetString(StringName.Version);

    }

    protected unsafe override void OnLoad()
    {
        base.OnLoad();
        lapp = new LAppDelegateOpenGL(new OpenTKApi(this), Console.WriteLine)
        {
            BGColor = new(0, 1, 0, 1)
        };

        //var model = lapp.Live2dManager.LoadModel("F:\\live2d\\Resources\\Mao", "Mao");
        //model.ModelMatrix.TranslateX(0.9f);

        var model = lapp.Live2dManager.LoadModel("F:\\live2d\\Resources\\Haru\\", "Haru");
        //model.ModelMatrix.TranslateX(0.8f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        //model.ModelMatrix.TranslateX(0.7f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        //model.ModelMatrix.TranslateX(0.6f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        //model.ModelMatrix.TranslateX(0.5f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        //model.ModelMatrix.TranslateX(0.4f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        //model.ModelMatrix.TranslateX(0.3f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        //model.ModelMatrix.TranslateX(0.1f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        //model.ModelMatrix.TranslateX(0.0f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.0f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.1f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.2f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.3f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.4f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.5f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.6f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.7f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.8f);

        //model = lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model.ModelMatrix.TranslateX(-0.9f);

        //WindowT.SetFather(GLFW.GetWin32Window(WindowPtr));
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        lapp.Run((float)UpdateTime);

        var code = GL.GetError();
        if (code != ErrorCode.NoError)
        {
            throw new Exception();
        }

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        lapp.Resize();

        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        KeyboardState input = KeyboardState;

        if (input.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }
}
