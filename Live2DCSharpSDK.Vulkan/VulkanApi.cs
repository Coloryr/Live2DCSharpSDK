using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

public abstract class VulkanApi
{
    public abstract void GetWindowSize(out int width, out int height);
    public abstract unsafe byte** GetRequiredExtensions(out uint count);
    public abstract unsafe VkNonDispatchableHandle CreateSurface<T>(VkHandle handle, T* handel1) where T : struct;
}
