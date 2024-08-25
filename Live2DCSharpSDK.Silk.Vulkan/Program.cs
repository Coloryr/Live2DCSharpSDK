using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Live2DCSharpSDK.Silk.Vulkan;

internal class Program
{
    private static IWindow? window;
    private static Vk? vk;

    private static bool frameBufferResized = false;

    static void Main(string[] args)
    {
        InitWindow();
        InitVulkan();
        MainLoop();
    }

    private static void InitWindow()
    {
        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "Vulkan",
        };

        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        window.Resize += FramebufferResizeCallback;
    }

    private static void InitVulkan()
    {
        vk = Vk.GetApi();
    }

    private static void MainLoop()
    {
        window!.Render += DrawFrame;
        window!.Run();
    }

    private static void DrawFrame(double delta)
    { 
        
    }


    private static void FramebufferResizeCallback(Vector2D<int> obj)
    {
        frameBufferResized = true;
    }
}
