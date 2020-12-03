﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Xamarin.Essentials;

namespace MSALSample.Services
{
    public class AuthService
    {
        string RedirectUri
        {
            get
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                    return $"msauth://{AppId}/wFHf8fqQKWnprsTx7vOxL4nppi8%3D";
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                    return $"msauth.{AppId}://auth";

                return string.Empty;
            }
        }

        readonly string AppId = "io.thewissen.msalsample";
        readonly string ClientID = "da34e4be-2759-4d21-a6eb-45c82e96a437";
        readonly string[] Scopes = { "User.Read" };
        readonly IPublicClientApplication _pca;

        // Android uses this to determine which activity to use to show
        // the login screen dialog from.
        public static object ParentWindow { get; set; }

        public AuthService()
        {
            _pca = PublicClientApplicationBuilder.Create(ClientID)
                .WithIosKeychainSecurityGroup(AppId)
                .WithRedirectUri(RedirectUri)
                .WithAuthority("https://login.microsoftonline.com/common")
                .WithBroker(true)
                .Build();
        }

        public async Task<bool> SignInAsync()
        {
            try
            {
                IEnumerable<IAccount> accounts = new List<IAccount>();
                try
                {
                    accounts = await _pca.GetAccountsAsync();

                }
                catch (System.Security.Cryptography.CryptographicException exn)
                {
                }
                var firstAccount = accounts.FirstOrDefault();
                var authResult = await _pca.AcquireTokenSilent(Scopes, firstAccount).ExecuteAsync();

                // Store the access token securely for later use.
                await SecureStorage.SetAsync("AccessToken", authResult?.AccessToken);

                return true;
            }
            
            catch (MsalUiRequiredException)
            {
                try
                {
                    // This means we need to login again through the MSAL window.
                    var authResult = await _pca.AcquireTokenInteractive(Scopes)
                                                .WithParentActivityOrWindow(ParentWindow)
                                                .WithUseEmbeddedWebView(true)
                                                .ExecuteAsync();

                    // Store the access token securely for later use.
                    await SecureStorage.SetAsync("AccessToken", authResult?.AccessToken);

                    return true;
                }
                
                catch (Exception ex2)
                {
                    Debug.WriteLine(ex2.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        public async Task<bool> SignOutAsync()
        {
            try
            {
                var accounts = await _pca.GetAccountsAsync();

                // Go through all accounts and remove them.
                while (accounts.Any())
                {
                    await _pca.RemoveAsync(accounts.FirstOrDefault());
                    accounts = await _pca.GetAccountsAsync();
                }

                // Clear our access token from secure storage.
                SecureStorage.Remove("AccessToken");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
