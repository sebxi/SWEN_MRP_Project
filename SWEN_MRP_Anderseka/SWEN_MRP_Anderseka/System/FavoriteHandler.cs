using System;
using System.Net;
using System.Text.Json.Nodes;
using MyMediaList.Server;
using MyMediaList.Handlers;
using Npgsql;

namespace MyMediaList.System
{
    public sealed class FavoriteHandler : Handler, IHandler
    {
        private const string BASE = "/favorites";

        public override void Handle(HttpRestEventArgs e)
        {
            if (!e.Path.StartsWith(BASE)) return;

            // Session prüfen
            var session = e.Session;
            if (session == null)
            {
                e.Respond(HttpStatusCode.Unauthorized, new JsonObject
                {
                    ["success"] = false,
                    ["reason"] = "Login required."
                });
                e.Responded = true;
                return;
            }

            try
            {
                string path = e.Path.TrimEnd('/');
                string[] parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

                // POST /favorites -> Add Favorite
                if (e.Method == HttpMethod.Post && path == BASE)
                {
                    AddFavorite(e, session);
                }
                // GET /favorites -> List all favorites
                else if (e.Method == HttpMethod.Get && path == BASE)
                {
                    GetFavorites(e, session);
                }
                // DELETE /favorites/{mediaId} -> Remove Favorite
                else if (e.Method == HttpMethod.Delete && parts.Length == 2 && parts[0] == "favorites")
                {
                    if (!int.TryParse(parts[1], out int mediaId))
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Invalid media id."
                        });
                    }
                    else
                    {
                        RemoveFavorite(e, session, mediaId);
                    }
                }
                else
                {
                    e.Respond(HttpStatusCode.BadRequest, new JsonObject
                    {
                        ["success"] = false,
                        ["reason"] = "Invalid endpoint or method."
                    });
                }
            }
            catch (Exception ex)
            {
                e.Respond(HttpStatusCode.BadRequest, new JsonObject
                {
                    ["success"] = false,
                    ["reason"] = ex.Message
                });
            }

            e.Responded = true;
        }

        // --------------------------------------------------
        // Hilfsmethode: user_id aus session holen
        // --------------------------------------------------
        private static int GetUserId(string username)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT user_id FROM users WHERE username=@u", conn);
            cmd.Parameters.AddWithValue("u", username);

            var result = cmd.ExecuteScalar();
            if (result == null)
                throw new Exception("User not found.");

            return Convert.ToInt32(result);
        }

        // --------------------------------------------------
        // Add Favorite
        // --------------------------------------------------
        private static void AddFavorite(HttpRestEventArgs e, Session session)
        {
            if (!e.Content.TryGetPropertyValue("mediaId", out var node) ||
                !int.TryParse(node?.ToString(), out int mediaId))
            {
                e.Respond(HttpStatusCode.BadRequest, new JsonObject
                {
                    ["success"] = false,
                    ["reason"] = "Invalid or missing mediaId."
                });
                return;
            }

            int userId = GetUserId(session.UserName);

            var fav = Favorite.Add(userId, mediaId);
            e.Respond(HttpStatusCode.OK, new JsonObject
            {
                ["success"] = true,
                ["mediaId"] = fav.MediaId,
                ["createdAt"] = fav.CreatedAt.ToString("o")
            });
        }

        // --------------------------------------------------
        // Get all favorites
        // --------------------------------------------------
        private static void GetFavorites(HttpRestEventArgs e, Session session)
        {
            int userId = GetUserId(session.UserName);

            var list = Favorite.GetAll(userId);
            var arr = new JsonArray();
            foreach (var f in list)
            {
                arr.Add(new JsonObject
                {
                    ["mediaId"] = f.MediaId,
                    ["createdAt"] = f.CreatedAt.ToString("o")
                });
            }

            e.Respond(HttpStatusCode.OK, new JsonObject
            {
                ["success"] = true,
                ["data"] = arr
            });
        }

        // --------------------------------------------------
        // Remove favorite
        // --------------------------------------------------
        private static void RemoveFavorite(HttpRestEventArgs e, Session session, int mediaId)
        {
            int userId = GetUserId(session.UserName);

            bool ok = Favorite.Remove(userId, mediaId);
            e.Respond(ok ? HttpStatusCode.OK : HttpStatusCode.NotFound, new JsonObject
            {
                ["success"] = ok
            });
        }
    }
}