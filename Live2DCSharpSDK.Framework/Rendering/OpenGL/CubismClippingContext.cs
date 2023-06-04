using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Rendering.OpenGL;

public class CubismClippingContext
{
    /// <summary>
    /// 現在の描画状態でマスクの準備が必要ならtrue
    /// </summary>
    public bool _isUsing;
    /// <summary>
    /// クリッピングマスクのIDリスト
    /// </summary>
    public  int _clippingIdList;
    /// <summary>
    /// クリッピングマスクの数
    /// </summary>
    public int _clippingIdCount;
    /// <summary>
    /// RGBAのいずれのチャンネルにこのクリップを配置するか(0:R , 1:G , 2:B , 3:A)
    /// </summary>
    public int _layoutChannelNo;
    /// <summary>
    /// マスク用チャンネルのどの領域にマスクを入れるか(View座標-1..1, UVは0..1に直す)
    /// </summary>
    public csmRectF _layoutBounds;
    /// <summary>
    /// このクリッピングで、クリッピングされる全ての描画オブジェクトの囲み矩形（毎回更新）
    /// </summary>
    public csmRectF _allClippedDrawRect;
    /// <summary>
    /// マスクの位置計算結果を保持する行列
    /// </summary>
    public CubismMatrix44 _matrixForMask;
    /// <summary>
    /// 描画オブジェクトの位置計算結果を保持する行列
    /// </summary>
    public CubismMatrix44 _matrixForDraw;
    /// <summary>
    /// このマスクにクリップされる描画オブジェクトのリスト
    /// </summary>
    public List<int> _clippedDrawableIndexList;
    /// <summary>
    /// このマスクが割り当てられるレンダーテクスチャ（フレームバッファ）やカラーバッファのインデックス
    /// </summary>
    public int _bufferIndex;                          

    /// <summary>
    /// このマスクを管理しているマネージャのインスタンス
    /// </summary>
    internal CubismClippingManager_OpenGLES2 _owner;        

    /// <summary>
    /// 引数付きコンストラクタ
    /// </summary>
    internal CubismClippingContext(CubismClippingManager_OpenGLES2 manager, int clippingDrawableIndices, int clipCount)
    { 
        
    }

    /// <summary>
    /// このマスクにクリップされる描画オブジェクトを追加する
    /// </summary>
    /// <param name="drawableIndex">クリッピング対象に追加する描画オブジェクトのインデックス</param>
    internal void AddClippedDrawable(int drawableIndex)
    { 
    
    }

    /// <summary>
    /// このマスクを管理するマネージャのインスタンスを取得する。
    /// </summary>
    /// <returns>クリッピングマネージャのインスタンス</returns>
    internal CubismClippingManager_OpenGLES2 GetClippingManager()
    { 
    
    }
}
