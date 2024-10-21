namespace Live2DCSharpSDK.Vulkan;

public class QueueFamilyIndices
{
    // 描画用と表示用のキューファミリは必ずしも一致するとは限らないので分ける

    /// <summary>
    /// 描画コマンドに使用するキューファミリ
    /// </summary>
    public int GraphicsFamily = -1;
    /// <summary>
    /// 表示に使用するキューファミリ
    /// </summary>
    public int PresentFamily = -1;

    /// <summary>
    /// 対応するキューファミリがあるか
    /// </summary>
    /// <returns></returns>
    public bool IsComplete()
    {
        return GraphicsFamily != -1 && PresentFamily != -1;
    }
}
