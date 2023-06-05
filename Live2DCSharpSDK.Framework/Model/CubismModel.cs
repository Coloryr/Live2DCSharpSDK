using Live2DCSharpSDK.Core;
using Live2DCSharpSDK.Framework.Rendering;
using System.Numerics;

namespace Live2DCSharpSDK.Framework.Model;
using csmParameterType = Int32;

public unsafe class CubismModel : IDisposable
{
    /// <summary>
    /// 存在していないパーツの不透明度のリスト
    /// </summary>
    private readonly Dictionary<int, float> _notExistPartOpacities = new();
    /// <summary>
    /// 存在していないパーツIDのリスト
    /// </summary>
    private readonly Dictionary<string, int> _notExistPartId = new();
    /// <summary>
    /// 存在していないパラメータの値のリスト
    /// </summary>
    private readonly Dictionary<int, float> _notExistParameterValues = new();
    /// <summary>
    /// 存在していないパラメータIDのリスト
    /// </summary>
    private readonly Dictionary<string, int> _notExistParameterId = new();
    /// <summary>
    /// 保存されたパラメータ
    /// </summary>
    private readonly List<float> _savedParameters = new();
    /// <summary>
    /// モデル
    /// </summary>
    public IntPtr Model { get; }
    /// <summary>
    /// パラメータの値のリスト
    /// </summary>
    private float* _parameterValues;
    /// <summary>
    /// パラメータの最大値のリスト
    /// </summary>
    private float* _parameterMaximumValues;
    /// <summary>
    /// パラメータの最小値のリスト
    /// </summary>
    private float* _parameterMinimumValues;
    /// <summary>
    /// パーツの不透明度のリスト
    /// </summary>
    private float* _partOpacities;
    /// <summary>
    /// モデルの不透明度
    /// </summary>
    private float _modelOpacity;

    private readonly List<string> _parameterIds = new();
    private readonly List<string> _partIds = new();
    private readonly List<string> _drawableIds = new();
    /// <summary>
    /// Drawable 乗算色の配列
    /// </summary>
    private readonly List<DrawableColorData> _userScreenColors = new();
    /// <summary>
    /// Drawable スクリーン色の配列
    /// </summary>
    private readonly List<DrawableColorData> _userMultiplyColors = new();
    /// <summary>
    /// カリング設定の配列
    /// </summary>
    private readonly List<DrawableCullingData> _userCullings = new();
    /// <summary>
    /// Part 乗算色の配列
    /// </summary>
    private readonly List<PartColorData> _userPartScreenColors = new();
    /// <summary>
    /// Part スクリーン色の配列
    /// </summary>
    private readonly List<PartColorData> _userPartMultiplyColors = new();
    /// <summary>
    /// Partの子DrawableIndexの配列
    /// </summary>
    private List<int>[] _partChildDrawables;
    /// <summary>
    /// 乗算色を全て上書きするか？
    /// </summary>
    private bool _isOverwrittenModelMultiplyColors;
    /// <summary>
    /// スクリーン色を全て上書きするか？
    /// </summary>
    private bool _isOverwrittenModelScreenColors;
    /// <summary>
    /// モデルのカリング設定をすべて上書きするか？
    /// </summary>
    private bool _isOverwrittenCullings;

    public CubismModel(IntPtr model)
    {
        Model = model;
        _modelOpacity = 1.0f;
    }

    public void Dispose()
    {
        CubismFramework.DeallocateAligned(new IntPtr(Model));
        GC.SuppressFinalize(this);
    }

    public unsafe void Initialize()
    {
        _parameterValues = CubismCore.csmGetParameterValues(Model);
        _partOpacities = CubismCore.csmGetPartOpacities(Model);
        _parameterMaximumValues = CubismCore.csmGetParameterMaximumValues(Model);
        _parameterMinimumValues = CubismCore.csmGetParameterMinimumValues(Model);

        {
            var parameterIds = CubismCore.csmGetParameterIds(Model);
            var parameterCount = CubismCore.csmGetParameterCount(Model);

            for (int i = 0; i < parameterCount; ++i)
            {
                var str = new string((sbyte*)parameterIds[i]);
                _parameterIds.Add(CubismFramework.GetIdManager().GetId(str));
            }
        }

        int partCount = CubismCore.csmGetPartCount(Model);
        {
            var partIds = CubismCore.csmGetPartIds(Model);

            _partChildDrawables = new List<csmParameterType>[partCount];
            for (int i = 0; i < partCount; ++i)
            {
                var str = new string((sbyte*)partIds[i]);
                _partIds.Add(CubismFramework.GetIdManager().GetId(str));
                _partChildDrawables[i] = new();
            }
        }

        {
            var drawableIds = CubismCore.csmGetDrawableIds(Model);
            var drawableCount = CubismCore.csmGetDrawableCount(Model);

            // カリング設定
            DrawableCullingData userCulling = new()
            {
                IsOverwritten = false,
                IsCulling = 0
            };

            // 乗算色
            CubismTextureColor multiplyColor = new()
            {
                R = 1.0f,
                G = 1.0f,
                B = 1.0f,
                A = 1.0f
            };

            // スクリーン色
            CubismTextureColor screenColor = new()
            {
                R = 0.0f,
                G = 0.0f,
                B = 0.0f,
                A = 1.0f
            };

            // Parts
            {
                // 乗算色
                PartColorData userMultiplyColor = new()
                {
                    IsOverwritten = false,
                    Color = multiplyColor
                };

                // スクリーン色
                PartColorData userScreenColor = new()
                {
                    IsOverwritten = false,
                    Color = screenColor
                };

                for (int i = 0; i < partCount; ++i)
                {
                    _userPartMultiplyColors.Add(userMultiplyColor);
                    _userPartScreenColors.Add(userScreenColor);
                }
            }

            // Drawables
            {
                // 乗算色
                DrawableColorData userMultiplyColor = new()
                {
                    IsOverwritten = false,
                    Color = multiplyColor
                };

                // スクリーン色
                DrawableColorData userScreenColor = new()
                {
                    IsOverwritten = false,
                    Color = screenColor
                };

                for (int i = 0; i < drawableCount; ++i)
                {
                    var str = new string((sbyte*)drawableIds[i]);
                    _drawableIds.Add(CubismFramework.GetIdManager().GetId(str));
                    _userMultiplyColors.Add(userMultiplyColor);
                    _userScreenColors.Add(userScreenColor);
                    _userCullings.Add(userCulling);

                    var parentIndex = CubismCore.csmGetDrawableParentPartIndices(Model)[i];
                    if (parentIndex >= 0)
                    {
                        _partChildDrawables[parentIndex].Add(i);
                    }
                }
            }
        }
    }

