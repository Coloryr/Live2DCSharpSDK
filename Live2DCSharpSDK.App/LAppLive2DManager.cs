using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Motion;

namespace Live2DCSharpSDK.App;

/// <summary>
/// サンプルアプリケーションにおいてCubismModelを管理するクラス
/// モデル生成と破棄、タップイベントの処理、モデル切り替えを行う。
/// </summary>
public class LAppLive2DManager : IDisposable
{
    /// <summary>
    /// モデル描画に用いるView行列
    /// </summary>
    private CubismMatrix44 _viewMatrix;
    /// <summary>
    /// モデルインスタンスのコンテナ
    /// </summary>
    private readonly List<LAppModel> _models = new();

    private LAppDelegate Lapp;

    public event Action<ACubismMotion>? MotionFinished;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public LAppLive2DManager(LAppDelegate lapp)
    {
        _viewMatrix = new CubismMatrix44();
        Lapp = lapp;
    }

    /// <summary>
    /// 現在のシーンで保持しているモデルを返す
    /// </summary>
    /// <param name="no">モデルリストのインデックス値</param>
    /// <returns>モデルのインスタンスを返す。インデックス値が範囲外の場合はNULLを返す。</returns>
    public LAppModel GetModel(int no)
    {
        if (no < _models.Count)
        {
            return _models[no];
        }

        return null;
    }

    /// <summary>
    /// 現在のシーンで保持しているすべてのモデルを解放する
    /// </summary>
    public void ReleaseAllModel()
    {
        for (int i = 0; i < _models.Count; i++)
        {
            _models[i].Dispose();
        }

        _models.Clear();
    }

    /// <summary>
    /// 画面をドラッグしたときの処理
    /// </summary>
    /// <param name="x">画面のX座標</param>
    /// <param name="y">画面のY座標</param>
    public void OnDrag(float x, float y)
    {
        for (int i = 0; i < _models.Count; i++)
        {
            LAppModel model = GetModel(i);

            model.SetDragging(x, y);
        }
    }

    /// <summary>
    /// 画面をタップしたときの処理
    /// </summary>
    /// <param name="x">画面のX座標</param>
    /// <param name="y">画面のY座標</param>
    public void OnTap(float x, float y)
    {
        if (LAppDefine.DebugLogEnable)
        {
            LAppPal.PrintLog($"[APP]tap point: x:{x:#.##} y:{y:#.##}");
        }

        for (int i = 0; i < _models.Count; i++)
        {
            if (_models[i].HitTest(LAppDefine.HitAreaNameHead, x, y))
            {
                if (LAppDefine.DebugLogEnable)
                {
                    LAppPal.PrintLog($"[APP]hit area: [{LAppDefine.HitAreaNameHead}]");
                }
                _models[i].SetRandomExpression();
            }
            else if (_models[i].HitTest(LAppDefine.HitAreaNameBody, x, y))
            {
                if (LAppDefine.DebugLogEnable)
                {
                    LAppPal.PrintLog($"[APP]hit area: [{LAppDefine.HitAreaNameBody}]");
                }
                _models[i].StartRandomMotion(LAppDefine.MotionGroupTapBody, LAppDefine.PriorityNormal, OnFinishedMotion);
            }
        }
    }

    private void OnFinishedMotion(ACubismMotion self)
    {
        LAppPal.PrintLog($"Motion Finished: {self}");
        MotionFinished?.Invoke(self);
    }

    /// <summary>
    /// 画面を更新するときの処理
    /// モデルの更新処理および描画処理を行う
    /// </summary>
    public void OnUpdate()
    {
        Lapp.GL.GetWindowSize(out int width, out int height);

        int modelCount = _models.Count;
        for (int i = 0; i < modelCount; ++i)
        {
            CubismMatrix44 projection = new();
            LAppModel model = GetModel(i);

            if (model.GetModel() == null)
            {
                LAppPal.PrintLog("Failed to model->GetModel().");
                continue;
            }

            if (model.GetModel().GetCanvasWidth() > 1.0f && width < height)
            {
                // 横に長いモデルを縦長ウィンドウに表示する際モデルの横サイズでscaleを算出する
                model.GetModelMatrix().SetWidth(2.0f);
                projection.Scale(1.0f, (float)width / height);
            }
            else
            {
                projection.Scale((float)height / width, 1.0f);
            }

            // 必要があればここで乗算
            if (_viewMatrix != null)
            {
                projection.MultiplyByMatrix(_viewMatrix);
            }

            model.Update();
            model.Draw(projection);///< 参照渡しなのでprojectionは変質する
        }
    }

    public void LoadModel(string dir, string name)
    {
        if (LAppDefine.DebugLogEnable)
        {
            LAppPal.PrintLog($"[APP]model load: {name}");
        }

        // ModelDir[]に保持したディレクトリ名から
        // model3.jsonのパスを決定する.
        // ディレクトリ名とmodel3.jsonの名前を一致させておくこと.
        var modelJsonName = Path.GetFullPath($"{dir}{name}.model3.json");

        var model = new LAppModel(Lapp);
        _models.Add(model);
        model.LoadAssets(dir, modelJsonName);

        /*
         * モデル半透明表示を行うサンプルを提示する。
         * ここでUSE_RENDER_TARGET、USE_MODEL_RENDER_TARGETが定義されている場合
         * 別のレンダリングターゲットにモデルを描画し、描画結果をテクスチャとして別のスプライトに張り付ける。
         */
        {
            Lapp.GetView().SwitchRenderingTarget(LAppDefine.RenderTarget);

            // 別レンダリング先を選択した際の背景クリア色
            float[] clearColor = new[] { 1.0f, 1.0f, 1.0f };
            Lapp.GetView().SetRenderTargetClearColor(clearColor[0], clearColor[1], clearColor[2]);
        }
    }

    /// <summary>
    /// モデル個数を得る
    /// </summary>
    /// <returns>所持モデル個数</returns>
    public int GetModelNum()
    {
        return _models.Count;
    }

    /// <summary>
    /// viewMatrixをセットする
    /// </summary>
    public void SetViewMatrix(CubismMatrix44 m)
    {
        for (int i = 0; i < 16; i++)
        {
            _viewMatrix.GetArray()[i] = m.GetArray()[i];
        }
    }

    public void Dispose()
    {
        ReleaseAllModel();
    }
}
