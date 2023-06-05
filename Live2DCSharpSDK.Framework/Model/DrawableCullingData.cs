using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Model;

/// <summary>
/// テクスチャのカリング設定を管理するための構造体
/// </summary>
public record DrawableCullingData
{
    public bool IsOverwritten { get; set; }
    public int IsCulling { get; set; }
}
