using Live2DCSharpSDK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Model;

public record DrawableColorData
{
    public bool IsOverwritten;
    public CubismTextureColor Color;

    public DrawableColorData()
    {
        Color = new();
    }

    public DrawableColorData(bool isOverwritten, CubismTextureColor color)
    {
        IsOverwritten = isOverwritten;
        Color = color;
    }
}

public record DrawableCullingData
{
    public bool IsOverwritten;
    public  int IsCulling;

    public DrawableCullingData()
    { 
        
    }

    public DrawableCullingData(bool isOverwritten, int isCulling)
    {
        IsOverwritten = isOverwritten;
        IsCulling = isCulling;
    }
}

public record PartColorData
{
    public bool IsOverwritten;
    public CubismTextureColor Color;

    public PartColorData()
    {
        Color = new();
    }

    public PartColorData(bool isOverwritten, CubismTextureColor color)
        {
        IsOverwritten = isOverwritten;
        Color = color;
    }
}

public unsafe class CubismModel : IDisposable
{
    /// <summary>
    /// 存在していないパーツの不透明度のリスト
    /// </summary>
    private Dictionary<int, float> _notExistPartOpacities;
    /// <summary>
    /// 存在していないパーツIDのリスト
    /// </summary>
    private Dictionary<string, int> _notExistPartId;
    /// <summary>
    /// 存在していないパラメータの値のリスト
    /// </summary>
    private Dictionary<int, float> _notExistParameterValues;
    /// <summary>
    /// 存在していないパラメータIDのリスト
    /// </summary>
    private Dictionary<string, int> _notExistParameterId;
    /// <summary>
    /// 保存されたパラメータ
    /// </summary>
    private List<float> _savedParameters;
    /// <summary>
    /// モデル
    /// </summary>
    private csmModel* _model;
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


    private List<string> _parameterIds;
    private List<string> _partIds;
    private List<string> _drawableIds;
    /// <summary>
    /// Drawable 乗算色の配列
    /// </summary>
    private List<DrawableColorData> _userScreenColors;
    /// <summary>
    /// Drawable スクリーン色の配列
    /// </summary>
    private List<DrawableColorData> _userMultiplyColors;
    /// <summary>
    /// カリング設定の配列
    /// </summary>
    private List<DrawableCullingData> _userCullings;
    /// <summary>
    /// Part 乗算色の配列
    /// </summary>
    private List<PartColorData> _userPartScreenColors;
    /// <summary>
    /// Part スクリーン色の配列
    /// </summary>
    private List<PartColorData> _userPartMultiplyColors;
    /// <summary>
    /// Partの子DrawableIndexの配列
    /// </summary>
    private List<List<int>> _partChildDrawables;
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

    public CubismModel(csmModel* model)
    {
        _model = model;
        _modelOpacity = 1.0f;
    }

    public void Dispose()
    {
        CubismFramework.DeallocateAligned(new IntPtr(_model));
    }

