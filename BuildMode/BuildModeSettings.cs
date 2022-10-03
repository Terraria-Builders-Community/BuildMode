using Auxiliary.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BuildMode
{
    public class BuildModeSettings : ISettings
    {
        [JsonPropertyName("defaultbuffs")]
        public int[] DefaultBuffs { get; set; } = Array.Empty<int>();
    }
}
