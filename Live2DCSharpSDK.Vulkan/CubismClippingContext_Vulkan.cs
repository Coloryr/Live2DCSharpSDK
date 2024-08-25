using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Live2DCSharpSDK.Framework.Rendering;

namespace Live2DCSharpSDK.Vulkan;

public class CubismClippingContext_Vulkan : CubismClippingContext
{
    public unsafe CubismClippingContext_Vulkan(CubismClippingManager manager, int* clippingDrawableIndices, int clipCount) : base(manager, clippingDrawableIndices, clipCount)
    {
        
    }
}
