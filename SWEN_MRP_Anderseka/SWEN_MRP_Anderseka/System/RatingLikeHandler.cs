using System;
using System.Net;
using System.Text.Json.Nodes;
using MyMediaList.Server;
using MyMediaList.Handlers;
using Npgsql;

namespace MyMediaList.System
{
    public sealed class RatingLikeHandler : Handler, IHandler
    {
        private const string BASE = "/rating-likes";

        public override void Handle(HttpRestEventArgs e)
        {
            if (!e.Path.StartsWith(BASE)) return;

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

                // POST /rating-likes -> Like a rating
                if (e.Method == HttpMethod.Post && path == BASE)
                {
                    AddLike(e, session);
                }
                // GET /rating-likes/{ratingId} -> List likes
                else if (e.Method == HttpMethod.Get && parts.Length == 2 && parts[0] == "rating-likes")
                {
                    if (!int.TryParse(parts[1], out int ratingId))
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Invalid rating id."
                        });
                    }
                    else
                    {
                        GetLikes(e, ratingId);
                    }
                }
                // DELETE /rating-likes/{ratingId} -> Remove like
                else if (e.Method == HttpMethod.Delete && parts.Length == 2 && parts[0] == "rating-likes")
                {
                    if (!int.TryParse(parts[1], out int ratingId))
                    {
                        e.Respond(HttpStatusCode.BadRequest, new JsonObject
                        {
                            ["success"] = false,
                            ["reason"] = "Invalid rating id."
                        });
                    }
                    else
                    {
                        RemoveLike(e, session, ratingId);
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

        // ----------------------------------------------
        // Get user_id from username
        // ----------------------------------------------
        private static int GetUserId(string username)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT user_id FROM users WHERE username=@u", conn);
            cmd.Parameters.AddWithValue("u", username);

            var result = cmd.ExecuteScalar();
            if (result == null) throw new Exception("User not found.");
            return Convert.ToInt32(result);
        }

        // ----------------------------------------------
        // Add like
        // ----------------------------------------------
        private static void AddLike(HttpRestEventArgs e, Session session)
        {
            if (!e.Content.TryGetPropertyValue("ratingId", out var node) ||
                !int.TryParse(node?.ToString(), out int ratingId))
            {
                e.Respond(HttpStatusCode.BadRequest, new JsonObject
                {
                    ["success"] = false,
                    ["reason"] = "Invalid or missing ratingId."
                });
                return;
            }

            int userId = GetUserId(session.UserName);
            var like = RatingLike.Add(userId, ratingId);

            e.Respond(HttpStatusCode.OK, new JsonObject
            {
                ["success"] = true,
                ["ratingId"] = like.RatingId,
                ["createdAt"] = like.CreatedAt.ToString("o")
            });
        }

        // ----------------------------------------------
        // Get likes
        // ----------------------------------------------
        private static void GetLikes(HttpRestEventArgs e, int ratingId)
        {
            var list = RatingLike.GetAll(ratingId);
            var arr = new JsonArray();

            foreach (var l in list)
            {
                arr.Add(new JsonObject
                {
                    ["userId"] = l.UserId,
                    ["createdAt"] = l.CreatedAt.ToString("o")
                });
            }

            e.Respond(HttpStatusCode.OK, new JsonObject
            {
                ["success"] = true,
                ["data"] = arr
            });
        }

        // ----------------------------------------------
        // Remove like
        // ----------------------------------------------
        private static void RemoveLike(HttpRestEventArgs e, Session session, int ratingId)
        {
            int userId = GetUserId(session.UserName);
            bool ok = RatingLike.Remove(userId, ratingId);

            e.Respond(ok ? HttpStatusCode.OK : HttpStatusCode.NotFound, new JsonObject
            {
                ["success"] = ok
            });
        }
    }
}