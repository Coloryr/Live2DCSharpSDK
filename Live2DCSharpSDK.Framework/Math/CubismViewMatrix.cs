using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Math;

public class CubismViewMatrix : CubismMatrix44
{
    /// <summary>
    /// デバイスに対応する論理座標上の範囲（左辺X軸位置）
    /// </summary>
    private float _screenLeft;
    /// <summary>
    /// デバイスに対応する論理座標上の範囲（右辺X軸位置）
    /// </summary>
    private float _screenRight;
    /// <summary>
    /// デバイスに対応する論理座標上の範囲（下辺Y軸位置）
    /// </summary>
    private float _screenTop;
    /// <summary>
    /// デバイスに対応する論理座標上の範囲（上辺Y軸位置）
    /// </summary>
    private float _screenBottom;
    /// <summary>
    /// 論理座標上の移動可能範囲（左辺X軸位置）
    /// </summary>
    private float _maxLeft;
    /// <summary>
    /// 論理座標上の移動可能範囲（右辺X軸位置）
    /// </summary>
    private float _maxRight;
    /// <summary>
    /// 論理座標上の移動可能範囲（下辺Y軸位置）
    /// </summary>
    private float _maxTop;
    /// <summary>
    /// 論理座標上の移動可能範囲（上辺Y軸位置）
    /// </summary>
    private float _maxBottom;
    /// <summary>
    /// 拡大率の最大値
    /// </summary>
    private float _maxScale;
    /// <summary>
    /// 拡大率の最小値
    /// </summary>
    private float _minScale;

    /// <summary>
    /// 移動を調整する。
    /// </summary>
    /// <param name="x">X軸の移動量</param>
    /// <param name="y">Y軸の移動量</param>
    public void AdjustTranslate(float x, float y)
    {
        if (_tr[0] * _maxLeft + (_tr[12] + x) > _screenLeft)
        {
            x = _screenLeft - _tr[0] * _maxLeft - _tr[12];
        }

        if (_tr[0] * _maxRight + (_tr[12] + x) < _screenRight)
        {
            x = _screenRight - _tr[0] * _maxRight - _tr[12];
        }


        if (_tr[5] * _maxTop + (_tr[13] + y) < _screenTop)
        {
            y = _screenTop - _tr[5] * _maxTop - _tr[13];
        }

        if (_tr[5] * _maxBottom + (_tr[13] + y) > _screenBottom)
        {
            y = _screenBottom - _tr[5] * _maxBottom - _tr[13];
        }

        float[] tr1 = new[] { 1.0f,   0.0f,   0.0f, 0.0f,
                        0.0f,   1.0f,   0.0f, 0.0f,
                        0.0f,   0.0f,   1.0f, 0.0f,
                        x,      y,      0.0f, 1.0f };
        Multiply(tr1, _tr, _tr);
    }

    /// <summary>
    /// 拡大率を調整する。
    /// </summary>
    /// <param name="cx">拡大を行うX軸の中心位置</param>
    /// <param name="cy">拡大を行うY軸の中心位置</param>
    /// <param name="scale">拡大率</param>
    public void AdjustScale(float cx, float cy, float scale)
    {
        float MaxScale = GetMaxScale();
        float MinScale = GetMinScale();

        float targetScale = scale * _tr[0]; //

        if (targetScale < MinScale)
        {
            if (_tr[0] > 0.0f)
            {
                scale = MinScale / _tr[0];
            }
        }
        else if (targetScale > MaxScale)
        {
            if (_tr[0] > 0.0f)
            {
                scale = MaxScale / _tr[0];
            }
        }

        float[] tr1 = new[]{
                          1.0f, 0.0f, 0.0f, 0.0f,
                          0.0f, 1.0f, 0.0f, 0.0f,
                          0.0f, 0.0f, 1.0f, 0.0f,
                          cx,   cy,   0.0f, 1.0f
                          };
        float[] tr2 = new[]{
                          scale, 0.0f,  0.0f, 0.0f,
                          0.0f,  scale, 0.0f, 0.0f,
                          0.0f,  0.0f,  1.0f, 0.0f,
                          0.0f,  0.0f,  0.0f, 1.0f
                          };
        float[] tr3 = new[]{
                          1.0f, 0.0f, 0.0f, 0.0f,
                          0.0f, 1.0f, 0.0f, 0.0f,
                          0.0f, 0.0f, 1.0f, 0.0f,
                          -cx,  -cy,  0.0f, 1.0f
                          };

        Multiply(tr3, _tr, _tr);
        Multiply(tr2, _tr, _tr);
        Multiply(tr1, _tr, _tr);
    }

