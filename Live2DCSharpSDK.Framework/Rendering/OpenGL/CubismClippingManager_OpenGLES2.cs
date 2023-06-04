using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public class CubismClippingManager_OpenGLES2
{
    private OpenGLApi GL;

    /// <summary>
    /// オフスクリーンフレームのアドレス
    /// </summary>
    internal CubismOffscreenFrame_OpenGLES2 _currentOffscreenFrame;
    /// <summary>
    /// マスクのクリアフラグの配列
    /// </summary>
    internal List<csmBool> _clearedFrameBufferFlags;

    internal List<CubismTextureColor> _channelColors;
    /// <summary>
    /// マスク用クリッピングコンテキストのリスト
    /// </summary>
    internal List<CubismClippingContext> _clippingContextListForMask;
    /// <summary>
    /// 描画用クリッピングコンテキストのリスト
    /// </summary>
    internal List<CubismClippingContext> _clippingContextListForDraw;
    /// <summary>
    /// クリッピングマスクのバッファサイズ（初期値:256）
    /// </summary>
    internal Vector2 _clippingMaskBufferSize;
    /// <summary>
    /// 生成するレンダーテクスチャの枚数
    /// </summary>
    internal csmInt32 _renderTextureCount;

    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    internal CubismMatrix44 _tmpMatrix;
    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    internal CubismMatrix44 _tmpMatrixForMask;
    /// <summary>
    /// マスク計算用の行列
    /// </summary>
    internal CubismMatrix44 _tmpMatrixForDraw;
    /// <summary>
    /// マスク配置計算用の矩形
    /// </summary>
    internal csmRectF _tmpBoundsOnModel;       

    internal CubismClippingManager_OpenGLES2()
    {

    }

    /// <summary>
    /// カラーチャンネル(RGBA)のフラグを取得する
    /// </summary>
    /// <param name="channelNo">カラーチャンネル(RGBA)の番号(0:R , 1:G , 2:B, 3:A)</param>
    /// <returns></returns>
    internal CubismTextureColor GetChannelFlagAsColor(csmInt32 channelNo)
    {
    }

    /// <summary>
    /// マスクされる描画オブジェクト群全体を囲む矩形(モデル座標系)を計算する
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="clippingContext">クリッピングマスクのコンテキスト</param>
    internal void CalcClippedDrawTotalBounds(CubismModel model, CubismClippingContext clippingContext)
    {

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
    internal void Initialize(CubismModel model, csmInt32 drawableCount, csmInt32** drawableMasks, csmInt32* drawableMaskCounts, csmInt32 maskBufferCount)
    { 
        
    }

    /// <summary>
    /// クリッピングコンテキストを作成する。モデル描画時に実行する。
    /// </summary>
    /// <param name="model">モデルのインスタンス</param>
    /// <param name="renderer">レンダラのインスタンス</param>
    /// <param name="lastFBO">フレームバッファ</param>
    /// <param name="lastViewport">ビューポート</param>
    internal void SetupClippingContext(CubismModel model, CubismRenderer_OpenGLES2 renderer, int lastFBO, int[] lastViewport)
    { 
        
    }

    /// <summary>
    /// 既にマスクを作っているかを確認。
    /// 作っているようであれば該当するクリッピングマスクのインスタンスを返す。
    /// 作っていなければNULLを返す
    /// </summary>
    /// <param name="drawableMasks">描画オブジェクトをマスクする描画オブジェクトのリスト</param>
    /// <param name="drawableMaskCounts">描画オブジェクトをマスクする描画オブジェクトの数</param>
    /// <returns>該当するクリッピングマスクが存在すればインスタンスを返し、なければNULLを返す。</returns>
    internal CubismClippingContext FindSameClip(csmInt32 drawableMasks, csmInt32 drawableMaskCounts)
    { 
    
    }

    /// <summary>
    /// クリッピングコンテキストを配置するレイアウト。
    /// ひとつのレンダーテクスチャを極力いっぱいに使ってマスクをレイアウトする。
    ///  マスクグループの数が4以下ならRGBA各チャンネルに１つずつマスクを配置し、5以上6以下ならRGBAを2,2,1,1と配置する。
    /// </summary>
    /// <param name="usingClipCount">配置するクリッピングコンテキストの数</param>
    internal void SetupLayoutBounds(csmInt32 usingClipCount)
    { 
    
    }

    /// <summary>
    /// 画面描画に使用するクリッピングマスクのリストを取得する
    /// </summary>
    /// <returns>画面描画に使用するクリッピングマスクのリスト</returns>
    internal List<CubismClippingContext> GetClippingContextListForDraw()
    { 
        
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを設定する
    /// </summary>
    /// <param name="width">クリッピングマスクバッファのサイズ</param>
    /// <param name="height">クリッピングマスクバッファのサイズ</param>
    internal void SetClippingMaskBufferSize(csmFloat32 width, csmFloat32 height)
    { 
        
    }

    /// <summary>
    /// クリッピングマスクバッファのサイズを取得する
    /// </summary>
    /// <returns>クリッピングマスクバッファのサイズ</returns>
    internal CubismVector2 GetClippingMaskBufferSize()
    { 
    
    }

    /// <summary>
    /// このバッファのレンダーテクスチャの枚数を取得する。
    /// </summary>
    /// <returns>このバッファのレンダーテクスチャの枚数</returns>
    internal csmInt32 GetRenderTextureCount()
    { 
    
    }
}
