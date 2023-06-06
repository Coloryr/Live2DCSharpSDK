using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework.Model;

public record CubismModelUserDataObj
{
    public record MetaObj
    {
        public int UserDataCount { get; set; }
        public int TotalUserDataSize { get; set; }
    }

    public record UserDataObj
    {
        public string Target { get; set; }
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public MetaObj Meta { get; set; }
    public List<UserDataObj> UserData { get; set; }
}
