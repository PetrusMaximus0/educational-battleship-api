﻿using System.Text.Json.Serialization;

namespace api.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EShipSectionStatus
{
    ok,
    hit,
}