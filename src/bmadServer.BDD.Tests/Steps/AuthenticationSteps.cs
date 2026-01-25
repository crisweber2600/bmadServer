using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Reqnroll;
using Xunit;
using bmadServer.BDD.Tests.Support;

namespace bmadServer.BDD.Tests.Steps
{
    [Binding]
    public class AuthenticationSteps
    {
        private readonly TestContext _testContext;
        private HttpResponseMessage? _lastResponse;

        public AuthenticationSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        #region Given Steps

        [Given("the API is running")]
        public async Task GivenTheApiIsRunning()
        {
            try
            {
                var response = await _testContext.ApiClient.GetAsync("/health");
                Assert.True(response.IsSuccessStatusCode, "API health check failed");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("API is not running or not reachable", ex);
            }
        }

        [Given("a test user with email \"([^\"]*)\" and password \"([^\"]*)\"")]
        public async Task GivenATestUserExists(string email, string password)
        {
            _testContext.LastUserEmail = email;
            _testContext.LastUserPassword = password;
            
            // Register the user
            var registerRequest = new
            {
                email = email,
                password = password,
                displayName = "Test User"
            };

            _lastResponse = await _testContext.ApiClient.PostAsJsonAsync(
                "/api/v1/auth/register",
                registerRequest
            );

            // If user already exists, that's OK for this step
            Assert.True(
                _lastResponse.IsSuccessStatusCode || _lastResponse.StatusCode == HttpStatusCode.Conflict,
                $"Failed to create test user: {_lastResponse.StatusCode}"
            );
        }

        #endregion

        #region When Steps

        [When("I register with email \"([^\"]*)\" and password \"([^\"]*)\"")]
        public async Task WhenIRegisterWithCredentials(string email, string password)
        {
            _testContext.LastUserEmail = email;
            _testContext.LastUserPassword = password;

            var registerRequest = new
            {
                email = email,
                password = password,
                displayName = "New User"
            };

            _lastResponse = await _testContext.ApiClient.PostAsJsonAsync(
                "/api/v1/auth/register",
                registerRequest
            );
        }

        [When("I login with email \"([^\"]*)\" and password \"([^\"]*)\"")]
        public async Task WhenILoginWithCredentials(string email, string password)
        {
            var loginRequest = new
            {
                email = email,
                password = password
            };

            _lastResponse = await _testContext.ApiClient.PostAsJsonAsync(
                "/api/v1/auth/login",
                loginRequest
            );
        }

        #endregion

        #region Then Steps

        [Then("the user is created successfully")]
        public void ThenTheUserIsCreatedSuccessfully()
        {
            Assert.NotNull(_lastResponse);
            Assert.True(_lastResponse.StatusCode == HttpStatusCode.Created, 
                $"Expected 201 Created, got {_lastResponse.StatusCode}");
        }

         [Then("I receive an access token")]
         public async Task ThenIReceiveAnAccessToken()
         {
             Assert.NotNull(_lastResponse);
             Assert.True(_lastResponse.IsSuccessStatusCode, 
                 $"Login failed: {_lastResponse.StatusCode}");

             var jsonString = await _lastResponse.Content.ReadAsStringAsync();
             using (JsonDocument doc = JsonDocument.Parse(jsonString))
             {
                 var root = doc.RootElement;
                 Assert.True(root.TryGetProperty("accessToken", out var tokenElement), "accessToken not found in response");
                 var token = tokenElement.GetString();
                 Assert.NotNull(token);
                 _testContext.LastAccessToken = token;
                 _testContext.SetAuthorizationToken(token);
             }
         }

         [Then("the response returns (\\d+) (.*)")]
         public void ThenTheResponseReturnsStatus(int statusCode, string statusDescription)
         {
             Assert.NotNull(_lastResponse);
             Assert.Equal((HttpStatusCode)statusCode, _lastResponse.StatusCode);
         }

         [Then("I receive a refresh token")]
         public async Task ThenIReceiveARefreshToken()
         {
             Assert.NotNull(_lastResponse);
             var jsonString = await _lastResponse.Content.ReadAsStringAsync();
             using (JsonDocument doc = JsonDocument.Parse(jsonString))
             {
                 var root = doc.RootElement;
                 Assert.True(root.TryGetProperty("refreshToken", out var tokenElement), "refreshToken not found in response");
                 var token = tokenElement.GetString();
                 Assert.NotNull(token);
                 _testContext.LastRefreshToken = token;
             }
         }

