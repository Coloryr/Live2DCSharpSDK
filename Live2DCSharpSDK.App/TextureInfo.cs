namespace Live2DCSharpSDK.App;

/// <summary>
/// 画像情報構造体
/// </summary>
public abstract record TextureInfo
{
    /// <summary>
    /// 横幅
    /// </summary>
    public int Width;
    /// <summary>
    /// 高さ
    /// </summary>
    public int Height;
    /// <summary>
    /// ファイル名
    /// </summary>
    public string FileName;
};