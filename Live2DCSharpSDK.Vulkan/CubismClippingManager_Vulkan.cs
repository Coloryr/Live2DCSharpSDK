using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Rendering;
using Silk.NET.Vulkan;

namespace Live2DCSharpSDK.Vulkan;

public class CubismClippingManager_Vulkan(Vk vk) : CubismClippingManager
{
    public override RenderType RenderType => RenderType.Vulkan;

    public override unsafe CubismClippingContext CreateClippingContext(CubismClippingManager manager,
        CubismModel model, int* clippingDrawableIndices, int clipCount)
    {
        return new CubismClippingContext_Vulkan(manager, clippingDrawableIndices, clipCount);
    }

    /// <summary>
    /// クリッピングコンテキストを作成する。モデル描画時に実行する。
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="commandBuffer">コマンドバッファ</param>
    /// <param name="updateCommandBuffer">更新用コマンドバッファ</param>
    /// <param name="renderer">レンダラのインスタンス</param>
    public unsafe void SetupClippingContext(CubismModel model, CommandBuffer commandBuffer, CommandBuffer updateCommandBuffer,
                              CubismRenderer_Vulkan renderer)
    {
        // 全てのクリッピングを用意する
        // 同じクリップ（複数の場合はまとめて１つのクリップ）を使う場合は１度だけ設定する
        int usingClipCount = 0;

        for (int clipIndex = 0; clipIndex < ClippingContextListForMask.Count; clipIndex++)
        {
            // １つのクリッピングマスクに関して
            var cc = ClippingContextListForMask[clipIndex];

            // このクリップを利用する描画オブジェクト群全体を囲む矩形を計算
            CalcClippedDrawTotalBounds(model, cc);

            if (cc.IsUsing)
            {
                usingClipCount++; //使用中としてカウント
            }
        }

        if (usingClipCount <= 0)
        {
            return;
        }

        // マスク作成処理
        // 後の計算のためにインデックスの最初をセット
        CurrentMaskBuffer = renderer.GetMaskBuffer(0);

        var buffer = (CurrentMaskBuffer as CubismOffscreenSurface_Vulkan)!;

        // 1が無効（描かれない）領域、0が有効（描かれる）領域。（シェーダで Cd*Csで0に近い値をかけてマスクを作る。1をかけると何も起こらない）
        buffer.BeginDraw(commandBuffer, 1.0f, 1.0f, 1.0f, 1.0f);

        // 生成したFrameBufferと同じサイズでビューポートを設定
        var viewport = CubismRenderer_Vulkan.GetViewport(ClippingMaskBufferSize.X, ClippingMaskBufferSize.Y, 0.0f, 1.0f);
        vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        var rect = CubismRenderer_Vulkan.GetScissor(0.0f, 0.0f, ClippingMaskBufferSize.X, ClippingMaskBufferSize.Y);
        vk.CmdSetScissor(commandBuffer, 0, 1, &rect);

        // 各マスクのレイアウトを決定していく
        SetupLayoutBounds(usingClipCount);

        // サイズがレンダーテクスチャの枚数と合わない場合は合わせる
        if (ClearedMaskBufferFlags.Count != RenderTextureCount)
        {
            ClearedMaskBufferFlags.Clear();

            for (int i = 0; i < RenderTextureCount; ++i)
            {
                ClearedMaskBufferFlags.Add(false);
            }
        }
        else
        {
            // マスクのクリアフラグを毎フレーム開始時に初期化
            for (int i = 0; i < RenderTextureCount; ++i)
            {
                ClearedMaskBufferFlags[i] = false;
            }
        }

        // 実際にマスクを生成する
        // 全てのマスクをどの様にレイアウトして描くかを決定し、ClipContext , ClippedDrawContext に記憶する
        for (int clipIndex = 0; clipIndex < ClippingContextListForMask.Count; clipIndex++)
        {
            // --- 実際に１つのマスクを描く ---
            var clipContext = (ClippingContextListForMask[clipIndex] as CubismClippingContext_Vulkan)!;
            var allClippedDrawRect = clipContext.AllClippedDrawRect; //このマスクを使う、全ての描画オブジェクトの論理座標上の囲み矩形
            var layoutBoundsOnTex01 = clipContext.LayoutBounds; //この中にマスクを収める
            var MARGIN = 0.05f;

            // clipContextに設定したオフスクリーンサーフェイスをインデックスで取得
            var clipContextOffscreenSurface = renderer.GetMaskBuffer(clipContext.BufferIndex)!;

            // 現在のオフスクリーンサーフェイスがclipContextのものと異なる場合
            if (CurrentMaskBuffer != clipContextOffscreenSurface)
            {
                buffer.EndDraw(commandBuffer);
                CurrentMaskBuffer = clipContextOffscreenSurface;
                buffer = (CurrentMaskBuffer as CubismOffscreenSurface_Vulkan)!;
                // マスク用RenderTextureをactiveにセット
                buffer.BeginDraw(commandBuffer, 1.0f, 1.0f, 1.0f, 1.0f);
            }

            // モデル座標上の矩形を、適宜マージンを付けて使う
            TmpBoundsOnModel.SetRect(allClippedDrawRect);
            TmpBoundsOnModel.Expand(allClippedDrawRect.Width * MARGIN, allClippedDrawRect.Height * MARGIN);
            //########## 本来は割り当てられた領域の全体を使わず必要最低限のサイズがよい
            // シェーダ用の計算式を求める。回転を考慮しない場合は以下のとおり
            // movePeriod' = movePeriod * scaleX + offX     [[ movePeriod' = (movePeriod - tmpBoundsOnModel.movePeriod)*scale + layoutBoundsOnTex01.movePeriod ]]
            var scaleX = layoutBoundsOnTex01.Width / TmpBoundsOnModel.Width;
            var scaleY = layoutBoundsOnTex01.Height / TmpBoundsOnModel.Height;

            // マスク生成時に使う行列を求める
            CreateMatrixForMask(false, layoutBoundsOnTex01, scaleX, scaleY);

            clipContext.MatrixForMask.SetMatrix(TmpMatrixForMask);
            clipContext.MatrixForDraw.SetMatrix(TmpMatrixForDraw);

            // 実際の描画を行う
            int clipDrawCount = clipContext.ClippingIdCount;
            for (int i = 0; i < clipDrawCount; i++)
            {
                int clipDrawIndex = clipContext.ClippingIdList[i];

                // 頂点情報が更新されておらず、信頼性がない場合は描画をパスする
                if (!model.GetDrawableDynamicFlagVertexPositionsDidChange(clipDrawIndex))
                {
                    continue;
                }

                renderer.IsCulling = model.GetDrawableCulling(clipDrawIndex);

                // レンダーパス開始時にマスクはクリアされているのでクリアする必要なし
                // 今回専用の変換を適用して描く
                // チャンネルも切り替える必要がある(A,R,G,B)
                renderer.ClippingContextBufferForMask = clipContext;
                renderer.DrawMeshVulkan(model, clipDrawIndex, commandBuffer, updateCommandBuffer);
            }
        }
        // --- 後処理 ---
        buffer.EndDraw(commandBuffer);
        renderer.ClippingContextBufferForMask = null;
    }
}
