using System.Security.Cryptography;
using System.Text;

namespace ProjectOrganizer.Api.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data like API keys and tokens
/// </summary>
public class EncryptionService
{
    private readonly byte[] _encryptionKey;
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _logger = logger;
        
        // Get encryption key from configuration
        var keyString = configuration["Security:EncryptionKey"];
        
        if (string.IsNullOrEmpty(keyString))
        {
            // Generate a warning and use a default key (NOT RECOMMENDED for production)
            _logger.LogWarning("No encryption key found in configuration. Using default key. THIS IS NOT SECURE FOR PRODUCTION!");
            keyString = "DefaultKey_CHANGE_THIS_IN_PRODUCTION_12345678901234567890"; // 32+ chars
        }
        
        // Ensure key is 32 bytes for AES-256
        _encryptionKey = DeriveKeyFromString(keyString, 32);
    }

    /// <summary>
    /// Encrypts a string value
    /// </summary>
    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return null;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            // Prepend IV to the encrypted data
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw;
        }
    }

    /// <summary>
    /// Decrypts an encrypted string value
    /// </summary>
    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return null;

        try
        {
            var buffer = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            // Extract IV from the beginning of the encrypted data
            var iv = new byte[aes.IV.Length];
            Array.Copy(buffer, 0, iv, 0, iv.Length);
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var msDecrypt = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data");
            return null;
        }
    }

    /// <summary>
    /// Masks a sensitive value for display (e.g., "sk-...****abcd")
    /// </summary>
    public string? MaskValue(string? value, int visibleChars = 4, string prefix = "")
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (value.Length <= visibleChars)
            return "****";

        var masked = value.Substring(value.Length - visibleChars);
        
        if (!string.IsNullOrEmpty(prefix))
            return $"{prefix}...****{masked}";
        
        return $"****{masked}";
    }

    /// <summary>
    /// Masks OpenAI API key (shows "sk-...****abcd")
    /// </summary>
    public string? MaskOpenAiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return null;

        // OpenAI keys typically start with "sk-"
        if (apiKey.StartsWith("sk-"))
            return MaskValue(apiKey, 4, "sk");
        
        return MaskValue(apiKey, 4);
    }

    /// <summary>
    /// Derives a key of specific length from a string using PBKDF2
    /// </summary>
    private byte[] DeriveKeyFromString(string keyString, int keySize)
    {
        // Use a fixed salt (in production, consider storing this in configuration too)
        var salt = Encoding.UTF8.GetBytes("ProjectOrganizer_Salt_2024");
        
        using var pbkdf2 = new Rfc2898DeriveBytes(keyString, salt, 10000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keySize);
    }
}
