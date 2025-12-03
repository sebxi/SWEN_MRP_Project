using System.Net;
using System.Text.Json.Nodes;
using MyMediaList.Server;

namespace MyMediaList.System
{
    public sealed class RatingHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs e)
        {
            if (!e.Path.StartsWith("/ratings"))
            {
                return; // Not for this handler
            }

            try
            {
                if (e.Path == "/ratings" && e.Method == HttpMethod.Post)
                {
                    HandleCreate(e);
                }
                else if (e.Method == HttpMethod.Get && e.Path.StartsWith("/ratings/"))
                {
                    HandleGet(e);
                }
                else if (e.Method == HttpMethod.Put && e.Path.StartsWith("/ratings/"))
                {
                    HandleUpdate(e);
                }
                else if (e.Method == HttpMethod.Delete && e.Path.StartsWith("/ratings/"))
                {
                    HandleDelete(e);
                }
                else
                {
                    e.Respond(HttpStatusCode.BadRequest, new JsonObject {
                        ["success"] = false,
                        ["reason"] = "Invalid ratings endpoint."
                    });

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{nameof(RatingHandler)}] Invalid endpoint.");
                }
            }
            catch (Exception ex)
            {
                e.Respond(HttpStatusCode.InternalServerError, new JsonObject {
                    ["success"] = false,
                    ["reason"] = ex.Message
                });

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{nameof(RatingHandler)}] Exception: {ex.Message}");
            }

            e.Responded = true;
        }

        private void HandleCreate(HttpRestEventArgs e)
        {
            Rating rating = new()
            {
                UserName = e.Content?["username"]?.GetValue<string>() ?? string.Empty,
                MediaId = e.Content?["mediaId"]?.GetValue<int>() ?? 0,
                Value = e.Content?["value"]?.GetValue<int>() ?? 0,
                Comment = e.Content?["comment"]?.GetValue<string>() ?? string.Empty
            };

            rating.Save();

            e.Respond(HttpStatusCode.OK, new JsonObject {
                ["success"] = true,
                ["message"] = "Rating created.",
                ["id"] = rating.Id
            });

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[{nameof(RatingHandler)}] Created rating {rating.Id}.");
        }

        private void HandleGet(HttpRestEventArgs e)
        {
            string idStr = e.Path.Substring("/ratings/".Length);

            if (!int.TryParse(idStr, out int id))
            {
                e.Respond(HttpStatusCode.BadRequest, new { success = false, reason = "Invalid rating id." });
                return;
            }

            var rating = Rating.Get(id);

            if (rating == null)
            {
                e.Respond(HttpStatusCode.NotFound, new { success = false, reason = "Rating not found." });
                return;
            }

            e.Respond(HttpStatusCode.OK, new JsonObject {
                ["id"] = rating.Id,
                ["username"] = rating.UserName,
                ["mediaId"] = rating.MediaId,
                ["value"] = rating.Value,
                ["comment"] = rating.Comment
            });
        }

        private void HandleUpdate(HttpRestEventArgs e)
        {
            string idStr = e.Path.Substring("/ratings/".Length);

            if (!int.TryParse(idStr, out int id))
            {
                e.Respond(HttpStatusCode.BadRequest, new { success = false, reason = "Invalid rating id." });
                return;
            }

            var rating = Rating.Get(id);
            if (rating == null)
            {
                e.Respond(HttpStatusCode.NotFound, new { success = false, reason = "Rating not found." });
                return;
            }

            rating.UserName = e.Content?["username"]?.GetValue<string>() ?? rating.UserName;
            rating.MediaId = e.Content?["mediaId"]?.GetValue<int>() ?? rating.MediaId;
            rating.Value = e.Content?["value"]?.GetValue<int>() ?? rating.Value;
            rating.Comment = e.Content?["comment"]?.GetValue<string>() ?? rating.Comment;

            rating.Save();

            e.Respond(HttpStatusCode.OK, new { success = true, message = "Rating updated." });
        }

        private void HandleDelete(HttpRestEventArgs e)
        {
            string idStr = e.Path.Substring("/ratings/".Length);

            if (!int.TryParse(idStr, out int id))
            {
                e.Respond(HttpStatusCode.BadRequest, new { success = false, reason = "Invalid rating id." });
                return;
            }

            var rating = Rating.Get(id);
            if (rating == null)
            {
                e.Respond(HttpStatusCode.NotFound, new { success = false, reason = "Rating not found." });
                return;
            }

            rating.Delete();

            e.Respond(HttpStatusCode.OK, new { success = true, message = "Rating deleted." });
        }
    }
}