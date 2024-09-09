using System;
using System.Collections.Generic;
using IdentityCore.DAL.Models;
using IdentityCore.DAL.Models.enums;

namespace IdentityCore.Tests.Helpers;

public static class TestDbEntityBuilder
{
    public static List<User> GenerateUsers(string password, int userCount = 1, int tokensPerUser = 0, int tokenDurationDays = 1)
    {
        var users = new List<User>();
        for (var i = 0; i < userCount; i++)
        {
            var username = $"user{i + 1}";
            var salt = TestDataHelper.GenerateSalt();
            var user = new User
            {
                Username = username,
                Email = $"{username}@example.com",
                Password = TestDataHelper.GetPasswordHash(password, salt),
                Salt = salt,
                Provider = Provider.Local,
                IsActive = true
            };

            if (tokensPerUser > 0)
            {
                var tokens = GenerateRefreshTokens(tokensPerUser, tokenDurationDays, user.Id);
                user.RefreshTokens = new HashSet<RefreshToken>(tokens);
            }

            users.Add(user);
        }

        return users;
    }

    private static List<RefreshToken> GenerateRefreshTokens(int count = 1, int durationDays = 1, Guid userId = default)
    {
        var tokens = new List<RefreshToken>();
        for (var i = 0; i < count; i++)
        {
            var token = new RefreshToken
            {
                RefToken = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(durationDays),
                UserId = userId
            };

            tokens.Add(token);
        }

        return tokens;
    }
}