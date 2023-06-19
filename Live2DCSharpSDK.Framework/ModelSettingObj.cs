﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            [DefaultValue(-1f)]
            public float FadeInTime { get; set; }
            [DefaultValue(-1f)]
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
