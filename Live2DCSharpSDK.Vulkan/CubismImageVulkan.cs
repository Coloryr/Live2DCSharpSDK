using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Live2DCSharpSDK.Framework;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

/// <summary>
/// イメージを扱うクラス
/// </summary>
public class CubismImageVulkan(Vk vk)
{
    /// <summary>
    /// バッファ
    /// </summary>
    public Image Image => _image;

    private Image _image;

    /// <summary>
    /// メモリ
    /// </summary>
    public DeviceMemory Memory => _memory;

    private DeviceMemory _memory;

    /// <summary>
    /// ビュー
    /// </summary>
    public ImageView View => _view;

    private ImageView _view;

    /// <summary>
    /// サンプラー
    /// </summary>
    public Sampler Sampler => _sampler;

    private Sampler _sampler;

    /// <summary>
    /// 現在のイメージレイアウト
    /// </summary>
    public ImageLayout CurrentLayout { get; private set; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }

    /// <summary>
    /// 物理デバイスのメモリタイプのインデックスを探す
    /// </summary>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="typeFilter">メモリタイプが存在していたら設定されるビットマスク</param>
    /// <param name="properties">メモリがデバイスにアクセスするときのタイプ</param>
    /// <returns></returns>
    public unsafe int FindMemoryType(PhysicalDevice physicalDevice, uint typeFilter, MemoryPropertyFlags properties)
    {
        vk.GetPhysicalDeviceMemoryProperties(physicalDevice, out var memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }
        CubismLog.Error("[Live2D Vulkan]failed to find suitable memory type!");
        return 0;
    }

    /// <summary>
    /// イメージを作成する
    /// </summary>
    /// <param name="device">デバイス</param>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="w">横幅</param>
    /// <param name="h">高さ</param>
    /// <param name="mipLevel">ミップマップのレベル</param>
    /// <param name="format">フォーマット</param>
    /// <param name="tiling">タイリング配置の設定</param>
    /// <param name="usage">イメージの使用目的を指定するビットマスク</param>
    public unsafe void CreateImage(Device device, PhysicalDevice physicalDevice, uint w, uint h,
                     uint mipLevel, Format format, ImageTiling tiling, ImageUsageFlags usage)
    {
        Width = w;
        Height = h;

        var imageInfo = new ImageCreateInfo()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            MipLevels = mipLevel,
            ArrayLayers = 1,
            Format = format,
            Tiling = tiling,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive,
            Extent = new()
            { 
                Width = w,
                Height = h,
                Depth = 1
            }
        };

        if (vk.CreateImage(device, ref imageInfo, null, out _image) != Result.Success)
        {
            CubismLog.Error("[Live2D Vulkan]failed to create image!");
        }

        vk.GetImageMemoryRequirements(device, _image, out var memRequirements);

        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = (uint)FindMemoryType(physicalDevice, memRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        if (vk.AllocateMemory(device, ref allocInfo, null, out _memory) != Result.Success)
        {
            CubismLog.Error("[Live2D Vulkan]failed to allocate image memory!");
        }
        vk.BindImageMemory(device, _image, Memory, 0);
    }

    /// <summary>
    /// ビューを作成する
    /// </summary>
    /// <param name="device">デバイス</param>
    /// <param name="format">フォーマット</param>
    /// <param name="aspectFlags">どのアスペクトマスクがビューに含まれるかを指定するビットマスク</param>
    /// <param name="mipLevel">ミップマップのレベル</param>
    public unsafe void CreateView(Device device, Format format, ImageAspectFlags aspectFlags, uint mipLevel)
    {
        var viewInfo = new ImageViewCreateInfo()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            SubresourceRange = new(aspectFlags, 0, mipLevel, 0, 1)
        };

