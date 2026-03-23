using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using KingOfTheColonyApi.Data;
using KingOfTheColonyApi.Models;
using KingOfTheColonyApi.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KingOfTheColonyApi.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHashService _passwordHashService;

    public AuthService(AppDbContext db, IConfiguration config, PasswordHashService passwordHashService)
    {
        _db = db;
        _config = config;
        _passwordHashService = passwordHashService;
    }

    public async Task<AuthResponse> AuthenticateWithGoogleAsync(string idToken)
    {
        var googleClientId = _config["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId not configured");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [googleClientId]
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        var normalizedEmail = NormalizeEmail(payload.Email);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

        if (user is null)
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user is not null)
                user.GoogleId = payload.Subject;
        }

        if (user is null)
        {
            user = new User
            {
                GoogleId = payload.Subject,
                Email = normalizedEmail,
                DisplayName = payload.Name ?? payload.Email,
                AvatarUrl = payload.Picture ?? "",
                Credits = 3 // Welcome bonus
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            // Update profile info from Google
            user.Email = normalizedEmail;
            user.DisplayName = payload.Name ?? user.DisplayName;
            user.AvatarUrl = payload.Picture ?? user.AvatarUrl;
            await _db.SaveChangesAsync();
        }

        var jwt = GenerateJwt(user);
        var profile = MapToProfile(user);

        return new AuthResponse(jwt, profile);
    }

    public async Task<AuthResponse> RegisterWithEmailAsync(string email, string password, string displayName)
    {
        var normalizedEmail = NormalizeEmail(email);
        ValidatePassword(password);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is not null)
        {
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                throw new InvalidOperationException("Ya existe una cuenta registrada con este correo.");

            user.PasswordHash = _passwordHashService.HashPassword(password);
            if (!string.IsNullOrWhiteSpace(displayName))
                user.DisplayName = displayName.Trim();

            await _db.SaveChangesAsync();
            return CreateAuthResponse(user);
        }

        user = new User
        {
            Email = normalizedEmail,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedEmail : displayName.Trim(),
            PasswordHash = _passwordHashService.HashPassword(password),
            Credits = 3
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginWithEmailAsync(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
            throw new UnauthorizedAccessException("Correo o clave inválidos.");

        if (!_passwordHashService.VerifyPassword(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Correo o clave inválidos.");

        return CreateAuthResponse(user);
    }

    public static UserProfile MapToProfile(User user)
    {
        var total = user.Wins + user.Losses;
        var winRate = total > 0 ? (double)user.Wins / total : 0;
        var score = user.Wins * 3 + user.BestStreak * 2 - user.Losses;

        return new UserProfile(
            user.Id, user.DisplayName, user.AvatarUrl, user.Email,
            user.Credits, user.Wins, user.Losses,
            user.BestStreak, user.CurrentStreak,
            Math.Round(winRate, 4), score);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var jwt = GenerateJwt(user);
        var profile = MapToProfile(user);
        return new AuthResponse(jwt, profile);
    }

    private static string NormalizeEmail(string email)
    {
        var trimmedEmail = email.Trim().ToLowerInvariant();

        try
        {
            var mailAddress = new MailAddress(trimmedEmail);
            return mailAddress.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Debes ingresar un correo válido.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new InvalidOperationException("La clave debe tener al menos 8 caracteres.");
    }

    private string GenerateJwt(User user)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
