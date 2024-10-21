using Live2DCSharpSDK.App;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Live2DCSharpSDK.Vulkan;

public class TextureInfoVulkan : TextureInfo
{
    private static int _sequenceId;

    private readonly Vk _vk;
    private readonly LAppDelegateVulkan _lapp;
    private readonly CubismImageVulkan _image;

    /// <summary>
    /// 画像ファイルを読み込む
    /// </summary>
    /// <param name="fileName">読み込む画像ファイルパス名</param>
    /// <param name="format">画像フォーマット</param>
    /// <param name="tiling">画像データのタイリング配置の設定</param>
    /// <param name="usage">画像の使用フラグ</param>
    /// <param name="imageProperties"></param>
    public unsafe TextureInfoVulkan(Vk vk, LAppDelegateVulkan lapp, LAppModel model,
        Format format, ImageTiling tiling, ImageUsageFlags usage,
        uint width, uint height, nint data)
    {
        _vk = vk;
        _lapp = lapp;
        Id = ++_sequenceId;

        Device device = lapp.VulkanManager.Device;
        PhysicalDevice physicalDevice = lapp.VulkanManager.PhysicalDevice;

        ulong imageSize = width * height * 4;

        var stagingBuffer = new CubismBufferVulkan(vk);
        stagingBuffer.CreateBuffer(device, physicalDevice, imageSize, BufferUsageFlags.TransferSrcBit,
                                   MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        stagingBuffer.Map(device, imageSize);
        stagingBuffer.MemCpy((void*)data, imageSize);
        stagingBuffer.UnMap(device);

        uint _mipLevels = (uint)Math.Floor(Math.Log2(Math.Max(width, height))) + 1;

        _image = new CubismImageVulkan(vk);
        _image.CreateImage(device, physicalDevice, width, height, _mipLevels, format, tiling, usage);
        CommandBuffer commandBuffer = lapp.VulkanManager.BeginSingleTimeCommands();
        _image.SetImageLayout(commandBuffer, ImageLayout.TransferDstOptimal, _mipLevels, ImageAspectFlags.ColorBit);
        CopyBufferToImage(commandBuffer, stagingBuffer.Buffer, _image.Image, width, height);
        lapp.VulkanManager.SubmitCommand(commandBuffer);
        GenerateMipmaps(_image, width, height, _mipLevels);
        _image.CreateView(device, format, ImageAspectFlags.ColorBit, _mipLevels);
        vk.GetPhysicalDeviceProperties(physicalDevice, out var properties);
        _image.CreateSampler(device, properties.Limits.MaxSamplerAnisotropy, _mipLevels);
        stagingBuffer.Destroy(device);

        (model.Renderer as CubismRenderer_Vulkan)?.BindTexture(_image);
    }

    /// <summary>
    /// バッファをイメージにコピーする
    /// </summary>
    /// <param name="commandBuffer">コマンドバッファ</param>
    /// <param name="buffer">バッファ</param>
    /// <param name="image">イメージ</param>
    /// <param name="width">画像の横幅</param>
    /// <param name="height">画像の縦幅</param>
    public unsafe void CopyBufferToImage(CommandBuffer commandBuffer, Buffer buffer, Image image, uint width,
                           uint height)
    {
        var region = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            ImageOffset = new(0, 0, 0),
            ImageExtent = new(width, height, 1)
        };

        _vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, ref region);
    }

    /// <summary>
    /// ミップマップを作成する
    /// </summary>
    /// <param name="image">イメージ</param>
    /// <param name="texWidth">画像の幅</param>
    /// <param name="texHeight">画像の高さ</param>
    /// <param name="mipLevels">ミップレべル</param>
    public unsafe void GenerateMipmaps(CubismImageVulkan image, uint texWidth, uint texHeight,
                         uint mipLevels)
    {
        CommandBuffer commandBuffer = _lapp.VulkanManager.BeginSingleTimeCommands();

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            Image = image.Image,
            SrcQueueFamilyIndex = uint.MaxValue,
            DstQueueFamilyIndex = uint.MaxValue,
            SubresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseArrayLayer = 0,
                LayerCount = 1,
                LevelCount = 1
            }
        };

        int mipWidth = (int)texWidth;
        int mipHeight = (int)texHeight;

        for (uint i = 1; i < mipLevels; i++)
        {
            barrier.SubresourceRange.BaseMipLevel = i - 1;
            barrier.OldLayout = ImageLayout.TransferDstOptimal;
            barrier.NewLayout = ImageLayout.TransferSrcOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.TransferReadBit;

            _vk.CmdPipelineBarrier(commandBuffer,
                                 PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, 0,
                                 0, null,
                                 0, null,
                                 1, ref barrier);

            var blit = new ImageBlit
            {
                SrcSubresource = new()
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = i - 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                },
                DstSubresource = new()
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = i,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                },
                SrcOffsets = new(),
                DstOffsets = new()
            };

            blit.SrcOffsets[0] = new(0, 0, 0);
            blit.SrcOffsets[1] = new(mipWidth, mipHeight, 1);
            blit.DstOffsets[0] = new(0, 0, 0);
            blit.DstOffsets[1] = new(mipWidth > 1 ? mipWidth / 2 : 1, mipHeight > 1 ? mipHeight / 2 : 1, 1);

            _vk.CmdBlitImage(commandBuffer,
                           image.Image, ImageLayout.TransferSrcOptimal,
                           image.Image, ImageLayout.TransferDstOptimal,
                           1, &blit,
                           Filter.Linear);

            barrier.OldLayout = ImageLayout.TransferSrcOptimal;
            barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
            barrier.SrcAccessMask = AccessFlags.TransferReadBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            _vk.CmdPipelineBarrier(commandBuffer,
                                 PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0,
                                 0, null,
                                 0, null,
                                 1, ref barrier);

            if (mipWidth > 1) mipWidth /= 2;
            if (mipHeight > 1) mipHeight /= 2;
        }

        barrier.SubresourceRange.BaseMipLevel = mipLevels - 1;
        barrier.OldLayout = ImageLayout.TransferDstOptimal;
        barrier.NewLayout = ImageLayout.ShaderReadOnlyOptimal;
        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = AccessFlags.ShaderReadBit;

        _vk.CmdPipelineBarrier(commandBuffer,
                             PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0,
                             0, null,
                             0, null,
                             1, ref barrier);

        _lapp.VulkanManager.SubmitCommand(commandBuffer);
    }

    /// <summary>
    /// 画像の解放
    /// </summary>
    public override void Dispose()
    {
        _image.Destroy(_lapp.VulkanManager.Device);
    }
}