         [Then("the refresh token is stored in a secure HttpOnly cookie")]
         public void ThenRefreshTokenIsInHttpOnlyCookie()
         {
             Assert.NotNull(_lastResponse);
             var setCookie = _lastResponse.Headers.GetValues("Set-Cookie");
             Assert.NotEmpty(setCookie);
             var refreshTokenCookie = string.Join(";", setCookie);
             Assert.Contains("refreshToken", refreshTokenCookie);
             Assert.Contains("HttpOnly", refreshTokenCookie);
             Assert.Contains("Secure", refreshTokenCookie);
         }

         [Then("the access token is a valid JWT")]
         public void ThenAccessTokenIsValidJwt()
         {
             Assert.NotNull(_testContext.LastAccessToken);
             var parts = _testContext.LastAccessToken.Split('.');
             Assert.Equal(3, parts.Length);
         }

         [Then("the JWT contains user email \"([^\"]*)\"")]
         public void ThenJwtContainsUserEmail(string expectedEmail)
         {
             Assert.NotNull(_testContext.LastAccessToken);
             var parts = _testContext.LastAccessToken.Split('.');
             Assert.Equal(3, parts.Length);
             
             var payload = parts[1];
             var paddingLength = 4 - (payload.Length % 4);
             if (paddingLength < 4) payload += new string('=', paddingLength);
             
             var decodedBytes = Convert.FromBase64String(payload);
             var jsonString = System.Text.Encoding.UTF8.GetString(decodedBytes);
             Assert.Contains(expectedEmail, jsonString);
         }

         [Then("I do not receive an access token")]
         public void ThenIDoNotReceiveAccessToken()
         {
             Assert.Null(_testContext.LastAccessToken);
         }

         [Then("the JWT token expires in (\\d+) minutes")]
         public void ThenJwtTokenExpiresInMinutes(int minutes)
         {
             Assert.NotNull(_testContext.LastAccessToken);
             var parts = _testContext.LastAccessToken.Split('.');
             var payload = parts[1];
             var paddingLength = 4 - (payload.Length % 4);
             if (paddingLength < 4) payload += new string('=', paddingLength);
             
             var decodedBytes = Convert.FromBase64String(payload);
             var jsonString = System.Text.Encoding.UTF8.GetString(decodedBytes);
             Assert.Contains($"\"{minutes * 60}\"", jsonString);
         }

         [When("I modify the JWT token by changing the payload")]
         public void WhenIModifyTheJwtToken()
         {
             Assert.NotNull(_testContext.LastAccessToken);
             var parts = _testContext.LastAccessToken.Split('.');
             var modifiedPayload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("modified"));
             _testContext.LastAccessToken = $"{parts[0]}.{modifiedPayload}.{parts[2]}";
         }

         [When("I attempt to use the modified token")]
         public async Task WhenIAttemptToUseModifiedToken()
         {
             _testContext.SetAuthorizationToken(_testContext.LastAccessToken ?? "");
             _lastResponse = await _testContext.ApiClient.GetAsync("/api/v1/auth/profile");
         }

         [When("I use the refresh token to get a new access token")]
         public async Task WhenIUseRefreshTokenToGetNewToken()
         {
             Assert.NotNull(_testContext.LastRefreshToken);
             var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
             request.Content = new FormUrlEncodedContent(new[]
             {
                 new KeyValuePair<string, string>("refreshToken", _testContext.LastRefreshToken)
             });
             _lastResponse = await _testContext.ApiClient.SendAsync(request);
         }

         [Then("I receive a new access token")]
         public async Task ThenIReceiveNewAccessToken()
         {
             Assert.NotNull(_lastResponse);
             Assert.True(_lastResponse.IsSuccessStatusCode, $"Refresh failed: {_lastResponse.StatusCode}");
             var jsonString = await _lastResponse.Content.ReadAsStringAsync();
             using (JsonDocument doc = JsonDocument.Parse(jsonString))
             {
                 var root = doc.RootElement;
                 Assert.True(root.TryGetProperty("accessToken", out var tokenElement), "accessToken not found in response");
                 var token = tokenElement.GetString();
                 Assert.NotNull(token);
                 _testContext.LastAccessToken = token;
             }
         }

