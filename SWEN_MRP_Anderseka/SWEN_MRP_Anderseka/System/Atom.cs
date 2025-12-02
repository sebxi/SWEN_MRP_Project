namespace MyMediaList.System;

/// <summary>This class provides a base implementation for data objects.</summary>
public abstract class Atom: IAtom
{
    protected Session? _EditingSession = null;


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // protected methods                                                                                                //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Verifies a session.</summary>
    /// <param name="session">Session.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the session could not be verified.</exception>
    protected void _VerifySession(Session? session = null)
    {
        if(session is not null) { _EditingSession = session; }
        if(_EditingSession is null || !_EditingSession.Valid) { throw new UnauthorizedAccessException("Invalid session."); }
    }


    /// <summary>Ends editing the object.</summary>
    protected void _EndEdit()
    {
        _EditingSession = null;
    }


    /// <summary>Checks if the session has administrative privileges.</summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when the session doesn't have access.</exception>
    protected void _EnsureAdmin()
    {
        _VerifySession();
        if(!_EditingSession!.IsAdmin) { throw new UnauthorizedAccessException("Admin privileges required."); }
    }


    /// <summary>Checks if the session has administrative privileges or represents the objevt owner.</summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when the session doesn't have access.</exception>
    protected void _EnsureAdminOrOwner(string owner)
    {
        _VerifySession();
        if(!(_EditingSession!.IsAdmin || (_EditingSession.UserName == owner)))
        {
            throw new UnauthorizedAccessException("Admin or owner privileges required.");
        }
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // [interface] IAtom                                                                                                //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Begins editing the object.</summary>
    /// <param name="session">Session.</param>
    public virtual void BeginEdit(Session session)
    {
        _VerifySession(session);
    }


    /// <summary>Saves the object.</summary>
    public abstract void Save();


    /// <summary>Deletes the object.</summary>
    public abstract void Delete();


    /// <summary>Refreshes the object.</summary>
    public abstract void Refresh();
}
