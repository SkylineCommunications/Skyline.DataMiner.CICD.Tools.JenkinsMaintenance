﻿// <auto-generated>Classes for JSON parsing</auto-generated>
#nullable enable
namespace Skyline.DataMiner.CICD.Tools.JenkinsMaintenance.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    internal class KillInfo : MaintenanceInfo
    {
        [JsonPropertyName("disabledWorkflows")]
        public List<string> DisabledWorkflows { get; set; } = [];
    }
}