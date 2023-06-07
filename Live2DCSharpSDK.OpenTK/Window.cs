using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Framework.Motion;
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

    protected override void OnLoad()
    {
        base.OnLoad();
        lapp = new(new OpenTKApi(this));
        //lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
        //var model = lapp.Live2dManager.GetModel(0);
        //var list = model.Expressions;
        //var list1 = model.Motions;
        //model.ModelMatrix.TranslateX(0.5f);
        //model.StartMotion(list1[2], MotionPriority.PriorityNormal);

        lapp.Live2dManager.LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Hiyori\\", "Hiyori");
        //model = lapp.Live2dManager.GetModel(1);
        //model.ModelMatrix.TranslateX(-0.5f);
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);

        lapp.Run((float)RenderTime);

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
