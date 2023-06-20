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