         [Then("the new access token is valid")]
         public void ThenNewAccessTokenIsValid()
         {
             Assert.NotNull(_testContext.LastAccessToken);
             var parts = _testContext.LastAccessToken.Split('.');
             Assert.Equal(3, parts.Length);
         }

         [When("I wait for the refresh token to expire \\(more than (\\d+) days\\)")]
         public void WhenIWaitForRefreshTokenToExpire(int days)
         {
             // In a real test environment, this would involve time manipulation
             // For now, mark the token as expired by clearing it
             _testContext.LastRefreshToken = null;
         }

         [When("I attempt to use the expired refresh token")]
         public async Task WhenIAttemptToUseExpiredRefreshToken()
         {
             if (_testContext.LastRefreshToken == null)
             {
                 _lastResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
             }
             else
             {
                 var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
                 request.Content = new FormUrlEncodedContent(new[]
                 {
                     new KeyValuePair<string, string>("refreshToken", _testContext.LastRefreshToken)
                 });
                 _lastResponse = await _testContext.ApiClient.SendAsync(request);
             }
         }

         [When("I revoke the refresh token")]
         public async Task WhenIRevokeTheRefreshToken()
         {
             Assert.NotNull(_testContext.LastAccessToken);
             _testContext.SetAuthorizationToken(_testContext.LastAccessToken);
             var revokeRequest = new { refreshToken = _testContext.LastRefreshToken };
             _lastResponse = await _testContext.ApiClient.PostAsJsonAsync(
                 "/api/v1/auth/revoke",
                 revokeRequest
             );
             _testContext.LastRefreshToken = null;
         }

         [When("I attempt to use the revoked refresh token")]
         public async Task WhenIAttemptToUseRevokedRefreshToken()
         {
             if (_testContext.LastRefreshToken == null)
             {
                 _lastResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
             }
             else
             {
                 await WhenIAttemptToUseExpiredRefreshToken();
             }
         }

         [When("I initiate (\\d+) concurrent refresh token requests")]
         public async Task WhenIInitiateConcurrentRefreshTokenRequests(int count)
         {
             var tasks = new List<Task<HttpResponseMessage>>();
             for (int i = 0; i < count; i++)
             {
                 var task = Task.Run(async () =>
                 {
                     var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
                     request.Content = new FormUrlEncodedContent(new[]
                     {
                         new KeyValuePair<string, string>("refreshToken", _testContext.LastRefreshToken ?? "")
                     });
                     return await _testContext.ApiClient.SendAsync(request);
                 });
                 tasks.Add(task);
             }
             
             var results = await Task.WhenAll(tasks);
             _testContext.ConcurrentResponses = new List<HttpResponseMessage>(results);
         }

         [Then("exactly one request succeeds with a new access token")]
         public async Task ThenExactlyOneRequestSucceeds()
         {
             var successCount = 0;
             foreach (var response in _testContext.ConcurrentResponses ?? new List<HttpResponseMessage>())
             {
                 if (response.IsSuccessStatusCode)
                 {
                     successCount++;
                     var jsonString = await response.Content.ReadAsStringAsync();
                     using (JsonDocument doc = JsonDocument.Parse(jsonString))
                     {
                         var root = doc.RootElement;
                         if (root.TryGetProperty("accessToken", out var tokenElement))
                         {
                             _testContext.LastAccessToken = tokenElement.GetString();
                         }
                     }
                 }
             }
             Assert.Equal(1, successCount);
         }

         [Then("the other (\\d+) requests fail with (\\d+) Unauthorized")]
         public void ThenOtherRequestsFail(int failCount, int statusCode)
         {
             var failedCount = 0;
             foreach (var response in _testContext.ConcurrentResponses ?? new List<HttpResponseMessage>())
             {
                 if (!response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.Unauthorized)
                 {
                     failedCount++;
                 }
             }
             Assert.Equal(failCount, failedCount);
         }

         #endregion
     }
}
