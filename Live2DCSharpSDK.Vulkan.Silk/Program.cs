using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.Vulkan;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Live2DCSharpSDK.Silk.Vulkan;

internal class Program : VulkanApi
{
    private IWindow window;
    private Vk vk;

    static void Main(string[] args)
    {
        var cubismAllocator = new LAppAllocator();
        var cubismOption = new CubismOption()
        {
            LogFunction = Console.WriteLine,
            LoggingLevel = LAppDefine.CubismLoggingLevel
        };
        CubismFramework.StartUp(cubismAllocator, cubismOption);

        new Program().InitWindow();
    }

    public override unsafe VkNonDispatchableHandle CreateSurface<T>(VkHandle handle, T* handel1)
    {
        return window.VkSurface!.Create(handle, handel1);
    }

    public override unsafe byte** GetRequiredExtensions(out uint count)
    {
        return window.VkSurface!.GetRequiredExtensions(out count);
    }

    public override void GetWindowSize(out int width, out int height)
    {
        width = window.FramebufferSize.X;
        height = window.FramebufferSize.Y;
    }

    private void InitWindow()
    {
        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(400, 400),
            Title = "Vulkan",
        };

        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
        vk = Vk.GetApi();
        var live2d = new LAppDelegateVulkan(this)
        {
            BGColor = new(0, 1, 0, 1)
        };

        var model = live2d.Live2dManager.LoadModel("F:\\live2d\\Resources\\Haru\\", "Haru");

        window.Resize += (size) =>
        {
            live2d.Resize();
        };
        window.Render += (time) =>
        {
            live2d.Run((float)time);
        };
        window.Run();
    }
}