    /// <summary>
    /// partのOverwriteColor Set関数
    /// </summary>
    public void SetPartColor(
       int partIndex,
       float r, float g, float b, float a,
       List<PartColorData> partColors,
       List<DrawableColorData> drawableColors)
    {
        partColors[partIndex].Color.R = r;
        partColors[partIndex].Color.G = g;
        partColors[partIndex].Color.B = b;
        partColors[partIndex].Color.A = a;

        if (partColors[partIndex].IsOverwritten)
        {
            for (int i = 0; i < _partChildDrawables[partIndex].Count; i++)
            {
                int drawableIndex = _partChildDrawables[partIndex][i];
                drawableColors[drawableIndex].Color.R = r;
                drawableColors[drawableIndex].Color.G = g;
                drawableColors[drawableIndex].Color.B = b;
                drawableColors[drawableIndex].Color.A = a;
            }
        }
    }

    /// <summary>
    /// partのOverwriteFlag Set関数
    /// </summary>
    public void SetOverwriteColorForPartColors(
        int partIndex,
        bool value,
        List<PartColorData> partColors,
        List<DrawableColorData> drawableColors)
    {
        partColors[partIndex].IsOverwritten = value;

        for (int i = 0; i < _partChildDrawables[partIndex].Count; i++)
        {
            int drawableIndex = _partChildDrawables[partIndex][i];
            drawableColors[drawableIndex].IsOverwritten = value;
            if (value)
            {
                drawableColors[drawableIndex].Color.R = partColors[partIndex].Color.R;
                drawableColors[drawableIndex].Color.G = partColors[partIndex].Color.G;
                drawableColors[drawableIndex].Color.B = partColors[partIndex].Color.B;
                drawableColors[drawableIndex].Color.A = partColors[partIndex].Color.A;
            }
        }
    }

    /// <summary>
    /// モデルのパラメータを更新する。
    /// </summary>
    public void Update()
    {
        // Update model.
        CubismCore.csmUpdateModel(Model);
        // Reset dynamic drawable flags.
        CubismCore.csmResetDrawableDynamicFlags(Model);
    }

    /// <summary>
    /// Pixel単位でキャンバスの幅の取得
    /// </summary>
    /// <returns>キャンバスの幅(pixel)</returns>
    public float GetCanvasWidthPixel()
    {
        if (new IntPtr(Model) == IntPtr.Zero)
        {
            return 0.0f;
        }

        Vector2 tmpSizeInPixels;
        Vector2 tmpOriginInPixels;
        float tmpPixelsPerUnit;

        CubismCore.csmReadCanvasInfo(Model, &tmpSizeInPixels, &tmpOriginInPixels, &tmpPixelsPerUnit);

        return tmpSizeInPixels.X;
    }

    /// <summary>
    /// Pixel単位でキャンバスの高さの取得
    /// </summary>
    /// <returns>キャンバスの高さ(pixel)</returns>
    public float GetCanvasHeightPixel()
    {
        if (new IntPtr(Model) == IntPtr.Zero)
        {
            return 0.0f;
        }

        Vector2 tmpSizeInPixels;
        Vector2 tmpOriginInPixels;
        float tmpPixelsPerUnit;

        CubismCore.csmReadCanvasInfo(Model, &tmpSizeInPixels, &tmpOriginInPixels, &tmpPixelsPerUnit);

        return tmpSizeInPixels.Y;
    }

    /// <summary>
    /// PixelsPerUnitを取得する。
    /// </summary>
    /// <returns>PixelsPerUnit</returns>
    public float GetPixelsPerUnit()
    {
        if (new IntPtr(Model) == IntPtr.Zero)
        {
            return 0.0f;
        }

        Vector2 tmpSizeInPixels;
        Vector2 tmpOriginInPixels;
        float tmpPixelsPerUnit;

        CubismCore.csmReadCanvasInfo(Model, &tmpSizeInPixels, &tmpOriginInPixels, &tmpPixelsPerUnit);

        return tmpPixelsPerUnit;
    }

    /// <summary>
    /// Unit単位でキャンバスの幅の取得
    /// </summary>
    /// <returns>キャンバスの幅(Unit)</returns>
    public float GetCanvasWidth()
    {
        if (new IntPtr(Model) == IntPtr.Zero)
        {
            return 0.0f;
        }

        Vector2 tmpSizeInPixels;
        Vector2 tmpOriginInPixels;
        float tmpPixelsPerUnit;

        CubismCore.csmReadCanvasInfo(Model, &tmpSizeInPixels, &tmpOriginInPixels, &tmpPixelsPerUnit);

        return tmpSizeInPixels.X / tmpPixelsPerUnit;
    }

