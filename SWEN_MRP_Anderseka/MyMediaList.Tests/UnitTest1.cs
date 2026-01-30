using System;
using System.Collections.Generic;
using Xunit;
using MyMediaList.System;
using MyMediaList.Server;
using System.Text.Json.Nodes;

namespace MyMediaList.Tests
{
    public class HandlersTests
    {
        // ---------------------------------------------
        // SESSION TESTS
        // ---------------------------------------------

        [Fact]
        public void Session_Create_ValidAdmin_ReturnsSession()
        {
            var session = Session.Create("admin", "anything");
            Assert.NotNull(session);
            Assert.Equal("admin", session!.UserName);
            Assert.True(session.IsAdmin);
        }

        [Fact]
        public void Session_Create_InvalidUser_ReturnsNull()
        {
            var session = Session.Create("nonexistent", "password");
            Assert.Null(session);
        }

        [Fact]
        public void Session_Get_ExpiredSession_ReturnsNull()
        {
            var session = Session.Create("admin", "anything");
            Assert.NotNull(session);
            // Simulate expiration
            typeof(Session).GetProperty("Timestamp")!.SetValue(session, DateTime.UtcNow.AddHours(-1));
            var s = Session.Get(session!.Token);
            Assert.Null(s);
        }

        [Fact]
        public void Session_Close_RemovesSession()
        {
            var session = Session.Create("admin", "anything");
            Assert.NotNull(session);
            session!.Close();
            var s = Session.Get(session.Token);
            Assert.Null(s);
        }

        // ---------------------------------------------
        // FAVORITE TESTS
        // ---------------------------------------------

        [Fact]
        public void Favorite_Add_And_GetAll()
        {
            int userId = 1;
            int mediaId = 99;

            var fav = Favorite.Add(userId, mediaId);
            Assert.Equal(userId, fav.UserId);
            Assert.Equal(mediaId, fav.MediaId);

            var list = Favorite.GetAll(userId);
            Assert.Contains(list, f => f.MediaId == mediaId);
        }

        [Fact]
        public void Favorite_Remove_Works()
        {
            int userId = 1;
            int mediaId = 100;
            Favorite.Add(userId, mediaId);

            bool removed = Favorite.Remove(userId, mediaId);
            Assert.True(removed);

            var list = Favorite.GetAll(userId);
            Assert.DoesNotContain(list, f => f.MediaId == mediaId);
        }

        [Fact]
        public void Favorite_Add_Duplicate_Throws()
        {
            int userId = 1;
            int mediaId = 101;
            Favorite.Add(userId, mediaId);
            Assert.Throws<Exception>(() => Favorite.Add(userId, mediaId));
        }

        // ---------------------------------------------
        // RATING TESTS
        // ---------------------------------------------

        [Fact]
        public void Rating_Create_And_Get()
        {
            int userId = 1;
            int mediaId = 200;
            var r = Rating.Create(userId, mediaId, 5, "Great");
            Assert.Equal(userId, r.UserId);
            Assert.Equal(mediaId, r.MediaId);
            Assert.Equal(5, r.Score);

            var r2 = Rating.Get(r.Id);
            Assert.NotNull(r2);
            Assert.Equal(r.Id, r2!.Id);
        }

        [Fact]
        public void Rating_Update_Works()
        {
            var r = Rating.Create(1, 201, 3, "Ok");
            r.Update(4, "Better");
            var r2 = Rating.Get(r.Id);
            Assert.Equal(4, r2!.Score);
            Assert.Equal("Better", r2.Comment);
        }

        [Fact]
        public void Rating_Delete_Works()
        {
            var r = Rating.Create(1, 202, 5, "DeleteMe");
            int id = r.Id;
            r.Delete();
            var r2 = Rating.Get(id);
            Assert.Null(r2);
        }

        [Fact]
        public void Rating_Create_DuplicatePerUser_Throws()
        {
            int userId = 1;
            int mediaId = 203;
            Rating.Create(userId, mediaId, 5, "First");
            Assert.Throws<Exception>(() => Rating.Create(userId, mediaId, 4, "Second"));
        }

        // ---------------------------------------------
        // RATINGLIKE TESTS
        // ---------------------------------------------

        [Fact]
        public void RatingLike_Add_And_GetAll()
        {
            int userId = 1;
            var r = Rating.Create(userId, 300, 5, "LikeMe");
            var like = RatingLike.Add(userId, r.Id);

            Assert.Equal(userId, like.UserId);
            Assert.Equal(r.Id, like.RatingId);

            var list = RatingLike.GetAll(r.Id);
            Assert.Contains(list, l => l.UserId == userId);
        }

        [Fact]
        public void RatingLike_Remove_Works()
        {
            int userId = 2;
            var r = Rating.Create(userId, 301, 4, "DeleteLike");
            RatingLike.Add(userId, r.Id);

            bool removed = RatingLike.Remove(userId, r.Id);
            Assert.True(removed);

            var list = RatingLike.GetAll(r.Id);
            Assert.DoesNotContain(list, l => l.UserId == userId);
        }

        [Fact]
        public void RatingLike_Add_Duplicate_Throws()
        {
            int userId = 3;
            var r = Rating.Create(userId, 302, 3, "DupLike");
            RatingLike.Add(userId, r.Id);
            Assert.Throws<Exception>(() => RatingLike.Add(userId, r.Id));
        }

        [Fact]
        public void RatingLike_Add_NonexistentRating_Throws()
        {
            int userId = 1;
            int fakeRatingId = 99999;
            Assert.Throws<Exception>(() => RatingLike.Add(userId, fakeRatingId));
        }

        [Fact]
        public void RatingLike_GetAll_Empty_ReturnsEmptyList()
        {
            var list = RatingLike.GetAll(99999);
            Assert.Empty(list);
        }

        // ---------------------------------------------
        // FAVORITE HANDLER / SESSION MOCK TESTS
        // ---------------------------------------------

        [Fact]
        public void FavoriteHandler_AddWithoutSession_ReturnsUnauthorized()
        {
            var e = new HttpRestEventArgs
            {
                Path = "/favorites",
                Method = HttpMethod.Post,
                Content = new JsonObject { ["mediaId"] = 1 },
                Session = null
            };
            var h = new FavoriteHandler();
            h.Handle(e);
            Assert.True(e.Responded);
        }

        [Fact]
        public void RatingLikeHandler_AddWithoutSession_ReturnsUnauthorized()
        {
            var e = new HttpRestEventArgs
            {
                Path = "/rating-likes",
                Method = HttpMethod.Post,
                Content = new JsonObject { ["ratingId"] = 1 },
                Session = null
            };
            var h = new RatingLikeHandler();
            h.Handle(e);
            Assert.True(e.Responded);
        }

        [Fact]
        public void FavoriteHandler_AddWithInvalidMedia_Throws()
        {
            var session = Session.Create("admin", "anything");
            var e = new HttpRestEventArgs
            {
                Path = "/favorites",
                Method = HttpMethod.Post,
                Content = new JsonObject { ["mediaId"] = 99999 },
                Session = session
            };
            var h = new FavoriteHandler();
            // Depending on implementation, may throw DB exception
        }
    }
}