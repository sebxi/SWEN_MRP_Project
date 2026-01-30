using System.Net;
using System.Text.Json.Nodes;
using MyMediaList.Server;
using MyMediaList.Handlers;
using Npgsql;

namespace MyMediaList.System
{
    public sealed class RatingHandler : Handler, IHandler
    {
        private const string BASE = "/ratings";

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
                string[] parts = e.Path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

                // POST /ratings
                if (e.Method == HttpMethod.Post && e.Path == BASE)
                {
                    Create(e, session);
                }
                // GET /ratings/{id}
                else if (e.Method == HttpMethod.Get && parts.Length == 2)
                {
                    if (!int.TryParse(parts[1], out int id))
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Invalid rating id."
                        });
                    }
                    else
                    {
                        Get(e, id);
                    }
                }
                // PUT /ratings/{id}
                else if (e.Method == HttpMethod.Put && parts.Length == 2)
                {
                    if (!int.TryParse(parts[1], out int id))
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Invalid rating id."
                        });
                    }
                    else
                    {
                        Update(e, session, id);
                    }
                }
                // DELETE /ratings/{id}
                else if (e.Method == HttpMethod.Delete && parts.Length == 2)
                {
                    if (!int.TryParse(parts[1], out int id))
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Invalid rating id."
                        });
                    }
                    else
                    {
                        Delete(e, session, id);
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

        // --------------------------
        // Helper: Get user id safely
        // --------------------------
        private static int GetUserId(string username)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT user_id FROM users WHERE username=@u", conn);
            cmd.Parameters.AddWithValue("u", username);

            object? result = cmd.ExecuteScalar();
            if (result == null)
                throw new Exception("User not found");

            return Convert.ToInt32(result);
        }

        // --------------------------
        // Create
        // --------------------------
        private void Create(HttpRestEventArgs e, Session session)
        {
            int userId = GetUserId(session.UserName);

            var rating = Rating.Create(
                userId,
                e.Content["mediaId"]!.GetValue<int>(),
                e.Content["score"]!.GetValue<int>(),
                e.Content["comment"]?.GetValue<string>() ?? ""
            );

            e.Respond(HttpStatusCode.OK, new JsonObject
            {
                ["success"] = true,
                ["id"] = rating.Id
            });
        }

        // --------------------------
        // Get
        // --------------------------
        private void Get(HttpRestEventArgs e, int id)
        {
            var r = Rating.Get(id);
            if (r == null)
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false });
                return;
            }

            var json = new JsonObject
            {
                ["id"] = r.Id,
                ["mediaId"] = r.MediaId,
                ["score"] = r.Score
            };

            if (r.IsConfirmed)
                json["comment"] = r.Comment;

            e.Respond(HttpStatusCode.OK, json);
        }

        // --------------------------
        // Update
        // --------------------------
        private void Update(HttpRestEventArgs e, Session session, int id)
        {
            var r = Rating.Get(id);
            if (r == null)
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = "Rating not found." });
                return;
            }

            int userId = GetUserId(session.UserName);
            if (r.UserId != userId)
            {
                e.Respond(HttpStatusCode.Forbidden, new JsonObject { ["success"] = false, ["reason"] = "Not your rating." });
                return;
            }

            r.Update(
                e.Content["score"]!.GetValue<int>(),
                e.Content["comment"]?.GetValue<string>() ?? ""
            );

            e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        // --------------------------
        // Delete
        // --------------------------
        private void Delete(HttpRestEventArgs e, Session session, int id)
        {
            var r = Rating.Get(id);
            if (r == null)
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = "Rating not found." });
                return;
            }

            int userId = GetUserId(session.UserName);
            if (r.UserId != userId)
            {
                e.Respond(HttpStatusCode.Forbidden, new JsonObject { ["success"] = false, ["reason"] = "Not your rating." });
                return;
            }

            r.Delete();
            e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }
    }
}