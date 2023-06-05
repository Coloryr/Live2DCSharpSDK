namespace Live2DCSharpSDK.App;

/// <summary>
/// 画像情報構造体
/// </summary>
public record TextureInfo
{
    /// <summary>
    /// テクスチャID
    /// </summary>
    public int id;
    /// <summary>
    /// 横幅
    /// </summary>
    public int width;
    /// <summary>
    /// 高さ
    /// </summary>
    public int height;
    /// <summary>
    /// ファイル名
    /// </summary>
    public string fileName;
};

/// <summary>
/// 画像読み込み、管理を行うクラス。
/// </summary>
public class LAppTextureManager
{
    private readonly LAppDelegate Lapp;
    private readonly List<TextureInfo> _textures = new();

    public LAppTextureManager(LAppDelegate lapp)
    {
        Lapp = lapp;
    }

    /// <summary>
    /// 画像読み込み
    /// </summary>
    /// <param name="fileName">読み込む画像ファイルパス名</param>
    /// <returns>画像情報。読み込み失敗時はNULLを返す</returns>
    public unsafe TextureInfo CreateTextureFromPngFile(string fileName)
    {
        //search loaded texture already.
        var item = _textures.FirstOrDefault(a => a.fileName == fileName);
        if (item != null)
            return item;

        int textureId;
        var data = File.ReadAllBytes(fileName);

        using var image = Image.Load<Rgba32>(data);
        image.Mutate(x => x.Flip(FlipMode.Vertical));
        var pixels = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(pixels);

        var GL = Lapp.GL;
        // OpenGL用のテクスチャを生成する
        GL.glGenTextures(1, &textureId);
        GL.glBindTexture(GL.GL_TEXTURE_2D, textureId);
        fixed (byte* ptr = pixels)
            GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, image.Width, image.Height, 0, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, new IntPtr(ptr));
        GL.glGenerateMipmap(GL.GL_TEXTURE_2D);
        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR_MIPMAP_LINEAR);
        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
        GL.glBindTexture(GL.GL_TEXTURE_2D, 0);

        var info = new TextureInfo()
        {
            fileName = fileName,
            width = image.Width,
            height = image.Height,
            id = textureId
        };

        _textures.Add(info);

        return info;
    }

    /// <summary>
    /// 画像の解放
    /// 配列に存在する画像全てを解放する
    /// </summary>
    public void ReleaseTextures()
    {
        for (int i = 0; i < _textures.Count; i++)
        {
            var item = _textures[i];
        }

        _textures.Clear();
    }

    /// <summary>
    /// 指定したテクスチャIDの画像を解放する
    /// </summary>
    /// <param name="textureId">解放するテクスチャID</param>
    public void ReleaseTexture(int textureId)
    {
        foreach (var item in _textures)
        {
            if (item.id == textureId)
            {
                _textures.Remove(item);
                break;
            }
        }
    }

    /// <summary>
    /// テクスチャIDからテクスチャ情報を得る
    /// </summary>
    /// <param name="textureId">取得したいテクスチャID</param>
    /// <returns>テクスチャが存在していればTextureInfoが返る</returns>
    public TextureInfo GetTextureInfoById(int textureId)
    {
        return _textures.FirstOrDefault(a => a.id == textureId);
    }
}
