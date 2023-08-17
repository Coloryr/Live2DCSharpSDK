using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Rendering.OpenGL;
using Live2DCSharpSDK.Framework.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Rendering;

public class CubismClippingManager
{
    /// <summary>
    /// 実験時に1チャンネルの場合は1、RGBだけの場合は3、アルファも含める場合は4
    /// </summary>
    public const int ColorChannelCount = 4;
    /// <summary>
    /// 通常のフレームバッファ1枚あたりのマスク最大数
    /// </summary>
    public const int ClippingMaskMaxCountOnDefault = 36;
    /// <summary>
    /// フレームバッファが2枚以上ある場合のフレームバッファ1枚あたりのマスク最大数
    /// </summary>
    public const int ClippingMaskMaxCountOnMultiRenderTexture = 32;

    /// <summary>
    /// オフスクリーンサーフェイスのアドレス
    /// </summary>
    protected CubismOffscreenSurface_OpenGLES2 _currentMaskBuffer;
    /// <summary>
    /// マスクのクリアフラグの配列
    /// </summary>
    protected List<bool> _clearedMaskBufferFlags = new();

    protected List<CubismTextureColor> _channelColors = new();
    /// <summary>
    /// マスク用クリッピングコンテキストのリスト
    /// </summary>
    protected List<CubismClippingContext> _clippingContextListForMask = new();
    /// <summary>
    /// 描画用クリッピングコンテキストのリスト
    /// </summary>
    public List<CubismClippingContext> ClippingContextListForDraw { get; init; } = new();
    /// <summary>
    /// クリッピングマスクのバッファサイズ（初期値:256）
    /// </summary>
    public Vector2 ClippingMaskBufferSize { get; private set; }
    /// <summary>
    /// 生成するレンダーテクスチャの枚数
    /// </summary>
    public int RenderTextureCount { get; private set; }

    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    protected CubismMatrix44 _tmpMatrix = new();
    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    protected CubismMatrix44 _tmpMatrixForMask = new();
    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    protected CubismMatrix44 _tmpMatrixForDraw = new();
    /// <summary>
    /// マスク配置計算用の矩形
    /// </summary>
    protected RectF _tmpBoundsOnModel = new();

    private RenderType _renderType;

    public CubismClippingManager(RenderType render)
    {
        _renderType = render;
        ClippingMaskBufferSize = new(256, 256);

        _channelColors.Add(new(1.0f, 0f, 0f, 0f));
        _channelColors.Add(new(0f, 1.0f, 0f, 0f));
        _channelColors.Add(new(0f, 0f, 1.0f, 0f));
        _channelColors.Add(new(0f, 0f, 0f, 1.0f));
    }

    public void Close()
    {
        ClippingContextListForDraw.Clear();
        _clippingContextListForMask.Clear();
        _channelColors.Clear();
        _clearedMaskBufferFlags.Clear();
    }

    /// <summary>
    /// マネージャの初期化処理
    /// クリッピングマスクを使う描画オブジェクトの登録を行う
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="maskBufferCount">バッファの生成数</param>
    public unsafe void Initialize(CubismModel model, int maskBufferCount)
    {
        RenderTextureCount = maskBufferCount;

        // レンダーテクスチャのクリアフラグの設定
        for (int i = 0; i < RenderTextureCount; ++i)
        {
            _clearedMaskBufferFlags.Add(false);
        }

        //クリッピングマスクを使う描画オブジェクトを全て登録する
        //クリッピングマスクは、通常数個程度に限定して使うものとする
        for (int i = 0; i < model.GetDrawableCount(); i++)
        {
            if (model.GetDrawableMaskCounts()[i] <= 0)
            {
                //クリッピングマスクが使用されていないアートメッシュ（多くの場合使用しない）
                ClippingContextListForDraw.Add(null);
                continue;
            }

            // 既にあるClipContextと同じかチェックする
            var cc = FindSameClip(model.GetDrawableMasks()[i], model.GetDrawableMaskCounts()[i]);
            if (cc == null)
            {
                // 同一のマスクが存在していない場合は生成する
                if (_renderType == RenderType.OpenGL)
                {
                    cc = new CubismClippingContext_OpenGLES2(this, model, model.GetDrawableMasks()[i], model.GetDrawableMaskCounts()[i]);
                }
                _clippingContextListForMask.Add(cc);
            }

            cc.AddClippedDrawable(i);

            ClippingContextListForDraw.Add(cc);
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
    private unsafe CubismClippingContext? FindSameClip(int* drawableMasks, int drawableMaskCounts)
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
    /// 高精細マスク処理用の行列を計算する
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="isRightHanded">処理が右手系であるか</param>
    private void SetupMatrixForHighPrecision(CubismModel model, bool isRightHanded)
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

        if (usingClipCount <= 0)
        {
            return;
        }
        // マスク行列作成処理
        SetupLayoutBounds(0);

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
            CubismClippingContext clipContext = _clippingContextListForMask[clipIndex];
            RectF allClippedDrawRect = clipContext._allClippedDrawRect; //このマスクを使う、全ての描画オブジェクトの論理座標上の囲み矩形
            RectF layoutBoundsOnTex01 = clipContext._layoutBounds; //この中にマスクを収める
            float MARGIN = 0.05f;
            float scaleX = 0.0f;
            float scaleY = 0.0f;
            float ppu = model.GetPixelsPerUnit();
            float maskPixelWidth = clipContext.Manager.ClippingMaskBufferSize.X;
            float maskPixelHeight = clipContext.Manager.ClippingMaskBufferSize.Y;
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


            // マスク生成時に使う行列を求める
            createMatrixForMask(isRightHanded, layoutBoundsOnTex01, scaleX, scaleY);

            clipContext._matrixForMask.SetMatrix(_tmpMatrixForMask.Tr);
            clipContext._matrixForDraw.SetMatrix(_tmpMatrixForDraw.Tr);
        }
    }

