using Newtonsoft.Json.Linq;

namespace Live2DCSharpSDK.Framework;

public record ModelSettingObj
{
    public record FileReference
    {
        public record Expression
        {
            public string Name { get; set; }
            public string File { get; set; }
        }
        public record Motion
        {
            public string File { get; set; }
            public string Sound { get; set; }
            public float FadeInTime { get; set; }
            public float FadeOutTime { get; set; }
        }
        public string Moc { get; set; }
        public List<string> Textures { get; set; }
        public string Physics { get; set; }
        public string Pose { get; set; }
        public string DisplayInfo { get; set; }
        public List<Expression> Expressions { get; set; }
        public Dictionary<string, List<Motion>> Motions { get; set; }
        public string UserData { get; set; }
    }

    public record HitArea
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public record Parameter
    {
        public string Name { get; set; }
        public List<string> Ids { get; set; }
    }

    public FileReference FileReferences { get; set; }
    public List<HitArea> HitAreas { get; set; }
    public Dictionary<string, float> Layout { get; set; }
    public List<Parameter> Groups { get; set; }
}

public class CubismModelSettingJson
{
    // JSON keys
    public const string Version = "Version";
    public const string FileReferences = "FileReferences";
    public const string Groups = "Groups";
    public const string Layout = "Layout";
    public const string HitAreas = "HitAreas";

    public const string Moc = "Moc";
    public const string Textures = "Textures";
    public const string Physics = "Physics";
    public const string DisplayInfo = "DisplayInfo";
    public const string Pose = "Pose";
    public const string Expressions = "Expressions";
    public const string Motions = "Motions";

    public const string UserData = "UserData";
    public const string Name = "Name";
    public const string FilePath = "File";
    public const string Id = "Id";
    public const string Ids = "Ids";
    public const string Target = "Target";

    // Motions
    public const string Idle = "Idle";
    public const string TapBody = "TapBody";
    public const string PinchIn = "PinchIn";
    public const string PinchOut = "PinchOut";
    public const string Shake = "Shake";
    public const string FlickHead = "FlickHead";
    public const string Parameter = "Parameter";

    public const string SoundPath = "Sound";
    public const string FadeInTime = "FadeInTime";
    public const string FadeOutTime = "FadeOutTime";

    // Layout
    public const string CenterX = "CenterX";
    public const string CenterY = "CenterY";
    public const string X = "X";
    public const string Y = "Y";
    public const string Width = "Width";
    public const string Height = "Height";

    public const string LipSync = "LipSync";
    public const string EyeBlink = "EyeBlink";

    public const string InitParameter = "init_param";
    public const string InitPartsVisible = "init_parts_visible";
    public const string Val = "val";

    private JObject json;
    private ModelSettingObj Obj;

    /// <summary>
    /// 引数付きコンストラクタ
    /// </summary>
    /// <param name="buffer">Model3Jsonをバイト配列として読み込んだデータバッファ</param>
    public CubismModelSettingJson(string buffer)
    {
        json = JObject.Parse(buffer);
        Obj = json.ToObject<ModelSettingObj>()!;
    }

    /// <summary>
    /// CubismJsonオブジェクトのポインタを取得する
    /// </summary>
    /// <returns>CubismJsonのポインタ</returns>
    public JObject GetJsonPointer()
    {
        return json;
    }

    public string GetModelFileName()
    {
        var node = Obj.FileReferences?.Moc;
        if (node == null) return "";
        return node;
    }

    // テクスチャについて
    public int GetTextureCount()
    {
        var node = Obj.FileReferences?.Textures;
        if (node == null) return 0;
        return node.Count;
    }

    public string GetTextureDirectory()
    {
        var node = Obj.FileReferences?.Textures;
        if (node == null) return "";

        var node1 = node.First();
        return Path.GetDirectoryName(node1);
    }

    public string GetTextureFileName(int index)
    {
        return Obj.FileReferences?.Textures[index];
    }

    // あたり判定について
    public int GetHitAreasCount()
    {
        var node = Obj.HitAreas;
        if (node == null) return 0;
        return node.Count;
    }

    public string GetHitAreaId(int index)
    {
        return CubismFramework.GetIdManager().GetId(Obj.HitAreas[index].Id);
    }

    public string GetHitAreaName(int index)
    {
        return Obj.HitAreas[index].Name;
    }

    // 物理演算、表示名称、パーツ切り替え、表情ファイルについて
    public string GetPhysicsFileName()
    {
        return Obj.FileReferences.Physics;
    }

