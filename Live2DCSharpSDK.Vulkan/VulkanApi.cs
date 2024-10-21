using Silk.NET.Core.Native;

namespace Live2DCSharpSDK.Vulkan;

public abstract class VulkanApi
{
    public abstract void GetWindowSize(out int width, out int height);
    public abstract unsafe byte** GetRequiredExtensions(out uint count);
    public abstract unsafe VkNonDispatchableHandle CreateSurface<T>(VkHandle handle, T* handel1) where T : unmanaged;
}
