using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GGPOLauncher.Core.Interfaces;
using GGPOLauncher.Core.Models;

namespace GGPOLauncher.Core.Services;

public sealed class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string? Token { get; private set; }
    public UserProfile? CurrentUser { get; private set; }

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
    }

    public async Task<AuthResponse> LoginWithGoogleAsync(string idToken)
    {
        var response = await _http.PostAsJsonAsync("api/auth/google", new GoogleLoginRequest(idToken));
        await EnsureSuccessWithMessageAsync(response);

        return await HandleAuthResponseAsync(response);
    }

    public async Task<AuthResponse> LoginWithEmailAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new EmailLoginRequest(email, password));
        await EnsureSuccessWithMessageAsync(response);

        return await HandleAuthResponseAsync(response);
    }

    public async Task<AuthResponse> RegisterWithEmailAsync(string email, string password, string displayName)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", new EmailRegisterRequest(email, password, displayName));
        await EnsureSuccessWithMessageAsync(response);

        return await HandleAuthResponseAsync(response);
    }

    public async Task<UserProfile> GetMyProfileAsync()
    {
        var profile = await _http.GetFromJsonAsync<UserProfile>("api/profile/me", _jsonOptions)
            ?? throw new InvalidOperationException("No se pudo cargar el perfil.");
        CurrentUser = profile;
        return profile;
    }

    public async Task<List<RankingEntry>> GetRankingAsync(int page = 1, int pageSize = 20)
    {
        return await _http.GetFromJsonAsync<List<RankingEntry>>($"api/ranking?page={page}&pageSize={pageSize}", _jsonOptions)
            ?? [];
    }

    public async Task<List<RoomStateDto>> GetRoomsAsync()
    {
        return await _http.GetFromJsonAsync<List<RoomStateDto>>("api/rooms", _jsonOptions)
            ?? [];
    }

    public async Task<RoomStateDto> GetRoomAsync(int roomId)
    {
        return await _http.GetFromJsonAsync<RoomStateDto>($"api/rooms/{roomId}", _jsonOptions)
            ?? throw new InvalidOperationException("No se pudo cargar la sala.");
    }

    public async Task<RoomStateDto> CreateRoomAsync(string name, string hostIp)
    {
        var response = await _http.PostAsync($"api/rooms?name={Uri.EscapeDataString(name)}&hostIp={Uri.EscapeDataString(hostIp)}", null);
        await EnsureSuccessWithMessageAsync(response);

        return await response.Content.ReadFromJsonAsync<RoomStateDto>(_jsonOptions)
            ?? throw new InvalidOperationException("No se pudo crear la sala.");
    }

    public async Task JoinSpectatorAsync(int roomId)
    {
        var response = await _http.PostAsync($"api/rooms/{roomId}/spectate", null);
        await EnsureSuccessWithMessageAsync(response);
    }

    public async Task JoinQueueAsync(int roomId)
    {
        var response = await _http.PostAsync($"api/rooms/{roomId}/join-queue", null);
        await EnsureSuccessWithMessageAsync(response);
    }

    public async Task<MatchSessionDto> CreateMatchSessionAsync(int roomId, CreateMatchSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/rooms/{roomId}/matches", request, _jsonOptions);
        await EnsureSuccessWithMessageAsync(response);

        return await response.Content.ReadFromJsonAsync<MatchSessionDto>(_jsonOptions)
            ?? throw new InvalidOperationException("No se pudo crear la sesión de partida.");
    }

    public async Task<MatchSessionDto> MarkMatchStartedAsync(int roomId, Guid matchSessionId, StartMatchSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/rooms/{roomId}/matches/{matchSessionId}/started", request, _jsonOptions);
        await EnsureSuccessWithMessageAsync(response);

        return await response.Content.ReadFromJsonAsync<MatchSessionDto>(_jsonOptions)
            ?? throw new InvalidOperationException("No se pudo marcar la sesión como iniciada.");
    }

    public async Task<MatchSessionCompletionResponse> CompleteMatchSessionAsync(int roomId, Guid matchSessionId, CompleteMatchSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/rooms/{roomId}/matches/{matchSessionId}/complete", request, _jsonOptions);
        await EnsureSuccessWithMessageAsync(response);

        return await response.Content.ReadFromJsonAsync<MatchSessionCompletionResponse>(_jsonOptions)
            ?? throw new InvalidOperationException("No se pudo registrar el resultado de la partida.");
    }

    public async Task<MatchSessionDto> CancelMatchSessionAsync(int roomId, Guid matchSessionId, CancelMatchSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/rooms/{roomId}/matches/{matchSessionId}/cancel", request, _jsonOptions);
        await EnsureSuccessWithMessageAsync(response);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        return payload.GetProperty("session").Deserialize<MatchSessionDto>(_jsonOptions)
            ?? throw new InvalidOperationException("No se pudo cancelar la sesión de partida.");
    }

    public async Task<MatchSessionDto> ReviewMatchSessionAsync(int roomId, Guid matchSessionId, ReviewMatchSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/rooms/{roomId}/matches/{matchSessionId}/review", request, _jsonOptions);
        await EnsureSuccessWithMessageAsync(response);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        return payload.GetProperty("session").Deserialize<MatchSessionDto>(_jsonOptions)
            ?? throw new InvalidOperationException("No se pudo marcar la partida para revisión.");
    }

    public async Task<int> AddCreditsAsync(int amount)
    {
        var response = await _http.PostAsJsonAsync("api/credits/add", new { Amount = amount });
        await EnsureSuccessWithMessageAsync(response);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        return payload.GetProperty("credits").GetInt32();
    }

    public async Task<List<CreditTransactionDto>> GetCreditHistoryAsync()
    {
        return await _http.GetFromJsonAsync<List<CreditTransactionDto>>("api/credits/history", _jsonOptions)
            ?? [];
    }

    private async Task<AuthResponse> HandleAuthResponseAsync(HttpResponseMessage response)
    {
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions)
            ?? throw new InvalidOperationException("No se pudo leer la respuesta de autenticación.");

        Token = auth.Token;
        CurrentUser = auth.Profile;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return auth;
    }

    private static async Task EnsureSuccessWithMessageAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (payload.TryGetProperty("message", out var messageProperty))
                throw new InvalidOperationException(messageProperty.GetString() ?? "La solicitud falló.");
        }
        catch (JsonException)
        {
        }

        response.EnsureSuccessStatusCode();
    }
}
