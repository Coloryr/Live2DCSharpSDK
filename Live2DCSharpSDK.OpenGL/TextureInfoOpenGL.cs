using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Live2DCSharpSDK.App;

namespace Live2DCSharpSDK.OpenGL;

public record TextureInfoOpenGL : TextureInfo
{
    /// <summary>
    /// テクスチャID
    /// </summary>
    public int ID;
}
