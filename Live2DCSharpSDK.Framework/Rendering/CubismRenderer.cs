using Live2DCSharpSDK.Framework.Math;
using Live2DCSharpSDK.Framework.Model;
using Live2DCSharpSDK.Framework.Rendering.OpenGL;

namespace Live2DCSharpSDK.Framework.Rendering;

/// <summary>
/// モデル描画を処理するレンダラ
/// サブクラスに環境依存の描画命令を記述する
/// </summary>
public abstract class CubismRenderer : IDisposable
{
    /// <summary>
    /// テクスチャの異方性フィルタリングのパラメータ
    /// </summary>
    public float Anisotropy { get; set; }
    /// <summary>
    /// レンダリング対象のモデル
    /// </summary>
    public CubismModel Model { get; private set; }

    /// <summary>
    /// モデル自体のカラー(RGBA)
    /// </summary>
    public CubismTextureColor ModelColor = new();

    public CubismTextureColor ClearColor = new(0, 0, 0, 0);

    /// <summary>
    /// Model-View-Projection 行列
    /// </summary>
    private readonly CubismMatrix44 _mvpMatrix4x4 = new();
    /// <summary>
    /// カリングが有効ならtrue
    /// </summary>
    private bool _isCulling;
    /// <summary>
    /// 乗算済みαならtrue
    /// </summary>
    private bool _isPremultipliedAlpha;
    /// <summary>
    /// falseの場合、マスクを纏めて描画する trueの場合、マスクはパーツ描画ごとに書き直す
    /// </summary>
    private bool _useHighPrecisionMask;

    /// <summary>
    /// レンダラのインスタンスを生成して取得する
    /// </summary>
    public CubismRenderer(CubismModel model)
    {
        _mvpMatrix4x4.LoadIdentity();
        Model = model ?? throw new Exception("model is null");
    }

    /// <summary>
    /// レンダラのインスタンスを解放する
    /// </summary>
    public abstract void Dispose();

    /// <summary>
    /// モデルを描画する
    /// </summary>
    public void DrawModel()
    {
        /**
         * DoDrawModelの描画前と描画後に以下の関数を呼んでください。
         * ・SaveProfile();
         * ・RestoreProfile();
         * これはレンダラの描画設定を保存・復帰させることで、
         * モデル描画直前の状態に戻すための処理です。
         */

        SaveProfile();

        DoDrawModel();

        RestoreProfile();
    }

    /// <summary>
    /// Model-View-Projection 行列をセットする
    /// 配列は複製されるので元の配列は外で破棄して良い
    /// </summary>
    /// <param name="matrix4x4">Model-View-Projection 行列</param>
    public void SetMvpMatrix(CubismMatrix44 matrix4x4)
    {
        _mvpMatrix4x4.SetMatrix(matrix4x4.Tr);
    }

    /// <summary>
    /// Model-View-Projection 行列を取得する
    /// </summary>
    /// <returns>Model-View-Projection 行列</returns>
    public CubismMatrix44 GetMvpMatrix()
    {
        return _mvpMatrix4x4;
    }

    /// <summary>
    /// 透明度を考慮したモデルの色を計算する。
    /// </summary>
    /// <param name="opacity">透明度</param>
    /// <returns>RGBAのカラー情報</returns>
    public CubismTextureColor GetModelColorWithOpacity(float opacity)
    {
        CubismTextureColor modelColorRGBA = new(ModelColor);
        modelColorRGBA.A *= opacity;
        if (IsPremultipliedAlpha())
        {
            modelColorRGBA.R *= modelColorRGBA.A;
            modelColorRGBA.G *= modelColorRGBA.A;
            modelColorRGBA.B *= modelColorRGBA.A;
        }
        return modelColorRGBA;
    }