    public void Initialize()
    {
        _parameterValues = CubismCore.csmGetParameterValues(_model);
        _partOpacities = CubismCore.csmGetPartOpacities(_model);
        _parameterMaximumValues = CubismCore.csmGetParameterMaximumValues(_model);
        _parameterMinimumValues = CubismCore.csmGetParameterMinimumValues(_model);

        {
            var parameterIds = CubismCore.csmGetParameterIds(_model);
            var parameterCount = CubismCore.csmGetParameterCount(_model);

            for (int i = 0; i < parameterCount; ++i)
            {
                _parameterIds.Add(CubismFramework.GetIdManager().GetId(parameterIds[i]));
            }
        }

        int partCount = CubismCore.csmGetPartCount(_model);
        {
            var partIds = CubismCore.csmGetPartIds(_model);

            for (int i = 0; i < partCount; ++i)
            {
                _partIds.Add(CubismFramework.GetIdManager().GetId(partIds[i]));
            }
        }

        {
            var drawableIds = CubismCore.csmGetDrawableIds(_model);
            var drawableCount = CubismCore.csmGetDrawableCount(_model);

            // カリング設定
            DrawableCullingData userCulling = new()
            {
                IsOverwritten = false,
                IsCulling = 0
            };

            // 乗算色
            CubismTextureColor multiplyColor;
            multiplyColor.R = 1.0f;
            multiplyColor.G = 1.0f;
            multiplyColor.B = 1.0f;
            multiplyColor.A = 1.0f;

            // スクリーン色
            CubismTextureColor screenColor;
            screenColor.R = 0.0f;
            screenColor.G = 0.0f;
            screenColor.B = 0.0f;
            screenColor.A = 1.0f;

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
                    _drawableIds.Add(CubismFramework.GetIdManager().GetId(drawableIds[i]));
                    _userMultiplyColors.Add(userMultiplyColor);
                    _userScreenColors.Add(userScreenColor);
                    _userCullings.Add(userCulling);

                    var parentIndex = CubismCore.csmGetDrawableParentPartIndices(_model)[i];
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

    }

    /// <summary>
    /// Pixel単位でキャンバスの幅の取得
    /// </summary>
    /// <returns>キャンバスの幅(pixel)</returns>
    csmFloat32 GetCanvasWidthPixel()
    {

    }

    /// <summary>
    /// Pixel単位でキャンバスの高さの取得
    /// </summary>
    /// <returns>キャンバスの高さ(pixel)</returns>
    csmFloat32 GetCanvasHeightPixel()
    {

    }

    /// <summary>
    /// PixelsPerUnitを取得する。
    /// </summary>
    /// <returns>PixelsPerUnit</returns>
    csmFloat32 GetPixelsPerUnit()
    {

    }

    /// <summary>
    /// Unit単位でキャンバスの幅の取得
    /// </summary>
    /// <returns>キャンバスの幅(Unit)</returns>
    csmFloat32 GetCanvasWidth()
    {

    }

    /// <summary>
    /// Unit単位でキャンバスの高さの取得
    /// </summary>
    /// <returns>キャンバスの高さ(Unit)</returns>
    csmFloat32 GetCanvasHeight()
    { 
    
    }

    /// <summary>
    /// パーツのインデックスを取得する。
    /// </summary>
    /// <param name="partId">パーツのID</param>
    /// <returns>パーツのインデックス</returns>
    csmInt32 GetPartIndex(CubismIdHandle partId)
    { 
    
    }

    /// <summary>
    /// パーツのIDを取得する。
    /// </summary>
    /// <param name="partIndex">パーツのIndex</param>
    /// <returns>パーツのID</returns>
    CubismIdHandle GetPartId(csmUint32 partIndex)
    { 
    
    }

    /// <summary>
    /// パーツの個数を取得する。
    /// </summary>
    /// <returns>パーツの個数</returns>
    csmInt32 GetPartCount()
    { 
    
    }

    /// <summary>
    /// パーツの不透明度を設定する。
    /// </summary>
    /// <param name="partId">パーツのID</param>
    /// <param name="opacity">不透明度</param>
    void SetPartOpacity(CubismIdHandle partId, csmFloat32 opacity)
    { 
    
    }

    /// <summary>
    /// パーツの不透明度を設定する。
    /// </summary>
    /// <param name="partIndex">パーツのインデックス</param>
    /// <param name="opacity">パーツの不透明度</param>
    void SetPartOpacity(csmInt32 partIndex, csmFloat32 opacity)
    { 
    
    }

    /// <summary>
    /// パーツの不透明度を取得する。
    /// </summary>
    /// <param name="partId">パーツのID</param>
    /// <returns>パーツの不透明度</returns>
    csmFloat32 GetPartOpacity(CubismIdHandle partId)
    { 
    
    }

    /// <summary>
    /// パーツの不透明度を取得する。
    /// </summary>
    /// <param name="partIndex">パーツのインデックス</param>
    /// <returns>パーツの不透明度</returns>
    csmFloat32 GetPartOpacity(csmInt32 partIndex)
    { 
    
    }

    /// <summary>
    /// パラメータのインデックスを取得する。
    /// </summary>
    /// <param name="parameterId">パラメータID</param>
    /// <returns>パラメータのインデックス</returns>
    csmInt32 GetParameterIndex(CubismIdHandle parameterId)
    { 
    
    }

    /// <summary>
    /// パラメータの個数を取得する。
    /// </summary>
    /// <returns>パラメータの個数</returns>
    csmInt32 GetParameterCount()
    { 
    
    }
}
