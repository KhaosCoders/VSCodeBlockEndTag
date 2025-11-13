using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodeBlockEndTag.Services;

/// <summary>
/// Service for managing PRO license activation and validation
/// </summary>
internal static class LicenseService
{
    private const string ActivationApiUrl = "https://kc-license-store-dev.azurewebsites.net/api/license/activate?code=gCh2P8MIv15ko9M6_kkxweexIe6f0O87b2HvuF4OzeCdAzFulmxhkg==";
    private const string GetTokenApiUrl = "https://kc-license-store-dev.azurewebsites.net/api/license/token?code=Tm3XlpoN5j1Hg99fJao001IJZo-5JU2AvRZQUkx4RF5TAzFuRfpQsw==";

    // Embedded RSA public key for JWT validation
    private const string PublicKeyPem = @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAp4ReH608ElYa3BOanoK0
v1PahbOveDTJj1BWrA8cM2nHqujkA9kWddeW2+e5ZPscJqA5D//yI9yADu1k4hMc
ipzXmwpae9bZt8MFU5H7H9pqcyuAkBPwNhxTbnDdKMVt9Bce/3oJC0HDpUHwHfT+
xmK4Mi4KQk2Bz6RAH47NU1tppC+UIXeRDbn5+RejRbjtWvsBBCgZ9MHRW47dkyIz
46f1ceTrESbo9w4AuF7gZMsLCTJ0C2l7X0pVCdXBGG+t1Q02Vh6ewXVGKrKmx25H
vXFGZwsgXRlBLpOtjK2G8EKXNPFHo3ouIinIudGjvEmgKADwyqeZ+dz67rUiwC98
zQIDAQAB
-----END PUBLIC KEY-----";

    private static string _currentLicenseToken;
    private static RsaSecurityKey _publicKey;
    private static readonly HttpClient _httpClient = new HttpClient();

    /// <summary>
    /// Initialize the license service with the stored license token
    /// </summary>
    public static void Initialize(string licenseToken)
    {
        _currentLicenseToken = licenseToken;
        InitializePublicKey();
    }

    /// <summary>
    /// Activates a license key with the provided email address
    /// </summary>
    /// <param name="licenseKey">The license key to activate</param>
    /// <param name="email">The user's email address</param>
    /// <returns>The JWT license token</returns>
    public static async Task<string> ActivateLicenseAsync(string licenseKey, string email)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            throw new ArgumentException("License key cannot be empty", nameof(licenseKey));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email address cannot be empty", nameof(email));

        // Track activation attempt
        try
        {
            Telemetry.TelemetryEvents.TrackLicenseActivationAttempted(!string.IsNullOrWhiteSpace(email), "new");
        }
        catch
        {
            // Telemetry should never break functionality
        }

        try
        {
            var payload = $"{{\"licenseKey\":\"{licenseKey}\",\"email\":\"{email}\"}}";
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ActivationApiUrl, content);

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Try to extract error message from JSON response
                var errorMessage = TryExtractErrorMessage(jsonResponse);

                // Track failure
                try
                {
                    Telemetry.TelemetryEvents.TrackLicenseActivationFailed("ApiError", errorMessage ?? $"Status {response.StatusCode}", "new");
                }
                catch
                {
                    // Telemetry should never break functionality
                }

