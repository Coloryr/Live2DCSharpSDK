using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Vulkan;

public class ShaderNames
{
    // SetupMask
    public const int ShaderNames_SetupMask = 0;

    //Normal
    public const int ShaderNames_Normal = 1;
    public const int ShaderNames_NormalMasked = 2;
    public const int ShaderNames_NormalMaskedInverted = 3;
    public const int ShaderNames_NormalPremultipliedAlpha = 4;
    public const int ShaderNames_NormalMaskedPremultipliedAlpha = 5;
    public const int ShaderNames_NormalMaskedInvertedPremultipliedAlpha = 7;

    //Add
    public const int ShaderNames_Add = 8;
    public const int ShaderNames_AddMasked = 9;
    public const int ShaderNames_AddMaskedInverted = 10;
    public const int ShaderNames_AddPremultipliedAlpha = 11;
    public const int ShaderNames_AddMaskedPremultipliedAlpha = 12;
    public const int ShaderNames_AddMaskedPremultipliedAlphaInverted = 13;

    //Mult
    public const int ShaderNames_Mult = 14;
    public const int ShaderNames_MultMasked = 15;
    public const int ShaderNames_MultMaskedInverted = 16;
    public const int ShaderNames_MultPremultipliedAlpha = 17;
    public const int ShaderNames_MultMaskedPremultipliedAlpha = 18;
    public const int ShaderNames_MultMaskedPremultipliedAlphaInverted = 19;
}
