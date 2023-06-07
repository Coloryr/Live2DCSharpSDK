using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Rendering;

/// <summary>
/// カラーブレンディングのモード
/// </summary>
public enum CubismBlendMode
{
    /// <summary>
    /// 通常
    /// </summary>
    CubismBlendMode_Normal = 0,
    /// <summary>
    /// 加算
    /// </summary>
    CubismBlendMode_Additive = 1,
    /// <summary>
    /// 乗算
    /// </summary>
    CubismBlendMode_Multiplicative = 2,
};
