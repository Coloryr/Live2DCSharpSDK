using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Rendering;

public abstract class CubismOffscreenSurface
{
    public abstract void EndDraw();
    public abstract void BeginDraw(int fbo);
}