    /// <summary>
    /// マスク作成・描画用の行列を作成する。
    /// </summary>
    /// <param name="isRightHanded">座標を右手系として扱うかを指定</param>
    /// <param name="layoutBoundsOnTex01">マスクを収める領域</param>
    /// <param name="scaleX">描画オブジェクトの伸縮率</param>
    /// <param name="scaleY">描画オブジェクトの伸縮率</param>
    protected void createMatrixForMask(bool isRightHanded, RectF layoutBoundsOnTex01, float scaleX, float scaleY)
    {
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

        _tmpMatrix.LoadIdentity();
        {
            _tmpMatrix.TranslateRelative(layoutBoundsOnTex01.X, layoutBoundsOnTex01.Y * ((isRightHanded) ? -1.0f : 1.0f)); //new = [translate]
            _tmpMatrix.ScaleRelative(scaleX, scaleY * ((isRightHanded) ? -1.0f : 1.0f)); //new = [translate][scale]
            _tmpMatrix.TranslateRelative(-_tmpBoundsOnModel.X, -_tmpBoundsOnModel.Y); //new = [translate][scale][translate]
        }

        _tmpMatrixForDraw.SetMatrix(_tmpMatrix.Tr);
    }

    /// <summary>
    /// クリッピングコンテキストを配置するレイアウト。
    /// ひとつのレンダーテクスチャを極力いっぱいに使ってマスクをレイアウトする。
    /// マスクグループの数が4以下ならRGBA各チャンネルに１つずつマスクを配置し、5以上6以下ならRGBAを2,2,1,1と配置する。
    /// </summary>
    /// <param name="usingClipCount">配置するクリッピングコンテキストの数</param>
    protected void SetupLayoutBounds(int usingClipCount)
    {
        int useClippingMaskMaxCount = RenderTextureCount <= 1
        ? ClippingMaskMaxCountOnDefault
        : ClippingMaskMaxCountOnMultiRenderTexture * RenderTextureCount;

        if (usingClipCount <= 0 || usingClipCount > useClippingMaskMaxCount)
        {
            if (usingClipCount > useClippingMaskMaxCount)
            {
                // マスクの制限数の警告を出す
                int count = usingClipCount - useClippingMaskMaxCount;
                CubismLog.Error("not supported mask count : %d\n[Details] render texture count: %d\n, mask count : %d"
                    , count, RenderTextureCount, usingClipCount);
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
        int layoutCountMaxValue = RenderTextureCount <= 1 ? 9 : 8;

        // ひとつのRenderTextureを極力いっぱいに使ってマスクをレイアウトする
        // マスクグループの数が4以下ならRGBA各チャンネルに１つずつマスクを配置し、5以上6以下ならRGBAを2,2,1,1と配置する
        int countPerSheetDiv = usingClipCount / RenderTextureCount; // レンダーテクスチャ1枚あたり何枚割り当てるか
        int countPerSheetMod = usingClipCount % RenderTextureCount; // この番号のレンダーテクスチャまでに一つずつ配分する

        // RGBAを順番に使っていく。
        int div = countPerSheetDiv / ColorChannelCount; //１チャンネルに配置する基本のマスク個数
        int mod = countPerSheetDiv % ColorChannelCount; //余り、この番号のチャンネルまでに１つずつ配分する

        // RGBAそれぞれのチャンネルを用意していく(0:R , 1:G , 2:B, 3:A, )
        int curClipIndex = 0; //順番に設定していく

        for (int renderTextureNo = 0; renderTextureNo < RenderTextureCount; renderTextureNo++)
        {
            for (int channelNo = 0; channelNo < ColorChannelCount; channelNo++)
            {
                // このチャンネルにレイアウトする数
                int layoutCount = div + (channelNo < mod ? 1 : 0);

                // このレンダーテクスチャにまだ割り当てられていなければ追加する
                int checkChannelNo = mod + 1 >= ColorChannelCount ? 0 : mod + 1;
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
                    CubismLog.Error("not supported mask count : %d\n[Details] render texture count: %d\n, mask count : %d"
                        , count, RenderTextureCount, usingClipCount);

                    // 開発モードの場合は停止させる
                    //CSM_ASSERT(0);

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
                }
            }
        }
    }

    /// <summary>
    /// マスクされる描画オブジェクト群全体を囲む矩形(モデル座標系)を計算する
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="clippingContext">クリッピングマスクのコンテキスト</param>
    protected unsafe void CalcClippedDrawTotalBounds(CubismModel model, CubismClippingContext clippingContext)
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
            float* drawableVertexes = (float*)(model.GetDrawableVertices(drawableIndex));

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
    /// カラーチャンネル(RGBA)のフラグを取得する
    /// </summary>
    /// <param name="channelNo">カラーチャンネル(RGBA)の番号(0:R , 1:G , 2:B, 3:A)</param>
    /// <returns></returns>
    public CubismTextureColor GetChannelFlagAsColor(int channelNo)
    {
        return _channelColors[channelNo];
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを設定する
    /// </summary>
    /// <param name="width">クリッピングマスクバッファのサイズ</param>
    /// <param name="height">クリッピングマスクバッファのサイズ</param>
    public void SetClippingMaskBufferSize(float width, float height)
    {
        ClippingMaskBufferSize = new Vector2(width, height);
    }
}