    /// <summary>
    /// Unit単位でキャンバスの高さの取得
    /// </summary>
    /// <returns>キャンバスの高さ(Unit)</returns>
    public float GetCanvasHeight()
    {
        if (new IntPtr(Model) == IntPtr.Zero)
        {
            return 0.0f;
        }

        Vector2 tmpSizeInPixels;
        Vector2 tmpOriginInPixels;
        float tmpPixelsPerUnit;

        CubismCore.csmReadCanvasInfo(Model, &tmpSizeInPixels, &tmpOriginInPixels, &tmpPixelsPerUnit);

        return tmpSizeInPixels.Y / tmpPixelsPerUnit;
    }

    /// <summary>
    /// パーツのインデックスを取得する。
    /// </summary>
    /// <param name="partId">パーツのID</param>
    /// <returns>パーツのインデックス</returns>
    public int GetPartIndex(string partId)
    {
        int partIndex;
        int idCount = CubismCore.csmGetPartCount(Model);

        for (partIndex = 0; partIndex < idCount; ++partIndex)
        {
            if (partId == _partIds[partIndex])
            {
                return partIndex;
            }
        }

        int partCount = CubismCore.csmGetPartCount(Model);

        // モデルに存在していない場合、非存在パーツIDリスト内にあるかを検索し、そのインデックスを返す
        if (_notExistPartId.ContainsKey(partId))
        {
            return _notExistPartId[partId];
        }

        // 非存在パーツIDリストにない場合、新しく要素を追加する
        partIndex = partCount + _notExistPartId.Count;

        _notExistPartId[partId] = partIndex;
        _notExistPartOpacities.Add(partIndex, 0);

        return partIndex;
    }

    /// <summary>
    /// パーツのIDを取得する。
    /// </summary>
    /// <param name="partIndex">パーツのIndex</param>
    /// <returns>パーツのID</returns>
    public string GetPartId(int partIndex)
    {
        var partIds = CubismCore.csmGetPartIds(Model);
        var str = new string((sbyte*)partIds[partIndex]);
        return CubismFramework.GetIdManager().GetId(str);
    }

    /// <summary>
    /// パーツの個数を取得する。
    /// </summary>
    /// <returns>パーツの個数</returns>
    public int GetPartCount()
    {
        return CubismCore.csmGetPartCount(Model);
    }

    /// <summary>
    /// パーツの不透明度を設定する。
    /// </summary>
    /// <param name="partId">パーツのID</param>
    /// <param name="opacity">不透明度</param>
    public void SetPartOpacity(string partId, float opacity)
    {
        // 高速化のためにPartIndexを取得できる機構になっているが、外部からの設定の時は呼び出し頻度が低いため不要
        int index = GetPartIndex(partId);

        if (index < 0)
        {
            return; // パーツが無いのでスキップ
        }

        SetPartOpacity(index, opacity);
    }

    /// <summary>
    /// パーツの不透明度を設定する。
    /// </summary>
    /// <param name="partIndex">パーツのインデックス</param>
    /// <param name="opacity">パーツの不透明度</param>
    public void SetPartOpacity(int partIndex, float opacity)
    {
        if (_notExistPartOpacities.ContainsKey(partIndex))
        {
            _notExistPartOpacities[partIndex] = opacity;
            return;
        }

        //インデックスの範囲内検知
        if (0 > partIndex || partIndex >= GetPartCount())
        {
            throw new ArgumentException($"partIndex out of range");
        }

        _partOpacities[partIndex] = opacity;
    }

    /// <summary>
    /// パーツの不透明度を取得する。
    /// </summary>
    /// <param name="partId">パーツのID</param>
    /// <returns>パーツの不透明度</returns>
    public float GetPartOpacity(string partId)
    {
        // 高速化のためにPartIndexを取得できる機構になっているが、外部からの設定の時は呼び出し頻度が低いため不要
        int index = GetPartIndex(partId);

        if (index < 0)
        {
            return 0; //パーツが無いのでスキップ
        }

        return GetPartOpacity(index);
    }

    /// <summary>
    /// パーツの不透明度を取得する。
    /// </summary>
    /// <param name="partIndex">パーツのインデックス</param>
    /// <returns>パーツの不透明度</returns>
    public float GetPartOpacity(int partIndex)
    {
        if (_notExistPartOpacities.ContainsKey(partIndex))
        {
            // モデルに存在しないパーツIDの場合、非存在パーツリストから不透明度を返す
            return _notExistPartOpacities[partIndex];
        }

        //インデックスの範囲内検知
        if (0 > partIndex || partIndex >= GetPartCount())
        {
            throw new ArgumentException($"partIndex out of range");
        }

        return _partOpacities[partIndex];
    }

    /// <summary>
    /// パラメータのインデックスを取得する。
    /// </summary>
    /// <param name="parameterId">パラメータID</param>
    /// <returns>パラメータのインデックス</returns>
    public int GetParameterIndex(string parameterId)
    {
        int parameterIndex;
        int idCount = CubismCore.csmGetParameterCount(Model);

        for (parameterIndex = 0; parameterIndex < idCount; ++parameterIndex)
        {
            if (parameterId != _parameterIds[parameterIndex])
            {
                continue;
            }

            return parameterIndex;
        }

        // モデルに存在していない場合、非存在パラメータIDリスト内を検索し、そのインデックスを返す
        if (_notExistParameterId.ContainsKey(parameterId))
        {
            return _notExistParameterId[parameterId];
        }

        // 非存在パラメータIDリストにない場合、新しく要素を追加する
        parameterIndex = CubismCore.csmGetParameterCount(Model) + _notExistParameterId.Count;

        _notExistParameterId[parameterId] = parameterIndex;
        _notExistParameterValues.Add(parameterIndex, 0);

        return parameterIndex;
    }

