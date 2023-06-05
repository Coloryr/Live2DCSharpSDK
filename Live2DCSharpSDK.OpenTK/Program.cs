using Live2DCSharpSDK.OpenTK;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Live2DCSharpSDK.OpenTK;

public static class Program
{
    private static void Main()
    {
        var nativeWindowSettings = new NativeWindowSettings()
        {
            Size = new Vector2i(600, 600),
            Title = "Live2D",
            // This is needed to run on macos
            Flags = ContextFlags.ForwardCompatible,
            Vsync = VSyncMode.On,
            Profile = ContextProfile.Compatability
        };

        using var window = new Window(GameWindowSettings.Default, nativeWindowSettings);
        window.Run();
    }
}