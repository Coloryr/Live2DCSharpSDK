using Live2DCSharpSDK.Framework.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public class CubismClippingContext_OpenGLES2 : CubismClippingContext
{
    private CubismModel _model;
    public unsafe CubismClippingContext_OpenGLES2(CubismClippingManager manager, CubismModel model, int* clippingDrawableIndices, int clipCount) :base(manager,  clippingDrawableIndices, clipCount)
    {
        _model = model;

    }
}
