using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Type;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public class CubismClippingManager_OpenGLES2 : CubismClippingManager
{
    private readonly OpenGLApi GL;

    public CubismClippingManager_OpenGLES2(OpenGLApi gl) : base(RenderType.OpenGL)
    {
        GL = gl;
    }

    /// <summary>
    /// クリッピングコンテキストを作成する。モデル描画時に実行する。
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="renderer">レンダラのインスタンス</param>
    /// <param name="lastFBO">フレームバッファ</param>
    /// <param name="lastViewport">ビューポート</param>
    internal unsafe void SetupClippingContext(CubismModel model, CubismRenderer_OpenGLES2 renderer, int lastFBO, int[] lastViewport)
    {
        // 全てのクリッピングを用意する
        // 同じクリップ（複数の場合はまとめて１つのクリップ）を使う場合は１度だけ設定する
        int usingClipCount = 0;
        for (int clipIndex = 0; clipIndex < _clippingContextListForMask.Count; clipIndex++)
        {
            // １つのクリッピングマスクに関して
            var cc = _clippingContextListForMask[clipIndex] as CubismClippingContext_OpenGLES2;

            // このクリップを利用する描画オブジェクト群全体を囲む矩形を計算
            CalcClippedDrawTotalBounds(model, cc!);

            if (cc._isUsing)
            {
                usingClipCount++; //使用中としてカウント
            }
        }

        if (usingClipCount <= 0)
        {
            return;
        }

        // マスク作成処理
        // 生成したOffscreenSurfaceと同じサイズでビューポートを設定
        GL.glViewport(0, 0, (int)ClippingMaskBufferSize.X, (int)ClippingMaskBufferSize.Y);

        // 後の計算のためにインデックスの最初をセット
        _currentMaskBuffer = renderer.GetMaskBuffer(0);
        // ----- マスク描画処理 -----
        _currentMaskBuffer.BeginDraw(lastFBO);

        renderer.PreDraw(); // バッファをクリアする

        // 各マスクのレイアウトを決定していく
        SetupLayoutBounds(usingClipCount);

        // サイズがレンダーテクスチャの枚数と合わない場合は合わせる
        if (_clearedMaskBufferFlags.Count != RenderTextureCount)
        {
            _clearedMaskBufferFlags.Clear();

            for (int i = 0; i < RenderTextureCount; ++i)
            {
                _clearedMaskBufferFlags.Add(false);
            }
        }
        else
        {
            // マスクのクリアフラグを毎フレーム開始時に初期化
            for (int i = 0; i < RenderTextureCount; ++i)
            {
                _clearedMaskBufferFlags[i] = false;
            }
        }

        // 実際にマスクを生成する
        // 全てのマスクをどの様にレイアウトして描くかを決定し、ClipContext , ClippedDrawContext に記憶する
        for (int clipIndex = 0; clipIndex < _clippingContextListForMask.Count; clipIndex++)
        {
            // --- 実際に１つのマスクを描く ---
            var clipContext = _clippingContextListForMask[clipIndex]
                as CubismClippingContext_OpenGLES2;
            RectF allClippedDrawRect = clipContext!._allClippedDrawRect; //このマスクを使う、全ての描画オブジェクトの論理座標上の囲み矩形
            RectF layoutBoundsOnTex01 = clipContext._layoutBounds; //この中にマスクを収める
            float MARGIN = 0.05f;

            // clipContextに設定したオフスクリーンサーフェイスをインデックスで取得
            var clipContextOffscreenSurface = renderer.GetMaskBuffer(clipContext._bufferIndex);

            // 現在のオフスクリーンサーフェイスがclipContextのものと異なる場合
            if (_currentMaskBuffer != clipContextOffscreenSurface)
            {
                _currentMaskBuffer.EndDraw();
                _currentMaskBuffer = clipContextOffscreenSurface;
                // マスク用RenderTextureをactiveにセット
                _currentMaskBuffer.BeginDraw(lastFBO);

                // バッファをクリアする。
                renderer.PreDraw();
            }

            // モデル座標上の矩形を、適宜マージンを付けて使う
            _tmpBoundsOnModel.SetRect(allClippedDrawRect);
            _tmpBoundsOnModel.Expand(allClippedDrawRect.Width * MARGIN, allClippedDrawRect.Height * MARGIN);
            //########## 本来は割り当てられた領域の全体を使わず必要最低限のサイズがよい
            // シェーダ用の計算式を求める。回転を考慮しない場合は以下のとおり
            // movePeriod' = movePeriod * scaleX + offX     [[ movePeriod' = (movePeriod - tmpBoundsOnModel.movePeriod)*scale + layoutBoundsOnTex01.movePeriod ]]
            float scaleX = layoutBoundsOnTex01.Width / _tmpBoundsOnModel.Width;
            float scaleY = layoutBoundsOnTex01.Height / _tmpBoundsOnModel.Height;

            // マスク生成時に使う行列を求める
            createMatrixForMask(false, layoutBoundsOnTex01, scaleX, scaleY);

            clipContext._matrixForMask.SetMatrix(_tmpMatrixForMask.Tr);
            clipContext._matrixForDraw.SetMatrix(_tmpMatrixForDraw.Tr);

            // 実際の描画を行う
            int clipDrawCount = clipContext._clippingIdCount;
            for (int i = 0; i < clipDrawCount; i++)
            {
                int clipDrawIndex = clipContext._clippingIdList[i];

                // 頂点情報が更新されておらず、信頼性がない場合は描画をパスする
                if (!model.GetDrawableDynamicFlagVertexPositionsDidChange(clipDrawIndex))
                {
                    continue;
                }

                renderer.IsCulling(model.GetDrawableCulling(clipDrawIndex));

                // マスクがクリアされていないなら処理する
                if (!_clearedMaskBufferFlags[clipContext._bufferIndex])
                {
                    // マスクをクリアする
                    // 1が無効（描かれない）領域、0が有効（描かれる）領域。（シェーダーCd*Csで0に近い値をかけてマスクを作る。1をかけると何も起こらない）
                    GL.glClearColor(1.0f, 1.0f, 1.0f, 1.0f);
                    GL.glClear(GL.GL_COLOR_BUFFER_BIT);
                    _clearedMaskBufferFlags[clipContext._bufferIndex] = true;
                }

                // 今回専用の変換を適用して描く
                // チャンネルも切り替える必要がある(A,R,G,B)
                renderer.ClippingContextBufferForMask = clipContext;

                renderer.DrawMeshOpenGL(model, clipDrawIndex);
            }
        }

        // --- 後処理 ---
        _currentMaskBuffer.EndDraw();
        renderer.ClippingContextBufferForMask = null;
        GL.glViewport(lastViewport[0], lastViewport[1], lastViewport[2], lastViewport[3]);
    }
}
