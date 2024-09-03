using System.Collections.Generic;
using IdentityCore.DAL.Models;

namespace IdentityCore.Tests.Helpers;

public static class TestDbEntityBuilder
{
    public static List<User> GenerateUsers(string password, int count = 1, List<RefreshToken> refreshTokens = null)
    {
        var users = new List<User>();

        for (var i = 0; i < count; i++)
        {
            var username = $"user{i + 1}";
            var email = $"{username}@example.com";
            var salt = TestDataHelper.GenerateSalt();
            var user = new User
            {
                Username = username,
                Email = email,
                Password = TestDataHelper.GetPasswordHash(password, salt),
                Salt = salt,
                IsActive = true
            };

            if (refreshTokens != null)
                user.RefreshTokens = refreshTokens;

            users.Add(user);
        }

        return users;
    }
}