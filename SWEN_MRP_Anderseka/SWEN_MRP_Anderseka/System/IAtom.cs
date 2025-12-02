namespace MyMediaList.System;

/// <summary>Data objects implement this interface.</summary>
public interface IAtom
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public methods                                                                                                   //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Begins editing the object.</summary>
    /// <param name="session">Session.</param>
    public void BeginEdit(Session session);


    /// <summary>Saves the object.</summary>
    public void Save();


    /// <summary>Deletes the object.</summary>
    public void Delete();


    /// <summary>Refreshes the object.</summary>
    public void Refresh();
}
