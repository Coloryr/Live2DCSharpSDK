using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Rendering;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

public class LAppDelegateVulkan : LAppDelegate
{
    public readonly VulkanApi _api;
    public readonly Vk _vk;
    public readonly VulkanManager VulkanManager;

    public LAppDelegateVulkan(VulkanApi api)
    {
        _api = api;
        _vk = Vk.GetApi();

        VulkanManager = new VulkanManager(this, _vk, _api);
        VulkanManager.Initialize();

        var swapchainManager = VulkanManager.SwapchainManager;
        // レンダラにvulkanManagerの変数を渡す
        CubismRenderer_Vulkan.InitializeConstantSettings(
            VulkanManager.Device, VulkanManager.PhysicalDevice,
            VulkanManager.CommandPool, VulkanManager.GraphicQueue,
            swapchainManager.Extent, swapchainManager.SwapchainFormat,
            VulkanManager.SurfaceFormat,
            VulkanManager.GetSwapchainImage(), VulkanManager.GetSwapchainImageView(),
            VulkanManager.DepthFormat
        );

        View = new LAppViewVulkan(this);
        InitApp();
    }

    public new void Resize()
    {
        VulkanManager.FramebufferResized = true;
        base.Resize();
    }

    public override CubismRenderer CreateRenderer(CubismModel model)
    {
        return new CubismRenderer_Vulkan(_vk, VulkanManager, model);
    }

    public override TextureInfo CreateTexture(LAppModel model, int index, int width, int height, nint data)
    {
        _vk.DeviceWaitIdle(VulkanManager.Device);

        return new TextureInfoVulkan(_vk, this, model, VulkanManager.SurfaceFormat, ImageTiling.Optimal,
                   ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
                    (uint)width, (uint)height, data);
    }

    public override void GetWindowSize(out int width, out int height)
    {
        _api.GetWindowSize(out width, out height);
    }

    public override void OnUpdatePre()
    {
        CubismRenderer_Vulkan.UpdateRendererSettings(VulkanManager.GetSwapchainImage(), VulkanManager.GetSwapchainImageView());
    }

    public override void RunPost()
    {
        VulkanManager.PostDraw();
        RecreateSwapchain();
    }

    public override bool RunPre()
    {
        VulkanManager.UpdateDrawFrame();

        return RecreateSwapchain();
    }

    private bool RecreateSwapchain()
    {
        if (VulkanManager.IsSwapchainInvalid)
        {
            GetWindowSize(out var width, out var height);
            if (width == 0 || height == 0)
            {
                return true;
            }

            VulkanManager.RecreateSwapchain();
            CubismRenderer_Vulkan.UpdateSwapchainVariable(
                VulkanManager.SwapchainManager.Extent,
                VulkanManager.GetSwapchainImage(),
                VulkanManager.GetSwapchainImageView()
            );

            // AppViewの初期化
            View.Initialize();
            // サイズを保存しておく
            WindowWidth = width;
            WindowHeight = height;
            VulkanManager.IsSwapchainInvalid = false;
            return true;
        }
        return false;
    }
}
