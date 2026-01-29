using System;
using System.Collections.Generic;
using Npgsql;

namespace MyMediaList.System;

/// <summary>Represents a media entry (movie, series, or game).</summary>
public sealed class MediaEntry
{
    public enum MediaType { Movie, Series, Game }

    public int Id { get; private set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public int ReleaseYear { get; set; }
    public List<string> Genres { get; set; } = new();
    public int AgeRestriction { get; set; }
    public string CreatedByUsername { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private MediaEntry() { }

    // -------------------------
    // CREATE
    // -------------------------
    public static MediaEntry Create(string createdByUsername, string title, string description, MediaType type, int releaseYear, List<string>? genres, int ageRestriction)
    {
        if (string.IsNullOrWhiteSpace(createdByUsername)) throw new ArgumentException("Creator username is required.");
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");

        using var conn = Server.Database.GetConnection();
        conn.Open();

        // --- get creator id ---
        int creatorId;
        using (var cmd = new NpgsqlCommand("SELECT user_id FROM users WHERE username=@username", conn))
        {
            cmd.Parameters.AddWithValue("username", createdByUsername);
            var res = cmd.ExecuteScalar();
            if (res == null) throw new Exception("Creator user not found.");
            creatorId = Convert.ToInt32(res);
        }

        // --- insert media ---
        int mediaId;
        DateTime createdAt;
        using (var cmd = new NpgsqlCommand(
            @"INSERT INTO media_entries (title, description, type, release_year, age_restriction, creator_id)
              VALUES (@title,@desc,@type,@year,@age,@creator) RETURNING media_id, created_at", conn))
        {
            cmd.Parameters.AddWithValue("title", title);
            cmd.Parameters.AddWithValue("desc", description ?? string.Empty);
            cmd.Parameters.AddWithValue("type", type.ToString());
            cmd.Parameters.AddWithValue("year", releaseYear);
            cmd.Parameters.AddWithValue("age", ageRestriction);
            cmd.Parameters.AddWithValue("creator", creatorId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) throw new Exception("Failed to insert media entry.");
            mediaId = reader.GetInt32(0);
            createdAt = reader.GetDateTime(1);
        }

        var entry = new MediaEntry
        {
            Id = mediaId,
            Title = title,
            Description = description,
            Type = type,
            ReleaseYear = releaseYear,
            AgeRestriction = ageRestriction,
            CreatedByUsername = createdByUsername,
            CreatedAt = createdAt
        };

        // --- handle genres ---
        if (genres != null)
        {
            foreach (var g in genres)
            {
                int genreId;
                using (var cmdG = new NpgsqlCommand("INSERT INTO genres(name) VALUES(@name) ON CONFLICT(name) DO UPDATE SET name=EXCLUDED.name RETURNING genre_id", conn))
                {
                    cmdG.Parameters.AddWithValue("name", g);
                    genreId = Convert.ToInt32(cmdG.ExecuteScalar());
                }

                using (var cmdMG = new NpgsqlCommand("INSERT INTO media_genres(media_id, genre_id) VALUES(@mid,@gid) ON CONFLICT DO NOTHING", conn))
                {
                    cmdMG.Parameters.AddWithValue("mid", mediaId);
                    cmdMG.Parameters.AddWithValue("gid", genreId);
                    cmdMG.ExecuteNonQuery();
                }

                entry.Genres.Add(g);
            }
        }

        return entry;
    }

    // -------------------------
    // GET BY ID
    // -------------------------
    public static MediaEntry? Get(int id)
    {
        using var conn = Server.Database.GetConnection();
        conn.Open();

        MediaEntry? entry = null;

        using (var cmd = new NpgsqlCommand(
            @"SELECT m.media_id, m.title, m.description, m.type, m.release_year, m.age_restriction, u.username, m.created_at
              FROM media_entries m
              JOIN users u ON m.creator_id = u.user_id
              WHERE m.media_id=@id", conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                entry = new MediaEntry
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Type = Enum.TryParse<MediaType>(reader.GetString(3), true, out var t) ? t : MediaType.Movie,
                    ReleaseYear = reader.GetInt32(4),
                    AgeRestriction = reader.GetInt32(5),
                    CreatedByUsername = reader.GetString(6),
                    CreatedAt = reader.GetDateTime(7)
                };
            }
        }

        if (entry != null)
        {
            // Load genres
            using var cmdG = new NpgsqlCommand(
                @"SELECT g.name FROM media_genres mg
                  JOIN genres g ON mg.genre_id = g.genre_id
                  WHERE mg.media_id=@id", conn);
            cmdG.Parameters.AddWithValue("id", id);
            using var reader = cmdG.ExecuteReader();
            while (reader.Read())
            {
                entry.Genres.Add(reader.GetString(0));
            }
        }

        return entry;
    }

    // -------------------------
    // GET ALL
    // -------------------------
    public static List<MediaEntry> GetAll()
    {
        var list = new List<MediaEntry>();
        using var conn = Server.Database.GetConnection();
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT m.media_id, m.title, m.description, m.type, m.release_year, m.age_restriction, u.username, m.created_at
              FROM media_entries m
              JOIN users u ON m.creator_id = u.user_id", conn);
        using var reader = cmd.ExecuteReader();
        var ids = new List<int>();
        while (reader.Read())
        {
            ids.Add(reader.GetInt32(0));
        }

        foreach (var id in ids)
        {
            var me = Get(id);
            if (me != null) list.Add(me);
        }

        return list;
    }

    // -------------------------
    // DELETE
    // -------------------------
    public static bool Delete(int id, string deletedBy)
    {
        var entry = Get(id);
        if (entry == null || entry.CreatedByUsername != deletedBy) return false;

        using var conn = Server.Database.GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand("DELETE FROM media_entries WHERE media_id=@id", conn);
        cmd.Parameters.AddWithValue("id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // -------------------------
    // UPDATE
    // -------------------------
    public static MediaEntry? Update(int id, string updatedBy, string title, string description, MediaType type, int releaseYear, List<string>? genres, int ageRestriction)
    {
        var entry = Get(id);
        if (entry == null || entry.CreatedByUsername != updatedBy) return null;

        using var conn = Server.Database.GetConnection();
        conn.Open();

        using (var cmd = new NpgsqlCommand(
            @"UPDATE media_entries
              SET title=@title, description=@desc, type=@type, release_year=@year, age_restriction=@age
              WHERE media_id=@id", conn))
        {
            cmd.Parameters.AddWithValue("title", title);
            cmd.Parameters.AddWithValue("desc", description ?? string.Empty);
            cmd.Parameters.AddWithValue("type", type.ToString());
            cmd.Parameters.AddWithValue("year", releaseYear);
            cmd.Parameters.AddWithValue("age", ageRestriction);
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }

        // Update genres
        using (var cmdDel = new NpgsqlCommand("DELETE FROM media_genres WHERE media_id=@id", conn))
        {
            cmdDel.Parameters.AddWithValue("id", id);
            cmdDel.ExecuteNonQuery();
        }

        if (genres != null)
        {
            foreach (var g in genres)
            {
                int genreId;
                using (var cmdG = new NpgsqlCommand("INSERT INTO genres(name) VALUES(@name) ON CONFLICT(name) DO UPDATE SET name=EXCLUDED.name RETURNING genre_id", conn))
                {
                    cmdG.Parameters.AddWithValue("name", g);
                    genreId = Convert.ToInt32(cmdG.ExecuteScalar());
                }

                using (var cmdMG = new NpgsqlCommand("INSERT INTO media_genres(media_id, genre_id) VALUES(@mid,@gid) ON CONFLICT DO NOTHING", conn))
                {
                    cmdMG.Parameters.AddWithValue("mid", id);
                    cmdMG.Parameters.AddWithValue("gid", genreId);
                    cmdMG.ExecuteNonQuery();
                }
            }
        }

        return Get(id);
    }
}