    public string GetPoseFileName()
    {
        var node = Obj.FileReferences.Pose;
        return node.ToString();
    }

    public string GetDisplayInfoFileName()
    {
        var node = Obj.FileReferences.DisplayInfo;
        return node.ToString();
    }

    public int GetExpressionCount()
    {
        var node = Obj.FileReferences.Expressions;
        if (node == null) return 0;
        return node.Count();
    }

    public string GetExpressionName(int index)
    {
        var node = Obj.FileReferences.Expressions;
        return node[index].Name;
    }

    public string GetExpressionFileName(int index)
    {
        var node = Obj.FileReferences.Expressions;
        return node[index].File;
    }

    // モーションについて
    public int GetMotionGroupCount()
    {
        var node = Obj.FileReferences.Motions;
        if (node == null) return 0;
        return node.Count();
    }

    public string GetMotionGroupName(int index)
    {
        var node = Obj.FileReferences.Motions;
        if (node == null) return null;
        return node.Keys.ToList()[index];
    }

    public int GetMotionCount(string groupName)
    {
        return Obj.FileReferences.Motions[groupName].Count;
    }

    public string GetMotionFileName(string groupName, int index)
    {
        var node = Obj.FileReferences.Motions[groupName][index];
        if (node == null) return "";
        return node.File;
    }

    public string GetMotionSoundFileName(string groupName, int index)
    {
        var node = Obj.FileReferences.Motions[groupName][index];
        if (node == null) return "";
        return node.Sound;
    }

    public float GetMotionFadeInTimeValue(string groupName, int index)
    {
        var node = Obj.FileReferences.Motions[groupName][index];
        if (node == null) return -1.0f;
        return node.FadeInTime;
    }

    public float GetMotionFadeOutTimeValue(string groupName, int index)
    {
        var node = Obj.FileReferences.Motions[groupName][index];
        if (node == null) return -1.0f;
        return node.FadeOutTime;
    }

    public string GetUserDataFile()
    {
        var node = Obj.FileReferences.UserData;
        if (node == null) return "";
        return node.ToString();
    }

    public bool GetLayoutMap(Dictionary<string, float> outLayoutMap)
    {
        var node = Obj.Layout;
        if (node == null)
            return false;

        var ret = false;
        foreach (var item in node)
        {
            if (outLayoutMap.ContainsKey(item.Key))
            {
                outLayoutMap[item.Key] = item.Value;
            }
            else
            {
                outLayoutMap.Add(item.Key, item.Value);
            }
            ret = true;
        }
        return ret;
    }

    public int GetEyeBlinkParameterCount()
    {
        if (!IsExistEyeBlinkParameters())
            return 0;

        int num = 0;
        var node = Obj.Groups;
        foreach (var item in node)
        {
            if (item == null)
            {
                continue;
            }
            if (item.Name == EyeBlink)
            {
                num = item.Ids.Count;
                break;
            }
        }

        return num;
    }

    public string? GetEyeBlinkParameterId(int index)
    {
        if (!IsExistEyeBlinkParameters())
        {
            return null;
        }

        var node = Obj.Groups;
        foreach (var item in node)
        {
            if (item == null)
            {
                continue;
            }
            if (item.Name == EyeBlink)
            {
                return CubismFramework.GetIdManager().GetId(item.Ids[index]);
            }
        }

        return null;
    }

    public int GetLipSyncParameterCount()
    {
        if (!IsExistLipSyncParameters())
        {
            return 0;
        }

        int num = 0;
        var node = Obj.Groups;
        foreach (var item in node)
        {
            if (item == null)
            {
                continue;
            }
            if (item.Name == LipSync)
            {
                num = item.Ids.Count;
                break;
            }
        }

        return num;
    }

    public string GetLipSyncParameterId(int index)
    {
        if (!IsExistLipSyncParameters())
        {
            return null;
        }

        var node = Obj.Groups;
        foreach (var item in node)
        {
            if (item == null)
            {
                continue;
            }
            if (item.Name == LipSync)
            {
                return CubismFramework.GetIdManager().GetId(item.Ids[index]);
            }
        }

        return null;
    }

    private bool IsExistEyeBlinkParameters()
    {
        var node = Obj.Groups;
        if (node == null)
        {
            return false;
        }

        foreach (var item in node)
        {
            if (item.Name == EyeBlink)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsExistLipSyncParameters()
    {
        var node = Obj.Groups;
        if (node == null)
        {
            return false;
        }

        foreach (var item in node)
        {
            if (item.Name == LipSync)
            {
                return true;
            }
        }

        return false;
    }
}
