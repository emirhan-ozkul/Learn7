using System.Security.Cryptography;
using System.Text;
using lrnWebApi.SettingCS;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;

namespace lrnWebApi.Services;

public sealed class UserApiKeyStore
{
    private readonly IDataProtector _protector;

    public UserApiKeyStore(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("lrnWebApi.UserApiKey.v1");
    }

    public void SaveApiKey(int userId, string apiKey)
    {
        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO UserApiKeys (UserId, EncryptedApiKey, UpdatedAt)
            VALUES (@userId, @encryptedApiKey, @updatedAt)
            ON CONFLICT(UserId) DO UPDATE SET
                EncryptedApiKey = excluded.EncryptedApiKey,
                UpdatedAt = excluded.UpdatedAt;";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@encryptedApiKey", _protector.Protect(apiKey));
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    public string? GetApiKey(int userId)
    {
        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EncryptedApiKey FROM UserApiKeys WHERE UserId = @userId;";
        command.Parameters.AddWithValue("@userId", userId);
        var encrypted = command.ExecuteScalar() as string;
        return encrypted is null ? null : _protector.Unprotect(encrypted);
    }

    public void DeleteApiKey(int userId)
    {
        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM UserApiKeys WHERE UserId = @userId;";
        command.Parameters.AddWithValue("@userId", userId);
        command.ExecuteNonQuery();
    }

    public bool HasApiKey(int userId)
    {
        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM UserApiKeys WHERE UserId = @userId;";
        command.Parameters.AddWithValue("@userId", userId);
        return command.ExecuteScalar() is not null;
    }
}
