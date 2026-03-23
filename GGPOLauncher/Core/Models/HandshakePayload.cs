using System.Text.Json.Serialization;

namespace GGPOLauncher.Core.Models;

public record HandshakePayload(
    [property: JsonPropertyName("Type")] string Type,
    [property: JsonPropertyName("HostIp")] string? HostIp = null,
    [property: JsonPropertyName("UdpPort")] int UdpPort = 6000,
    [property: JsonPropertyName("GameRom")] string GameRom = "kof2002",
    [property: JsonPropertyName("DelayFrames")] int DelayFrames = 2
);