    /// <summary>
    /// パラメータの個数を取得する。
    /// </summary>
    /// <returns>パラメータの個数</returns>
    public int GetParameterCount()
    {
        return CubismCore.csmGetParameterCount(Model);
    }

    /// <summary>
    /// パラメータの種類を取得する。
    /// </summary>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    /// <returns>csmParameterType_Normal -> 通常のパラメータ
    /// csmParameterType_BlendShape -> ブレンドシェイプパラメータ</returns>
    public csmParameterType GetParameterType(int parameterIndex)
    {
        return CubismCore.csmGetParameterTypes(Model)[parameterIndex];
    }

    /// <summary>
    /// パラメータの最大値を取得する。
    /// </summary>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    /// <returns>パラメータの最大値</returns>
    public float GetParameterMaximumValue(int parameterIndex)
    {
        return CubismCore.csmGetParameterMaximumValues(Model)[parameterIndex];
    }

    /// <summary>
    /// パラメータの最小値を取得する。
    /// </summary>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    /// <returns>パラメータの最小値</returns>
    public float GetParameterMinimumValue(int parameterIndex)
    {
        return CubismCore.csmGetParameterMinimumValues(Model)[parameterIndex];
    }

    /// <summary>
    /// パラメータのデフォルト値を取得する。
    /// </summary>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    /// <returns> パラメータのデフォルト値</returns>
    public float GetParameterDefaultValue(int parameterIndex)
    {
        return CubismCore.csmGetParameterDefaultValues(Model)[parameterIndex];
    }

    /// <summary>
    /// パラメータの値を取得する。
    /// </summary>
    /// <param name="parameterId">パラメータID</param>
    /// <returns>パラメータの値</returns>
    public float GetParameterValue(string parameterId)
    {
        // 高速化のためにParameterIndexを取得できる機構になっているが、外部からの設定の時は呼び出し頻度が低いため不要
        int parameterIndex = GetParameterIndex(parameterId);
        return GetParameterValue(parameterIndex);
    }

    /// <summary>
    /// パラメータの値を取得する。
    /// </summary>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    /// <returns>パラメータの値</returns>
    public float GetParameterValue(int parameterIndex)
    {
        if (_notExistParameterValues.ContainsKey(parameterIndex))
        {
            return _notExistParameterValues[parameterIndex];
        }

        //インデックスの範囲内検知
        if (0 > parameterIndex || parameterIndex >= GetParameterCount())
        {
            throw new ArgumentException($"parameterIndex out of range");
        }

        return _parameterValues[parameterIndex];
    }

    /// <summary>
    /// パラメータの値を設定する。
    /// </summary>
    /// <param name="parameterId">パラメータID</param>
    /// <param name="value">パラメータの値</param>
    /// <param name="weight">重み</param>
    public void SetParameterValue(string parameterId, float value, float weight = 1.0f)
    {
        int index = GetParameterIndex(parameterId);
        SetParameterValue(index, value, weight);
    }

    /// <summary>
    /// パラメータの値を設定する。
    /// </summary>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    /// <param name="value">パラメータの値</param>
    /// <param name="weight">重み</param>
    public void SetParameterValue(int parameterIndex, float value, float weight = 1.0f)
    {
        if (_notExistParameterValues.ContainsKey(parameterIndex))
        {
            _notExistParameterValues[parameterIndex] = (weight == 1)
                                                             ? value
                                                             : (_notExistParameterValues[parameterIndex] * (1 - weight)) +
                                                             (value * weight);
            return;
        }

        //インデックスの範囲内検知
        if (0 > parameterIndex || parameterIndex >= GetParameterCount())
        {
            throw new ArgumentException($"parameterIndex out of range");
        }

        if (CubismCore.csmGetParameterMaximumValues(Model)[parameterIndex] < value)
        {
            value = CubismCore.csmGetParameterMaximumValues(Model)[parameterIndex];
        }
        if (CubismCore.csmGetParameterMinimumValues(Model)[parameterIndex] > value)
        {
            value = CubismCore.csmGetParameterMinimumValues(Model)[parameterIndex];
        }

        _parameterValues[parameterIndex] = (weight == 1)
                                          ? value
                                          : _parameterValues[parameterIndex] = (_parameterValues[parameterIndex] * (1 - weight)) + (value * weight);
    }

    /// <summary>
    /// パラメータの値を加算する。
    /// </summary>
    /// <param name="parameterId">パラメータID</param>
    /// <param name="value">加算する値</param>
    /// <param name="weight">重み</param>
    public void AddParameterValue(string parameterId, float value, float weight = 1.0f)
    {
        int index = GetParameterIndex(parameterId);
        AddParameterValue(index, value, weight);
    }

    /// <summary>
    /// パラメータの値を加算する。
    /// </summary>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    /// <param name="value">加算する値</param>
    /// <param name="weight">重み</param>
    public void AddParameterValue(int parameterIndex, float value, float weight = 1.0f)
    {
        SetParameterValue(parameterIndex, (GetParameterValue(parameterIndex) + (value * weight)));
    }

    /// <summary>
    /// パラメータの値を乗算する。
    /// </summary>
    /// <param name="parameterId">パラメータID</param>
    /// <param name="value">乗算する値</param>
    /// <param name="weight">重み</param>
    public void MultiplyParameterValue(string parameterId, float value, float weight = 1.0f)
    {
        int index = GetParameterIndex(parameterId);
        MultiplyParameterValue(index, value, weight);
    }