    /// <summary>
    /// デバイスに対応する論理座標上の範囲の設定を行う。
    /// </summary>
    /// <param name="left">左辺のX軸の位置</param>
    /// <param name="right">右辺のX軸の位置</param>
    /// <param name="bottom">下辺のY軸の位置</param>
    /// <param name="top">上辺のY軸の位置</param>
    public void SetScreenRect(float left, float right, float bottom, float top)
    {
        _screenLeft = left;
        _screenRight = right;
        _screenTop = top;
        _screenBottom = bottom;
    }

    /// <summary>
    /// デバイスに対応する論理座標上の移動可能範囲の設定を行う。
    /// </summary>
    /// <param name="left">左辺のX軸の位置</param>
    /// <param name="right">右辺のX軸の位置</param>
    /// <param name="bottom">下辺のY軸の位置</param>
    /// <param name="top">上辺のY軸の位置</param>
    public void SetMaxScreenRect(float left, float right, float bottom, float top)
    {
        _maxLeft = left;
        _maxRight = right;
        _maxTop = top;
        _maxBottom = bottom;
    }

    /// <summary>
    /// 最大拡大率を設定する。
    /// </summary>
    /// <param name="maxScale">最大拡大率</param>
    public void SetMaxScale(float maxScale)
    {
        _maxScale = maxScale;
    }

    /// <summary>
    /// 最小拡大率を設定する。
    /// </summary>
    /// <param name="minScale">最小拡大率</param>
    public void SetMinScale(float minScale)
    {
        _minScale = minScale;
    }

    /// <summary>
    /// 最大拡大率を取得する。
    /// </summary>
    /// <returns>最大拡大率</returns>
    public float GetMaxScale()
    {
        return _maxScale;
    }

    /// <summary>
    /// 最小拡大率を取得する。
    /// </summary>
    /// <returns>最小拡大率</returns>
    public float GetMinScale()
    {
        return _minScale;
    }

    /// <summary>
    /// 拡大率が最大になっているかどうかを確認する。
    /// </summary>
    /// <returns>true    拡大率は最大になっている
    /// false   拡大率は最大になっていない</returns>
    public bool IsMaxScale()
    {
        return GetScaleX() >= _maxScale;
    }

    /// <summary>
    /// 拡大率が最小になっているかどうかを確認する。
    /// </summary>
    /// <returns>true    拡大率は最小になっている
    /// false   拡大率は最小になっていない</returns>
    public bool IsMinScale()
    {
        return GetScaleX() <= _minScale;
    }

    /// <summary>
    /// デバイスに対応する論理座標の左辺のX軸位置を取得する。
    /// </summary>
    /// <returns>デバイスに対応する論理座標の左辺のX軸位置</returns>
    public float GetScreenLeft()
    {
        return _screenLeft;
    }

    /// <summary>
    /// デバイスに対応する論理座標の右辺のX軸位置を取得する。
    /// </summary>
    /// <returns>デバイスに対応する論理座標の右辺のX軸位置</returns>
    public float GetScreenRight()
    {
        return _screenRight;
    }

    /// <summary>
    /// デバイスに対応する論理座標の下辺のY軸位置を取得する。
    /// </summary>
    /// <returns>デバイスに対応する論理座標の下辺のY軸位置</returns>
    public float GetScreenBottom()
    {
        return _screenBottom;
    }

    /// <summary>
    /// デバイスに対応する論理座標の上辺のY軸位置を取得する。
    /// </summary>
    /// <returns>デバイスに対応する論理座標の上辺のY軸位置</returns>
    public float GetScreenTop()
    {
        return _screenTop;
    }

    /// <summary>
    /// 左辺のX軸位置の最大値を取得する。
    /// </summary>
    /// <returns>左辺のX軸位置の最大値</returns>
    public float GetMaxLeft()
    {
        return _maxLeft;
    }

    /// <summary>
    /// 右辺のX軸位置の最大値を取得する。
    /// </summary>
    /// <returns>右辺のX軸位置の最大値</returns>
    public float GetMaxRight()
    {
        return _maxRight;
    }

    /// <summary>
    /// 下辺のY軸位置の最大値を取得する。
    /// </summary>
    /// <returns>下辺のY軸位置の最大値</returns>
    public float GetMaxBottom()
    {
        return _maxBottom;
    }

    /// <summary>
    /// 上辺のY軸位置の最大値を取得する。
    /// </summary>
    /// <returns>上辺のY軸位置の最大値</returns>
    public float GetMaxTop()
    {
        return _maxTop;
    }
}
