using System;
using System.Net;
using System.Text.Json.Nodes;
using MyMediaList.Handlers;
using MyMediaList.Server;

namespace MyMediaList.System;

public sealed class UserHandler : Handler, IHandler
{
    public override void Handle(HttpRestEventArgs e)
    {
        if (!e.Path.StartsWith("/users"))
            return;

        // -------------------------
        // POST: User erstellen
        // -------------------------
        if ((e.Path == "/users") && (e.Method == HttpMethod.Post))
        {
            try
            {
                string username = e.Content?["username"]?.GetValue<string>() ?? string.Empty;
                string password = e.Content?["password"]?.GetValue<string>() ?? string.Empty;

                string token = User.CreateAndGetToken(username, password);

                e.Respond(HttpStatusCode.OK, new JsonObject
                {
                    ["success"] = true,
                    ["message"] = "User created.",
                    ["token"] = token
                });

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[UserHandler] Created user {username} with session token {token}");
            }
            catch (Exception ex)
            {
                e.Respond(HttpStatusCode.InternalServerError, new JsonObject
                {
                    ["success"] = false,
                    ["reason"] = ex.Message
                });

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[UserHandler] Exception creating user: {ex.Message}");
            }
        }
        // -------------------------
        // GET: User abrufen
        // -------------------------
        else if (e.Path.StartsWith("/users/") && e.Method == HttpMethod.Get)
        {
            string username = e.Path.Substring("/users/".Length);
            User? user = User.Get(username);

            if (user != null)
            {
                e.Respond(HttpStatusCode.OK, new JsonObject
                {
                    ["user_id"] = user.UserId,
                    ["username"] = user.UserName,
                    ["created_at"] = user.CreatedAt
                });

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"[UserHandler] Retrieved user {user.UserName}");
            }
            else
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject
                {
                    ["success"] = false,
                    ["reason"] = "User not found."
                });

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[UserHandler] User not found: {username}");
            }
        }
        // -------------------------
        // DELETE: User löschen
        // -------------------------
        else if (e.Path.StartsWith("/users/") && e.Method == HttpMethod.Delete)
        {
            string username = e.Path.Substring("/users/".Length);
            User? user = User.Get(username);

            if (user != null)
            {
                try
                {
                    user.Delete();
                    e.Respond(HttpStatusCode.OK, new JsonObject
                    {
                        ["success"] = true,
                        ["message"] = $"User {username} deleted."
                    });

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[UserHandler] Deleted user {username}");
                }
                catch (Exception ex)
                {
                    e.Respond(HttpStatusCode.InternalServerError, new JsonObject
                    {
                        ["success"] = false,
                        ["reason"] = ex.Message
                    });

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[UserHandler] Exception deleting user {username}: {ex.Message}");
                }
            }
            else
            {
                e.Respond(HttpStatusCode.NotFound, new JsonObject
                {
                    ["success"] = false,
                    ["reason"] = "User not found."
                });

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[UserHandler] User not found: {username}");
            }
        }
        // -------------------------
        // Ungültiger Endpoint
        // -------------------------
        else
        {
            e.Respond(HttpStatusCode.BadRequest, new JsonObject
            {
                ["success"] = false,
                ["reason"] = "Invalid user endpoint."
            });

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[UserHandler] Invalid endpoint: {e.Path}");
        }

        e.Responded = true;
    }
}