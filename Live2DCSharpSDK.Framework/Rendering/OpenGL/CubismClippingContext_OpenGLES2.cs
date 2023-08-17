using Live2DCSharpSDK.Framework.Model;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public class CubismClippingContext_OpenGLES2 : CubismClippingContext
{
    private CubismModel _model;
    public unsafe CubismClippingContext_OpenGLES2(CubismClippingManager manager, CubismModel model, int* clippingDrawableIndices, int clipCount) : base(manager, clippingDrawableIndices, clipCount)
    {
        _model = model;

    }
}