    /// <summary>
    /// モデルの色をセットする。
    /// 各色0.0f～1.0fの間で指定する(1.0fが標準の状態）。
    /// </summary>
    /// <param name="red">赤チャンネルの値</param>
    /// <param name="green">緑チャンネルの値</param>
    /// <param name="blue">青チャンネルの値</param>
    /// <param name="alpha">αチャンネルの値</param>
    public void SetModelColor(float red, float green, float blue, float alpha)
    {
        if (red < 0.0f) red = 0.0f;
        else if (red > 1.0f) red = 1.0f;

        if (green < 0.0f) green = 0.0f;
        else if (green > 1.0f) green = 1.0f;

        if (blue < 0.0f) blue = 0.0f;
        else if (blue > 1.0f) blue = 1.0f;

        if (alpha < 0.0f) alpha = 0.0f;
        else if (alpha > 1.0f) alpha = 1.0f;

        ModelColor.R = red;
        ModelColor.G = green;
        ModelColor.B = blue;
        ModelColor.A = alpha;
    }

    /// <summary>
    /// 乗算済みαの有効・無効をセットする。
    /// 有効にするならtrue, 無効にするならfalseをセットする。
    /// </summary>
    /// <param name="enable"></param>
    public void IsPremultipliedAlpha(bool enable)
    {
        _isPremultipliedAlpha = enable;
    }

    /// <summary>
    /// 乗算済みαの有効・無効を取得する。
    /// </summary>
    /// <returns>true    ->  乗算済みα有効
    /// false   ->  乗算済みα無効</returns>
    public bool IsPremultipliedAlpha()
    {
        return _isPremultipliedAlpha;
    }

    /// <summary>
    /// カリング（片面描画）の有効・無効をセットする。
    /// 有効にするならtrue, 無効にするならfalseをセットする。
    /// </summary>
    public void IsCulling(bool culling)
    {
        _isCulling = culling;
    }

    /// <summary>
    /// カリング（片面描画）の有効・無効を取得する。
    /// </summary>
    /// <returns>true    ->  カリング有効
    /// false   ->  カリング無効</returns>
    public bool IsCulling()
    {
        return _isCulling;
    }

    /// <summary>
    /// マスク描画の方式を変更する。
    ///  falseの場合、マスクを1枚のテクスチャに分割してレンダリングする（デフォルトはこちら）。
    ///  高速だが、マスク個数の上限が36に限定され、質も荒くなる。
    ///  trueの場合、パーツ描画の前にその都度必要なマスクを描き直す
    ///  レンダリング品質は高いが描画処理負荷は増す。
    /// </summary>
    public void UseHighPrecisionMask(bool high)
    {
        _useHighPrecisionMask = high;
    }

    /// <summary>
    /// マスク描画の方式を取得する。
    /// </summary>
    public bool IsUsingHighPrecisionMask()
    {
        return _useHighPrecisionMask;
    }

    /// <summary>
    /// モデル描画の実装
    /// </summary>
    protected abstract void DoDrawModel();

    /// <summary>
    /// 描画オブジェクト（アートメッシュ）を描画する。
    /// ポリゴンメッシュとテクスチャ番号をセットで渡す。
    /// </summary>
    /// <param name="textureNo">描画するテクスチャ番号</param>
    /// <param name="indexCount">描画オブジェクトのインデックス値</param>
    /// <param name="vertexCount">ポリゴンメッシュの頂点数</param>
    /// <param name="indexArray">ポリゴンメッシュ頂点のインデックス配列</param>
    /// <param name="vertexArray">ポリゴンメッシュの頂点配列</param>
    /// <param name="uvArray">uv配列</param>
    /// <param name="opacity">不透明度</param>
    /// <param name="colorBlendMode">カラーブレンディングのタイプ</param>
    /// <param name="invertedMask">マスク使用時のマスクの反転使用</param>
    internal abstract unsafe void DrawMesh(int textureNo, int indexCount, int vertexCount
                          , ushort* indexArray, float* vertexArray, float* uvArray
                          , float opacity, CubismBlendMode colorBlendMode, bool invertedMask);

    /// <summary>
    /// モデル描画直前のレンダラのステートを保持する
    /// </summary>
    protected abstract void SaveProfile();

    /// <summary>
    /// モデル描画直前のレンダラのステートを復帰させる
    /// </summary>
    protected abstract void RestoreProfile();
}
