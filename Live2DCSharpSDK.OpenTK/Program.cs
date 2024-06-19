using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Live2DCSharpSDK.OpenTK;

public static class Program
{
    private static void Main()
    {
        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(600, 600),
            Title = "Live2D",
            // This is needed to run on macos
            Flags = ContextFlags.ForwardCompatible,
            Vsync = VSyncMode.Adaptive
        };

        using var window = new Window(GameWindowSettings.Default, nativeWindowSettings);
        window.Run();
    }
}