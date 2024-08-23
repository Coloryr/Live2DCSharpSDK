using System.Runtime.InteropServices;
using Live2DCSharpSDK.Framework;
using Silk.NET.Vulkan;

using Buffer = Silk.NET.Vulkan.Buffer;

namespace Live2DCSharpSDK.Vulkan;

/// <summary>
/// バッファを扱うクラス
/// </summary>
/// <param name="vk"></param>
public class CubismBufferVulkan(Vk vk)
{
    //VkDeviceSize ulong

    /// <summary>
    /// バッファ
    /// </summary>
    public Buffer Buffer => _buffer;

    private Buffer _buffer;

    /// <summary>
    /// メモリ
    /// </summary>
    private DeviceMemory _memory;
    /// <summary>
    /// マップ領域へのアドレス
    /// </summary>
    private unsafe void* _mapped;

    /// <summary>
    /// 物理デバイスのメモリタイプのインデックスを探す
    /// </summary>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="typeFilter">メモリタイプが存在していたら設定されるビットマスク</param>
    /// <param name="properties">メモリがデバイスにアクセスするときのタイプ</param>
    /// <returns></returns>
    public unsafe int FindMemoryType(PhysicalDevice physicalDevice, int typeFilter, MemoryPropertyFlags properties)
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
    /// バッファを作成する
    /// </summary>
    /// <param name="device">デバイス</param>
    /// <param name="physicalDevice">物理デバイス</param>
    /// <param name="size">バッファサイズ</param>
    /// <param name="usage">バッファの使用法を指定するビットマスク</param>
    /// <param name="properties">メモリがデバイスにアクセスする際のタイプ</param>
    public unsafe void CreateBuffer(Device device, PhysicalDevice physicalDevice, ulong size, BufferUsageFlags usage,
                      MemoryPropertyFlags properties)
    {
        var bufferInfo = new BufferCreateInfo()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };
        if (vk.CreateBuffer(device, ref bufferInfo, null, out _buffer) != Result.Success)
        {
            CubismLog.Error("[Live2D Vulkan]failed to create buffer!");
        }

        vk.GetBufferMemoryRequirements(device, _buffer, out var memRequirements);

        var allocInfo = new MemoryAllocateInfo()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = (uint)FindMemoryType(physicalDevice, (int)memRequirements.MemoryTypeBits, properties)
        };
        if (vk.AllocateMemory(device, ref allocInfo, null, out _memory) != Result.Success)
        {
            CubismLog.Error("[Live2D Vulkan]failed to allocate buffer memory!");
        }
        vk.BindBufferMemory(device, _buffer, _memory, 0);
    }

    /// <summary>
    /// メモリをアドレス空間にマップし、そのアドレスポインタを取得する
    /// </summary>
    /// <param name="device">デバイス</param>
    /// <param name="size">マップするサイズ</param>
    public unsafe void Map(Device device, ulong size)
    {
        vk.MapMemory(device, _memory, 0, size, 0, ref _mapped);
    }

    /// <summary>
    /// メモリブロックをコピーする
    /// </summary>
    /// <param name="src">コピーするデータ</param>
    /// <param name="size">コピーするサイズ</param>
    public unsafe void MemCpy(void* src, long size)
    {
        System.Buffer.MemoryCopy(src, _mapped, size, size);
    }

    /// <summary>
    /// リソースを破棄する
    /// </summary>
    /// <param name="device">デバイス</param>
    public void UnMap(Device device)
    {
        vk.UnmapMemory(device, _memory);
    }

    public unsafe void Destroy(Device device)
    {
        vk.DestroyBuffer(device, Buffer, null);
        vk.FreeMemory(device, _memory, null);
    }
}
