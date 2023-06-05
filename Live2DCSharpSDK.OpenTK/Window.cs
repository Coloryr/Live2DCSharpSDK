using Live2DCSharpSDK.App;
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
        lapp = new(new OpenTKApi(this));

        var version = GL.GetString(StringName.Version);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        var res = lapp.Initialize();
        if (!res)
        {
            throw new Exception();
        }
        lapp.GetLive2D().LoadModel("E:\\code\\Live2DCSharpSDK\\Resources\\Haru\\", "Haru");
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
