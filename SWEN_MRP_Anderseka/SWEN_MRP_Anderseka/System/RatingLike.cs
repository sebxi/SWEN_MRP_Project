using System;
using System.Collections.Generic;
using Npgsql;
using MyMediaList.Server;

namespace MyMediaList.System
{
    public sealed class RatingLike
    {
        public int RatingId { get; private set; }
        public int UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private RatingLike() { }

        // --------------------------------------------------
        // ADD LIKE
        // --------------------------------------------------
        public static RatingLike Add(int userId, int ratingId)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            // Prüfen, ob Rating existiert
            using (var checkRating = new NpgsqlCommand(
                "SELECT 1 FROM ratings WHERE rating_id=@r", conn))
            {
                checkRating.Parameters.AddWithValue("r", ratingId);
                if (checkRating.ExecuteScalar() == null)
                    throw new Exception("Rating does not exist.");
            }

            // Prüfen, ob der User das Rating schon geliked hat
            using (var check = new NpgsqlCommand(
                "SELECT 1 FROM rating_likes WHERE rating_id=@r AND user_id=@u", conn))
            {
                check.Parameters.AddWithValue("r", ratingId);
                check.Parameters.AddWithValue("u", userId);
                if (check.ExecuteScalar() != null)
                    throw new Exception("Rating already liked by this user.");
            }

            // Like einfügen
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO rating_likes (rating_id, user_id)
                  VALUES (@r,@u)
                  RETURNING created_at", conn);
            cmd.Parameters.AddWithValue("r", ratingId);
            cmd.Parameters.AddWithValue("u", userId);

            var createdAt = (DateTime)cmd.ExecuteScalar()!;

            return new RatingLike
            {
                RatingId = ratingId,
                UserId = userId,
                CreatedAt = createdAt
            };
        }

        // --------------------------------------------------
        // GET ALL LIKES FOR A RATING
        // --------------------------------------------------
        public static List<RatingLike> GetAll(int ratingId)
        {
            var list = new List<RatingLike>();
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT user_id, created_at FROM rating_likes WHERE rating_id=@r ORDER BY created_at DESC", conn);
            cmd.Parameters.AddWithValue("r", ratingId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new RatingLike
                {
                    RatingId = ratingId,
                    UserId = r.GetInt32(0),
                    CreatedAt = r.GetDateTime(1)
                });
            }

            return list;
        }

        // --------------------------------------------------
        // REMOVE LIKE
        // --------------------------------------------------
        public static bool Remove(int userId, int ratingId)
        {
            using var conn = Database.GetConnection();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "DELETE FROM rating_likes WHERE rating_id=@r AND user_id=@u", conn);
            cmd.Parameters.AddWithValue("r", ratingId);
            cmd.Parameters.AddWithValue("u", userId);

            int affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }
    }
}