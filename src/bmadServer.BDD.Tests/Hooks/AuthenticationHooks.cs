using Reqnroll;
using bmadServer.BDD.Tests.Support;

namespace bmadServer.BDD.Tests.Hooks
{
    [Binding]
    public class AuthenticationHooks
    {
        private readonly TestContext _testContext;

        public AuthenticationHooks(TestContext testContext)
        {
            _testContext = testContext;
        }

        [BeforeScenario("authentication")]
        public void BeforeAuthenticationScenario()
        {
            // Clear any previous authentication state
            _testContext.ClearAuthorizationToken();
            _testContext.LastAccessToken = null;
            _testContext.LastRefreshToken = null;
            _testContext.LastUserId = Guid.Empty;
        }

        [AfterScenario("authentication")]
        public void AfterAuthenticationScenario()
        {
            // Cleanup: Clear tokens and logout
            _testContext.ClearAuthorizationToken();
        }
    }
}
