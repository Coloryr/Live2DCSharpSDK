using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Live2DCSharpSDK.Framework;

public record ModelObj
{
    public record Parameter
    {
        public string Id { get; set; }
        public string GroupId { get; set; }
        public string Name { get; set; }
    }

    public record Part
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public List<Parameter> Parameters { get; set; }
    public List<Parameter> ParameterGroups { get; set; }
    public List<Part> Parts { get; set; }
}
