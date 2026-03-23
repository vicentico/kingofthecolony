using System.Net.Http;

namespace GGPOLauncher.Utils;

public static class IpHelper
{
    private static readonly HttpClient Http = new();

    public static async Task<string> GetPublicIpAsync()
    {
        try
        {
            var ip = await Http.GetStringAsync("https://api.ipify.org");
            return ip.Trim();
        }
        catch
        {
            return "No se pudo obtener la IP pública";
        }
    }
}
