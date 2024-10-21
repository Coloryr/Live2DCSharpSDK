using Live2DCSharpSDK.Framework.Rendering;

namespace Live2DCSharpSDK.Vulkan;

public unsafe class CubismClippingContext_Vulkan(CubismClippingManager manager, int* clippingDrawableIndices, int clipCount)
    : CubismClippingContext(manager, clippingDrawableIndices, clipCount)
{

}
