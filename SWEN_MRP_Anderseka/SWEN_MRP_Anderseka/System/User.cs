using System.Security.Cryptography;
using System.Text;



namespace MyMediaList.System;

public sealed class User: Atom, IAtom
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private static members                                                                                           //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>In-memory user storage.</summary>
    private static readonly Dictionary<string, User> _Users = new();


    private string? _UserName = null;
    private string? _FullName = null;
    private string? _EMail = null;
    private bool _New;
    private string? _PasswordHash = null;


    public User(Session? session = null)
    {
        _EditingSession = session;
        _New = true;
    }


    public static User? Get(string userName, Session? session = null)
    {
        // TODO: load user from database and return if admin or owner.
        lock(_Users)
        {
            if(_Users.ContainsKey(userName))
            {
                User user = _Users[userName];
                // Allow access if admin or owner
                if(session != null && (session.IsAdmin || session.UserName == userName))
                {
                    user._EditingSession = session;
                    return user;
                }
                else if(session == null)
                {
                    return user;
                }
            }
        }
        return null;
    }


    public string UserName
    {
        get { return _UserName ?? string.Empty; }
        set 
        {
            if(!_New) { throw new InvalidOperationException("User name cannot be changed."); }
            if(string.IsNullOrWhiteSpace(value)) { throw new ArgumentException("User name must not be empty."); }
            
            _UserName = value; 
        }
    }


    internal static string _HashPassword(string userName, string password)
    {
        StringBuilder rval = new();
        foreach(byte i in SHA256.HashData(Encoding.UTF8.GetBytes(userName + password)))
        {
            rval.Append(i.ToString("x2"));
        }
        return rval.ToString();
    }


    internal string? PasswordHash
    {
        get { return _PasswordHash; }
    }


    public string FullName
    {
        get { return _FullName ?? string.Empty; }
        set { _FullName = value; }
    }


    public string EMail
    {
        get { return _EMail ?? string.Empty; }
        set { _EMail = value; }
    }


    public void SetPassword(string password)
    {
        _PasswordHash = _HashPassword(UserName, password);
    }


    public override void Save()
    {
        if(!_New) { _EnsureAdminOrOwner(UserName); }

        // TODO: save user to database
        lock(_Users)
        {
            _Users[UserName] = this;
        }
        _New = false;
        _EndEdit();
    }


    public override void Delete()
    {
        _EnsureAdminOrOwner(UserName);

        // TODO: delete user from database
        lock(_Users)
        {
            if(_Users.ContainsKey(UserName)) { _Users.Remove(UserName); }
        }

        _EndEdit();
    }


    public override void Refresh()
    {
        // TODO: refresh user from database
        lock(_Users)
        {
            if(_Users.ContainsKey(UserName))
            {
                User stored = _Users[UserName];
                _FullName = stored._FullName;
                _EMail = stored._EMail;
                _PasswordHash = stored._PasswordHash;
            }
        }
        _EndEdit();
    }
}
