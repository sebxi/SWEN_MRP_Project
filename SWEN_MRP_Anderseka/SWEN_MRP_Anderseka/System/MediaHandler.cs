using System;
using System.Net;
using System.Text.Json.Nodes;
using System.Collections.Generic;

using MyMediaList.Handlers;
using MyMediaList.Server;


namespace MyMediaList.System;

/// <summary>Handler for media CRUD endpoints under /api/media</summary>
public sealed class MediaHandler : Handler, IHandler
{
    public override void Handle(HttpRestEventArgs e)
    {
        if (!e.Path.StartsWith("/api/media")) return;

        try
        {
            // route: /api/media or /api/media/{id}
            string[] parts = e.Path.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            // /api/media -> parts = ["api","media"]
            if (parts.Length == 2 && parts[0] == "api" && parts[1] == "media")
            {
                if (e.Method == HttpMethod.Get)
                {
                    // list all
                    var arr = new JsonArray();
                    foreach (var me in MediaEntry.GetAll())
                    {
                        arr.Add(MediaToJson(me));
                    }
                    e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["data"] = arr });
                }
                else if (e.Method == HttpMethod.Post)
                {
                    // create - allow without token; use provided createdBy or 'anonymous'
                    var session = e.Session;
                    string creator = session?.UserName ?? e.Content["createdBy"]?.GetValue<string>() ?? "anonymous";

                    // read fields
                    string title = e.Content["title"]?.GetValue<string>() ?? string.Empty;
                    string description = e.Content["description"]?.GetValue<string>() ?? string.Empty;
                    string typeStr = e.Content["type"]?.GetValue<string>() ?? "Movie";
                    int releaseYear = e.Content["releaseYear"]?.GetValue<int>() ?? 0;
                    JsonNode? genresNode = e.Content["genres"];
                    List<string> genres = new();
                    if (genresNode is JsonArray ja)
                    {
                        foreach (var x in ja)
                        {
                            if (x != null) genres.Add(x.GetValue<string>() ?? string.Empty);
                        }
                    }
                    else if (e.Content["genres"] != null)
                    {
                        // maybe comma separated
                        string g = e.Content["genres"]?.GetValue<string>() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(g)) genres.AddRange(g.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    }
                    int ageRestriction = e.Content["ageRestriction"]?.GetValue<int>() ?? 0;

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject() { ["success"] = false, ["reason"] = "Title is required." });
                    }
                    else
                    {
                        MediaEntry.MediaType type = ParseMediaType(typeStr);
                        var entry = MediaEntry.Create(creator, title, description, type, releaseYear, genres, ageRestriction);

                        e.Respond(HttpStatusCode.Created, new JsonObject() { ["success"] = true, ["data"] = MediaToJson(entry) });
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.MethodNotAllowed, new JsonObject() { ["success"] = false, ["reason"] = "Method not allowed on collection." });
                }
            }
            else if (parts.Length == 3 && parts[0] == "api" && parts[1] == "media")
            {
                // /api/media/{id}
                if (!int.TryParse(parts[2], out int id))
                {
                    e.Respond(HttpStatusCode.BadRequest, new JsonObject() { ["success"] = false, ["reason"] = "Invalid id." });
                }
                else if (e.Method == HttpMethod.Get)
                {
                    var entry = MediaEntry.Get(id);
                    if (entry == null)
                    {
                        e.Respond(HttpStatusCode.NotFound, new JsonObject() { ["success"] = false, ["reason"] = "Not found." });
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["data"] = MediaToJson(entry) });
                    }
                }
                else if (e.Method == HttpMethod.Put)
                {
                    // allow update without token; use session username or provided 'updatedBy' or 'anonymous'
                    var session = e.Session;
                    string updater = session?.UserName ?? e.Content["updatedBy"]?.GetValue<string>() ?? "anonymous";

                    string title = e.Content["title"]?.GetValue<string>() ?? string.Empty;
                    string description = e.Content["description"]?.GetValue<string>() ?? string.Empty;
                    string typeStr = e.Content["type"]?.GetValue<string>() ?? "Movie";
                    int releaseYear = e.Content["releaseYear"]?.GetValue<int>() ?? 0;
                    JsonNode? genresNode = e.Content["genres"];
                    List<string> genres = new();
                    if (genresNode is JsonArray ja)
                    {
                        foreach (var x in ja)
                        {
                            if (x != null) genres.Add(x.GetValue<string>() ?? string.Empty);
                        }
                    }
                    else if (e.Content["genres"] != null)
                    {
                        string g = e.Content["genres"]?.GetValue<string>() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(g)) genres.AddRange(g.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    }
                    int ageRestriction = e.Content["ageRestriction"]?.GetValue<int>() ?? 0;

                    MediaEntry.MediaType type = ParseMediaType(typeStr);
                    var updated = MediaEntry.Update(id, updater, title, description, type, releaseYear, genres, ageRestriction);
                    if (updated == null)
                    {
                        e.Respond(HttpStatusCode.NotFound, new JsonObject() { ["success"] = false, ["reason"] = "Not found." });
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true, ["data"] = MediaToJson(updated) });
                    }
                }
                else if (e.Method == HttpMethod.Delete)
                {
                    // allow delete without token; use session username or provided 'deletedBy' or 'anonymous'
                    var session = e.Session;
                    string deletedBy = session?.UserName ?? e.Content["deletedBy"]?.GetValue<string>() ?? "anonymous";

                    bool ok = MediaEntry.Delete(id, deletedBy);
                    if (!ok)
                    {
                        e.Respond(HttpStatusCode.NotFound, new JsonObject() { ["success"] = false, ["reason"] = "Not found." });
                    }
                    else
                    {
                        e.Respond(HttpStatusCode.OK, new JsonObject() { ["success"] = true });
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.MethodNotAllowed, new JsonObject() { ["success"] = false, ["reason"] = "Method not allowed on resource." });
                }
            }
            else
            {
                e.Respond(HttpStatusCode.BadRequest, new JsonObject() { ["success"] = false, ["reason"] = "Invalid media endpoint." });
            }
        }
        catch (Exception ex)
        {
            e.Respond(HttpStatusCode.InternalServerError, new JsonObject() { ["success"] = false, ["reason"] = ex.Message });
        }

        e.Responded = true;
    }

    private static MediaEntry.MediaType ParseMediaType(string s)
    {
        if (Enum.TryParse<MediaEntry.MediaType>(s, true, out var t)) return t;
        return MediaEntry.MediaType.Movie;
    }

    private static JsonObject MediaToJson(MediaEntry m)
    {
        var jo = new JsonObject();
        jo["id"] = m.Id;
        jo["title"] = m.Title;
        jo["description"] = m.Description;
        jo["type"] = m.Type.ToString();
        jo["releaseYear"] = m.ReleaseYear;
        var genres = new JsonArray();
        foreach (var g in m.Genres) genres.Add(g);
        jo["genres"] = genres;
        jo["ageRestriction"] = m.AgeRestriction;
        jo["createdBy"] = m.CreatedByUsername;
        jo["createdAt"] = m.CreatedAt.ToString("o");
        return jo;
    }
}
