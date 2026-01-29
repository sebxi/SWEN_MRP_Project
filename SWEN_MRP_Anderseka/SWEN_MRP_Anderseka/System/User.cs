using System;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using MyMediaList.Server;

namespace MyMediaList.System;

public sealed class User : Atom, IAtom
{
    private bool _New;
    private int _UserId;
    private string? _UserName;
    private string? _PasswordHash;

    public int UserId => _UserId;
    public string UserName
    {
        get => _UserName ?? string.Empty;
        set
        {
            if (!_New) throw new InvalidOperationException("Username cannot be changed.");
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Username must not be empty.");
            _UserName = value;
        }
    }

    public string? PasswordHash => _PasswordHash;

    public DateTime CreatedAt { get; private set; }

    public User()
    {
        _New = true;
    }

    public void SetPassword(string password)
    {
        _PasswordHash = _HashPassword(UserName, password);
    }

    internal static string _HashPassword(string userName, string password)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userName + password));
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    public static User? Get(string username)
    {
        using var conn = Database.GetConnection();
        conn.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT user_id, username, password_hash, created_at FROM users WHERE username=@username", conn);
        cmd.Parameters.AddWithValue("username", username);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                _UserId = reader.GetInt32(0),
                _UserName = reader.GetString(1),
                _PasswordHash = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3),
                _New = false
            };
        }

        return null;
    }

    public override void Save()
    {
        using var conn = Database.GetConnection();
        conn.Open();

        if (_New)
        {
            using var cmd = new NpgsqlCommand(
                "INSERT INTO users(username, password_hash) VALUES(@username, @password_hash) RETURNING user_id, created_at", conn);
            cmd.Parameters.AddWithValue("username", UserName);
            cmd.Parameters.AddWithValue("password_hash", PasswordHash ?? "");

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                _UserId = reader.GetInt32(0);
                CreatedAt = reader.GetDateTime(1);
            }

            _New = false;
        }
        else
        {
            using var cmd = new NpgsqlCommand(
                "UPDATE users SET password_hash=@password_hash WHERE username=@username", conn);
            cmd.Parameters.AddWithValue("username", UserName);
            cmd.Parameters.AddWithValue("password_hash", PasswordHash ?? "");
            cmd.ExecuteNonQuery();
        }

        _EndEdit();
    }

    public override void Delete()
    {
        using var conn = Database.GetConnection();
        conn.Open();

        using var cmd = new NpgsqlCommand("DELETE FROM users WHERE username=@username", conn);
        cmd.Parameters.AddWithValue("username", UserName);
        cmd.ExecuteNonQuery();

        _EndEdit();
    }

    public override void Refresh()
    {
        var refreshed = Get(UserName);
        if (refreshed != null)
        {
            _UserId = refreshed.UserId;
            _PasswordHash = refreshed.PasswordHash;
            CreatedAt = refreshed.CreatedAt;
        }
        _EndEdit();
    }

    /// <summary>
    /// Creates a new user and automatically returns a session token.
    /// </summary>
    public static string CreateAndGetToken(string username, string password)
    {
        // Prüfen ob User schon existiert
        var existing = Get(username);
        if (existing != null)
        {
            throw new Exception("User already exists.");
        }

        // Neuen User erstellen
        var user = new User { UserName = username };
        user.SetPassword(password);
        user.Save();

        // Session für neuen User erstellen
        var session = Session.Create(username, password);
        if (session == null)
        {
            throw new Exception("Failed to create session for user.");
        }

        return session.Token;
    }

}
