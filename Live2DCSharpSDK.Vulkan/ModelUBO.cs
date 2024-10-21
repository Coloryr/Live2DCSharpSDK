using System.Runtime.InteropServices;

namespace Live2DCSharpSDK.Vulkan;

/// <summary>
/// モデル用ユニフォームバッファオブジェクトの中身を保持する構造体
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ModelUBO
{
    /// <summary>
    /// シェーダープログラムに渡すデータ(ProjectionMatrix)
    /// </summary>
    public fixed float ProjectionMatrix[16];
    /// <summary>
    /// シェーダープログラムに渡すデータ(ClipMatrix)
    /// </summary>
    public fixed float ClipMatrix[16];
    /// <summary>
    /// シェーダープログラムに渡すデータ(BaseColor)
    /// </summary>
    public fixed float BaseColor[4];
    /// <summary>
    /// シェーダープログラムに渡すデータ(MultiplyColor)
    /// </summary>
    public fixed float MultiplyColor[4];
    /// <summary>
    /// シェーダープログラムに渡すデータ(ScreenColor)
    /// </summary>
    public fixed float ScreenColor[4];
    /// <summary>
    /// シェーダープログラムに渡すデータ(ChannelFlag)
    /// </summary>
    public fixed float ChannelFlag[4];
}