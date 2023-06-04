using Live2DCSharpSDK.Framework.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Motion;

/// <summary>
/// 表情パラメータ値の計算方式
/// </summary>
public enum ExpressionBlendType
{
    /// <summary>
    /// 加算
    /// </summary>
    ExpressionBlendType_Add = 0,
    /// <summary>
    /// 乗算
    /// </summary>
    ExpressionBlendType_Multiply = 1,
    /// <summary>
    /// 上書き
    /// </summary>
    ExpressionBlendType_Overwrite = 2   
};

/// <summary>
/// 表情のパラメータ情報の構造体。
/// </summary>
public record ExpressionParameter
{
    /// <summary>
    /// パラメータID
    /// </summary>
    public string ParameterId;
    /// <summary>
    /// パラメータの演算種類
    /// </summary>
    public ExpressionBlendType BlendType;
    /// <summary>
    /// 値
    /// </summary>
    public float Value;              
}

/// <summary>
/// 表情のモーションクラス。
/// </summary>
public class CubismExpressionMotion : ACubismMotion
{
    public const string ExpressionKeyFadeIn = "FadeInTime";
    public const string ExpressionKeyFadeOut = "FadeOutTime";
    public const string ExpressionKeyParameters = "Parameters";
    public const string ExpressionKeyId = "Id";
    public const string ExpressionKeyValue = "Value";
    public const string ExpressionKeyBlend = "Blend";
    public const string BlendValueAdd = "Add";
    public const string BlendValueMultiply = "Multiply";
    public const string BlendValueOverwrite = "Overwrite";
    public const float DefaultFadeTime = 1.0f;

    /// <summary>
    /// 表情のパラメータ情報リスト
    /// </summary>
    protected readonly List<ExpressionParameter> _parameters = new();

    /// <summary>
    /// インスタンスを作成する。
    /// </summary>
    /// <param name="buffer">expファイルが読み込まれているバッファ</param>
    public CubismExpressionMotion(string buf)
    {
        var json = JObject.Parse(buf);

        SetFadeInTime(json.ContainsKey(ExpressionKeyFadeIn)
            ? (float)json[ExpressionKeyFadeIn]! : DefaultFadeTime);   // フェードイン
        SetFadeOutTime(json.ContainsKey(ExpressionKeyFadeOut)
            ? (float)json[ExpressionKeyFadeOut]! : DefaultFadeTime); // フェードアウト

        // 各パラメータについて
        int parameterCount = json[ExpressionKeyParameters].Count();

        for (int i = 0; i < parameterCount; ++i)
        {
            var param = json[ExpressionKeyParameters][i];
            var parameterId = CubismFramework.GetIdManager().GetId(param[ExpressionKeyId].ToString()); // パラメータID
            var value = (float)param[ExpressionKeyValue]; // 値

            // 計算方法の設定
            ExpressionBlendType blendType;
            var type = param[ExpressionKeyBlend]?.ToString();
            if (type == null || type == BlendValueAdd)
            {
                blendType = ExpressionBlendType.ExpressionBlendType_Add;
            }
            else if (type == BlendValueMultiply)
            {
                blendType = ExpressionBlendType.ExpressionBlendType_Multiply;
            }
            else if (type == BlendValueOverwrite)
            {
                blendType = ExpressionBlendType.ExpressionBlendType_Overwrite;
            }
            else
            {
                // その他 仕様にない値を設定したときは加算モードにすることで復旧
                blendType = ExpressionBlendType.ExpressionBlendType_Add;
            }

            // 設定オブジェクトを作成してリストに追加する
            ExpressionParameter item = new()
            {
                ParameterId = parameterId,
                BlendType = blendType,
                Value = value
            };

            _parameters.Add(item);
        }
    }

    public override void DoUpdateParameters(CubismModel model, float userTimeSeconds, float weight, CubismMotionQueueEntry motionQueueEntry)
    {
        foreach (var item in _parameters)
        {
            switch (item.BlendType)
            {
                case ExpressionBlendType.ExpressionBlendType_Add:
                    {
                        model.AddParameterValue(item.ParameterId, item.Value, weight);            // 相対変化 加算
                        break;
                    }
                case ExpressionBlendType.ExpressionBlendType_Multiply:
                    {
                        model.MultiplyParameterValue(item.ParameterId, item.Value, weight);       // 相対変化 乗算
                        break;
                    }
                case ExpressionBlendType.ExpressionBlendType_Overwrite:
                    {
                        model.SetParameterValue(item.ParameterId, item.Value, weight);            // 絶対変化 上書き
                        break;
                    }
                default:
                    // 仕様にない値を設定したときは既に加算モードになっている
                    break;
            }
        }
    }
}