        if (vk.CreateImageView(device, ref viewInfo, null, out _view) != Result.Success)
        {
            CubismLog.Error("[Live2D Vulkan]failed to create texture image view!");
        }
    }

    /// <summary>
    /// サンプラーを作成する
    /// </summary>
    /// <param name="device">デバイス</param>
    /// <param name="maxAnistropy">異方性の値の最大値</param>
    /// <param name="mipLevel">ミップマップのレベル</param>
    public unsafe void CreateSampler(Device device, float maxAnistropy, uint mipLevel)
    {
        var samplerInfo = new SamplerCreateInfo()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            AnisotropyEnable = true,
            MaxAnisotropy = maxAnistropy,
            BorderColor = BorderColor.FloatTransparentBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
            MinLod = 0.0f,
            MaxLod = mipLevel,
            MipLodBias = 0.0f
        };

        if (vk.CreateSampler(device, ref samplerInfo, null, out _sampler) != Result.Success)
        {
            CubismLog.Error("[Live2D Vulkan]failed to create texture sampler!");
        }
    }

    /// <summary>
    /// イメージのレイアウトを変更する
    /// </summary>
    /// <param name="commandBuffer">コマンドバッファ</param>
    /// <param name="newLayout">新しいレイアウト</param>
    /// <param name="mipLevels">ミップマップのレベル</param>
    /// <param name="aspectMask"></param>
    public unsafe void SetImageLayout(CommandBuffer commandBuffer, ImageLayout newLayout, uint mipLevels, ImageAspectFlags aspectMask)
    {
        var barrier = new ImageMemoryBarrier()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = CurrentLayout,
            NewLayout = newLayout,
            Image = Image,
            SubresourceRange = new(aspectMask, 0, mipLevels, 0, 1)
        };

        var sourceStage = PipelineStageFlags.AllCommandsBit;
        var destinationStage = PipelineStageFlags.AllCommandsBit;

        switch (CurrentLayout)
        {
            case  ImageLayout.Undefined:
                barrier.SrcAccessMask = 0;
                break;

            case ImageLayout.ColorAttachmentOptimal:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                break;

            case ImageLayout.DepthStencilAttachmentOptimal:
                barrier.SrcAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;
                break;

            case ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                break;

            case ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                sourceStage = PipelineStageFlags.TransferBit;
                break;

            case ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
                break;

            default:
                break;
        }

        switch (newLayout)
        {
            case ImageLayout.TransferSrcOptimal:
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                destinationStage = PipelineStageFlags.TransferBit;
                break;

            case ImageLayout.TransferDstOptimal:
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                destinationStage = PipelineStageFlags.TransferBit;
                break;
            //TODO Test this
            case ImageLayout.ReadOnlyOptimal:
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                destinationStage = PipelineStageFlags.FragmentShaderBit;
                destinationStage = PipelineStageFlags.RayTracingShaderBitKhr;
                break;

            default:
                break;
        }

        vk.CmdPipelineBarrier(
            commandBuffer,
            sourceStage,
            destinationStage,
            0,
            0,
            null,
            0,
            null,
            1,
            ref barrier
        );

        CurrentLayout = newLayout;
    }

    /// <summary>
    /// vkCmdEndRendering後のパイプラインバリアで変わってしまうイメージレイアウトを保存する
    /// </summary>
    /// <param name="newLayout">新しいレイアウト</param>
    public void SetCurrentLayout(ImageLayout newLayout)
    {
        CurrentLayout = newLayout;
    }

    /// <summary>
    /// リソースを破棄する
    /// </summary>
    /// <param name="device">デバイス</param>
    public unsafe void Destroy(Device device)
    {
        if (_image.Handle != 0)
        {
            vk.DestroyImage(device, _image, null);
            _image.Handle = 0;
        }
        if (_view.Handle != 0)
        {
            vk.DestroyImageView(device, _view, null);
            _view.Handle = 0;
        }
        if (_memory.Handle != 0)
        {
            vk.FreeMemory(device, _memory, null);
            _memory.Handle = 0;
        }
        if (_sampler.Handle != 0)
        {
            vk.DestroySampler(device, _sampler, null);
            _sampler.Handle = 0;
        }
    }
}
