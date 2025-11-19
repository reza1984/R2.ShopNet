using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace R2.ShopNet.Web.Portal.Client;

public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _unauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask = _unauthenticatedTask;

    public PersistentAuthenticationStateProvider(PersistentComponentState persistentState)
    {
        if (!persistentState.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
        {
            return;
        }

        _authenticationStateTask = Task.FromResult(
            new AuthenticationState(userInfo.ToClaimsPrincipal()));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return _authenticationStateTask;
    }

    private class UserInfo
    {
        public required string UserId { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required IEnumerable<string> Roles { get; set; }
        public required Dictionary<string, string> Claims { get; set; }

        public ClaimsPrincipal ToClaimsPrincipal()
        {
            var identity = new ClaimsIdentity(
                Claims.Select(kvp => new Claim(kvp.Key, kvp.Value))
                    .Concat(Roles.Select(role => new Claim(ClaimTypes.Role, role)))
                    .Append(new Claim(ClaimTypes.NameIdentifier, UserId)),
                authenticationType: nameof(PersistentAuthenticationStateProvider),
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);

            return new ClaimsPrincipal(identity);
        }
    }
}