    /// <summary>
    /// パラメータの値を乗算する。
    /// </summary>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    /// <param name="value">乗算する値</param>
    /// <param name="weight">重み</param>
    public void MultiplyParameterValue(int parameterIndex, float value, float weight = 1.0f)
    {
        SetParameterValue(parameterIndex, GetParameterValue(parameterIndex) * (1.0f + (value - 1.0f) * weight));
    }

    /// <summary>
    /// Drawableのインデックスを取得する。
    /// </summary>
    /// <param name="drawableId">DrawableのID</param>
    /// <returns>Drawableのインデックス</returns>
    public int GetDrawableIndex(string drawableId)
    {
        int drawableCount = CubismCore.csmGetDrawableCount(Model);

        for (int drawableIndex = 0; drawableIndex < drawableCount; ++drawableIndex)
        {
            if (_drawableIds[drawableIndex] == drawableId)
            {
                return drawableIndex;
            }
        }

        return -1;
    }

    /// <summary>
    /// Drawableの個数を取得する。
    /// </summary>
    /// <returns>Drawableの個数</returns>
    public int GetDrawableCount()
    {
        int drawableCount = CubismCore.csmGetDrawableCount(Model);
        return drawableCount;
    }

    /// <summary>
    /// DrawableのIDを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>DrawableのID</returns>
    public string GetDrawableId(int drawableIndex)
    {
        var parameterIds = CubismCore.csmGetDrawableIds(Model);
        var str = new string((sbyte*)parameterIds[drawableIndex]);
        return CubismFramework.GetIdManager().GetId(str);
    }

    /// <summary>
    /// Drawableの描画順リストを取得する。
    /// </summary>
    /// <returns>Drawableの描画順リスト</returns>
    public int* GetDrawableRenderOrders()
    {
        return CubismCore.csmGetDrawableRenderOrders(Model);
    }

    /// <summary>
    /// Drawableのテクスチャインデックスリストの取得
    /// 関数名が誤っていたため、代替となる getDrawableTextureIndex を追加し、この関数は非推奨となりました。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableのテクスチャインデックスリスト</returns>
    public int GetDrawableTextureIndices(int drawableIndex)
    {
        return GetDrawableTextureIndex(drawableIndex);
    }

    /// <summary>
    /// Drawableのテクスチャインデックスを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableのテクスチャインデックス</returns>
    public int GetDrawableTextureIndex(int drawableIndex)
    {
        var textureIndices = CubismCore.csmGetDrawableTextureIndices(Model);
        return textureIndices[drawableIndex];
    }

    /// <summary>
    /// Drawableの頂点インデックスの個数を取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableの頂点インデックスの個数</returns>
    public int GetDrawableVertexIndexCount(int drawableIndex)
    {
        var indexCounts = CubismCore.csmGetDrawableIndexCounts(Model);
        return indexCounts[drawableIndex];
    }

    /// <summary>
    /// Drawableの頂点の個数を取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableの頂点の個数</returns>
    public int GetDrawableVertexCount(int drawableIndex)
    {
        var vertexCounts = CubismCore.csmGetDrawableVertexCounts(Model);
        return vertexCounts[drawableIndex];
    }

    /// <summary>
    /// Drawableの頂点リストを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableの頂点リスト</returns>
    public float* GetDrawableVertices(int drawableIndex)
    {
        return (float*)GetDrawableVertexPositions(drawableIndex);
    }

    /// <summary>
    /// Drawableの頂点インデックスリストを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableの頂点インデックスリスト</returns>
    public ushort* GetDrawableVertexIndices(int drawableIndex)
    {
        var indicesArray = CubismCore.csmGetDrawableIndices(Model);
        return indicesArray[drawableIndex];
    }

    /// <summary>
    /// Drawableの頂点リストを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableの頂点リスト</returns>
    public Vector2* GetDrawableVertexPositions(int drawableIndex)
    {
        var verticesArray = CubismCore.csmGetDrawableVertexPositions(Model);
        return verticesArray[drawableIndex];
    }

    /// <summary>
    /// Drawableの頂点のUVリストを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableの頂点のUVリスト</returns>
    public Vector2* GetDrawableVertexUvs(int drawableIndex)
    {
        var uvsArray = CubismCore.csmGetDrawableVertexUvs(Model);
        return uvsArray[drawableIndex];
    }

    /// <summary>
    /// Drawableの不透明度を取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableの不透明度</returns>
    public float GetDrawableOpacity(int drawableIndex)
    {
        var opacities = CubismCore.csmGetDrawableOpacities(Model);
        return opacities[drawableIndex];
    }

    /// <summary>
    /// Drawableの乗算色を取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableの乗算色</returns>
    public Vector4 GetDrawableMultiplyColor(int drawableIndex)
    {
        var multiplyColors = CubismCore.csmGetDrawableMultiplyColors(Model);
        return multiplyColors[drawableIndex];
    }

    /// <summary>
    /// Drawableのスクリーン色を取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableのスクリーン色</returns>
    public Vector4 GetDrawableScreenColor(int drawableIndex)
    {
        var screenColors = CubismCore.csmGetDrawableScreenColors(Model);
        return screenColors[drawableIndex];
    }

    /// <summary>
    /// Drawableの親パーツのインデックスを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>drawableの親パーツのインデックス</returns>
    public int GetDrawableParentPartIndex(int drawableIndex)
    {
        return CubismCore.csmGetDrawableParentPartIndices(Model)[drawableIndex];
    }

    /// <summary>
    /// Drawableのブレンドモードを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableのブレンドモード</returns>
    public CubismBlendMode GetDrawableBlendMode(int drawableIndex)
    {
        var constantFlags = CubismCore.csmGetDrawableConstantFlags(Model);
        return (IsBitSet(constantFlags[drawableIndex], csmEnum.csmBlendAdditive))
                   ? CubismBlendMode.CubismBlendMode_Additive
                   : (IsBitSet(constantFlags[drawableIndex], csmEnum.csmBlendMultiplicative))
                   ? CubismBlendMode.CubismBlendMode_Multiplicative
                   : CubismBlendMode.CubismBlendMode_Normal;
    }

