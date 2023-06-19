using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public enum ShaderNames : int
{
    // SetupMask
    SetupMask,

    //Normal
    Normal,
    NormalMasked,
    NormalMaskedInverted,
    NormalPremultipliedAlpha,
    NormalMaskedPremultipliedAlpha,
    NormalMaskedInvertedPremultipliedAlpha,

    //Add
    Add,
    AddMasked,
    AddMaskedInverted,
    AddPremultipliedAlpha,
    AddMaskedPremultipliedAlpha,
    AddMaskedPremultipliedAlphaInverted,

    //Mult
    Mult,
    MultMasked,
    MultMaskedInverted,
    MultPremultipliedAlpha,
    MultMaskedPremultipliedAlpha,
    MultMaskedPremultipliedAlphaInverted,
};