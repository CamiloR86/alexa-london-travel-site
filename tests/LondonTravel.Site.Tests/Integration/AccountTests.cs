// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.LondonTravel.Site.Integration
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Mime;
    using System.Text.Json;
    using System.Threading.Tasks;
    using MartinCostello.LondonTravel.Site.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using Shouldly;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// A class containing tests for user accounts.
    /// </summary>
    public class AccountTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountTests"/> class.
        /// </summary>
        /// <param name="fixture">The fixture to use.</param>
        /// <param name="outputHelper">The <see cref="ITestOutputHelper"/> to use.</param>
        public AccountTests(TestServerFixture fixture, ITestOutputHelper outputHelper)
            : base(fixture, outputHelper)
        {
        }

        [Fact]
        public async Task Can_Perform_Operations_On_Users_And_Get_Preferences_From_Api()
        {
            // Arrange
            var emailAddress = $"some.user.{Guid.NewGuid()}@some.domain.com";

            var user = new LondonTravelUser()
            {
                CreatedAt = DateTime.UtcNow,
                Email = emailAddress,
                EmailNormalized = emailAddress,
                GivenName = "Alexa",
                Surname = "Amazon",
                UserName = emailAddress,
                UserNameNormalized = emailAddress,
            };

            string accessToken = Controllers.AlexaController.GenerateAccessToken();
            string[] favoriteLines = new[] { "district", "northern" };
            string userId;

            // HACK Force server start-up
            using (Fixture.CreateDefaultClient())
            {
            }

            static IUserStore<LondonTravelUser> GetUserStore(IServiceProvider serviceProvider)
                => serviceProvider.GetRequiredService<IUserStore<LondonTravelUser>>();

            using (var scope = Fixture.Services.CreateScope())
            {
                using IUserStore<LondonTravelUser> store = GetUserStore(scope.ServiceProvider);

                // Act
                IdentityResult createResult = await store.CreateAsync(user, default);

                // Assert
                Assert.NotNull(createResult);
                Assert.True(createResult.Succeeded);
                Assert.NotEmpty(user.Id);

                // Arrange
                userId = user.Id!;

                // Act
                LondonTravelUser actual = await store.FindByIdAsync(userId, default);

                // Assert
                Assert.NotNull(actual);
                Assert.Equal(userId, actual.Id);
                Assert.Null(actual.AlexaToken);
                Assert.Equal(user.CreatedAt, actual.CreatedAt);
                Assert.Equal(user.Email, actual.Email);
                Assert.False(actual.EmailConfirmed);
                Assert.NotEmpty(actual.ETag);
                Assert.Equal(Array.Empty<string>(), actual.FavoriteLines);
                Assert.Equal(user.GivenName, actual.GivenName);
                Assert.Equal(Array.Empty<LondonTravelLoginInfo>(), actual.Logins);
                Assert.Equal(user.Surname, actual.Surname);
                Assert.Equal(user.UserName, actual.UserName);

                // Arrange
                string etag = actual.ETag!;

                actual.AlexaToken = accessToken;
                actual.FavoriteLines = favoriteLines;

                // Act
                IdentityResult updateResult = await store.UpdateAsync(actual, default);

                // Assert
                Assert.NotNull(updateResult);
                Assert.True(updateResult.Succeeded);

                // Act
                actual = await store.FindByNameAsync(emailAddress, default);

                // Assert
                Assert.NotNull(actual);
                Assert.Equal(userId, actual.Id);
                Assert.Equal(emailAddress, actual.Email);
                Assert.NotEqual(etag, actual.ETag);
                Assert.Equal(accessToken, actual.AlexaToken);
                Assert.Equal(favoriteLines, actual.FavoriteLines);
            }

            // Arrange
            using (var message = new HttpRequestMessage(HttpMethod.Get, "api/preferences"))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                // Act
                using var client = Fixture.CreateClient();
                using var response = await client.SendAsync(message);

                // Assert
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
                response.Content.ShouldNotBeNull();
                response.Content!.Headers.ShouldNotBeNull();
                response.Content!.Headers!.ContentType.ShouldNotBeNull();
                response.Content.Headers.ContentType!.MediaType.ShouldBe(MediaTypeNames.Application.Json);

                string json = await response.Content.ReadAsStringAsync();
                using var preferences = JsonDocument.Parse(json);

                Assert.Equal(userId, preferences.RootElement.GetString("userId"));
                Assert.Equal(favoriteLines, preferences.RootElement.GetStringArray("favoriteLines"));
            }

            // Arrange
            using (var scope = Fixture.Services.CreateScope())
            {
                using IUserStore<LondonTravelUser> store = GetUserStore(scope.ServiceProvider);

                // Act
                IdentityResult updateResult = await store.DeleteAsync(new LondonTravelUser() { Id = userId }, default);

                // Assert
                Assert.NotNull(updateResult);
                Assert.True(updateResult.Succeeded);
            }

            // Arrange
            using (var message = new HttpRequestMessage(HttpMethod.Get, "api/preferences"))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                // Act
                using var client = Fixture.CreateClient();
                using var response = await client.SendAsync(message);

                // Assert
                response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
                response.Content.ShouldNotBeNull();
                response.Content!.Headers.ShouldNotBeNull();
                response.Content!.Headers!.ContentType.ShouldNotBeNull();
                response.Content.Headers.ContentType!.MediaType.ShouldBe(MediaTypeNames.Application.Json);
            }
        }
    }
}
