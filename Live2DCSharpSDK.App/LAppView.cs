using Live2DCSharpSDK.Framework;
using Live2DCSharpSDK.Framework.Math;

namespace Live2DCSharpSDK.App;

/// <summary>
/// 描画クラス
/// </summary>
public class LAppView
{
    /// <summary>
    /// タッチマネージャー
    /// </summary>
    private TouchManager _touchManager;
    /// <summary>
    /// デバイスからスクリーンへの行列
    /// </summary>
    private readonly CubismMatrix44 _deviceToScreen;
    /// <summary>
    /// viewMatrix
    /// </summary>
    private readonly CubismViewMatrix _viewMatrix;

    /// <summary>
    /// レンダリングターゲットのクリアカラー
    /// </summary>
    private readonly float[] _clearColor = new float[4];

    private readonly LAppDelegate Lapp;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public LAppView(LAppDelegate lapp)
    {
        Lapp = lapp;

        _clearColor[0] = 1.0f;
        _clearColor[1] = 1.0f;
        _clearColor[2] = 1.0f;
        _clearColor[3] = 0.0f;

        // タッチ関係のイベント管理
        _touchManager = new TouchManager();

        // デバイス座標からスクリーン座標に変換するための
        _deviceToScreen = new CubismMatrix44();

        // 画面の表示の拡大縮小や移動の変換を行う行列
        _viewMatrix = new CubismViewMatrix();
    }

    /// <summary>
    /// 初期化する。
    /// </summary>
    public void Initialize()
    {
        Lapp.GL.GetWindowSize(out int width, out int height);

        if (width == 0 || height == 0)
        {
            return;
        }

        // 縦サイズを基準とする
        float ratio = (float)width / height;
        float left = -ratio;
        float right = ratio;
        float bottom = LAppDefine.ViewLogicalLeft;
        float top = LAppDefine.ViewLogicalRight;

        _viewMatrix.SetScreenRect(left, right, bottom, top); // デバイスに対応する画面の範囲。 Xの左端, Xの右端, Yの下端, Yの上端
        _viewMatrix.Scale(LAppDefine.ViewScale, LAppDefine.ViewScale);

        _deviceToScreen.LoadIdentity(); // サイズが変わった際などリセット必須
        if (width > height)
        {
            float screenW = MathF.Abs(right - left);
            _deviceToScreen.ScaleRelative(screenW / width, -screenW / width);
        }
        else
        {
            float screenH = MathF.Abs(top - bottom);
            _deviceToScreen.ScaleRelative(screenH / height, -screenH / height);
        }
        _deviceToScreen.TranslateRelative(-width * 0.5f, -height * 0.5f);

        // 表示範囲の設定
        _viewMatrix.MaxScale = LAppDefine.ViewMaxScale; // 限界拡大率
        _viewMatrix.MinScale = LAppDefine.ViewMinScale; // 限界縮小率

        // 表示できる最大範囲
        _viewMatrix.SetMaxScreenRect(
            LAppDefine.ViewLogicalMaxLeft,
            LAppDefine.ViewLogicalMaxRight,
            LAppDefine.ViewLogicalMaxBottom,
            LAppDefine.ViewLogicalMaxTop
        );
    }

    /// <summary>
    /// 描画する。
    /// </summary>
    public void Render()
    {
        var Live2DManager = Lapp.Live2dManager;
        Live2DManager.ViewMatrix.SetMatrix(_viewMatrix);

        // Cubism更新・描画
        Live2DManager.OnUpdate();
    }

    /// <summary>
    /// タッチされたときに呼ばれる。
    /// </summary>
    /// <param name="pointX">スクリーンX座標</param>
    /// <param name="pointY">スクリーンY座標</param>
    public void OnTouchesBegan(float pointX, float pointY)
    {
        _touchManager.TouchesBegan(pointX, pointY);
        CubismLog.CubismLogDebug($"[Live2D]touchesBegan x:{pointX:#.##} y:{pointY:#.##}");
    }

    /// <summary>
    /// タッチしているときにポインタが動いたら呼ばれる。
    /// </summary>
    /// <param name="pointX">スクリーンX座標</param>
    /// <param name="pointY">スクリーンY座標</param>
    public void OnTouchesMoved(float pointX, float pointY)
    {
        float viewX = TransformViewX(_touchManager.GetX());
        float viewY = TransformViewY(_touchManager.GetY());

        _touchManager.TouchesMoved(pointX, pointY);

        Lapp.Live2dManager.OnDrag(viewX, viewY);
    }

    /// <summary>
    /// タッチが終了したら呼ばれる。
    /// </summary>
    /// <param name="pointX">スクリーンX座標</param>
    /// <param name="pointY">スクリーンY座標</param>
    public void OnTouchesEnded(float pointX, float pointY)
    {
        // タッチ終了
        var live2DManager = Lapp.Live2dManager;
        live2DManager.OnDrag(0.0f, 0.0f);
        // シングルタップ
        float x = _deviceToScreen.TransformX(_touchManager.GetX()); // 論理座標変換した座標を取得。
        float y = _deviceToScreen.TransformY(_touchManager.GetY()); // 論理座標変換した座標を取得。
        CubismLog.CubismLogDebug($"[Live2D]touchesEnded x:{x:#.##} y:{y:#.##}");
        live2DManager.OnTap(x, y);
    }

    /// <summary>
    /// X座標をView座標に変換する。
    /// </summary>
    /// <param name="deviceX">デバイスX座標</param>
    public float TransformViewX(float deviceX)
    {
        float screenX = _deviceToScreen.TransformX(deviceX); // 論理座標変換した座標を取得。
        return _viewMatrix.InvertTransformX(screenX); // 拡大、縮小、移動後の値。
    }

    /// <summary>
    /// Y座標をView座標に変換する。
    /// </summary>
    /// <param name="deviceY">デバイスY座標</param>
    public float TransformViewY(float deviceY)
    {
        float screenY = _deviceToScreen.TransformY(deviceY); // 論理座標変換した座標を取得。
        return _viewMatrix.InvertTransformY(screenY); // 拡大、縮小、移動後の値。
    }

    /// <summary>
    /// X座標をScreen座標に変換する。
    /// </summary>
    /// <param name="deviceX">デバイスX座標</param>
    public float TransformScreenX(float deviceX)
    {
        return _deviceToScreen.TransformX(deviceX);
    }

    /// <summary>
    /// Y座標をScreen座標に変換する。
    /// </summary>
    /// <param name="deviceY">デバイスY座標</param>
    public float TransformScreenY(float deviceY)
    {
        return _deviceToScreen.TransformY(deviceY);
    }

    /// <summary>
    /// 別レンダリングターゲットにモデルを描画するサンプルで
    /// 描画時のαを決定する
    /// </summary>
    public float GetSpriteAlpha(int assign)
    {
        // assignの数値に応じて適当に決定
        float alpha = 0.25f + assign * 0.5f; // サンプルとしてαに適当な差をつける
        if (alpha > 1.0f)
        {
            alpha = 1.0f;
        }
        if (alpha < 0.1f)
        {
            alpha = 0.1f;
        }

        return alpha;
    }

    /// <summary>
    /// レンダリング先をデフォルト以外に切り替えた際の背景クリア色設定
    /// </summary>
    /// <param name="r">赤(0.0~1.0)</param>
    /// <param name="g">緑(0.0~1.0)</param>
    /// <param name="b">青(0.0~1.0)</param>
    public void SetRenderTargetClearColor(float r, float g, float b)
    {
        _clearColor[0] = r;
        _clearColor[1] = g;
        _clearColor[2] = b;
    }
}
