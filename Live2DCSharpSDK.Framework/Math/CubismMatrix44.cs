using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Math;

/// <summary>
/// 4x4行列の便利クラス。
/// </summary>
public class CubismMatrix44
{
    /// <summary>
    /// 4x4行列データ
    /// </summary>
    protected float[] _tr = new float[16];

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public CubismMatrix44()
    {
        LoadIdentity();
    }

    /// <summary>
    /// 受け取った２つの行列の乗算を行う。
    /// </summary>
    /// <param name="a">行列a</param>
    /// <param name="b">行列b</param>
    /// <param name="dst"格納先の行列></param>
    public static void Multiply(float[] a, float[] b, float[] dst)
    {
        float[] c = new[]{
                        0.0f, 0.0f, 0.0f, 0.0f,
                        0.0f, 0.0f, 0.0f, 0.0f,
                        0.0f, 0.0f, 0.0f, 0.0f,
                        0.0f, 0.0f, 0.0f, 0.0f
                        };
        int n = 4;

        for (int i = 0; i < n; ++i)
        {
            for (int j = 0; j < n; ++j)
            {
                for (int k = 0; k < n; ++k)
                {
                    c[j + i * 4] += a[k + i * 4] * b[j + k * 4];
                }
            }
        }

        for (int i = 0; i < 16; ++i)
        {
            dst[i] = c[i];
        }
    }

    /// <summary>
    /// 単位行列に初期化する。
    /// </summary>
    public void LoadIdentity()
    {
        float[] c = new[] {
        1.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 1.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 1.0f
    };

        SetMatrix(c);
    }

    /// <summary>
    /// 行列を浮動小数点数の配列で取得する。
    /// </summary>
    /// <returns>16個の浮動小数点数で表される4x4の行列</returns>
    public float[] GetArray()
    {
        return _tr;
    }

    /// <summary>
    /// 行列を設定する。
    /// </summary>
    /// <param name="tr">16個の浮動小数点数で表される4x4の行列</param>
    public void SetMatrix(float[] tr)
    {
        for (int i = 0; i < 16; ++i)
        {
            _tr[i] = tr[i];
        }
    }

    /// <summary>
    /// X軸の拡大率を取得する。
    /// </summary>
    /// <returns>X軸の拡大率</returns>
    public float GetScaleX()
    {
        return _tr[0];
    }

    /// <summary>
    /// Y軸の拡大率を取得する。
    /// </summary>
    /// <returns>Y軸の拡大率</returns>
    public float GetScaleY()
    {
        return _tr[5];
    }

    /// <summary>
    /// X軸の移動量を取得する。
    /// </summary>
    /// <returns>X軸の移動量</returns>
    public float GetTranslateX()
    {
        return _tr[12];
    }

    /// <summary>
    /// Y軸の移動量を取得する。
    /// </summary>
    /// <returns>Y軸の移動量</returns>
    public float GetTranslateY()
    {
        return _tr[13];
    }

    /// <summary>
    /// X軸の値を現在の行列で計算する。
    /// </summary>
    /// <param name="src">X軸の値</param>
    /// <returns>現在の行列で計算されたX軸の値</returns>
    public float TransformX(float src)
    {
        return _tr[0] * src + _tr[12];
    }

    /// <summary>
    /// Y軸の値を現在の行列で計算する。
    /// </summary>
    /// <param name="src">Y軸の値</param>
    /// <returns>現在の行列で計算されたY軸の値</returns>
    public float TransformY(float src)
    {
        return _tr[5] * src + _tr[13];
    }

    /// <summary>
    /// X軸の値を現在の行列で逆計算する。
    /// </summary>
    /// <param name="src">X軸の値</param>
    /// <returns>現在の行列で逆計算されたX軸の値</returns>
    public float InvertTransformX(float src)
    {
        return (src - _tr[12]) / _tr[0];
    }

    /// <summary>
    /// Y軸の値を現在の行列で逆計算する。
    /// </summary>
    /// <param name="src">Y軸の値</param>
    /// <returns>現在の行列で逆計算されたY軸の値</returns>
    public float InvertTransformY(float src)
    {
        return (src - _tr[13]) / _tr[5];
    }

    /// <summary>
    /// 現在の行列の位置を起点にして相対的に移動する。
    /// </summary>
    /// <param name="x">X軸の移動量</param>
    /// <param name="y">Y軸の移動量</param>
    public void TranslateRelative(float x, float y)
    {
        float[] tr1 = new[]{
                          1.0f, 0.0f, 0.0f, 0.0f,
                          0.0f, 1.0f, 0.0f, 0.0f,
                          0.0f, 0.0f, 1.0f, 0.0f,
                          x,    y,    0.0f, 1.0f
                          };

        Multiply(tr1, _tr, _tr);
    }

    /// <summary>
    /// 現在の行列の位置を指定した位置へ移動する。
    /// </summary>
    /// <param name="x">X軸の移動量</param>
    /// <param name="y">Y軸の移動量</param>
    public void Translate(float x, float y)
    {
        _tr[12] = x;
        _tr[13] = y;
    }

    /// <summary>
    /// 現在の行列のX軸の位置を指定した位置へ移動する。
    /// </summary>
    /// <param name="x">X軸の移動量</param>
    public void TranslateX(float x)
    {
        _tr[12] = x;
    }

    /// <summary>
    /// 現在の行列のY軸の位置を指定した位置へ移動する。
    /// </summary>
    /// <param name="y">Y軸の移動量</param>
    public void TranslateY(float y)
    {
        _tr[13] = y;
    }

    /// <summary>
    /// 現在の行列の拡大率を相対的に設定する。
    /// </summary>
    /// <param name="x">X軸の拡大率</param>
    /// <param name="y">Y軸の拡大率</param>
    public void ScaleRelative(float x, float y)
    {
        float[] tr1 = new[]{
                          x,      0.0f,   0.0f, 0.0f,
                          0.0f,   y,      0.0f, 0.0f,
                          0.0f,   0.0f,   1.0f, 0.0f,
                          0.0f,   0.0f,   0.0f, 1.0f
                          };

        Multiply(tr1, _tr, _tr);
    }

    /// <summary>
    /// 現在の行列の拡大率を指定した倍率に設定する。
    /// </summary>
    /// <param name="x">X軸の拡大率</param>
    /// <param name="y">Y軸の拡大率</param>
    public void Scale(float x, float y)
    {
        _tr[0] = x;
        _tr[5] = y;
    }

    /// <summary>
    /// 現在の行列に行列を乗算する。
    /// </summary>
    /// <param name="m">行列</param>
    public void MultiplyByMatrix(CubismMatrix44 m)
    {
        Multiply(m.GetArray(), _tr, _tr);
    }
}
