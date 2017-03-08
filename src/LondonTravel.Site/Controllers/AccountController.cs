﻿// Copyright (c) Martin Costello, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

namespace MartinCostello.LondonTravel.Site.Controllers
{
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using MartinCostello.LondonTravel.Site.Identity;
    using MartinCostello.LondonTravel.Site.Options;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// A class representing the controller for the <c>/account/</c> resource.
    /// </summary>
    [Authorize]
    [Route("account", Name = "Account")]
    public class AccountController : Controller
    {
        private readonly UserManager<LondonTravelUser> _userManager;
        private readonly SignInManager<LondonTravelUser> _signInManager;
        private readonly string _externalCookieScheme;
        private readonly bool _isEnabled;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<LondonTravelUser> userManager,
            SignInManager<LondonTravelUser> signInManager,
            IOptionsSnapshot<IdentityCookieOptions> identityCookieOptions,
            SiteOptions siteOptions,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _externalCookieScheme = identityCookieOptions.Value.ExternalCookieAuthenticationScheme;
            _isEnabled = siteOptions?.Authentication?.IsEnabled == true && siteOptions?.Authentication.ExternalProviders?.Count > 0;
            _logger = logger;
        }

        /// <summary>
        /// Gets the result for the <c>/account/access-denied/</c> action.
        /// </summary>
        /// <returns>
        /// The result for the <c>/account/access-denied/</c> action.
        /// </returns>
        [HttpGet]
        [Route("access-denied", Name = "AccessDenied")]
        public IActionResult AccessDenied() => View();

        /// <summary>
        /// Gets the result for the <c>/account/sign-in/</c> action.
        /// </summary>
        /// <param name="returnUrl">The optional return URL once the user is signed-in.</param>
        /// <returns>
        /// The result for the <c>/account/sign-in/</c> action.
        /// </returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("sign-in", Name = "SignIn")]
        public async Task<IActionResult> SignIn(string returnUrl = null)
        {
            if (!_isEnabled)
            {
                return NotFound();
            }

            await HttpContext.Authentication.SignOutAsync(_externalCookieScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Gets the result for the GET <c>/account/sign-out/</c> action.
        /// </summary>
        /// <returns>
        /// The result for the <c>/account/sign-out/</c> action.
        /// </returns>
        [HttpGet]
        [Route("sign-out", Name = "SignOut")]
        [ValidateAntiForgeryToken]
        public IActionResult SignOutGet() => RedirectToAction(nameof(HomeController.Index), "Home");

        /// <summary>
        /// Gets the result for the POST <c>/account/sign-out/</c> action.
        /// </summary>
        /// <returns>
        /// The result for the <c>/account/sign-out/</c> action.
        /// </returns>
        [HttpPost]
        [Route("sign-out", Name = "SignOut")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOutPost()
        {
            if (!_isEnabled)
            {
                return NotFound();
            }

            string userName = _userManager.GetUserName(User);

            await _signInManager.SignOutAsync();

            _logger.LogInformation($"User '{userName}' signed out.");

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        /// <summary>
        /// Gets the result for the <c>/account/external-sign-in/</c> action.
        /// </summary>
        /// <param name="provider">The external provider name.</param>
        /// <param name="returnUrl">The optional return URL once the user is signed-in.</param>
        /// <returns>
        /// The result for the <c>/account/external-sign-in/</c> action.
        /// </returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("external-sign-in", Name = "ExternalSignIn")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalSignIn(string provider, string returnUrl = null)
        {
            if (!_isEnabled)
            {
                return NotFound();
            }

            var redirectUrl = Url.RouteUrl("ExternalSignInCallback", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return Challenge(properties, provider);
        }

        /// <summary>
        /// Gets the result for the <c>/account/external-sign-in-callback/</c> action.
        /// </summary>
        /// <param name="returnUrl">The optional return URL once the user is signed-in.</param>
        /// <param name="remoteError">The remote error message, if any.</param>
        /// <returns>
        /// The result for the <c>/account/external-sign-in-callback/</c> action.
        /// </returns>
        [AllowAnonymous]
        [Route("external-sign-in-callback", Name = "ExternalSignInCallback")]
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (!_isEnabled)
            {
                return NotFound();
            }

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(SignIn));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                return RedirectToAction(nameof(SignIn));
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true);

            if (result.Succeeded)
            {
                _logger.LogInformation($"User '{_userManager.GetUserName(info.Principal)}' signed in with '{info.LoginProvider}' provider.");

                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return View("LockedOut");
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);

                var user = new LondonTravelUser()
                {
                    Email = email,
                    GivenName = givenName,
                    Surname = surname,
                    UserName = email,
                    EmailConfirmed = false,
                };

                user.Logins.Add(LondonTravelLoginInfo.FromUserLoginInfo(info));

                var identityResult = await _userManager.CreateAsync(user);

                if (identityResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: true);

                    _logger.LogInformation($"New user account '{user.Id}' created through '{info.LoginProvider}'.");

                    return RedirectToLocal(returnUrl);
                }

                bool isUserAlreadyRegistered = identityResult.Errors.Any((p) => p.Code.StartsWith("Duplicate"));

                AddErrors(identityResult);

                ViewBag.IsAlreadyRegistered = isUserAlreadyRegistered;
                ViewData["ReturnUrl"] = returnUrl ?? (isUserAlreadyRegistered ? Url.RouteUrl("Manage") : string.Empty);

                return View("SignIn");
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                _logger?.LogWarning($"{error.Code}: {error.Description}");
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}