namespace MyMediaList.System;

/// <summary>Represents a media entry (movie, series, or game).</summary>
public sealed class MediaEntry
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public enums                                                                                                     //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Type of media.</summary>
    public enum MediaType
    {
        Movie,
        Series,
        Game
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private static members                                                                                           //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>In-memory media entry storage.</summary>
    private static readonly Dictionary<int, MediaEntry> _MediaEntries = new();

    /// <summary>Counter for generating unique IDs.</summary>
    private static int _NextId = 1;



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public properties                                                                                                //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Gets the unique identifier for this media entry.</summary>
    public int Id { get; }

    /// <summary>Gets or sets the title of the media entry.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the media entry.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of media.</summary>
    public MediaType Type { get; set; }

    /// <summary>Gets or sets the release year.</summary>
    public int ReleaseYear { get; set; }

    /// <summary>Gets or sets the list of genres.</summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>Gets or sets the age restriction (0, 6, 12, 16, 18).</summary>
    public int AgeRestriction { get; set; }

    /// <summary>Gets the username of the user who created this media entry.</summary>
    public string CreatedByUsername { get; }

    /// <summary>Gets the timestamp when this media entry was created.</summary>
    public DateTime CreatedAt { get; }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // constructors                                                                                                     //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Creates a new media entry.</summary>
    /// <param name="createdByUsername">Username of the creator.</param>
    public MediaEntry(string createdByUsername)
    {
        Id = _NextId++;
        CreatedByUsername = createdByUsername;
        CreatedAt = DateTime.UtcNow;
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public static methods                                                                                            //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>Creates and saves a new media entry.</summary>
    /// <param name="createdByUsername">Username of the creator.</param>
    /// <param name="title">Title of the media entry.</param>
    /// <param name="description">Description of the media entry.</param>
    /// <param name="type">Type of media.</param>
    /// <param name="releaseYear">Release year.</param>
    /// <param name="genres">List of genres.</param>
    /// <param name="ageRestriction">Age restriction (0, 6, 12, 16, 18).</param>
    /// <returns>The created media entry.</returns>
    public static MediaEntry Create(string createdByUsername, string title, string description, MediaType type, int releaseYear, List<string> genres, int ageRestriction)
    {
        if (string.IsNullOrWhiteSpace(createdByUsername)) { throw new ArgumentException("Creator username must not be empty."); }
        if (string.IsNullOrWhiteSpace(title)) { throw new ArgumentException("Title must not be empty."); }

        MediaEntry entry = new(createdByUsername)
        {
            Title = title,
            Description = description,
            Type = type,
            ReleaseYear = releaseYear,
            Genres = genres ?? new(),
            AgeRestriction = ageRestriction
        };

        // TODO: save media entry to database
        lock (_MediaEntries)
        {
            _MediaEntries[entry.Id] = entry;
        }

        return entry;
    }


    /// <summary>Gets a media entry by ID.</summary>
    /// <param name="id">The ID of the media entry.</param>
    /// <returns>The media entry, or null if not found.</returns>
    public static MediaEntry? Get(int id)
    {
        // TODO: load media entry from database
        lock (_MediaEntries)
        {
            if (_MediaEntries.ContainsKey(id))
            {
                return _MediaEntries[id];
            }
        }
        return null;
    }


    /// <summary>Gets all media entries.</summary>
    /// <returns>List of all media entries.</returns>
    public static List<MediaEntry> GetAll()
    {
        // TODO: load all media entries from database
        lock (_MediaEntries)
        {
            return new List<MediaEntry>(_MediaEntries.Values);
        }
    }


    /// <summary>Updates a media entry. Only the creator can update it.</summary>
    /// <param name="id">The ID of the media entry.</param>
    /// <param name="updatedBy">Username of the user requesting the update.</param>
    /// <param name="title">New title.</param>
    /// <param name="description">New description.</param>
    /// <param name="type">New media type.</param>
    /// <param name="releaseYear">New release year.</param>
    /// <param name="genres">New genres.</param>
    /// <param name="ageRestriction">New age restriction.</param>
    /// <returns>The updated media entry, or null if not found or unauthorized.</returns>
    public static MediaEntry? Update(int id, string updatedBy, string title, string description, MediaType type, int releaseYear, List<string> genres, int ageRestriction)
    {
        lock (_MediaEntries)
        {
            if (!_MediaEntries.ContainsKey(id)) { return null; }

            MediaEntry entry = _MediaEntries[id];

            // In future, enforce creator-only updates by checking session username against entry.CreatedByUsername.

            entry.Title = title;
            entry.Description = description;
            entry.Type = type;
            entry.ReleaseYear = releaseYear;
            entry.Genres = genres ?? new();
            entry.AgeRestriction = ageRestriction;

            // TODO: update media entry in database
            return entry;
        }
    }


    /// <summary>Deletes a media entry. Only the creator can delete it.</summary>
    /// <param name="id">The ID of the media entry.</param>
    /// <param name="deletedBy">Username of the user requesting the deletion.</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized.</returns>
    public static bool Delete(int id, string deletedBy)
    {
        lock (_MediaEntries)
        {
            if (!_MediaEntries.ContainsKey(id)) { return false; }

            MediaEntry entry = _MediaEntries[id];

            // In future, enforce creator-only deletes by checking session username against entry.CreatedByUsername.
            _MediaEntries.Remove(id);

            // TODO: delete media entry from database
            return true;
        }
    }
}
