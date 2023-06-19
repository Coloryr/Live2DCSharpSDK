using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Type;
using System.Numerics;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public class CubismClippingManager_OpenGLES2
{
    private readonly OpenGLApi GL;

    /// <summary>
    /// オフスクリーンフレームのアドレス
    /// </summary>
    internal CubismOffscreenFrame_OpenGLES2? _currentOffscreenFrame;
    /// <summary>
    /// マスクのクリアフラグの配列
    /// </summary>
    internal readonly List<bool> _clearedFrameBufferFlags = new();

    internal readonly List<CubismTextureColor> _channelColors = new();
    /// <summary>
    /// マスク用クリッピングコンテキストのリスト
    /// </summary>
    internal readonly List<CubismClippingContext> _clippingContextListForMask = new();
    /// <summary>
    /// 描画用クリッピングコンテキストのリスト
    /// </summary>
    internal readonly List<CubismClippingContext?> _clippingContextListForDraw = new();
    /// <summary>
    /// クリッピングマスクのバッファサイズ（初期値:256）
    /// </summary>
    internal Vector2 _clippingMaskBufferSize;
    /// <summary>
    /// 生成するレンダーテクスチャの枚数
    /// </summary>
    internal int _renderTextureCount;

    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    internal readonly CubismMatrix44 _tmpMatrix = new();
    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    internal readonly CubismMatrix44 _tmpMatrixForMask = new();
    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    internal readonly CubismMatrix44 _tmpMatrixForDraw = new();
    /// <summary>
    /// マスク配置計算用の矩形
    /// </summary>
    internal readonly RectF _tmpBoundsOnModel = new();

    internal CubismClippingManager_OpenGLES2(OpenGLApi gl)
    {
        GL = gl;
        _clippingMaskBufferSize = new Vector2(256, 256);
        _channelColors.Add(new CubismTextureColor
        {
            R = 1.0f,
            G = 0.0f,
            B = 0.0f,
            A = 0.0f
        });
        _channelColors.Add(new CubismTextureColor
        {
            R = 0.0f,
            G = 1.0f,
            B = 0.0f,
            A = 0.0f
        });
        _channelColors.Add(new CubismTextureColor
        {
            R = 0.0f,
            G = 0.0f,
            B = 1.0f,
            A = 0.0f
        });
        _channelColors.Add(new CubismTextureColor
        {
            R = 0.0f,
            G = 0.0f,
            B = 0.0f,
            A = 1.0f
        });
    }

    /// <summary>
    /// カラーチャンネル(RGBA)のフラグを取得する
    /// </summary>
    /// <param name="channelNo">カラーチャンネル(RGBA)の番号(0:R , 1:G , 2:B, 3:A)</param>
    /// <returns></returns>
    internal CubismTextureColor GetChannelFlagAsColor(int channelNo)
    {
        return _channelColors[channelNo];
    }

    /// <summary>
    /// マスクされる描画オブジェクト群全体を囲む矩形(モデル座標系)を計算する
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="clippingContext">クリッピングマスクのコンテキスト</param>
    internal unsafe void CalcClippedDrawTotalBounds(CubismModel model, CubismClippingContext clippingContext)
    {
        // 被クリッピングマスク（マスクされる描画オブジェクト）の全体の矩形
        float clippedDrawTotalMinX = float.MaxValue, clippedDrawTotalMinY = float.MaxValue;
        float clippedDrawTotalMaxX = -float.MaxValue, clippedDrawTotalMaxY = -float.MaxValue;

        // このマスクが実際に必要か判定する
        // このクリッピングを利用する「描画オブジェクト」がひとつでも使用可能であればマスクを生成する必要がある

        int clippedDrawCount = clippingContext._clippedDrawableIndexList.Count;
        for (int clippedDrawableIndex = 0; clippedDrawableIndex < clippedDrawCount; clippedDrawableIndex++)
        {
            // マスクを使用する描画オブジェクトの描画される矩形を求める
            int drawableIndex = clippingContext._clippedDrawableIndexList[clippedDrawableIndex];

            int drawableVertexCount = model.GetDrawableVertexCount(drawableIndex);
            var drawableVertexes = model.GetDrawableVertices(drawableIndex);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = -float.MaxValue, maxY = -float.MaxValue;

            int loop = drawableVertexCount * CubismFramework.VertexStep;
            for (int pi = CubismFramework.VertexOffset; pi < loop; pi += CubismFramework.VertexStep)
            {
                float x = drawableVertexes[pi];
                float y = drawableVertexes[pi + 1];
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            //
            if (minX == float.MaxValue) continue; //有効な点がひとつも取れなかったのでスキップする

            // 全体の矩形に反映
            if (minX < clippedDrawTotalMinX) clippedDrawTotalMinX = minX;
            if (minY < clippedDrawTotalMinY) clippedDrawTotalMinY = minY;
            if (maxX > clippedDrawTotalMaxX) clippedDrawTotalMaxX = maxX;
            if (maxY > clippedDrawTotalMaxY) clippedDrawTotalMaxY = maxY;
        }
        if (clippedDrawTotalMinX == float.MaxValue)
        {
            clippingContext._allClippedDrawRect.X = 0.0f;
            clippingContext._allClippedDrawRect.Y = 0.0f;
            clippingContext._allClippedDrawRect.Width = 0.0f;
            clippingContext._allClippedDrawRect.Height = 0.0f;
            clippingContext._isUsing = false;
        }
        else
        {
            clippingContext._isUsing = true;
            float w = clippedDrawTotalMaxX - clippedDrawTotalMinX;
            float h = clippedDrawTotalMaxY - clippedDrawTotalMinY;
            clippingContext._allClippedDrawRect.X = clippedDrawTotalMinX;
            clippingContext._allClippedDrawRect.Y = clippedDrawTotalMinY;
            clippingContext._allClippedDrawRect.Width = w;
            clippingContext._allClippedDrawRect.Height = h;
        }
    }

    /// <summary>
    /// マネージャの初期化処理
    /// クリッピングマスクを使う描画オブジェクトの登録を行う
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="drawableCount">描画オブジェクトの数</param>
    /// <param name="drawableMasks">描画オブジェクトをマスクする描画オブジェクトのインデックスのリスト</param>
    /// <param name="drawableMaskCounts">描画オブジェクトをマスクする描画オブジェクトの数</param>
    /// <param name="maskBufferCount">バッファの生成数</param>
    internal unsafe void Initialize(CubismModel model, int drawableCount, int** drawableMasks, int* drawableMaskCounts, int maskBufferCount)
    {
        _renderTextureCount = maskBufferCount;

        // レンダーテクスチャのクリアフラグの設定
        for (int i = 0; i < _renderTextureCount; ++i)
        {
            _clearedFrameBufferFlags.Add(false);
        }

        //クリッピングマスクを使う描画オブジェクトを全て登録する
        //クリッピングマスクは、通常数個程度に限定して使うものとする
        for (int i = 0; i < drawableCount; i++)
        {
            if (drawableMaskCounts[i] <= 0)
            {
                //クリッピングマスクが使用されていないアートメッシュ（多くの場合使用しない）
                _clippingContextListForDraw.Add(null);
                continue;
            }

            // 既にあるClipContextと同じかチェックする
            var cc = FindSameClip(drawableMasks[i], drawableMaskCounts[i]);
            if (cc == null)
            {
                // 同一のマスクが存在していない場合は生成する
                cc = new CubismClippingContext(this, drawableMasks[i], drawableMaskCounts[i]);
                _clippingContextListForMask.Add(cc);
            }

            cc.AddClippedDrawable(i);

            _clippingContextListForDraw.Add(cc);
        }
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
            CubismClippingContext cc = _clippingContextListForMask[clipIndex];

            // このクリップを利用する描画オブジェクト群全体を囲む矩形を計算
            CalcClippedDrawTotalBounds(model, cc);

            if (cc._isUsing)
            {
                usingClipCount++; //使用中としてカウント
            }
        }

        // マスク作成処理
        if (usingClipCount > 0)
        {
            if (!renderer.IsUsingHighPrecisionMask())
            {
                // 生成したFrameBufferと同じサイズでビューポートを設定
                GL.glViewport(0, 0, (int)_clippingMaskBufferSize.X, (int)_clippingMaskBufferSize.Y);

                // 後の計算のためにインデックスの最初をセット
                _currentOffscreenFrame = renderer.GetMaskBuffer(0);
                // ----- マスク描画処理 -----
                _currentOffscreenFrame.BeginDraw(lastFBO);

                renderer.PreDraw(); // バッファをクリアする
            }

            // 各マスクのレイアウトを決定していく
            SetupLayoutBounds(renderer.IsUsingHighPrecisionMask() ? 0 : usingClipCount);

            // サイズがレンダーテクスチャの枚数と合わない場合は合わせる
            if (_clearedFrameBufferFlags.Count != _renderTextureCount)
            {
                _clearedFrameBufferFlags.Clear();

                for (int i = 0; i < _renderTextureCount; ++i)
                {
                    _clearedFrameBufferFlags.Add(false);
                }
            }
            else
            {
                // マスクのクリアフラグを毎フレーム開始時に初期化
                for (int i = 0; i < _renderTextureCount; ++i)
                {
                    _clearedFrameBufferFlags[i] = false;
                }
            }

            // 実際にマスクを生成する
            // 全てのマスクをどの様にレイアウトして描くかを決定し、ClipContext , ClippedDrawContext に記憶する
            for (int clipIndex = 0; clipIndex < _clippingContextListForMask.Count; clipIndex++)
            {
                // --- 実際に１つのマスクを描く ---
                CubismClippingContext clipContext = _clippingContextListForMask[clipIndex];
                RectF allClippedDrawRect = clipContext._allClippedDrawRect; //このマスクを使う、全ての描画オブジェクトの論理座標上の囲み矩形
                RectF layoutBoundsOnTex01 = clipContext._layoutBounds; //この中にマスクを収める
                float MARGIN = 0.05f;
                float scaleX = 0.0f;
                float scaleY = 0.0f;

                // clipContextに設定したオフスクリーンフレームをインデックスで取得
                CubismOffscreenFrame_OpenGLES2 clipContextOffscreenFrame = renderer.GetMaskBuffer(clipContext._bufferIndex);

                // 現在のオフスクリーンフレームがclipContextのものと異なる場合
                if (_currentOffscreenFrame != clipContextOffscreenFrame &&
                    !renderer.IsUsingHighPrecisionMask())
                {
                    _currentOffscreenFrame!.EndDraw();
                    _currentOffscreenFrame = clipContextOffscreenFrame;
                    // マスク用RenderTextureをactiveにセット
                    _currentOffscreenFrame.BeginDraw(lastFBO);

                    // バッファをクリアする。
                    renderer.PreDraw();
                }

                if (renderer.IsUsingHighPrecisionMask())
                {
                    float ppu = model.GetPixelsPerUnit();
                    float maskPixelWidth = clipContext.GetClippingManager()._clippingMaskBufferSize.X;
                    float maskPixelHeight = clipContext.GetClippingManager()._clippingMaskBufferSize.Y;
                    float physicalMaskWidth = layoutBoundsOnTex01.Width * maskPixelWidth;
                    float physicalMaskHeight = layoutBoundsOnTex01.Height * maskPixelHeight;

                    _tmpBoundsOnModel.SetRect(allClippedDrawRect);

                    if (_tmpBoundsOnModel.Width * ppu > physicalMaskWidth)
                    {
                        _tmpBoundsOnModel.Expand(allClippedDrawRect.Width * MARGIN, 0.0f);
                        scaleX = layoutBoundsOnTex01.Width / _tmpBoundsOnModel.Width;
                    }
                    else
                    {
                        scaleX = ppu / physicalMaskWidth;
                    }

                    if (_tmpBoundsOnModel.Height * ppu > physicalMaskHeight)
                    {
                        _tmpBoundsOnModel.Expand(0.0f, allClippedDrawRect.Height * MARGIN);
                        scaleY = layoutBoundsOnTex01.Height / _tmpBoundsOnModel.Height;
                    }
                    else
                    {
                        scaleY = ppu / physicalMaskHeight;
                    }
                }
                else
                {
                    // モデル座標上の矩形を、適宜マージンを付けて使う
                    _tmpBoundsOnModel.SetRect(allClippedDrawRect);
                    _tmpBoundsOnModel.Expand(allClippedDrawRect.Width * MARGIN, allClippedDrawRect.Height * MARGIN);
                    //########## 本来は割り当てられた領域の全体を使わず必要最低限のサイズがよい
                    // シェーダ用の計算式を求める。回転を考慮しない場合は以下のとおり
                    // movePeriod' = movePeriod * scaleX + offX     [[ movePeriod' = (movePeriod - tmpBoundsOnModel.movePeriod)*scale + layoutBoundsOnTex01.movePeriod ]]
                    scaleX = layoutBoundsOnTex01.Width / _tmpBoundsOnModel.Width;
                    scaleY = layoutBoundsOnTex01.Height / _tmpBoundsOnModel.Height;
                }

                // マスク生成時に使う行列を求める
                {
                    // シェーダに渡す行列を求める <<<<<<<<<<<<<<<<<<<<<<<< 要最適化（逆順に計算すればシンプルにできる）
                    _tmpMatrix.LoadIdentity();
                    {
                        // Layout0..1 を -1..1に変換
                        _tmpMatrix.TranslateRelative(-1.0f, -1.0f);
                        _tmpMatrix.ScaleRelative(2.0f, 2.0f);
                    }
                    {
                        // view to Layout0..1
                        _tmpMatrix.TranslateRelative(layoutBoundsOnTex01.X, layoutBoundsOnTex01.Y); //new = [translate]
                        _tmpMatrix.ScaleRelative(scaleX, scaleY); //new = [translate][scale]
                        _tmpMatrix.TranslateRelative(-_tmpBoundsOnModel.X, -_tmpBoundsOnModel.Y); //new = [translate][scale][translate]
                    }
                    // tmpMatrixForMask が計算結果
                    _tmpMatrixForMask.SetMatrix(_tmpMatrix.Tr);
                }

                //--------- draw時の mask 参照用行列を計算
                {
                    // シェーダに渡す行列を求める <<<<<<<<<<<<<<<<<<<<<<<< 要最適化（逆順に計算すればシンプルにできる）
                    _tmpMatrix.LoadIdentity();
                    {
                        _tmpMatrix.TranslateRelative(layoutBoundsOnTex01.X, layoutBoundsOnTex01.Y); //new = [translate]
                        _tmpMatrix.ScaleRelative(scaleX, scaleY); //new = [translate][scale]
                        _tmpMatrix.TranslateRelative(-_tmpBoundsOnModel.X, -_tmpBoundsOnModel.Y); //new = [translate][scale][translate]
                    }

                    _tmpMatrixForDraw.SetMatrix(_tmpMatrix.Tr);
                }

                clipContext._matrixForMask.SetMatrix(_tmpMatrixForMask.Tr);

                clipContext._matrixForDraw.SetMatrix(_tmpMatrixForDraw.Tr);

                if (!renderer.IsUsingHighPrecisionMask())
                {
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
                        if (!_clearedFrameBufferFlags[clipContext._bufferIndex])
                        {
                            // マスクをクリアする
                            // 1が無効（描かれない）領域、0が有効（描かれる）領域。（シェーダーCd*Csで0に近い値をかけてマスクを作る。1をかけると何も起こらない）
                            GL.glClearColor(1.0f, 1.0f, 1.0f, 1.0f);
                            GL.glClear(GL.GL_COLOR_BUFFER_BIT);
                            _clearedFrameBufferFlags[clipContext._bufferIndex] = true;
                        }

                        // 今回専用の変換を適用して描く
                        // チャンネルも切り替える必要がある(A,R,G,B)
                        renderer.ClippingContextBufferForMask = clipContext;

                        renderer.DrawMeshOpenGL(
                            model.GetDrawableTextureIndex(clipDrawIndex),
                            model.GetDrawableVertexIndexCount(clipDrawIndex),
                            model.GetDrawableVertexCount(clipDrawIndex),
                            model.GetDrawableVertexIndices(clipDrawIndex),
                            model.GetDrawableVertices(clipDrawIndex),
                            (float*)model.GetDrawableVertexUvs(clipDrawIndex),
                            model.GetMultiplyColor(clipDrawIndex),
                            model.GetScreenColor(clipDrawIndex),
                            model.GetDrawableOpacity(clipDrawIndex),
                            CubismBlendMode.Normal,   //クリッピングは通常描画を強制
                            false   // マスク生成時はクリッピングの反転使用は全く関係がない
                        );
                    }
                }
            }

            if (!renderer.IsUsingHighPrecisionMask())
            {
                // --- 後処理 ---
                _currentOffscreenFrame?.EndDraw();
                renderer.ClippingContextBufferForMask = null;
                GL.glViewport(lastViewport[0], lastViewport[1], lastViewport[2], lastViewport[3]);
            }
        }
    }

    /// <summary>
    /// 既にマスクを作っているかを確認。
    /// 作っているようであれば該当するクリッピングマスクのインスタンスを返す。
    /// 作っていなければNULLを返す
    /// </summary>
    /// <param name="drawableMasks">描画オブジェクトをマスクする描画オブジェクトのリスト</param>
    /// <param name="drawableMaskCounts">描画オブジェクトをマスクする描画オブジェクトの数</param>
    /// <returns>該当するクリッピングマスクが存在すればインスタンスを返し、なければNULLを返す。</returns>
    internal unsafe CubismClippingContext? FindSameClip(int* drawableMasks, int drawableMaskCounts)
    {
        // 作成済みClippingContextと一致するか確認
        for (int i = 0; i < _clippingContextListForMask.Count; i++)
        {
            CubismClippingContext cc = _clippingContextListForMask[i];
            int count = cc._clippingIdCount;
            if (count != drawableMaskCounts) continue; //個数が違う場合は別物
            int samecount = 0;

            // 同じIDを持つか確認。配列の数が同じなので、一致した個数が同じなら同じ物を持つとする。
            for (int j = 0; j < count; j++)
            {
                int clipId = cc._clippingIdList[j];
                for (int k = 0; k < count; k++)
                {
                    if (drawableMasks[k] == clipId)
                    {
                        samecount++;
                        break;
                    }
                }
            }
            if (samecount == count)
            {
                return cc;
            }
        }
        return null; //見つからなかった
    }

    /// <summary>
    /// クリッピングコンテキストを配置するレイアウト。
    /// ひとつのレンダーテクスチャを極力いっぱいに使ってマスクをレイアウトする。
    ///  マスクグループの数が4以下ならRGBA各チャンネルに１つずつマスクを配置し、5以上6以下ならRGBAを2,2,1,1と配置する。
    /// </summary>
    /// <param name="usingClipCount">配置するクリッピングコンテキストの数</param>
    internal void SetupLayoutBounds(int usingClipCount)
    {
        int useClippingMaskMaxCount = _renderTextureCount <= 1
            ? CubismRenderer_OpenGLES2.ClippingMaskMaxCountOnDefault
            : CubismRenderer_OpenGLES2.ClippingMaskMaxCountOnMultiRenderTexture * _renderTextureCount;

        if (usingClipCount <= 0 || usingClipCount > useClippingMaskMaxCount)
        {
            if (usingClipCount > useClippingMaskMaxCount)
            {
                // マスクの制限数の警告を出す
                int count = usingClipCount - useClippingMaskMaxCount;
                CubismLog.CubismLogError($"[Live2D SDK]not supported mask count : {count}\n[Details] render texture count : {_renderTextureCount}\n, mask count : {usingClipCount}");
            }

            // この場合は一つのマスクターゲットを毎回クリアして使用する
            for (int index = 0; index < _clippingContextListForMask.Count; index++)
            {
                CubismClippingContext cc = _clippingContextListForMask[index];
                cc._layoutChannelNo = 0; // どうせ毎回消すので固定で良い
                cc._layoutBounds.X = 0.0f;
                cc._layoutBounds.Y = 0.0f;
                cc._layoutBounds.Width = 1.0f;
                cc._layoutBounds.Height = 1.0f;
                cc._bufferIndex = 0;
            }
            return;
        }

        // レンダーテクスチャが1枚なら9分割する（最大36枚）
        int layoutCountMaxValue = _renderTextureCount <= 1 ? 9 : 8;

        // ひとつのRenderTextureを極力いっぱいに使ってマスクをレイアウトする
        // マスクグループの数が4以下ならRGBA各チャンネルに１つずつマスクを配置し、5以上6以下ならRGBAを2,2,1,1と配置する
        int countPerSheetDiv = usingClipCount / _renderTextureCount; // レンダーテクスチャ1枚あたり何枚割り当てるか
        int countPerSheetMod = usingClipCount % _renderTextureCount; // この番号のレンダーテクスチャまでに一つずつ配分する

        // RGBAを順番に使っていく。
        int div = countPerSheetDiv / CubismRenderer_OpenGLES2.ColorChannelCount; //１チャンネルに配置する基本のマスク個数
        int mod = countPerSheetDiv % CubismRenderer_OpenGLES2.ColorChannelCount; //余り、この番号のチャンネルまでに１つずつ配分する

        // RGBAそれぞれのチャンネルを用意していく(0:R , 1:G , 2:B, 3:A, )
        int curClipIndex = 0; //順番に設定していく

        for (int renderTextureNo = 0; renderTextureNo < _renderTextureCount; renderTextureNo++)
        {
            for (int channelNo = 0; channelNo < CubismRenderer_OpenGLES2.ColorChannelCount; channelNo++)
            {
                // このチャンネルにレイアウトする数
                int layoutCount = div + (channelNo < mod ? 1 : 0);

                // このレンダーテクスチャにまだ割り当てられていなければ追加する
                int checkChannelNo = mod + 1 >= CubismRenderer_OpenGLES2.ColorChannelCount ? 0 : mod + 1;
                if (layoutCount < layoutCountMaxValue && channelNo == checkChannelNo)
                {
                    layoutCount += renderTextureNo < countPerSheetMod ? 1 : 0;
                }

                // 分割方法を決定する
                if (layoutCount == 0)
                {
                    // 何もしない
                }
                else if (layoutCount == 1)
                {
                    //全てをそのまま使う
                    CubismClippingContext cc = _clippingContextListForMask[curClipIndex++];
                    cc._layoutChannelNo = channelNo;
                    cc._layoutBounds.X = 0.0f;
                    cc._layoutBounds.Y = 0.0f;
                    cc._layoutBounds.Width = 1.0f;
                    cc._layoutBounds.Height = 1.0f;
                    cc._bufferIndex = renderTextureNo;
                }
                else if (layoutCount == 2)
                {
                    for (int i = 0; i < layoutCount; i++)
                    {
                        int xpos = i % 2;

                        CubismClippingContext cc = _clippingContextListForMask[curClipIndex++];
                        cc._layoutChannelNo = channelNo;

                        cc._layoutBounds.X = xpos * 0.5f;
                        cc._layoutBounds.Y = 0.0f;
                        cc._layoutBounds.Width = 0.5f;
                        cc._layoutBounds.Height = 1.0f;
                        cc._bufferIndex = renderTextureNo;
                        //UVを2つに分解して使う
                    }
                }
                else if (layoutCount <= 4)
                {
                    //4分割して使う
                    for (int i = 0; i < layoutCount; i++)
                    {
                        int xpos = i % 2;
                        int ypos = i / 2;

                        CubismClippingContext cc = _clippingContextListForMask[curClipIndex++];
                        cc._layoutChannelNo = channelNo;

                        cc._layoutBounds.X = xpos * 0.5f;
                        cc._layoutBounds.Y = ypos * 0.5f;
                        cc._layoutBounds.Width = 0.5f;
                        cc._layoutBounds.Height = 0.5f;
                        cc._bufferIndex = renderTextureNo;
                    }
                }
                else if (layoutCount <= layoutCountMaxValue)
                {
                    //9分割して使う
                    for (int i = 0; i < layoutCount; i++)
                    {
                        int xpos = i % 3;
                        int ypos = i / 3;

                        CubismClippingContext cc = _clippingContextListForMask[curClipIndex++];
                        cc._layoutChannelNo = channelNo;

                        cc._layoutBounds.X = xpos / 3.0f;
                        cc._layoutBounds.Y = ypos / 3.0f;
                        cc._layoutBounds.Width = 1.0f / 3.0f;
                        cc._layoutBounds.Height = 1.0f / 3.0f;
                        cc._bufferIndex = renderTextureNo;
                    }
                }
                // マスクの制限枚数を超えた場合の処理
                else
                {
                    int count = usingClipCount - useClippingMaskMaxCount;

                    // 引き続き実行する場合、 SetupShaderProgramでオーバーアクセスが発生するので仕方なく適当に入れておく
                    // もちろん描画結果はろくなことにならない
                    for (int i = 0; i < layoutCount; i++)
                    {
                        CubismClippingContext cc = _clippingContextListForMask[curClipIndex++];
                        cc._layoutChannelNo = 0;
                        cc._layoutBounds.X = 0.0f;
                        cc._layoutBounds.Y = 0.0f;
                        cc._layoutBounds.Width = 1.0f;
                        cc._layoutBounds.Height = 1.0f;
                        cc._bufferIndex = 0;
                    }

                    // 開発モードの場合は停止させる
                    throw new Exception($"[Live2D Core]not supported mask count : {count}\n[Details] render texture count: {_renderTextureCount}\n, mask count : {usingClipCount}");
                }
            }
        }
    }

    /// <summary>
    /// 画面描画に使用するクリッピングマスクのリストを取得する
    /// </summary>
    /// <returns>画面描画に使用するクリッピングマスクのリスト</returns>
    internal List<CubismClippingContext?> GetClippingContextListForDraw()
    {
        return _clippingContextListForDraw;
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを設定する
    /// </summary>
    /// <param name="width">クリッピングマスクバッファのサイズ</param>
    /// <param name="height">クリッピングマスクバッファのサイズ</param>
    internal void SetClippingMaskBufferSize(float width, float height)
    {
        _clippingMaskBufferSize = new Vector2(width, height);
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを取得する
    /// </summary>
    /// <returns>クリッピングマスクバッファのサイズ</returns>
    internal Vector2 GetClippingMaskBufferSize()
    {
        return _clippingMaskBufferSize;
    }

    /// <summary>
    /// このバッファのレンダーテクスチャの枚数を取得する。
    /// </summary>
    /// <returns>このバッファのレンダーテクスチャの枚数</returns>
    internal int GetRenderTextureCount()
    {
        return _renderTextureCount;
    }
}
