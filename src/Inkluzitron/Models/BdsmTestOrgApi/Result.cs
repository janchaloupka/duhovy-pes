using Inkluzitron.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Inkluzitron.Models.BdsmTestOrgApi
{
    public class Result
    {
        [JsonConverter(typeof(MillisecondsSinceUnixEpochDateTimeConverter))]
        [JsonProperty("date", Required = Required.Always)]
        public DateTime Date { get; set; }

        [JsonProperty("scores", Required = Required.Always)]
        public List<ResultItem> Traits { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Gender Gender { get; set; } = Gender.Unspecified;
    }
}
