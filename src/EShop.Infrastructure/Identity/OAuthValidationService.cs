using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Identity;

/// <summary>
/// Service for validating OAuth tokens from external providers
/// </summary>
public interface IOAuthValidationService
{
    Task<OAuthUserInfo?> ValidateGoogleTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<OAuthUserInfo?> ValidateFacebookTokenAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<OAuthUserInfo?> ValidateAppleTokenAsync(string identityToken, CancellationToken cancellationToken = default);
    Task<OAuthUserInfo?> ValidateMicrosoftTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}

public class OAuthValidationService : IOAuthValidationService
{
    private readonly HttpClient _httpClient;
    private readonly OAuthSettings _settings;

    public OAuthValidationService(
        IHttpClientFactory httpClientFactory,
        IOptions<OAuthSettings> settings)
    {
        _httpClient = httpClientFactory.CreateClient("OAuth");
        _settings = settings.Value;
    }

    public async Task<OAuthUserInfo?> ValidateGoogleTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://www.googleapis.com/oauth2/v3/userinfo?access_token={accessToken}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var googleUser = await response.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken: cancellationToken);
            
            if (googleUser == null || string.IsNullOrEmpty(googleUser.Sub))
                return null;

            return new OAuthUserInfo
            {
                Provider = "Google",
                ProviderKey = googleUser.Sub,
                Email = googleUser.Email,
                FirstName = googleUser.GivenName,
                LastName = googleUser.FamilyName,
                ProfilePictureUrl = googleUser.Picture,
                EmailVerified = googleUser.EmailVerified
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<OAuthUserInfo?> ValidateFacebookTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://graph.facebook.com/me?fields=id,email,first_name,last_name,picture&access_token={accessToken}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var facebookUser = await response.Content.ReadFromJsonAsync<FacebookUserInfo>(cancellationToken: cancellationToken);
            
            if (facebookUser == null || string.IsNullOrEmpty(facebookUser.Id))
                return null;

            return new OAuthUserInfo
            {
                Provider = "Facebook",
                ProviderKey = facebookUser.Id,
                Email = facebookUser.Email,
                FirstName = facebookUser.FirstName,
                LastName = facebookUser.LastName,
                ProfilePictureUrl = facebookUser.Picture?.Data?.Url,
                EmailVerified = !string.IsNullOrEmpty(facebookUser.Email)
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<OAuthUserInfo?> ValidateAppleTokenAsync(string identityToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Apple uses ID tokens (JWT) which need to be validated
            // This is a simplified implementation - in production, validate the JWT signature
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            
            if (!handler.CanReadToken(identityToken))
                return null;

            var jwtToken = handler.ReadJwtToken(identityToken);
            
            var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var emailVerified = jwtToken.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true";

            if (string.IsNullOrEmpty(sub))
                return null;

            // Apple doesn't provide name in the token - it's only available on first sign-in
            // and must be captured from the authorization response
            return new OAuthUserInfo
            {
                Provider = "Apple",
                ProviderKey = sub,
                Email = email ?? string.Empty,
                EmailVerified = emailVerified
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<OAuthUserInfo?> ValidateMicrosoftTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var microsoftUser = await response.Content.ReadFromJsonAsync<MicrosoftUserInfo>(cancellationToken: cancellationToken);
            
            if (microsoftUser == null || string.IsNullOrEmpty(microsoftUser.Id))
                return null;

            return new OAuthUserInfo
            {
                Provider = "Microsoft",
                ProviderKey = microsoftUser.Id,
                Email = microsoftUser.Mail ?? microsoftUser.UserPrincipalName ?? string.Empty,
                FirstName = microsoftUser.GivenName,
                LastName = microsoftUser.Surname,
                EmailVerified = true // Microsoft accounts are verified
            };
        }
        catch
        {
            return null;
        }
    }
}

#region OAuth User Info Models

public class OAuthUserInfo
{
    public string Provider { get; set; } = string.Empty;
    public string ProviderKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool EmailVerified { get; set; }
}

internal class GoogleUserInfo
{
    public string Sub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Picture { get; set; }
}

internal class FacebookUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public FacebookPicture? Picture { get; set; }
}

internal class FacebookPicture
{
    public FacebookPictureData? Data { get; set; }
}

internal class FacebookPictureData
{
    public string? Url { get; set; }
}

internal class MicrosoftUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string? Mail { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? GivenName { get; set; }
    public string? Surname { get; set; }
}

#endregion
