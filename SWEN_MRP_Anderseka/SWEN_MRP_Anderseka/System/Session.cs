namespace MyMediaList.System;

/// <summary>This class represents a session.</summary>
public sealed class Session
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private constants                                                                                                //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Alphabet.</summary>
    private const string _ALPHABET = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>Session timeout in minutes.</summary>
    private const int TIMEOUT_MINUTES = 30;



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private static members                                                                                           //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Sessions.</summary>
    private static readonly Dictionary<string, Session> _Sessions = new();



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // constructors                                                                                                     //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Creates a new instance of a class.</summary>
    /// <param name="userName">User name.</param>
    /// <param name="password">Password.</param>
    private Session(string userName, string password)
    {
        UserName = userName;
        IsAdmin = (userName == "admin");
        Timestamp = DateTime.UtcNow;

        Token = string.Empty;
        Random rnd = new();
        for(int i = 0; i < 24; i++) { Token += _ALPHABET[rnd.Next(0, 62)]; }
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public properties                                                                                                //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Gets the session token.</summary>
    public string Token { get; }


    /// <summary>Gets the user name of the session owner.</summary>
    public string UserName { get; }


    /// <summary>Gets the session timestamp.</summary>
    public DateTime Timestamp
    {
        get; private set;
    }


    /// <summary>Gets if the session is valid.</summary>
    public bool Valid
    {
        get { return _Sessions.ContainsKey(Token); }
    }


    /// <summary>Gets a value indicating if the session owner has administrative privileges.</summary>
    public bool IsAdmin { get; }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public static methods                                                                                            //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Creates a new session.</summary>
    /// <param name="userName">User name.</param>
    /// <param name="password">Password.</param>
    /// <returns>Returns a session instance, or NULL if user couldn't be logged in.</returns>
    public static Session? Create(string userName, string password)
    {
        // TODO: implement password verification against database
        // Special case for admin user
        if(userName == "admin")
        {
            Session session = new Session(userName, password);
            lock(_Sessions)
            {
                _Sessions[session.Token] = session;
            }
            return session;
        }

        // Verify regular user credentials
        User? user = User.Get(userName);
        if(user != null)
        {
            string expectedHash = User._HashPassword(userName, password);
            if(user.PasswordHash == expectedHash)
            {
                Session session = new Session(userName, password);
                lock(_Sessions)
                {
                    _Sessions[session.Token] = session;
                }
                return session;
            }
        }

        return null;
    }


    /// <summary>Gets a session by its token.</summary>
    /// <param name="token">Session token.</param>
    /// <returns>Returns the session represented by the token, or NULL if there is no session for the token.</returns>
    public static Session? Get(string token)
    {
        Session? rval = null;

        _Cleanup();

        lock(_Sessions)
        {
            if(_Sessions.ContainsKey(token))
            {
                rval = _Sessions[token];
                rval.Timestamp = DateTime.UtcNow;
            }
        }

        return rval;
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public static methods                                                                                            //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Closes all outdated sessions.</summary>
    private static void _Cleanup()
    {
        List<string> toRemove = new();

        lock(_Sessions)
        {
            foreach(KeyValuePair<string, Session> pair in _Sessions)
            {
                if((DateTime.UtcNow - pair.Value.Timestamp).TotalMinutes > TIMEOUT_MINUTES) { toRemove.Add(pair.Key); }
            }
            foreach(string key in toRemove) { _Sessions.Remove(key); }
        }
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public static methods                                                                                            //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Closes the session.</summary>
    public void Close()
    {
        lock(_Sessions)
        {
            if(_Sessions.ContainsKey(Token)) { _Sessions.Remove(Token); }
        }
    }
}
