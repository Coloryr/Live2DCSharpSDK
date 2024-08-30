using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Live2DCSharpSDK.App;
using Live2DCSharpSDK.Framework;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Live2DCSharpSDK.Vulkan;

public class SwapchainManager
{
    /// <summary>
    /// 物理デバイスのサポートを確認する
    /// </summary>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="surface">スワップチェーンサーフェス</param>
    /// <returns>物理デバイスのサポート情報</returns>
    public static unsafe SwapchainSupportDetails QuerySwapchainSupport(KhrSurface vk, PhysicalDevice physicalDevice, SurfaceKHR surface)
    {
        var details = new SwapchainSupportDetails();
        vk.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out var khr);
        details.Capabilities = khr;

        uint formatCount = 0;
        vk.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, null);
        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];
            for (int a = 0; a < formatCount; a++)
            {
                details.Formats[a] = new();
            }
            fixed (SurfaceFormatKHR* ptr = details.Formats)
            {
                vk.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, ptr);
            }
        }

        uint presentModeCount = 0;
        vk.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);

        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            for (int a = 0; a < presentModeCount; a++)
            {
                details.PresentModes[a] = new();
            }

            fixed (PresentModeKHR* ptr = details.PresentModes)
            {
                vk.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, ptr);
            }
        }

        return details;
    }

    /// <summary>
    /// 表示モードを選択する
    /// </summary>
    /// <param name="availablePresentModes">使えるフォーマット</param>
    /// <returns></returns>
    public static PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] availablePresentModes)
    {
        for (int i = 0; i < availablePresentModes.Length; i++)
        {
            if (availablePresentModes[i] == PresentModeKHR.MailboxKhr)
            {
                return availablePresentModes[i];
            }
        }
        return PresentModeKHR.FifoKhr;
    }

    /// <summary>
    /// イメージの解像度
    /// </summary>
    public Extent2D Extent { get; private set; } = new(0, 0);
    /// <summary>
    /// イメージ数
    /// </summary>
    public uint ImageCount => _imageCount;

    private uint _imageCount;

    /// <summary>
    /// スワップチェーンのイメージ
    /// </summary>
    public Image[] Images { get; private set; } = [];
    /// <summary>
    /// スワップチェーンのイメージビュー
    /// </summary>
    public ImageView[] ImageViews { get; private set; }= [];
    /// <summary>
    /// スワップチェーン
    /// </summary>
    public SwapchainKHR Swapchain => _swapchain;

    private SwapchainKHR _swapchain;

    /// <summary>
    /// スワップチェーンのフォーマット
    /// </summary>
    public Format SwapchainFormat { get; private set; } = Format.B8G8R8A8Unorm;

    private readonly VulkanApi _api;
    private readonly KhrSurface _khr;
    private readonly KhrSwapchain _khrs;
    private readonly Vk _vk;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="api"></param>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="device">デバイス</param>
    /// <param name="surface">サーフェス</param>
    /// <param name="graphicsFamily">描画コマンドに使うキューファミリ</param>
    /// <param name="presentFamily">表示コマンドに使うキューファミリ</param>
    public SwapchainManager(KhrSwapchain swapchain, KhrSurface khr, VulkanApi api, Vk vk, PhysicalDevice physicalDevice, Device device, SurfaceKHR surface, int graphicsFamily, int presentFamily)
    {
        _khrs = swapchain;
        _khr = khr;
        _api = api;
        _vk = vk;

        CreateSwapchain(physicalDevice, device, surface, graphicsFamily, presentFamily);
    }

    /// <summary>
    /// サーフェスフォーマットを選択する
    /// </summary>
    /// <param name="availableFormats">使えるサーフェスフォーマット</param>
    /// <returns>選択するサーフェスフォーマット</returns>
    public SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] availableFormats)
    {
        for (int i = 0; i < availableFormats.Length; i++)
        {
            if (availableFormats[i].Format == SwapchainFormat && availableFormats[i].ColorSpace == ColorSpaceKHR.PaceSrgbNonlinearKhr)
            {
                return availableFormats[i];
            }
        }
        //他に使えるフォーマットが無かったら最初のフォーマットを使う
        return availableFormats[0];
    }

    /// <summary>
    /// スワップチェーンイメージの解像度を選択する
    /// </summary>
    /// <param name="capabilities">サーフェスの機能を保持する構造体</param>
    /// <returns>スワップチェーンイメージの解像度</returns>
    public Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }
        else
        {
            //for Retina display
            _api.GetWindowSize(out var width, out var height);
            var actualExtent = new Extent2D((uint)width, (uint)height);

            if (actualExtent.Width < capabilities.MinImageExtent.Width)
            {
                actualExtent.Width = capabilities.MinImageExtent.Width;
            }
            else if (actualExtent.Width > capabilities.MaxImageExtent.Width)
            {
                actualExtent.Width = capabilities.MaxImageExtent.Width;
            }

            if (actualExtent.Height < capabilities.MinImageExtent.Height)
            {
                actualExtent.Height = capabilities.MinImageExtent.Height;
            }
            else if (actualExtent.Height > capabilities.MaxImageExtent.Height)
            {
                actualExtent.Height = capabilities.MaxImageExtent.Height;
            }

            return actualExtent;
        }
    }

    /// <summary>
    /// スワップチェーンを作成する
    /// </summary>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="device">デバイス</param>
    /// <param name="surface">サーフェス</param>
    /// <param name="graphicsFamily">描画コマンドに使うキューファミリ</param>
    /// <param name="presentFamily">表示コマンドに使うキューファミリ</param>
    public unsafe void CreateSwapchain(PhysicalDevice physicalDevice, Device device, SurfaceKHR surface, int graphicsFamily, int presentFamily)
    {
        SwapchainSupportDetails swapChainSupport = QuerySwapchainSupport(_khr, physicalDevice, surface);
        SurfaceFormatKHR surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        PresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
        Extent = ChooseSwapExtent(swapChainSupport.Capabilities);

        _imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && _imageCount > swapChainSupport.Capabilities.MaxImageCount)
        {
            _imageCount = swapChainSupport.Capabilities.MaxImageCount;
        }

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,
            MinImageCount = _imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = Extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit |
                 ImageUsageFlags.TransferDstBit,
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default
        };

        var ptr1 = stackalloc[] { (uint)graphicsFamily, (uint)presentFamily };

        if (graphicsFamily != presentFamily)
        {
            createInfo.PQueueFamilyIndices = ptr1;
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
        }
        else
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
            createInfo.QueueFamilyIndexCount = 0;
            createInfo.PQueueFamilyIndices = null;
        }

        if (_khrs.CreateSwapchain(device, ref createInfo, null, out _swapchain) != Result.Success)
        {
            CubismLog.Error("failed to create swap chain");
        }

        // swapchain imageを取得する
        _khrs.GetSwapchainImages(device, _swapchain, ref _imageCount, null);
        Images = new Image[_imageCount];

        fixed (Image* ptr = Images)
        {
            _khrs.GetSwapchainImages(device, _swapchain, ref _imageCount, ptr);
        }

        ImageViews = new ImageView[_imageCount];
        for (int i = 0; i < _imageCount; i++)
        {
            var viewInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = Images[i],
                ViewType = ImageViewType.Type2D,
                Format = SwapchainFormat,
                Components = new(0, 0, 0, 0),
                SubresourceRange = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
            };

            if (_vk.CreateImageView(device, ref viewInfo, null, out ImageViews[i]) != Result.Success)
            {
                CubismLog.Error("failed to create texture image view");
            }
        }
    }

    /// <summary>
    /// 表示コマンドをキューに積む
    /// </summary>
    /// <param name="queue">キュー</param>
    /// <param name="imageIndex">イメージのインデックス</param>
    /// <returns></returns>
    public unsafe Result QueuePresent(Queue queue, uint imageIndex)
    {
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr
        };

        var swapChains = new SwapchainKHR[] { _swapchain };
        presentInfo.SwapchainCount = 1;
        fixed (SwapchainKHR* ptr = swapChains)
        {
            presentInfo.PSwapchains = ptr;
            presentInfo.PImageIndices = &imageIndex;
            return _khrs.QueuePresent(queue, &presentInfo);
        }
    }

    /// <summary>
    /// スワップチェーンのレイアウトの変更
    /// </summary>
    /// <param name="device">論理デバイス</param>
    /// <param name="commandPool">コマンドプール</param>
    /// <param name="queue">キュー</param>
    public unsafe void ChangeLayout(Device device, CommandPool commandPool, Queue queue)
    {
        for (int i = 0; i < _imageCount; i++)
        {
            var barrier = new ImageMemoryBarrier
            {
                SType = StructureType.ImageMemoryBarrier,
                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.PresentSrcKhr,
                Image = Images[i],
                SubresourceRange = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
                SrcAccessMask = 0
            };

            var sourceStage = PipelineStageFlags.AllCommandsBit;
            var destinationStage = PipelineStageFlags.AllCommandsBit;

            var allocInfo = new CommandBufferAllocateInfo
            {
                SType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1
            };

            _vk.AllocateCommandBuffers(device, ref allocInfo, out var commandBuffer);

            var beginInfo = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit
            };

            _vk.BeginCommandBuffer(commandBuffer, ref beginInfo);

            _vk.CmdPipelineBarrier(
                commandBuffer,
                sourceStage,
                destinationStage,
                0,
                0,
                null,
                0,
                null,
                1,
                &barrier
            );

            _vk.EndCommandBuffer(commandBuffer);

            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &commandBuffer
            };

            _vk.QueueSubmit(queue, 1, ref submitInfo, default);
            _vk.QueueWaitIdle(queue);
            _vk.FreeCommandBuffers(device, commandPool, 1, ref commandBuffer);
        }


        //sourceStage = VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;
        //destinationStage = VK_PIPELINE_STAGE_ALL_COMMANDS_BIT;
    }

    /// <summary>
    /// リソースを破棄＆再作成する
    /// </summary>
    /// <param name="device">デバイス</param>
    public unsafe void Cleanup(Device device)
    {
        if (_swapchain.Handle != 0)
        {
            for (int i = 0; i < ImageViews.Length; i++)
            {
                _vk.DestroyImageView(device, ImageViews[i], null);
            }
            Images = [];
            ImageViews = [];
            _khrs.DestroySwapchain(device, _swapchain, null);
        }
    }
}