    /// <summary>
    /// Drawableのマスク使用時の反転設定を取得する。
    /// マスクを使用しない場合は無視される
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableのマスクの反転設定</returns>
    public bool GetDrawableInvertedMask(int drawableIndex)
    {
        var constantFlags = CubismCore.csmGetDrawableConstantFlags(Model);
        return IsBitSet(constantFlags[drawableIndex], csmEnum.csmIsInvertedMask);
    }

    /// <summary>
    /// Drawableの表示情報を取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>true    Drawableが表示
    /// false   Drawableが非表示</returns>
    public bool GetDrawableDynamicFlagIsVisible(int drawableIndex)
    {
        var dynamicFlags = CubismCore.csmGetDrawableDynamicFlags(Model);
        return IsBitSet(dynamicFlags[drawableIndex], csmEnum.csmIsVisible);
    }

    /// <summary>
    /// 直近のCubismModel::Update関数でDrawableの表示状態が変化したかを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>true    Drawableの表示状態が直近のCubismModel::Update関数で変化した
    /// false   Drawableの表示状態が直近のCubismModel::Update関数で変化していない</returns>
    public bool GetDrawableDynamicFlagVisibilityDidChange(int drawableIndex)
    {
        var dynamicFlags = CubismCore.csmGetDrawableDynamicFlags(Model);
        return IsBitSet(dynamicFlags[drawableIndex], csmEnum.csmVisibilityDidChange);
    }

    /// <summary>
    /// 直近のCubismModel::Update関数でDrawableの不透明度が変化したかを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>true    Drawableの不透明度が直近のCubismModel::Update関数で変化した
    /// false   Drawableの不透明度が直近のCubismModel::Update関数で変化していない</returns>
    public bool GetDrawableDynamicFlagOpacityDidChange(int drawableIndex)
    {
        var dynamicFlags = CubismCore.csmGetDrawableDynamicFlags(Model);
        return IsBitSet(dynamicFlags[drawableIndex], csmEnum.csmOpacityDidChange);
    }

    /// <summary>
    /// 直近のCubismModel::Update関数でDrawableのDrawOrderが変化したかを取得する。
    /// DrawOrderはArtMesh上で指定する0から1000の情報
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>true    Drawableの不透明度が直近のCubismModel::Update関数で変化した
    /// false   Drawableの不透明度が直近のCubismModel::Update関数で変化していない</returns>
    public bool GetDrawableDynamicFlagDrawOrderDidChange(int drawableIndex)
    {
        var dynamicFlags = CubismCore.csmGetDrawableDynamicFlags(Model);
        return IsBitSet(dynamicFlags[drawableIndex], csmEnum.csmDrawOrderDidChange);
    }

    /// <summary>
    /// 直近のCubismModel::Update関数でDrawableの描画の順序が変化したかを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>true    Drawableの描画の順序が直近のCubismModel::Update関数で変化した
    /// false   Drawableの描画の順序が直近のCubismModel::Update関数で変化していない</returns>
    public bool GetDrawableDynamicFlagRenderOrderDidChange(int drawableIndex)
    {
        var dynamicFlags = CubismCore.csmGetDrawableDynamicFlags(Model);
        return IsBitSet(dynamicFlags[drawableIndex], csmEnum.csmRenderOrderDidChange);
    }

    /// <summary>
    /// 直近のCubismModel::Update関数でDrawableの頂点情報が変化したかを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>true    Drawableの頂点情報が直近のCubismModel::Update関数で変化した
    /// false   Drawableの頂点情報が直近のCubismModel::Update関数で変化していない</returns>
    public bool GetDrawableDynamicFlagVertexPositionsDidChange(int drawableIndex)
    {
        var dynamicFlags = CubismCore.csmGetDrawableDynamicFlags(Model);
        return IsBitSet(dynamicFlags[drawableIndex], csmEnum.csmVertexPositionsDidChange);
    }

    /// <summary>
    /// 直近のCubismModel::Update関数でDrawableの乗算色・スクリーン色が変化したかを取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>true    Drawableの乗算色・スクリーン色が直近のCubismModel::Update関数で変化した
    /// false   Drawableの乗算色・スクリーン色が直近のCubismModel::Update関数で変化していない</returns>
    public bool GetDrawableDynamicFlagBlendColorDidChange(int drawableIndex)
    {
        var dynamicFlags = CubismCore.csmGetDrawableDynamicFlags(Model);
        return IsBitSet(dynamicFlags[drawableIndex], csmEnum.csmBlendColorDidChange);
    }

    /// <summary>
    /// Drawableのクリッピングマスクリストを取得する。
    /// </summary>
    /// <returns>Drawableのクリッピングマスクリスト</returns>
    public int** GetDrawableMasks()
    {
        return CubismCore.csmGetDrawableMasks(Model);
    }

    /// <summary>
    /// Drawableのクリッピングマスクの個数リストを取得する。
    /// </summary>
    /// <returns>Drawableのクリッピングマスクの個数リスト</returns>
    public int* GetDrawableMaskCounts()
    {
        return CubismCore.csmGetDrawableMaskCounts(Model);
    }