                throw new Exception(errorMessage ?? $"License activation failed: {response.StatusCode}");
            }

            // Parse JSON response and extract token
            var jwt = ExtractTokenFromResponse(jsonResponse);

            // Validate the received token
            if (!ValidateLicenseToken(jwt))
            {
                // Track failure
                try
                {
                    Telemetry.TelemetryEvents.TrackLicenseActivationFailed("TokenInvalid", "Received token is invalid", "new");
                }
                catch
                {
                    // Telemetry should never break functionality
                }

                throw new Exception("Received license token is invalid");
            }

            _currentLicenseToken = jwt;

            // Track success
            try
            {
                Telemetry.TelemetryEvents.TrackLicenseActivationSucceeded("new");
            }
            catch
            {
                // Telemetry should never break functionality
            }

            return jwt;
        }
        catch (HttpRequestException ex)
        {
            // Track failure
            try
            {
                Telemetry.TelemetryEvents.TrackLicenseActivationFailed("NetworkError", ex.Message, "new");
            }
            catch
            {
                // Telemetry should never break functionality
            }

            throw new Exception($"Network error during license activation: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // Track failure if not already tracked
            try
            {
                if (!(ex is HttpRequestException))
                {
                    Telemetry.TelemetryEvents.TrackLicenseActivationFailed("UnknownError", ex.Message, "new");
                }
            }
            catch
            {
                // Telemetry should never break functionality
            }

            throw;
        }
    }

    public static async Task<string> RequireActivatedTokenAsync(string licenseKey, string email)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            throw new ArgumentException("License key cannot be empty", nameof(licenseKey));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email address cannot be empty", nameof(email));

        // Track token request attempt
        try
        {
            Telemetry.TelemetryEvents.TrackLicenseTokenRequested(!string.IsNullOrWhiteSpace(email));
        }
        catch
        {
            // Telemetry should never break functionality
        }

        try
        {
            var payload = $"{{\"licenseKey\":\"{licenseKey}\",\"email\":\"{email}\"}}";
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(GetTokenApiUrl, content);

            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Track failure
                try
                {
                    Telemetry.TelemetryEvents.TrackLicenseActivationFailed("TokenRequestFailed", $"Status {response.StatusCode}", "reactivation");
                }
                catch
                {
                    // Telemetry should never break functionality
                }

                return null;
            }

            // Parse JSON response and extract token
            var jwt = ExtractTokenFromResponse(jsonResponse);

            // Validate the received token
            if (!ValidateLicenseToken(jwt))
            {
                // Track failure
                try
                {
                    Telemetry.TelemetryEvents.TrackLicenseActivationFailed("TokenInvalid", "Received token is invalid", "reactivation");
                }
                catch
                {
                    // Telemetry should never break functionality
                }

                return null;
            }

            _currentLicenseToken = jwt;

            // Track success
            try
            {
                Telemetry.TelemetryEvents.TrackLicenseActivationSucceeded("reactivation");
            }
            catch
            {
                // Telemetry should never break functionality
            }

            return jwt;
        }
        catch (HttpRequestException)
        {
            // Track failure
            try
            {
                Telemetry.TelemetryEvents.TrackLicenseActivationFailed("NetworkError", "HTTP request failed", "reactivation");
            }
            catch
            {
                // Telemetry should never break functionality
            }

            return null;
        }
    }

    /// <summary>
    /// Tries to extract an error message from a JSON response
    /// </summary>
    /// <param name="jsonResponse">The JSON response</param>
    /// <returns>The error message if found, otherwise null</returns>
    private static string TryExtractErrorMessage(string jsonResponse)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            var root = jsonDoc.RootElement;

            // Try to get the Message property
            if (root.TryGetProperty("Message", out var msgProp))
            {
                var message = msgProp.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }
        catch
        {
            // If parsing fails, return null
        }

        return null;
    }

    /// <summary>
    /// Extracts the JWT token from the API JSON response
    /// </summary>
    /// <param name="jsonResponse">The JSON response from the activation API</param>
    /// <returns>The extracted JWT token</returns>
    private static string ExtractTokenFromResponse(string jsonResponse)
    {
        try
        {
            using (var jsonDoc = JsonDocument.Parse(jsonResponse))
            {
                var root = jsonDoc.RootElement;

                // Check if Success is true
                if (root.TryGetProperty("Success", out var successProp) && successProp.GetBoolean())
                {
                    // Extract the Token property
                    if (root.TryGetProperty("Token", out var tokenProp))
                    {
                        var jwt = tokenProp.GetString();

                        if (string.IsNullOrWhiteSpace(jwt))
                            throw new Exception("Token is empty in the response");

                        return jwt;
                    }
                    else
                    {
                        throw new Exception("Token property not found in response");
                    }
                }
                else
                {
                    // Extract error message if available
                    var message = root.TryGetProperty("Message", out var msgProp)
                        ? msgProp.GetString()
                        : "License activation failed";
                    throw new Exception(message);
                }
            }
        }
        catch (JsonException ex)
        {
            throw new Exception($"Failed to parse activation response: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates a JWT license token
    /// </summary>
    /// <param name="jwt">The JWT token to validate</param>
    /// <returns>True if the token is valid and not expired</returns>
    public static bool ValidateLicenseToken(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return false;

        try
        {
            if (_publicKey == null)
                InitializePublicKey();

            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _publicKey,
                // Key resolver provides compatibility for tokens with or without 'kid' header
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    return new[] { _publicKey };
                },
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(10)
            };

            tokenHandler.ValidateToken(jwt, validationParameters, out SecurityToken validatedToken);

            return true;
        }
        catch (SecurityTokenExpiredException ex2)
        {
            // Token has expired
            return false;
        }
        catch (Exception ex)
        {
            // Token is invalid
            return false;
        }
    }

    /// <summary>
    /// Checks if the user has a valid PRO license
    /// </summary>
    /// <returns>True if a valid PRO license exists</returns>
    public static bool HasValidProLicense()
    {
        return ValidateLicenseToken(_currentLicenseToken);
    }

    /// <summary>
    /// Gets the email address of the currently logged in Visual Studio user
    /// </summary>
    /// <returns>The user's email address or empty string if not available</returns>
    public static string GetVisualStudioEmail()
    {
        try
        {
            // Try to read from registry: HKEY_CURRENT_USER\Software\Microsoft\VSCommon\ConnectedUser\IdeUserVX\Cache\EmailAddress
            // where X can be 1, 2, 3, 4, etc. (different Visual Studio versions)

            using (var vsCommonKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\VSCommon\ConnectedUser"))
            {
                if (vsCommonKey != null)
                {
                    // Try different version keys, starting from the highest (most recent)
                    for (int version = 10; version >= 1; version--)
                    {
                        var versionKeyName = $"IdeUserV{version}";
                        using (var versionKey = vsCommonKey.OpenSubKey($@"{versionKeyName}\Cache"))
                        {
                            if (versionKey != null)
                            {
                                var email = versionKey.GetValue("EmailAddress") as string;
                                if (!string.IsNullOrWhiteSpace(email))
                                {
                                    return email;
                                }
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the expiration date of the current license token
    /// </summary>
    /// <returns>The expiration date or null if no valid license</returns>
    public static DateTime? GetLicenseExpirationDate()
    {
        if (string.IsNullOrWhiteSpace(_currentLicenseToken))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(_currentLicenseToken);

            var expClaim = token.Claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out long exp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    /// <summary>
    /// Initializes the RSA public key from the embedded PEM string
    /// </summary>
    private static void InitializePublicKey()
    {
        try
        {
            // Remove PEM headers and footers
            var pemContent = PublicKeyPem
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();

            var keyBytes = Convert.FromBase64String(pemContent);

            // Parse the SubjectPublicKeyInfo structure to extract RSA parameters
            var rsaParams = DecodeRsaPublicKey(keyBytes);

            // Use RSACryptoServiceProvider for .NET Framework 4.8
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(rsaParams);
            _publicKey = new RsaSecurityKey(rsa);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize public key: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decodes an RSA public key from SubjectPublicKeyInfo format (for .NET Framework 4.8 compatibility)
    /// Based on: https://jpassing.com/2021/12/05/importing-rsa-public-keys-in-downlevel-dotnet-and-dotnet-framework-versions/
    /// </summary>
    private static RSAParameters DecodeRsaPublicKey(byte[] x509Key)
    {
        using (var stream = new System.IO.MemoryStream(x509Key))
        using (var reader = new System.IO.BinaryReader(stream))
        {
            // Helper to read ASN.1 length
            int ReadLength()
            {
                var firstByte = reader.ReadByte();

                if ((firstByte & 0x80) == 0)
                {
                    // Short form: length is in the first byte (0-127)
                    return firstByte;
                }

                // Long form: first byte indicates how many subsequent bytes contain the length
                var lengthBytes = firstByte & 0x7F;
                if (lengthBytes == 0 || lengthBytes > 4)
                    throw new InvalidOperationException("Invalid length encoding");

                var length = 0;
                for (var i = 0; i < lengthBytes; i++)
                {
                    length = (length << 8) | reader.ReadByte();
                }

                return length;
            }

            // Helper to skip over a value
            void Skip(int count)
            {
                reader.ReadBytes(count);
            }

            // Helper to read a specific tag and return its content length
            int ReadTag(byte expectedTag)
            {
                var tag = reader.ReadByte();
                if (tag != expectedTag)
                    throw new InvalidOperationException($"Expected tag 0x{expectedTag:X2} but found 0x{tag:X2}");
                return ReadLength();
            }

            // Parse: SEQUENCE (SubjectPublicKeyInfo)
            ReadTag(0x30);

            // Parse: SEQUENCE (AlgorithmIdentifier)
            var algorithmIdentifierLength = ReadTag(0x30);
            Skip(algorithmIdentifierLength);

            // Parse: BIT STRING (subjectPublicKey)
            ReadTag(0x03);

            // Skip unused bits indicator
            reader.ReadByte();

            // Parse: SEQUENCE (RSAPublicKey)
            ReadTag(0x30);

            // Parse: INTEGER (modulus)
            var modulusLength = ReadTag(0x02);
            var modulus = reader.ReadBytes(modulusLength);

            // Remove leading zero byte if present (ASN.1 uses this to indicate positive integers)
            if (modulus.Length > 0 && modulus[0] == 0x00)
            {
                var temp = new byte[modulus.Length - 1];
                Array.Copy(modulus, 1, temp, 0, temp.Length);
                modulus = temp;
            }

            // Parse: INTEGER (exponent)
            var exponentLength = ReadTag(0x02);
            var exponent = reader.ReadBytes(exponentLength);

            return new RSAParameters
            {
                Modulus = modulus,
                Exponent = exponent
            };
        }
    }
}
