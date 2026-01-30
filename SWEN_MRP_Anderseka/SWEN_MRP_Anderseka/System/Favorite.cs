using System;
using System.Collections.Generic;
using Npgsql;
using MyMediaList.Server;

namespace MyMediaList.System
{
    public sealed class Favorite
    {
        public int UserId { get; private set; }
        public int MediaId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Favorite() { }

        // --------------------------
        // ADD FAVORITE
        // --------------------------
        public static Favorite Add(int userId, int mediaId)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            // Prüfen ob Favorite schon existiert
            using (var check = new NpgsqlCommand(
                "SELECT 1 FROM favorites WHERE user_id=@u AND media_id=@m", conn))
            {
                check.Parameters.AddWithValue("u", userId);
                check.Parameters.AddWithValue("m", mediaId);
                if (check.ExecuteScalar() != null)
                    throw new Exception("Media already in favorites.");
            }

            // Einfügen
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO favorites (user_id, media_id)
                  VALUES (@u, @m)
                  RETURNING created_at", conn);
            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("m", mediaId);

            var createdAt = (DateTime)cmd.ExecuteScalar()!;

            return new Favorite
            {
                UserId = userId,
                MediaId = mediaId,
                CreatedAt = createdAt
            };
        }

        // --------------------------
        // GET ALL FAVORITES FOR USER
        // --------------------------
        public static List<Favorite> GetAll(int userId)
        {
            var list = new List<Favorite>();
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT media_id, created_at FROM favorites WHERE user_id=@u ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("u", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Favorite
                {
                    UserId = userId,
                    MediaId = r.GetInt32(0),
                    CreatedAt = r.GetDateTime(1)
                });
            }

            return list;
        }

        // --------------------------
        // DELETE FAVORITE
        // --------------------------
        public static bool Remove(int userId, int mediaId)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "DELETE FROM favorites WHERE user_id=@u AND media_id=@m", conn);
            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("m", mediaId);

            int affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }
    }
}