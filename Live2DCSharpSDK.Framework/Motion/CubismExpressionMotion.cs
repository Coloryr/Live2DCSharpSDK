using Live2DCSharpSDK.Framework.Model;
using System.Text.Json.Nodes;

namespace Live2DCSharpSDK.Framework.Motion;

/// <summary>
/// 表情のモーションクラス。
/// </summary>
public class CubismExpressionMotion : ACubismMotion
{
    /// <summary>
    /// 加算適用の初期値
    /// </summary>
    public const float DefaultAdditiveValue = 0.0f;
    /// <summary>
    /// 乗算適用の初期値
    /// </summary>
    public const float DefaultMultiplyValue = 1.0f;  

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
    public List<ExpressionParameter> Parameters { get; init; } = new();

    public float FadeWeight { get; private set; }

    /// <summary>
    /// インスタンスを作成する。
    /// </summary>
    /// <param name="buffer">expファイルが読み込まれているバッファ</param>
    public CubismExpressionMotion(string buf)
    {
        using var stream = File.Open(buf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var obj = JsonNode.Parse(stream) ?? throw new Exception("the motion json is error");
        var json = obj.AsObject();

        FadeInSeconds = json.ContainsKey(ExpressionKeyFadeIn)
            ? (float)json[ExpressionKeyFadeIn]! : DefaultFadeTime;   // フェードイン
        FadeOutSeconds = json.ContainsKey(ExpressionKeyFadeOut)
            ? (float)json[ExpressionKeyFadeOut]! : DefaultFadeTime; // フェードアウト

        // 各パラメータについて
        var list = json[ExpressionKeyParameters]!;
        int parameterCount = list.AsArray().Count;

        for (int i = 0; i < parameterCount; ++i)
        {
            var param = list[i]!;
            var parameterId = CubismFramework.CubismIdManager.GetId(param[ExpressionKeyId]!.ToString()); // パラメータID
            var value = (float)param[ExpressionKeyValue]!; // 値

            // 計算方法の設定
            ExpressionBlendType blendType;
            var type = param[ExpressionKeyBlend]?.ToString();
            if (type == null || type == BlendValueAdd)
            {
                blendType = ExpressionBlendType.Add;
            }
            else if (type == BlendValueMultiply)
            {
                blendType = ExpressionBlendType.Multiply;
            }
            else if (type == BlendValueOverwrite)
            {
                blendType = ExpressionBlendType.Overwrite;
            }
            else
            {
                // その他 仕様にない値を設定したときは加算モードにすることで復旧
                blendType = ExpressionBlendType.Add;
            }

            // 設定オブジェクトを作成してリストに追加する
            Parameters.Add(new()
            {
                ParameterId = parameterId,
                BlendType = blendType,
                Value = value
            });
        }
    }

    public override void DoUpdateParameters(CubismModel model, float userTimeSeconds, float weight, CubismMotionQueueEntry motionQueueEntry)
    {
        foreach (var item in Parameters)
        {
            switch (item.BlendType)
            {
                case ExpressionBlendType.Add:
                    {
                        model.AddParameterValue(item.ParameterId, item.Value, weight);            // 相対変化 加算
                        break;
                    }
                case ExpressionBlendType.Multiply:
                    {
                        model.MultiplyParameterValue(item.ParameterId, item.Value, weight);       // 相対変化 乗算
                        break;
                    }
                case ExpressionBlendType.Overwrite:
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

    /// <summary>
    /// モデルの表情に関するパラメータを計算する。
    /// </summary>
    /// <param name="model">対象のモデル</param>
    /// <param name="userTimeSeconds">対象のモデル</param>
    /// <param name="motionQueueEntry">CubismMotionQueueManagerで管理されているモーション</param>
    /// <param name="expressionParameterValues">モデルに適用する各パラメータの値</param>
    /// <param name="expressionIndex">表情のインデックス</param>
    public void CalculateExpressionParameters(CubismModel model, float userTimeSeconds, CubismMotionQueueEntry motionQueueEntry,
    List<ExpressionParameterValue> expressionParameterValues, int expressionIndex)
    {
        if (!motionQueueEntry.Available || motionQueueEntry.Finished)
        {
            return;
        }

        if (!motionQueueEntry.Started)
        {
            motionQueueEntry.Started = true;
            motionQueueEntry.StartTime = userTimeSeconds - OffsetSeconds; //モーションの開始時刻を記録
            motionQueueEntry.FadeInStartTime = userTimeSeconds; //フェードインの開始時刻

            float duration = GetDuration();

            if (motionQueueEntry.EndTime < 0.0f)
            {
                //開始していないうちに終了設定している場合がある。
                motionQueueEntry.EndTime = (duration <= 0.0f) ? -1 : motionQueueEntry.StartTime + duration;
                //duration == -1 の場合はループする
            }
        }

        FadeWeight = UpdateFadeWeight(motionQueueEntry, userTimeSeconds);

        // モデルに適用する値を計算
        for (int i = 0; i < expressionParameterValues.Count; ++i)
        {
            ExpressionParameterValue expressionParameterValue = expressionParameterValues[i];

            if (expressionParameterValue.ParameterId == null)
            {
                continue;
            }

            float currentParameterValue = expressionParameterValue.OverwriteValue =
                model.GetParameterValue(expressionParameterValue.ParameterId);

            var expressionParameters = Parameters;
            int parameterIndex = -1;
            for (int j = 0; j < expressionParameters.Count; ++j)
            {
                if (expressionParameterValue.ParameterId != expressionParameters[j].ParameterId)
                {
                    continue;
                }

                parameterIndex = j;

                break;
            }

            // 再生中のExpressionが参照していないパラメータは初期値を適用
            if (parameterIndex < 0)
            {
                if (expressionIndex == 0)
                {
                    expressionParameterValues[i].AdditiveValue = DefaultAdditiveValue;

                    expressionParameterValues[i].MultiplyValue = DefaultMultiplyValue;

                    expressionParameterValues[i].OverwriteValue = currentParameterValue;
                }
                else
                {
                    expressionParameterValues[i].AdditiveValue =
                        CalculateValue(expressionParameterValue.AdditiveValue, DefaultAdditiveValue);

                    expressionParameterValues[i].MultiplyValue =
                        CalculateValue(expressionParameterValue.MultiplyValue, DefaultMultiplyValue);

                    expressionParameterValues[i].OverwriteValue =
                        CalculateValue(expressionParameterValue.OverwriteValue, currentParameterValue);
                }
                continue;
            }

            // 値を計算
            float value = expressionParameters[parameterIndex].Value;
            float newAdditiveValue, newMultiplyValue, newSetValue;
            switch (expressionParameters[parameterIndex].BlendType)
            {
                case ExpressionBlendType.Add:
                    newAdditiveValue = value;
                    newMultiplyValue = DefaultMultiplyValue;
                    newSetValue = currentParameterValue;
                    break;
                case ExpressionBlendType.Multiply:
                    newAdditiveValue = DefaultAdditiveValue;
                    newMultiplyValue = value;
                    newSetValue = currentParameterValue;
                    break;
                case ExpressionBlendType.Overwrite:
                    newAdditiveValue = DefaultAdditiveValue;
                    newMultiplyValue = DefaultMultiplyValue;
                    newSetValue = value;
                    break;
                default:
                    return;
            }

            if (expressionIndex == 0)
            {
                expressionParameterValues[i].AdditiveValue = newAdditiveValue;
                expressionParameterValues[i].MultiplyValue = newMultiplyValue;
                expressionParameterValues[i].OverwriteValue = newSetValue;
            }
            else
            {
                expressionParameterValues[i].AdditiveValue = (expressionParameterValue.AdditiveValue * (1.0f - FadeWeight)) + newAdditiveValue * FadeWeight;
                expressionParameterValues[i].MultiplyValue = (expressionParameterValue.MultiplyValue * (1.0f - FadeWeight)) + newMultiplyValue * FadeWeight;
                expressionParameterValues[i].OverwriteValue = (expressionParameterValue.OverwriteValue * (1.0f - FadeWeight)) + newSetValue * FadeWeight;
            }
        }
    }

    private float CalculateValue(float source, float destination)
    {
        return (source * (1.0f - FadeWeight)) + (destination * FadeWeight);
    }
}
