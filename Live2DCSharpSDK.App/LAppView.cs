using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Rendering.OpenGL;

namespace Live2DCSharpSDK.App;

public enum SelectTarget
{
    /// <summary>
    /// デフォルトのフレームバッファにレンダリング
    /// </summary>
    SelectTarget_None,
    /// <summary>
    /// LAppModelが各自持つフレームバッファにレンダリング
    /// </summary>
    SelectTarget_ModelFrameBuffer,
    /// <summary>
    /// LAppViewの持つフレームバッファにレンダリング
    /// </summary>
    SelectTarget_ViewFrameBuffer,
};

/// <summary>
/// 描画クラス
/// </summary>
public class LAppView : IDisposable
{
    /// <summary>
    /// タッチマネージャー
    /// </summary>
    private TouchManager _touchManager;
    /// <summary>
    /// デバイスからスクリーンへの行列
    /// </summary>
    private CubismMatrix44 _deviceToScreen;
    /// <summary>
    /// viewMatrix
    /// </summary>
    private CubismViewMatrix _viewMatrix;

    /// <summary>
    /// モードによってはCubismモデル結果をこっちにレンダリング
    /// </summary>
    private CubismOffscreenFrame_OpenGLES2 _renderBuffer;
    /// <summary>
    /// レンダリング先の選択肢
    /// </summary>
    private SelectTarget _renderTarget;
    /// <summary>
    /// レンダリングターゲットのクリアカラー
    /// </summary>
    private float[] _clearColor = new float[4];

    /// <summary>
    /// モードによっては_renderBufferのテクスチャを描画
    /// </summary>
    private LAppSprite _renderSprite;

    private LAppDelegate Lapp;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public LAppView(LAppDelegate lapp)
    {
        Lapp = lapp;

        _renderTarget = LAppDefine.RenderTarget;

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
    /// デストラクタ
    /// </summary>
    public void Dispose()
    {
        _renderBuffer.DestroyOffscreenFrame();
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
        _viewMatrix.SetMaxScale(LAppDefine.ViewMaxScale); // 限界拡大率
        _viewMatrix.SetMinScale(LAppDefine.ViewMinScale); // 限界縮小率

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
        LAppLive2DManager Live2DManager = Lapp.GetLive2D();

        Live2DManager.SetViewMatrix(_viewMatrix);

        // Cubism更新・描画
        Live2DManager.OnUpdate();

        // 各モデルが持つ描画ターゲットをテクスチャとする場合
        if (_renderTarget == SelectTarget.SelectTarget_ModelFrameBuffer && _renderSprite != null)
        {
            float[] uvVertex = new[]
            {
            1.0f, 1.0f,
            0.0f, 1.0f,
            0.0f, 0.0f,
            1.0f, 0.0f,
        };

            for (int i = 0; i < Live2DManager.GetModelNum(); i++)
            {
                LAppModel model = Live2DManager.GetModel(i);
                float alpha = i < 1 ? 1.0f : model.GetOpacity(); // 片方のみ不透明度を取得できるようにする
                //_renderSprite.SetColor(1.0f, 1.0f, 1.0f, alpha);

                if (model != null)
                {
                    //_renderSprite.RenderImmidiate(model.GetRenderBuffer().GetColorBuffer(), uvVertex);
                }
            }
        }
    }

    /// <summary>
    /// 画像の初期化を行う。
    /// </summary>
    public void InitializeSprite()
    {

    }

    /// <summary>
    /// タッチされたときに呼ばれる。
    /// </summary>
    /// <param name="pointX">スクリーンX座標</param>
    /// <param name="pointY">スクリーンY座標</param>
    public void OnTouchesBegan(float pointX, float pointY)
    {
        _touchManager.TouchesBegan(pointX, pointY);
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

        Lapp.GetLive2D().OnDrag(viewX, viewY);
    }

    /// <summary>
    /// タッチが終了したら呼ばれる。
    /// </summary>
    /// <param name="pointX">スクリーンX座標</param>
    /// <param name="pointY">スクリーンY座標</param>
    public void OnTouchesEnded(float pointX, float pointY)
    {
        // タッチ終了
        LAppLive2DManager live2DManager = Lapp.GetLive2D();
        live2DManager.OnDrag(0.0f, 0.0f);
        {

            // シングルタップ
            float x = _deviceToScreen.TransformX(_touchManager.GetX()); // 論理座標変換した座標を取得。
            float y = _deviceToScreen.TransformY(_touchManager.GetY()); // 論理座標変換した座標を取得。
            if (LAppDefine.DebugTouchLogEnable)
            {
                LAppPal.PrintLog($"[APP]touchesEnded x:{x:#.##} y:{y:#.##}");
            }
            live2DManager.OnTap(x, y);
        }
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
    /// モデル1体を描画する直前にコールされる
    /// </summary>
    public void PreModelDraw(LAppModel refModel)
    {
        // 別のレンダリングターゲットへ向けて描画する場合の使用するフレームバッファ
        CubismOffscreenFrame_OpenGLES2 useTarget;

        if (_renderTarget != SelectTarget.SelectTarget_None)
        {// 別のレンダリングターゲットへ向けて描画する場合

            // 使用するターゲット
            useTarget = (_renderTarget == SelectTarget.SelectTarget_ViewFrameBuffer) ? _renderBuffer : refModel.GetRenderBuffer();

            if (!useTarget.IsValid())
            {// 描画ターゲット内部未作成の場合はここで作成
                Lapp.GL.GetWindowSize(out int width, out int height);
                if (width != 0 && height != 0)
                {
                    // モデル描画キャンバス
                    useTarget.CreateOffscreenFrame((int)(width), (int)(height));
                }
            }

            // レンダリング開始
            useTarget.BeginDraw();
            useTarget.Clear(_clearColor[0], _clearColor[1], _clearColor[2], _clearColor[3]); // 背景クリアカラー
        }
    }

    /// <summary>
    /// モデル1体を描画した直後にコールされる
    /// </summary>
    public void PostModelDraw(LAppModel refModel)
    {
        // 別のレンダリングターゲットへ向けて描画する場合の使用するフレームバッファ
        CubismOffscreenFrame_OpenGLES2 useTarget;

        if (_renderTarget != SelectTarget.SelectTarget_None)
        {// 別のレンダリングターゲットへ向けて描画する場合

            // 使用するターゲット
            useTarget = (_renderTarget == SelectTarget.SelectTarget_ViewFrameBuffer) ? _renderBuffer : refModel.GetRenderBuffer();

            // レンダリング終了
            useTarget.EndDraw();

            // LAppViewの持つフレームバッファを使うなら、スプライトへの描画はここ
            if (_renderTarget == SelectTarget.SelectTarget_ViewFrameBuffer && _renderSprite != null)
            {
                float[] uvVertex = new[]
                {
                1.0f, 1.0f,
                0.0f, 1.0f,
                0.0f, 0.0f,
                1.0f, 0.0f,
            };

                //_renderSprite.SetColor(1.0f, 1.0f, 1.0f, GetSpriteAlpha(0));
                //_renderSprite.RenderImmidiate(useTarget.GetColorBuffer(), uvVertex);
            }
        }
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
    /// レンダリング先を切り替える
    /// </summary>
    public void SwitchRenderingTarget(SelectTarget targetType)
    {
        _renderTarget = targetType;
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
