using MyMediaList.Server;
using System;
using System.Collections.Generic;

namespace MyMediaList.System;

/// <summary>This class represents a session.</summary>
public sealed class Session
{
    // ------------------------------------------------------------------------------------------------------------------
    // private constants
    // ------------------------------------------------------------------------------------------------------------------
    
    private const string _ALPHABET = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int TIMEOUT_MINUTES = 30;

    // ------------------------------------------------------------------------------------------------------------------
    // private static members
    // ------------------------------------------------------------------------------------------------------------------
    
    private static readonly Dictionary<string, Session> _Sessions = new();

    // ------------------------------------------------------------------------------------------------------------------
    // constructors
    // ------------------------------------------------------------------------------------------------------------------
    
    private Session(string userName)
    {
        UserName = userName;
        IsAdmin = (userName == "admin");
        Timestamp = DateTime.UtcNow;

        Token = string.Empty;
        Random rnd = new();
        for (int i = 0; i < 24; i++)
        {
            Token += _ALPHABET[rnd.Next(0, _ALPHABET.Length)];
        }
    }

    // ------------------------------------------------------------------------------------------------------------------
    // public properties
    // ------------------------------------------------------------------------------------------------------------------
    
    public string Token { get; }
    public string UserName { get; }
    public DateTime Timestamp { get; private set; }
    public bool Valid => _Sessions.ContainsKey(Token);
    public bool IsAdmin { get; }

    // ------------------------------------------------------------------------------------------------------------------
    // public static methods
    // ------------------------------------------------------------------------------------------------------------------
    
    /// <summary>Creates a new session if credentials are correct.</summary>
    public static Session? Create(string userName, string password)
    {
        // Admin special case (optional)
        if (userName == "admin")
        {
            Session session = new Session(userName);
            lock (_Sessions)
            {
                _Sessions[session.Token] = session;
            }
            return session;
        }

        // Verify user credentials against database
        User? user = User.Get(userName);
        if (user != null)
        {
            string expectedHash = User._HashPassword(userName, password);
            if (user.PasswordHash == expectedHash)
            {
                Session session = new Session(userName);
                lock (_Sessions)
                {
                    _Sessions[session.Token] = session;
                }
                return session;
            }
        }

        return null; // login failed
    }

    /// <summary>Gets a session by token.</summary>
    public static Session? Get(string token)
    {
        _Cleanup();
        lock (_Sessions)
        {
            if (_Sessions.ContainsKey(token))
            {
                Session s = _Sessions[token];
                s.Timestamp = DateTime.UtcNow;
                return s;
            }
        }
        return null;
    }

    /// <summary>Closes the session.</summary>
    public void Close()
    {
        lock (_Sessions)
        {
            if (_Sessions.ContainsKey(Token))
            {
                _Sessions.Remove(Token);
            }
        }
    }

    // ------------------------------------------------------------------------------------------------------------------
    // private static methods
    // ------------------------------------------------------------------------------------------------------------------
    
    /// <summary>Removes all expired sessions.</summary>
    private static void _Cleanup()
    {
        List<string> toRemove = new();
        lock (_Sessions)
        {
            foreach (var pair in _Sessions)
            {
                if ((DateTime.UtcNow - pair.Value.Timestamp).TotalMinutes > TIMEOUT_MINUTES)
                {
                    toRemove.Add(pair.Key);
                }
            }
            foreach (var key in toRemove)
            {
                _Sessions.Remove(key);
            }
        }
    }
}