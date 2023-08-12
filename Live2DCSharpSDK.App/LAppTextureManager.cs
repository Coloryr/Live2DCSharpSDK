namespace Live2DCSharpSDK.App;

/// <summary>
/// 画像読み込み、管理を行うクラス。
/// </summary>
public class LAppTextureManager
{
    private readonly LAppDelegate _lapp;
    private readonly List<TextureInfo> _textures = new();

    public LAppTextureManager(LAppDelegate lapp)
    {
        _lapp = lapp;
    }

    /// <summary>
    /// 画像読み込み
    /// </summary>
    /// <param name="fileName">読み込む画像ファイルパス名</param>
    /// <returns>画像情報。読み込み失敗時はNULLを返す</returns>
    public unsafe TextureInfo CreateTextureFromPngFile(string fileName)
    {
        //search loaded texture already.
        var item = _textures.FirstOrDefault(a => a.FileName == fileName);
        if (item != null)
        {
            return item;
        }

        using var image = Image.Load<Rgba32>(fileName);
        var pixels = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(pixels);

        var GL = _lapp.GL;
        // OpenGL用のテクスチャを生成する
        int textureId = GL.glGenTexture();
        GL.glBindTexture(GL.GL_TEXTURE_2D, textureId);
        fixed (byte* ptr = pixels)
            GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, image.Width, image.Height, 0, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, new IntPtr(ptr));
        GL.glGenerateMipmap(GL.GL_TEXTURE_2D);
        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR_MIPMAP_LINEAR);
        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
        GL.glBindTexture(GL.GL_TEXTURE_2D, 0);

        var info = new TextureInfo()
        {
            FileName = fileName,
            Width = image.Width,
            Height = image.Height,
            ID = textureId
        };

        _textures.Add(info);

        return info;
    }

    /// <summary>
    /// 指定したテクスチャIDの画像を解放する
    /// </summary>
    /// <param name="textureId">解放するテクスチャID</param>
    public void ReleaseTexture(int textureId)
    {
        foreach (var item in _textures)
        {
            if (item.ID == textureId)
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
    public TextureInfo? GetTextureInfoById(int textureId)
    {
        return _textures.FirstOrDefault(a => a.ID == textureId);
    }
}
