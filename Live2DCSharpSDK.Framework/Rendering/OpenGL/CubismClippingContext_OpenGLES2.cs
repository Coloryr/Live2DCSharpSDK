using Live2DCSharpSDK.Framework.Model;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public unsafe class CubismClippingContext_OpenGLES2(CubismClippingManager manager, CubismModel model, 
    int* clippingDrawableIndices, int clipCount) : CubismClippingContext(manager, clippingDrawableIndices, clipCount)
{

}
