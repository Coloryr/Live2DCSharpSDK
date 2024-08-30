using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.Framework.Rendering;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

/// <summary>
/// オフスクリーン描画用構造体
/// </summary>
public class CubismOffscreenSurface_Vulkan(Vk vk) : CubismOffscreenSurface
{
    /// <summary>
    /// オフスクリーンの横幅
    /// </summary>
    public uint BufferWidth { get; private set; }
    /// <summary>
    /// オフスクリーンの縦幅
    /// </summary>
    public uint BufferHeight { get; private set; }
    /// <summary>
    /// カラーバッファ
    /// </summary>
    private CubismImageVulkan? _colorImage;
    /// <summary>
    /// 深度バッファ
    /// </summary>
    private CubismImageVulkan? _depthImage;

    /// <summary>
    /// レンダリングターゲットのクリア
    /// 呼ぶ場合はBeginDrawの後で呼ぶこと
    /// </summary>
    /// <param name="commandBuffer">コマンドバッファ</param>
    /// <param name="r">赤(0.0~1.0)</param>
    /// <param name="g">緑(0.0~1.0)</param>
    /// <param name="b">青(0.0~1.0)</param>
    /// <param name="a">α(0.0~1.0)</param>
    public unsafe void BeginDraw(CommandBuffer commandBuffer, float r, float g, float b, float a)
    {
        if (_colorImage == null)
        {
            CubismLog.Error("colorImage is null.");
            return;
        }

        _colorImage.SetImageLayout(commandBuffer, ImageLayout.ColorAttachmentOptimal, 1, ImageAspectFlags.ColorBit);

        var colorAttachment = new RenderingAttachmentInfo
        {
            SType = StructureType.RenderingAttachmentInfoKhr,
            ImageView = _colorImage.View,
            ImageLayout = ImageLayout.AttachmentOptimalKhr,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new(new(r, g, b, a))
        };

        if (_depthImage == null)
        {
            CubismLog.Error("depthImage is null.");
            return;
        }

        _depthImage.SetImageLayout(commandBuffer, ImageLayout.DepthStencilAttachmentOptimal,
            1, ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit);

        var depthStencilAttachment = new RenderingAttachmentInfo
        {
            SType = StructureType.RenderingAttachmentInfoKhr,
            ImageView = _depthImage.View,
            ImageLayout = ImageLayout.DepthStencilAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            ClearValue = new(new(1.0f, 0.0f))
        };

        var renderingInfo = new RenderingInfo
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new(BufferWidth, BufferHeight)),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachment,
            PDepthAttachment = &depthStencilAttachment
        };

        vk.CmdBeginRendering(commandBuffer, ref renderingInfo);
    }

    /// <summary>
    /// 描画終了
    /// </summary>
    /// <param name="commandBuffer">コマンドバッファ</param>
    public unsafe void EndDraw(CommandBuffer commandBuffer)
    {
        if (_colorImage == null)
        {
            CubismLog.Error("colorImage is null.");
            return;
        }

        vk.CmdEndRendering(commandBuffer);

        // レイアウト変更
        var memoryBarrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            SrcAccessMask = AccessFlags.ColorAttachmentWriteBit,
            OldLayout = ImageLayout.ColorAttachmentOptimal,
            NewLayout = ImageLayout.ShaderReadOnlyOptimal,
            Image = _colorImage.Image,
            SubresourceRange = new(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.ColorAttachmentOutputBit,
                             PipelineStageFlags.BottomOfPipeBit, 0, 0, null, 0, null, 1, ref memoryBarrier);

        _colorImage.SetCurrentLayout(ImageLayout.ShaderReadOnlyOptimal);

        if (_depthImage == null)
        {
            CubismLog.Error("depthImage is null.");
            return;
        }

        memoryBarrier.OldLayout = ImageLayout.DepthStencilAttachmentOptimal;
        memoryBarrier.NewLayout = ImageLayout.DepthStencilAttachmentOptimal;
        memoryBarrier.Image = _depthImage.Image;
        memoryBarrier.SubresourceRange = new(ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit, 0, 1, 0, 1);

        vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.ColorAttachmentOutputBit,
            PipelineStageFlags.BottomOfPipeBit, 0, 0, null, 0, null, 1, ref memoryBarrier);

        _depthImage.SetCurrentLayout(ImageLayout.DepthStencilAttachmentOptimal);
    }

    /// <summary>
    /// CubismOffscreenSurfaceを作成する。
    /// </summary>
    /// <param name="device">論理デバイス</param>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="displayBufferWidth">オフスクリーンの横幅</param>
    /// <param name="displayBufferHeight">オフスクリーンの縦幅</param>
    /// <param name="surfaceFormat">サーフェスフォーマット</param>
    /// <param name="depthFormat">深度フォーマット</param>
    public void CreateOffscreenSurface(Device device, PhysicalDevice physicalDevice,
        uint displayBufferWidth, uint displayBufferHeight, Format surfaceFormat, Format depthFormat)
    {
        _colorImage = new(vk);
        _colorImage.CreateImage(device, physicalDevice, displayBufferWidth, displayBufferHeight,
                                 1, surfaceFormat, ImageTiling.Optimal,
                                 ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.SampledBit |
                                 ImageUsageFlags.TransferDstBit);
        _colorImage.CreateView(device, surfaceFormat, ImageAspectFlags.ColorBit, 1);
        _colorImage.CreateSampler(device, 1.0f, 1);

        _depthImage = new(vk);
        _depthImage.CreateImage(device, physicalDevice, displayBufferWidth, displayBufferHeight,
                                 1, depthFormat, ImageTiling.Optimal,
                                ImageUsageFlags.DepthStencilAttachmentBit);

        _depthImage.CreateView(device, depthFormat, ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit, 1);

        BufferWidth = displayBufferWidth;
        BufferHeight = displayBufferHeight;
    }

    /// <summary>
    /// CubismOffscreenSurfaceの削除
    /// </summary>
    /// <param name="device">デバイス</param>
    public void DestroyOffscreenSurface(Device device)
    {
        if (_colorImage != null)
        {
            _colorImage.Destroy(device);
            _colorImage = null;
        }
        if (_depthImage != null)
        {
            _depthImage.Destroy(device);
            _depthImage = null;
        }
    }

    /// <summary>
    /// イメージへのアクセッサ
    /// </summary>
    /// <returns></returns>
    public Image GetTextureImage()
    {
        if (_colorImage == null)
        {
            CubismLog.Error("colorImage is null.");
            return default;
        }

        return _colorImage.Image;
    }

    /// <summary>
    /// テクスチャビューへのアクセッサ
    /// </summary>
    /// <returns></returns>
    public ImageView GetTextureView()
    {
        if (_colorImage == null)
        {
            CubismLog.Error("colorImage is null.");
            return default;
        }

        return _colorImage.View;
    }

    /// <summary>
    /// テクスチャサンプラーへのアクセッサ
    /// </summary>
    /// <returns></returns>
    public Sampler GetTextureSampler()
    {
        if (_colorImage == null)
        {
            CubismLog.Error("colorImage is null.");
            return default;
        }

        return _colorImage.Sampler;
    }

    /// <summary>
    /// 現在有効かどうかを返す
    /// </summary>
    /// <returns></returns>
    public bool IsValid()
    {
        if (_colorImage == null || _depthImage == null)
        {
            return false;
        }

        return true;
    }
}
