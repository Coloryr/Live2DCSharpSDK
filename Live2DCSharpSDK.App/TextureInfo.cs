using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.App;

/// <summary>
/// 画像情報構造体
/// </summary>
public record TextureInfo
{
    /// <summary>
    /// テクスチャID
    /// </summary>
    public int id;
    /// <summary>
    /// 横幅
    /// </summary>
    public int width;
    /// <summary>
    /// 高さ
    /// </summary>
    public int height;
    /// <summary>
    /// ファイル名
    /// </summary>
    public required string fileName;
};