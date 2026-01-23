using System.Net;
using System.Text.Json.Nodes;
using MyMediaList.Server;
using MyMediaList.Handlers;

namespace MyMediaList.System
{
    public sealed class RatingHandler : Handler, IHandler
    {
        private const string RATINGS_BASE = "/ratings";
        private const string RATINGS_WITH_ID = "/ratings/";

        public override void Handle(HttpRestEventArgs e)
        {
            if (!e.Path.StartsWith(RATINGS_BASE))
            {
                return; // Not for this handler
            }

            try
            {
                if (e.Path == RATINGS_BASE && e.Method == HttpMethod.Post)
                {
                    HandleCreate(e);
                }
                else if (e.Method == HttpMethod.Get && e.Path.StartsWith(RATINGS_WITH_ID))
                {
                    HandleGet(e);
                }
                else if (e.Method == HttpMethod.Put && e.Path.StartsWith(RATINGS_WITH_ID))
                {
                    HandleUpdate(e);
                }
                else if (e.Method == HttpMethod.Delete && e.Path.StartsWith(RATINGS_WITH_ID))
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
            string idStr = e.Path.Substring(RATINGS_WITH_ID.Length);

            if (!int.TryParse(idStr, out int id))
            {
                e.Respond(HttpStatusCode.BadRequest, new JsonObject { ["success"] = false, ["reason"] = "Invalid rating id." });
                return;
            }

            var rating = Rating.Get(id);

            if (rating == null)
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = "Rating not found." });
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
            string idStr = e.Path.Substring(RATINGS_WITH_ID.Length);

            if (!int.TryParse(idStr, out int id))
            {
                e.Respond(HttpStatusCode.BadRequest, new JsonObject { ["success"] = false, ["reason"] = "Invalid rating id." });
                return;
            }

            var rating = Rating.Get(id);
            if (rating == null)
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = "Rating not found." });
                return;
            }

            rating.UserName = e.Content?["username"]?.GetValue<string>() ?? rating.UserName;
            rating.MediaId = e.Content?["mediaId"]?.GetValue<int>() ?? rating.MediaId;
            rating.Value = e.Content?["value"]?.GetValue<int>() ?? rating.Value;
            rating.Comment = e.Content?["comment"]?.GetValue<string>() ?? rating.Comment;

            rating.Save();

            e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["message"] = "Rating updated." });
        }

        private void HandleDelete(HttpRestEventArgs e)
        {
            string idStr = e.Path.Substring(RATINGS_WITH_ID.Length);

            if (!int.TryParse(idStr, out int id))
            {
                e.Respond(HttpStatusCode.BadRequest, new JsonObject { ["success"] = false, ["reason"] = "Invalid rating id." });
                return;
            }

            var rating = Rating.Get(id);
            if (rating == null)
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject { ["success"] = false, ["reason"] = "Rating not found." });
                return;
            }

            rating.Delete();

            e.Respond(HttpStatusCode.OK, new JsonObject { ["success"] = true, ["message"] = "Rating deleted." });
        }
    }
}