    /// <summary>
    /// クリッピングマスクを使用しているかどうか？
    /// </summary>
    /// <returns>true    クリッピングマスクを使用している
    /// false   クリッピングマスクを使用していない</returns>
    public bool IsUsingMasking()
    {
        for (int d = 0; d < CubismCore.csmGetDrawableCount(Model); ++d)
        {
            if (CubismCore.csmGetDrawableMaskCounts(Model)[d] <= 0)
            {
                continue;
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// 保存されたパラメータを読み込む
    /// </summary>
    public void LoadParameters()
    {
        int parameterCount = CubismCore.csmGetParameterCount(Model);
        int savedParameterCount = _savedParameters.Count;

        if (parameterCount > savedParameterCount)
        {
            parameterCount = savedParameterCount;
        }

        for (int i = 0; i < parameterCount; ++i)
        {
            _parameterValues[i] = _savedParameters[i];
        }
    }

    /// <summary>
    /// パラメータを保存する。
    /// </summary>
    public void SaveParameters()
    {
        int parameterCount = CubismCore.csmGetParameterCount(Model);
        int savedParameterCount = _savedParameters.Count;

        for (int i = 0; i < parameterCount; ++i)
        {
            if (i < savedParameterCount)
            {
                _savedParameters[i] = _parameterValues[i];
            }
            else
            {
                _savedParameters.Add(_parameterValues[i]);
            }
        }
    }

    /// <summary>
    /// drawableの乗算色を取得する
    /// </summary>
    public CubismTextureColor GetMultiplyColor(int drawableIndex)
    {
        if (GetOverwriteFlagForModelMultiplyColors() || GetOverwriteFlagForDrawableMultiplyColors(drawableIndex))
        {
            return _userMultiplyColors[drawableIndex].Color;
        }

        Vector4 color = GetDrawableMultiplyColor(drawableIndex);

        return new CubismTextureColor(color.X, color.Y, color.Z, color.W);
    }

    /// <summary>
    ///  drawableのスクリーン色を取得する
    /// </summary>
    public CubismTextureColor GetScreenColor(int drawableIndex)
    {
        if (GetOverwriteFlagForModelScreenColors() || GetOverwriteFlagForDrawableScreenColors(drawableIndex))
        {
            return _userScreenColors[drawableIndex].Color;
        }

        Vector4 color = GetDrawableScreenColor(drawableIndex);
        return new CubismTextureColor(color.X, color.Y, color.Z, color.W);
    }

    /// <summary>
    /// drawableの乗算色を設定する
    /// </summary>
    public void SetMultiplyColor(int drawableIndex, CubismTextureColor color)
    {
        SetMultiplyColor(drawableIndex, color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// drawableの乗算色を設定する
    /// </summary>
    public void SetMultiplyColor(int drawableIndex, float r, float g, float b, float a = 1.0f)
    {
        _userMultiplyColors[drawableIndex].Color.R = r;
        _userMultiplyColors[drawableIndex].Color.G = g;
        _userMultiplyColors[drawableIndex].Color.B = b;
        _userMultiplyColors[drawableIndex].Color.A = a;
    }

    /// <summary>
    /// drawableのスクリーン色を設定する
    /// </summary>
    /// <param name="drawableIndex"></param>
    /// <param name="color"></param>
    public void SetScreenColor(int drawableIndex, CubismTextureColor color)
    {
        SetScreenColor(drawableIndex, color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// drawableのスクリーン色を設定する
    /// </summary>
    public void SetScreenColor(int drawableIndex, float r, float g, float b, float a = 1.0f)
    {
        _userScreenColors[drawableIndex].Color.R = r;
        _userScreenColors[drawableIndex].Color.G = g;
        _userScreenColors[drawableIndex].Color.B = b;
        _userScreenColors[drawableIndex].Color.A = a;
    }

    /// <summary>
    /// partの乗算色を取得する
    /// </summary>
    public CubismTextureColor GetPartMultiplyColor(int partIndex)
    {
        return _userPartMultiplyColors[partIndex].Color;
    }

    /// <summary>
    /// partの乗算色を取得する
    /// </summary>
    public CubismTextureColor GetPartScreenColor(int partIndex)
    {
        return _userPartScreenColors[partIndex].Color;
    }

    /// <summary>
    /// partのスクリーン色を設定する
    /// </summary>
    public void SetPartMultiplyColor(int partIndex, CubismTextureColor color)
    {
        SetPartMultiplyColor(partIndex, color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// partの乗算色を設定する
    /// </summary>
    public void SetPartMultiplyColor(int partIndex, float r, float g, float b, float a = 1.0f)
    {
        SetPartColor(partIndex, r, g, b, a, _userPartMultiplyColors, _userMultiplyColors);
    }

    /// <summary>
    /// partのスクリーン色を設定する
    /// </summary>
    public void SetPartScreenColor(int partIndex, CubismTextureColor color)
    {
        SetPartScreenColor(partIndex, color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// partのスクリーン色を設定する
    /// </summary>
    /// <param name="a"></param>
    public void SetPartScreenColor(int partIndex, float r, float g, float b, float a = 1.0f)
    {
        SetPartColor(partIndex, r, g, b, a, _userPartScreenColors, _userScreenColors);
    }

    /// <summary>
    /// SDKからモデル全体の乗算色を上書きするか。
    /// </summary>
    /// <returns>true    ->  SDK上の色情報を使用
    /// false   ->  モデルの色情報を使用</returns>
    public bool GetOverwriteFlagForModelMultiplyColors()
    {
        return _isOverwrittenModelMultiplyColors;
    }

    /// <summary>
    /// SDKからモデル全体のスクリーン色を上書きするか。
    /// </summary>
    /// <returns>true    ->  SDK上の色情報を使用
    /// false   ->  モデルの色情報を使用</returns>
    public bool GetOverwriteFlagForModelScreenColors()
    {
        return _isOverwrittenModelScreenColors;
    }

    /// <summary>
    /// SDKからモデル全体の乗算色を上書きするかをセットする
    /// SDK上の色情報を使うならtrue、モデルの色情報を使うならfalse
    /// </summary>
    public void SetOverwriteFlagForModelMultiplyColors(bool value)
    {
        _isOverwrittenModelMultiplyColors = value;
    }

    /// <summary>
    /// SDKからモデル全体のスクリーン色を上書きするかをセットする
    /// SDK上の色情報を使うならtrue、モデルの色情報を使うならfalse
    /// </summary>
    public void SetOverwriteFlagForModelScreenColors(bool value)
    {
        _isOverwrittenModelScreenColors = value;
    }

    /// <summary>
    /// SDKからdrawableの乗算色を上書きするか。
    /// </summary>
    /// <returns>true    ->  SDK上の色情報を使用
    /// false   ->  モデルの色情報を使用</returns>
    public bool GetOverwriteFlagForDrawableMultiplyColors(int drawableIndex)
    {
        return _userMultiplyColors[drawableIndex].IsOverwritten;
    }

    /// <summary>
    /// SDKからdrawableのスクリーン色を上書きするか。
    /// </summary>
    /// <returns>true    ->  SDK上の色情報を使用
    /// false   ->  モデルの色情報を使用</returns>
    public bool GetOverwriteFlagForDrawableScreenColors(int drawableIndex)
    {
        return _userScreenColors[drawableIndex].IsOverwritten;
    }

    /// <summary>
    /// SDKからdrawableの乗算色を上書きするかをセットする
    /// SDK上の色情報を使うならtrue、モデルの色情報を使うならfalse
    /// </summary>
    public void SetOverwriteFlagForDrawableMultiplyColors(int drawableIndex, bool value)
    {
        _userMultiplyColors[drawableIndex].IsOverwritten = value;
    }

    /// <summary>
    /// SDKからdrawableのスクリーン色を上書きするかをセットする
    /// SDK上の色情報を使うならtrue、モデルの色情報を使うならfalse
    /// </summary>
    public void SetOverwriteFlagForDrawableScreenColors(int drawableIndex, bool value)
    {
        _userScreenColors[drawableIndex].IsOverwritten = value;
    }

    /// <summary>
    /// SDKからpartの乗算色を上書きするか。
    /// </summary>
    /// <returns>true    ->  SDK上の色情報を使用
    /// false   ->  モデルの色情報を使用</returns>
    public bool GetOverwriteColorForPartMultiplyColors(int partIndex)
    {
        return _userPartMultiplyColors[partIndex].IsOverwritten;
    }

    /// <summary>
    /// SDKからpartのスクリーン色を上書きするかをセットする
    /// SDK上の色情報を使うならtrue、モデルの色情報を使うならfalse
    /// </summary>
    public bool GetOverwriteColorForPartScreenColors(int partIndex)
    {
        return _userPartScreenColors[partIndex].IsOverwritten;
    }

    /// <summary>
    /// Drawableのカリング情報を取得する。
    /// </summary>
    /// <param name="drawableIndex">Drawableのインデックス</param>
    /// <returns>Drawableのカリング情報</returns>
    public int GetDrawableCulling(int drawableIndex)
    {
        if (GetOverwriteFlagForModelCullings() || GetOverwriteFlagForDrawableCullings(drawableIndex))
        {
            return _userCullings[drawableIndex].IsCulling;
        }

        var constantFlags = CubismCore.csmGetDrawableConstantFlags(Model);
        return !IsBitSet(constantFlags[drawableIndex], csmEnum.csmIsDoubleSided) ? 1 : 0;
    }

    /// <summary>
    /// Drawableのカリング情報を設定する
    /// </summary>
    public void SetDrawableCulling(int drawableIndex, int isCulling)
    {
        _userCullings[drawableIndex].IsCulling = isCulling;
    }

    /// <summary>
    /// SDKからモデル全体のカリング設定を上書きするか。
    /// </summary>
    /// <returns>true    ->  SDK上のカリング設定を使用
    /// false   ->  モデルのカリング設定を使用</returns>
    public bool GetOverwriteFlagForModelCullings()
    {
        return _isOverwrittenCullings;
    }

    /// <summary>
    /// SDKからモデル全体のカリング設定を上書きするかをセットする
    /// SDK上のカリング設定を使うならtrue、モデルのカリング設定を使うならfalse
    /// </summary>
    public void SetOverwriteFlagForModelCullings(bool value)
    {
        _isOverwrittenCullings = value;
    }

    /// <summary>
    /// SDKからdrawableのカリング設定を上書きするか。
    /// </summary>
    /// <returns>true    ->  SDK上のカリング設定を使用
    /// false   ->  モデルのカリング設定を使用</returns>
    public bool GetOverwriteFlagForDrawableCullings(int drawableIndex)
    {
        return _userCullings[drawableIndex].IsOverwritten;
    }

    /// <summary>
    /// SDKからdrawableのカリング設定を上書きするかをセットする
    /// SDK上のカリング設定を使うならtrue、モデルのカリング設定を使うならfalse
    /// </summary>
    public void SetOverwriteFlagForDrawableCullings(int drawableIndex, bool value)
    {
        _userCullings[drawableIndex].IsOverwritten = value;
    }

    /// <summary>
    /// モデルの不透明度を取得する
    /// </summary>
    /// <returns>不透明度の値</returns>
    public float GetModelOpacity()
    {
        return _modelOpacity;
    }

    /// <summary>
    /// モデルの不透明度を設定する
    /// </summary>
    /// <param name="value">不透明度の値</param>
    public void SetModelOpacity(float value)
    {
        _modelOpacity = value;
    }

    private static bool IsBitSet(byte data, byte mask)
    {
        return (data & mask) == mask;
    }
}
