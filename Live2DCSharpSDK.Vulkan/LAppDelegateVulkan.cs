using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Framework.Core;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Rendering;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Live2DCSharpSDK.Vulkan;

public class LAppDelegateVulkan : LAppDelegate
{
    public readonly VulkanApi _api;
    public readonly Vk _vk;
    public readonly VulkanManager VulkanManager;

    public bool _framebufferResized = false;

    public LAppDelegateVulkan(VulkanApi api, LogFunction log) : base(log)
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

        var view = new LAppViewVulkan(this);
        View = view;
        InitApp();
    }

    public override CubismRenderer CreateRenderer(CubismModel model)
    {
        return new CubismRenderer_Vulkan(_vk, VulkanManager, model);
    }

    public override TextureInfo CreateTexture(LAppModel model, int index, int width, int height, nint data)
    {
        return new TextureInfoVulkan(_vk, this, model, VulkanManager.SurfaceFormat, ImageTiling.Optimal,
                   ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
                    (uint)width, (uint)height, data);
    }

    public override void GetWindowSize(out int width, out int height)
    {
        _api.GetWindowSize(out width, out height);
    }

    public override void RunPost()
    {
        VulkanManager.PostDraw();
        RecreateSwapchain();
    }

    public override bool RunPre()
    {
        if (RecreateSwapchain())
        {
            return false;
        }

        return true;
    }

    private bool RecreateSwapchain()
    {
        if (VulkanManager.IsSwapchainInvalid)
        {
            GetWindowSize(out var width, out var height);
            if (width == 0 || height == 0)
            {
                return false;
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
