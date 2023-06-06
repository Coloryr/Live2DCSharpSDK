using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Motion;

/// <summary>
/// 表情のパラメータ情報の構造体。
/// </summary>
public record ExpressionParameter
{
    /// <summary>
    /// パラメータID
    /// </summary>
    public required string ParameterId { get; set; }
    /// <summary>
    /// パラメータの演算種類
    /// </summary>
    public ExpressionBlendType BlendType { get; set; }
    /// <summary>
    /// 値
    /// </summary>
    public float Value { get; set; }
}
