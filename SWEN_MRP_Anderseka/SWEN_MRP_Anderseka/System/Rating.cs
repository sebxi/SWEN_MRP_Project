using System;
using Npgsql;
using MyMediaList.Server;

namespace MyMediaList.System
{
    public sealed class Rating
    {
        public int Id { get; private set; }
        public int UserId { get; private set; }
        public int MediaId { get; private set; }
        public int Score { get; private set; }
        public string Comment { get; private set; } = string.Empty;
        public bool IsConfirmed { get; private set; }

        private Rating() { }

        // -------------------------
        // CREATE
        // -------------------------
        public static Rating Create(int userId, int mediaId, int score, string comment)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            // one rating per user per media (CODE-handled)
            using (var check = new NpgsqlCommand(
                "SELECT rating_id FROM ratings WHERE user_id=@u AND media_id=@m", conn))
            {
                check.Parameters.AddWithValue("u", userId);
                check.Parameters.AddWithValue("m", mediaId);
                if (check.ExecuteScalar() != null)
                    throw new Exception("User already rated this media.");
            }

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO ratings (user_id, media_id, score, comment)
                  VALUES (@u,@m,@s,@c)
                  RETURNING rating_id, is_confirmed", conn);

            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("m", mediaId);
            cmd.Parameters.AddWithValue("s", score);
            cmd.Parameters.AddWithValue("c", comment ?? string.Empty);

            using var r = cmd.ExecuteReader();
            r.Read();

            return new Rating
            {
                Id = r.GetInt32(0),
                UserId = userId,
                MediaId = mediaId,
                Score = score,
                Comment = comment ?? string.Empty,
                IsConfirmed = r.GetBoolean(1)
            };
        }

        // -------------------------
        // GET
        // -------------------------
        public static Rating? Get(int id)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                @"SELECT rating_id, user_id, media_id, score, comment, is_confirmed
                  FROM ratings WHERE rating_id=@id", conn);

            cmd.Parameters.AddWithValue("id", id);
            using var r = cmd.ExecuteReader();

            if (!r.Read()) return null;

            return new Rating
            {
                Id = r.GetInt32(0),
                UserId = r.GetInt32(1),
                MediaId = r.GetInt32(2),
                Score = r.GetInt32(3),
                Comment = r.GetString(4),
                IsConfirmed = r.GetBoolean(5)
            };
        }

        // -------------------------
        // UPDATE
        // -------------------------
        public void Update(int score, string comment)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                @"UPDATE ratings
                  SET score=@s, comment=@c, is_confirmed=false
                  WHERE rating_id=@id", conn);

            cmd.Parameters.AddWithValue("s", score);
            cmd.Parameters.AddWithValue("c", comment ?? string.Empty);
            cmd.Parameters.AddWithValue("id", Id);
            cmd.ExecuteNonQuery();

            Score = score;
            Comment = comment ?? string.Empty;
            IsConfirmed = false;
        }

        // -------------------------
        // DELETE
        // -------------------------
        public void Delete()
        {
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "DELETE FROM ratings WHERE rating_id=@id", conn);
            cmd.Parameters.AddWithValue("id", Id);
            cmd.ExecuteNonQuery();
        }
    }
}