using Nancy.Authentication.Basic;
using System.Security.Claims;
using System.Security.Principal;

namespace computerMaintainHelper.Auth
{
    public class TokenValidator : IUserValidator

    {
        public ClaimsPrincipal Validate(string username, string password)
        {
            if (password==Bootstrapper.ServiceToken.ToString())
            {
                return new ClaimsPrincipal(new GenericIdentity(username));
            }
            return null;
        }
    }
}
