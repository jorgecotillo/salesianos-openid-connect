using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Julio.Francisco.De.Iriarte.IdentityServer.CustomAttributes;
using Julio.Francisco.De.Iriarte.IdentityServer.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Julio.Francisco.De.Iriarte.IdentityServer.Controllers
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [SecurityHeaders]
    public class AccountController : Controller
    {
        /*private readonly TestUserStore _users;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly AccountService _account;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityUser> userManager)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            //_users = users ?? new TestUserStore(TestUsers.Users);
            _interaction = interaction;
            _account = new AccountService(interaction, httpContextAccessor, clientStore);
            _userManager = userManager;
        }*/

        private readonly UserManager<IdentityUser> _userManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly AccountService _account;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            IEventService events,
            UserManager<IdentityUser> userManager) {
            _userManager = userManager;
            _interaction = interaction;
            _events = events;
            _account = new AccountService(interaction, httpContextAccessor, clientStore);
        }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var vm = await _account.BuildLoginViewModelAsync(returnUrl);
			object obj = null;

			var x = obj ?? true;

            if (vm.IsExternalLoginOnly)
            {
                // only one option for logging in
                return await ExternalLogin(vm.ExternalProviders.First().AuthenticationScheme, returnUrl);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model) 
        {
            if (ModelState.IsValid) {
                var identityUser = await _userManager.FindByNameAsync(model.Username);

                if (identityUser != null && await _userManager.CheckPasswordAsync(identityUser, model.Password)) {
                    AuthenticationProperties props = null;

                    if (AccountOptions.AllowRememberLogin && model.RememberLogin) {
                        props = new AuthenticationProperties {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                        };
                    };

                    await _events.RaiseAsync(new UserLoginSuccessEvent(
                            identityUser.UserName, identityUser.Id, identityUser.UserName));
                    await HttpContext.Authentication.SignInAsync(identityUser.Id, identityUser.UserName, props);

                    if (_interaction.IsValidReturnUrl(model.ReturnUrl) || Url.IsLocalUrl(model.ReturnUrl)) {
                        return Redirect(model.ReturnUrl);
                    }

                    return Redirect("~/");
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials"));

                ModelState.AddModelError("", AccountOptions.InvalidCredentialsErrorMessage);
            }

            var vm = await _account.BuildLoginViewModelAsync(model);
            return View(vm);
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            var vm = await _account.BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // no need to show prompt
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            var vm = await _account.BuildLoggedOutViewModelAsync(model.LogoutId);
            if (vm.TriggerExternalSignout)
            {
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });
                try
                {
                    // hack: try/catch to handle social providers that throw
                    await HttpContext.Authentication.SignOutAsync(vm.ExternalAuthenticationScheme, 
                        new AuthenticationProperties { RedirectUri = url });
                }
                catch(NotSupportedException) // this is for the external providers that don't have signout
                {
                }
                catch(InvalidOperationException) // this is for Windows/Negotiate
                {
                }
            }

            // delete local authentication cookie
            await HttpContext.Authentication.SignOutAsync();

            return View("LoggedOut", vm);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
        {
            returnUrl = Url.Action("ExternalLoginCallback", new { returnUrl = returnUrl });

            // windows authentication is modeled as external in the asp.net core authentication manager, so we need special handling
            if (AccountOptions.WindowsAuthenticationSchemes.Contains(provider))
            {
                // but they don't support the redirect uri, so this URL is re-triggered when we call challenge
                if (HttpContext.User is WindowsPrincipal)
                {
                    var props = new AuthenticationProperties();
                    props.Items.Add("scheme", HttpContext.User.Identity.AuthenticationType);

                    var id = new ClaimsIdentity(provider);
                    id.AddClaim(new Claim(ClaimTypes.NameIdentifier, HttpContext.User.Identity.Name));
                    id.AddClaim(new Claim(ClaimTypes.Name, HttpContext.User.Identity.Name));

                    await HttpContext.Authentication.SignInAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme, new ClaimsPrincipal(id), props);
                    return Redirect(returnUrl);
                }
                else
                {
                    // this triggers all of the windows auth schemes we're supporting so the browser can use what it supports
                    return new ChallengeResult(AccountOptions.WindowsAuthenticationSchemes);
                }
            }
            else
            {
                // start challenge and roundtrip the return URL
                var props = new AuthenticationProperties
                {
                    RedirectUri = returnUrl,
                    Items = { { "scheme", provider } }
                };
                return new ChallengeResult(provider, props);
            }
        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl) {
            var info = await HttpContext.Authentication.GetAuthenticateInfoAsync(IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme);
            var tempUser = info?.Principal;
            if (tempUser == null) {
                throw new Exception("External authentication error");
            }

            var claims = tempUser.Claims.ToList();

            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
            if (userIdClaim == null) {
                userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            }
            if (userIdClaim == null) {
                throw new Exception("Unknown userid");
            }

            claims.Remove(userIdClaim);
            var provider = info.Properties.Items["scheme"];
            var userId = userIdClaim.Value;

            var user = await _userManager.FindByLoginAsync(provider, userId);
            if (user == null) {
                user = new IdentityUser { UserName = Guid.NewGuid().ToString() };
                await _userManager.CreateAsync(user);
                await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, userId, provider));
            }

            var additionalClaims = new List<Claim>();

            var sid = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null) {
                additionalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            AuthenticationProperties props = null;
            var id_token = info.Properties.GetTokenValue("id_token");
            if (id_token != null) {
                props = new AuthenticationProperties();
                props.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
            }

            await _events.RaiseAsync(new UserLoginSuccessEvent(provider, userId, user.Id, user.UserName));
            await HttpContext.Authentication.SignInAsync(user.Id, user.UserName, provider, additionalClaims.ToArray());

            await HttpContext.Authentication.SignOutAsync(IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme);

            if (_interaction.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl)) {
                return Redirect(returnUrl);
            }

            return Redirect("~/");
        }
    }
}