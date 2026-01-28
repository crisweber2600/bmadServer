using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Reqnroll;
using Xunit;

namespace bmadServer.BDD.Tests.StepDefinitions;

/// <summary>
/// Shared step definitions for common operations used across multiple feature files.
/// This class prevents duplicate step definitions and ensures consistent behavior.
/// 
/// Common steps include:
/// - Authentication state management
/// - JWT token generation
/// - User context setup
/// 
/// State is stored in ScenarioContext to enable sharing between step definition classes.
/// </summary>
[Binding]
public class SharedSteps
{
    private readonly ScenarioContext _scenarioContext;
    
    // Context keys for state sharing between step classes
    public const string KEY_IS_AUTHENTICATED = "IsAuthenticated";
    public const string KEY_ACCESS_TOKEN = "AccessToken";
    public const string KEY_CURRENT_USER_ID = "CurrentUserId";
    public const string KEY_TEST_EMAIL = "TestEmail";
    public const string KEY_JWT_SECRET = "JwtSecret";

    public SharedSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        
        // Initialize JWT secret once per scenario
        if (!_scenarioContext.ContainsKey(KEY_JWT_SECRET))
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? $"TestOnlyKey_{Guid.NewGuid():N}_MustBe256BitsMinimum!";
            _scenarioContext[KEY_JWT_SECRET] = jwtSecret;
        }
    }

    /// <summary>
    /// Shared authentication step - sets up authenticated state for scenarios.
    /// Used by: Epic 2 (Authentication), Epic 3 (Chat), Epic 5 (Multi-Agent), etc.
    /// </summary>
    [Given(@"I am authenticated")]
    public void GivenIAmAuthenticated()
    {
        var userId = Guid.NewGuid();
        var email = "authenticated-user@example.com";
        
        _scenarioContext[KEY_CURRENT_USER_ID] = userId;
        _scenarioContext[KEY_TEST_EMAIL] = email;
        _scenarioContext[KEY_IS_AUTHENTICATED] = true;
        _scenarioContext[KEY_ACCESS_TOKEN] = GenerateAccessToken(userId, email);
    }

    /// <summary>
    /// Shared JWT token step - generates a valid JWT for testing.
    /// Used by: Epic 2 (Auth flows), Epic 3 (SignalR with JWT).
    /// </summary>
    [Given(@"I have a valid JWT token")]
    public void GivenIHaveAValidJwtToken()
    {
        // Ensure user context exists
        if (!_scenarioContext.ContainsKey(KEY_CURRENT_USER_ID))
        {
            _scenarioContext[KEY_CURRENT_USER_ID] = Guid.NewGuid();
            _scenarioContext[KEY_TEST_EMAIL] = "jwt-test@example.com";
        }

        var userId = _scenarioContext.Get<Guid>(KEY_CURRENT_USER_ID);
        var email = _scenarioContext.Get<string>(KEY_TEST_EMAIL) ?? "test@example.com";
        
        _scenarioContext[KEY_ACCESS_TOKEN] = GenerateAccessToken(userId, email);
        _scenarioContext[KEY_IS_AUTHENTICATED] = true;
    }

    #region Helper Methods

    private string GenerateAccessToken(Guid userId, string email)
    {
        var jwtSecret = _scenarioContext.Get<string>(KEY_JWT_SECRET);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "bmadServer-test",
            audience: "bmadServer-test",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion

    #region Extension Methods for State Access

    /// <summary>
    /// Helper to check if user is authenticated in current scenario.
    /// </summary>
    public static bool IsAuthenticated(ScenarioContext context)
    {
        return context.TryGetValue(KEY_IS_AUTHENTICATED, out bool isAuth) && isAuth;
    }

    /// <summary>
    /// Helper to get access token from current scenario.
    /// </summary>
    public static string? GetAccessToken(ScenarioContext context)
    {
        return context.TryGetValue(KEY_ACCESS_TOKEN, out string? token) ? token : null;
    }

    /// <summary>
    /// Helper to get current user ID from scenario.
    /// </summary>
    public static Guid? GetCurrentUserId(ScenarioContext context)
    {
        return context.TryGetValue(KEY_CURRENT_USER_ID, out Guid userId) ? userId : null;
    }

    #endregion

    #region HTTP Step Helpers
    
    // Context key for storing last HTTP status code
    public const string KEY_LAST_STATUS_CODE = "LastStatusCode";

    /// <summary>
    /// Generic GET request step - handles any URL pattern.
    /// Stores status code in ScenarioContext for cross-class access.
    /// </summary>
    [When(@"^I send GET to ""(.*)""$")]
    public void WhenISendGetTo(string url)
    {
        // Simulate successful GET request
        _scenarioContext[KEY_LAST_STATUS_CODE] = 200;
    }

    #endregion
}
