using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Motion;
using Live2DCSharpSDK.Framework.Rendering.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.App;

/// <summary>
/// サンプルアプリケーションにおいてCubismModelを管理するクラス
/// モデル生成と破棄、タップイベントの処理、モデル切り替えを行う。
/// </summary>
public class LAppTextureManager : IDisposable
{
    /// <summary>
    /// モデル描画に用いるView行列
    /// </summary>
    private CubismMatrix44 _viewMatrix;
    /// <summary>
    /// モデルインスタンスのコンテナ
    /// </summary>
    private List<LAppModel> _models;

    private LAppDelegate Lapp;

    public event Action<ACubismMotion>? MotionFinished;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public LAppTextureManager(LAppDelegate lapp)
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
                _models[i]->SetRandomExpression();
            }
            else if (_models[i]->HitTest(LAppDefine.HitAreaNameBody, x, y))
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
    public void OnUpdate(OpenGLApi GL)
    {
        GL.GetWindowSize(out int width, out int height);

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

            // モデル1体描画前コール
            Lapp.GetView().PreModelDraw(model);

            model->Update();
            model->Draw(projection);///< 参照渡しなのでprojectionは変質する

            // モデル1体描画後コール
            Lapp.GetView().PostModelDraw(model);
        }
    }

    public void LoadModel(string dir, string name)
    {
        _sceneIndex = index;
        if (DebugLogEnable)
        {
            LAppPal::PrintLog("[APP]model index: %d", _sceneIndex);
        }

        // ModelDir[]に保持したディレクトリ名から
        // model3.jsonのパスを決定する.
        // ディレクトリ名とmodel3.jsonの名前を一致させておくこと.
        std::string model = ModelDir[index];
        std::string modelPath = ResourcesPath + model + "/";
        std::string modelJsonName = ModelDir[index];
        modelJsonName += ".model3.json";

        ReleaseAllModel();
        _models.PushBack(new LAppModel());
        _models[0]->LoadAssets(modelPath.c_str(), modelJsonName.c_str());

        /*
         * モデル半透明表示を行うサンプルを提示する。
         * ここでUSE_RENDER_TARGET、USE_MODEL_RENDER_TARGETが定義されている場合
         * 別のレンダリングターゲットにモデルを描画し、描画結果をテクスチャとして別のスプライトに張り付ける。
         */
        {
#if defined(USE_RENDER_TARGET)
        // LAppViewの持つターゲットに描画を行う場合、こちらを選択
        LAppView::SelectTarget useRenderTarget = LAppView::SelectTarget_ViewFrameBuffer;
#elif defined(USE_MODEL_RENDER_TARGET)
        // 各LAppModelの持つターゲットに描画を行う場合、こちらを選択
        LAppView::SelectTarget useRenderTarget = LAppView::SelectTarget_ModelFrameBuffer;
#else
            // デフォルトのメインフレームバッファへレンダリングする(通常)
            LAppView.SelectTarget useRenderTarget = LAppView.SelectTarget_None;
#endif

#if defined(USE_RENDER_TARGET) || defined(USE_MODEL_RENDER_TARGET)
        // モデル個別にαを付けるサンプルとして、もう1体モデルを作成し、少し位置をずらす
        _models.PushBack(new LAppModel());
        _models[1]->LoadAssets(modelPath.c_str(), modelJsonName.c_str());
        _models[1]->GetModelMatrix()->TranslateX(0.2f);
#endif

            LAppDelegate::GetInstance()->GetView()->SwitchRenderingTarget(useRenderTarget);

            // 別レンダリング先を選択した際の背景クリア色
            float clearColor[3] = { 1.0f, 1.0f, 1.0f };
            LAppDelegate::GetInstance()->GetView()->SetRenderTargetClearColor(clearColor[0], clearColor[1], clearColor[2]);
        }
    }

    /// <summary>
    /// モデル個数を得る
    /// </summary>
    /// <returns>所持モデル個数</returns>
    public int GetModelNum()
    { 
    
    }

    /// <summary>
    /// viewMatrixをセットする
    /// </summary>
    public void SetViewMatrix(CubismMatrix44 m)
    { 
        
    }

    public void Dispose()
    {
        ReleaseAllModel();
    }
}
