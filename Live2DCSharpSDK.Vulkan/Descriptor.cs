using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

/// <summary>
/// ディスクリプタセットとUBOを保持する構造体コマンドバッファでまとめて描画する場合、ディスクリプタセットをバインド後に更新してはならない。
/// <para></para>
/// マスクされる描画対象のみ、フレームバッファをディスクリプタセットに設定する必要があるが、その際バインド済みのディスクリプタセットを更新しないよう、別でマスクされる用のディスクリプタセットを用意する。
/// </summary>
public class Descriptor
{
    /// <summary>
    /// ユニフォームバッファ
    /// </summary>
    public CubismBufferVulkan UniformBuffer;
    /// <summary>
    /// ディスクリプタセット
    /// </summary>
    public DescriptorSet DescriptorSet;
    /// <summary>
    /// ディスクリプタセットが更新されたか
    /// </summary>
    public bool IsDescriptorSetUpdated;
    /// <summary>
    /// マスクされる描画対象用のディスクリプタセット
    /// </summary>
    public DescriptorSet DescriptorSetMasked;
    /// <summary>
    /// マスクされる描画対象用のディスクリプタセットが更新されたか
    /// </summary>
    public bool IsDescriptorSetMaskedUpdated;
}
