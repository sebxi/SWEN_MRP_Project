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
            string[] parts = e.Path.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            // /api/media
            if (parts.Length == 2 && parts[0] == "api" && parts[1] == "media")
            {
                if (e.Method == HttpMethod.Get)
                {
                    var arr = new JsonArray();
                    foreach (var me in MediaEntry.GetAll())
                        arr.Add(MediaToJson(me));

                    e.Respond(HttpStatusCode.OK, new JsonObject
                    {
                        ["success"] = true,
                        ["data"] = arr
                    });
                }
                else if (e.Method == HttpMethod.Post)
                {
                    var session = e.Session;
                    if (session == null)
                    {
                        e.Respond(HttpStatusCode.Unauthorized, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Bearer token required to create media."
                        });
                        e.Responded = true;
                        return;
                    }

                    string creator = session.UserName;

                    string title = e.Content["title"]?.GetValue<string>() ?? string.Empty;
                    string description = e.Content["description"]?.GetValue<string>() ?? string.Empty;
                    string typeStr = e.Content["type"]?.GetValue<string>() ?? "Movie";
                    int releaseYear = e.Content["releaseYear"]?.GetValue<int>() ?? 0;
                    int ageRestriction = e.Content["ageRestriction"]?.GetValue<int>() ?? 0;

                    List<string> genres = new();
                    if (e.Content["genres"] is JsonArray ja)
                    {
                        foreach (var x in ja)
                            if (x != null) genres.Add(x.GetValue<string>() ?? string.Empty);
                    }
                    else if (!string.IsNullOrWhiteSpace(e.Content["genres"]?.GetValue<string>()))
                    {
                        genres.AddRange(e.Content["genres"].GetValue<string>()
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    }

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Title is required."
                        });
                        return;
                    }

                    var type = ParseMediaType(typeStr);
                    var entry = MediaEntry.Create(creator, title, description, type, releaseYear, genres, ageRestriction);

                    e.Respond(HttpStatusCode.Created, new JsonObject
                    {
                        ["success"] = true,
                        ["data"] = MediaToJson(entry)
                    });
                }
                else
                {
                    e.Respond(HttpStatusCode.MethodNotAllowed, new JsonObject
                    {
                        ["success"] = false,
                        ["reason"] = "Method not allowed on collection."
                    });
                }
            }
            // /api/media/{id}
            else if (parts.Length == 3 && parts[0] == "api" && parts[1] == "media")
            {
                if (!int.TryParse(parts[2], out int id))
                {
                    e.Respond(HttpStatusCode.BadRequest, new JsonObject { ["success"] = false, ["reason"] = "Invalid id." });
                    return;
                }

                if (e.Method == HttpMethod.Get)
                {
                    var entry = MediaEntry.Get(id);
                    if (entry == null)
                        e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = "Not found." });
                    else
                        e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["data"] = MediaToJson(entry) });
                }
                else if (e.Method == HttpMethod.Put || e.Method == HttpMethod.Delete)
                {
                    var session = e.Session;
                    if (session == null)
                    {
                        e.Respond(HttpStatusCode.Unauthorized, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Bearer token required to modify media."
                        });
                        e.Responded = true;
                        return;
                    }

                    var existing = MediaEntry.Get(id);
                    if (existing == null)
                    {
                        e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = "Media not found." });
                        return;
                    }

                    if (existing.CreatedByUsername != session.UserName)
                    {
                        e.Respond(HttpStatusCode.Forbidden, new JsonObject { ["success"] = false, ["reason"] = "You cannot modify media of other users." });
                        return;
                    }

                    if (e.Method == HttpMethod.Put)
                    {
                        string title = e.Content["title"]?.GetValue<string>() ?? existing.Title;
                        string description = e.Content["description"]?.GetValue<string>() ?? existing.Description;
                        string typeStr = e.Content["type"]?.GetValue<string>() ?? existing.Type.ToString();
                        int releaseYear = e.Content["releaseYear"]?.GetValue<int>() ?? existing.ReleaseYear;
                        int ageRestriction = e.Content["ageRestriction"]?.GetValue<int>() ?? existing.AgeRestriction;

                        List<string> genres = new();
                        if (e.Content["genres"] is JsonArray ja)
                        {
                            foreach (var x in ja) if (x != null) genres.Add(x.GetValue<string>() ?? string.Empty);
                        }
                        else if (!string.IsNullOrWhiteSpace(e.Content["genres"]?.GetValue<string>()))
                        {
                            genres.AddRange(e.Content["genres"].GetValue<string>().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        }

                        var updated = MediaEntry.Update(id, session.UserName, title, description, ParseMediaType(typeStr), releaseYear, genres, ageRestriction);
                        e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["data"] = MediaToJson(updated!) });
                    }
                    else // DELETE
                    {
                        bool ok = MediaEntry.Delete(id, session.UserName);
                        e.Respond(ok ? HttpStatusCode.OK : HttpStatusCode.NotFound, new JsonObject { ["success"] = ok });
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.MethodNotAllowed, new JsonObject { ["success"] = false, ["reason"] = "Method not allowed on resource." });
                }
            }
            else
            {
                e.Respond(HttpStatusCode.BadRequest, new JsonObject { ["success"] = false, ["reason"] = "Invalid media endpoint." });
            }
        }
        catch (Exception ex)
        {
            e.Respond(HttpStatusCode.InternalServerError, new JsonObject { ["success"] = false, ["reason"] = ex.Message });
        }

        e.Responded = true;
    }

    private static MediaEntry.MediaType ParseMediaType(string s) =>
        Enum.TryParse<MediaEntry.MediaType>(s, true, out var t) ? t : MediaEntry.MediaType.Movie;

    private static JsonObject MediaToJson(MediaEntry m)
    {
        var jo = new JsonObject
        {
            ["id"] = m.Id,
            ["title"] = m.Title,
            ["description"] = m.Description,
            ["type"] = m.Type.ToString(),
            ["releaseYear"] = m.ReleaseYear,
            ["ageRestriction"] = m.AgeRestriction,
            ["createdBy"] = m.CreatedByUsername,
            ["createdAt"] = m.CreatedAt.ToString("o")
        };

        var genres = new JsonArray();
        foreach (var g in m.Genres) genres.Add(g);
        jo["genres"] = genres;

        return jo;
    }
}
