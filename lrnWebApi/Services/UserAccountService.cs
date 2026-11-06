using System.Security.Cryptography;
using System.Text;
using lrnWebApi.Models;
using lrnWebApi.SettingCS;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

namespace lrnWebApi.Services;

public sealed record UserSession(int UserId, string Username, string Token);

public sealed class UserAccountService
{
    private static readonly PasswordHasher<User> PasswordHasher = new();
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    public UserSession Register(string username, string email, string password)
    {
        ValidateCredentials(username, email, password);
        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using var insertUser = connection.CreateCommand();
        insertUser.Transaction = transaction;
        insertUser.CommandText = @"
            INSERT INTO Users (Username, Email, PasswordHash, RoleId)
            VALUES (@username, @email, @passwordHash,
                (SELECT Id FROM Roles WHERE Name = 'User'));
            SELECT last_insert_rowid();";
        insertUser.Parameters.AddWithValue("@username", username.Trim());
        insertUser.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        insertUser.Parameters.AddWithValue("@passwordHash", PasswordHasher.HashPassword(new User(), password));
        var userId = Convert.ToInt32(insertUser.ExecuteScalar());

        using var stats = connection.CreateCommand();
        stats.Transaction = transaction;
        stats.CommandText = @"
            INSERT INTO UserStats (UserId, XP, Level, RankId, UpdatedAt)
            VALUES (@userId, 0, 1, 1, @updatedAt);";
        stats.Parameters.AddWithValue("@userId", userId);
        stats.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));
        stats.ExecuteNonQuery();

        var session = CreateSession(connection, transaction, userId, username.Trim());
        transaction.Commit();
        return session;
    }

    public UserSession? Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, PasswordHash FROM Users WHERE Email = @email;";
        command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        var userId = reader.GetInt32(0);
        var username = reader.GetString(1);
        var passwordHash = reader.GetString(2);
        if (PasswordHasher.VerifyHashedPassword(new User(), passwordHash, password) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        reader.Close();
        return CreateSession(connection, null, userId, username);
    }

    public bool TryGetUserId(string token, out int userId)
    {
        userId = 0;
        var tokenHash = HashToken(token);
        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT UserId FROM UserSessions
            WHERE TokenHash = @tokenHash AND ExpiresAt > @now;";
        command.Parameters.AddWithValue("@tokenHash", tokenHash);
        command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
        var result = command.ExecuteScalar();
        if (result is null) return false;
        userId = Convert.ToInt32(result);
        return true;
    }

    public void RevokeSession(string token)
    {
        using var connection = new SqliteConnection(DatabaseSettings.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM UserSessions WHERE TokenHash = @tokenHash;";
        command.Parameters.AddWithValue("@tokenHash", HashToken(token));
        command.ExecuteNonQuery();
    }

    private static UserSession CreateSession(SqliteConnection connection, SqliteTransaction? transaction, int userId, string username)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            INSERT INTO UserSessions (TokenHash, UserId, ExpiresAt)
            VALUES (@tokenHash, @userId, @expiresAt);";
        command.Parameters.AddWithValue("@tokenHash", HashToken(token));
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@expiresAt", DateTime.UtcNow.Add(SessionLifetime).ToString("O"));
        command.ExecuteNonQuery();
        return new UserSession(userId, username, token);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static void ValidateCredentials(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || password is null ||
            username.Trim().Length is < 2 or > 50 ||
            email.Trim().Length is < 5 or > 254 || !email.Contains('@') ||
            password.Length is < 12 or > 200)
        {
            throw new ArgumentException("Kullanıcı adı, e-posta veya parola kurallara uygun değil. Parola en az 12 karakter olmalı.");
        }
    }
}
