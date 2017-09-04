using System;
using System.Collections.Generic;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.Extensions.Configuration;

namespace Julio.Francisco.De.Iriarte.IdentityServer.Configuration
{
    public class Config
    {
        //This method is needed otherwise IdentiyServer4 was throwing an exception
        //related to invalid scope: openid
        public static List<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email() // <-- usefull
            };
        }
        
        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("api1", "My API"),
                new ApiResource("api-prom-2000", "Salesianos Prom 2000 API"),
                new ApiResource("postman_api", "Postman API")
            };
        }

        public static IEnumerable<Client> GetClients(IConfigurationRoot configuration)
        {
            var jsDomain = configuration["general:JSDomain"];
            var wpDomain = configuration["general:WordpressDomain"];
            var prom2000Domain = configuration["general:Prom2000Domain"];

            return new List<Client>
            {
                new Client
                {
                    ClientId = "js",
                    ClientName = "Angular2 JavaScript Client",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { jsDomain + "/callback" },
                    //NOTE: This link needs to match the link from the presentation layer - oidc-client
                    //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { jsDomain + "/home" }, 
                    AllowedCorsOrigins =     { jsDomain },
                    EnableLocalLogin = false,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "api1"
                    }
                },
                 new Client
                {
                    ClientId = "salesianos-js",
                    ClientName = "Salesianos Prom 2000 Client",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { prom2000Domain + "/callback" },
                    //NOTE: This link needs to match the link from the presentation layer - oidc-client
                    //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { prom2000Domain + "/home" }, 
                    AllowedCorsOrigins =     { prom2000Domain },
                    EnableLocalLogin = false,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "api-prom-2000"
                    }
                },
                new Client
                {
                    ClientId = "postman",
                    ClientName = "Postman Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { "https://www.getpostman.com/oauth2/callback" },
                    //NOTE: This link needs to match the link from the presentation layer - oidc-client
                    //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { "https://www.getpostman.com" }, 
                    AllowedCorsOrigins =     { "https://www.getpostman.com" },
                    EnableLocalLogin = false,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "api1"
                    },
                    ClientSecrets = new List<Secret>() { new Secret("VUdPR5HIlKLe4sVmMe6JbZk8v/JMZC5qy8VY2Chdfrg=".Sha256()) }
                },
                new Client
                {
                    ClientId = "postman-api",
                    ClientName = "Postman Test Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { "https://www.getpostman.com/oauth2/callback" },
                    //NOTE: This link needs to match the link from the presentation layer - oidc-client
                    //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { "https://www.getpostman.com" }, 
                    AllowedCorsOrigins =     { "https://www.getpostman.com" },
                    EnableLocalLogin = true,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "postman_api"
                    },
                    ClientSecrets = new List<Secret>() { new Secret("SomeValue".Sha256()) }
                },
                new Client
                {
                    ClientId = "local-js",
                    ClientName = "Angular2 JavaScript Client",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { "http://localhost:4040/callback" },
                    //NOTE: This link needs to match the link from the presentation layer - oidc-client
                    //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { "http://localhost:4040/home" }, 
                    AllowedCorsOrigins =     { "http://localhost:4040" },
                    EnableLocalLogin = false,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "api1"
                    }
                },
                new Client
                {
                    ClientId = "wordpress",
                    ClientName = "Wordpress Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { wpDomain + "/wp-admin/admin-ajax.php?action=openid-connect-authorize" },
                        //NOTE: This link needs to match the link from the presentation layer - oidc-client
                        //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { wpDomain },
                    AllowedCorsOrigins =     { wpDomain },
                    EnableLocalLogin = false,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email
                    },
                    //AlwaysIncludeUserClaimsInIdToken = true,
                    //AlwaysSendClientClaims = true,
                    ClientSecrets = new List<Secret>() { new Secret("VUdPR5HIlKLe4sVmMe6JbZk8v/JMZC5qy8VY2Chdfrg=".Sha256()) }
                },
                new Client
                {
                    ClientId = "wordpress-local",
                    ClientName = "Wordpress Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { "http://127.0.0.1:4080/wp-admin/admin-ajax.php?action=openid-connect-authorize" },
                        //NOTE: This link needs to match the link from the presentation layer - oidc-client
                        //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { "http://127.0.0.1:4080" },
                    AllowedCorsOrigins =     { "http://127.0.0.1:4080" },
                    EnableLocalLogin = false,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email
                    },
                    //AlwaysIncludeUserClaimsInIdToken = true,
                    //AlwaysSendClientClaims = true,
                    ClientSecrets = new List<Secret>() { new Secret("VUdPR5HIlKLe4sVmMe6JbZk8v/JMZC5qy8VY2Chdfrg=".Sha256()) }
                },
                new Client
                {
                    ClientId = "wordpress-az",
                    ClientName = "Wordpress Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { "http://wordpress-paas.azurewebsites.net/wp-admin/admin-ajax.php?action=openid-connect-authorize" },
                        //NOTE: This link needs to match the link from the presentation layer - oidc-client
                        //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { "http://wordpress-paas.azurewebsites.net" },
                    AllowedCorsOrigins =     { "http://wordpress-paas.azurewebsites.net" },
                    EnableLocalLogin = false,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email
                    },
                    //AlwaysIncludeUserClaimsInIdToken = true,
                    //AlwaysSendClientClaims = true,
                    ClientSecrets = new List<Secret>() { new Secret("VUdPR5HIlKLe4sVmMe6JbZk8v/JMZC5qy8VY2Chdfrg=".Sha256()) }
                },
                new Client
                {
                    ClientId = "wordpress-az-ssl",
                    ClientName = "Wordpress Client",
                    AllowedGrantTypes = GrantTypes.Code,
                    AllowAccessTokensViaBrowser = true,
                    RequireConsent = false,
                    RedirectUris =           { "https://wordpress-paas.azurewebsites.net/wp-admin/admin-ajax.php?action=openid-connect-authorize" },
                        //NOTE: This link needs to match the link from the presentation layer - oidc-client
                        //      otherwise IdentityServer won't display the link to go back to the site
                    PostLogoutRedirectUris = { "https://wordpress-paas.azurewebsites.net" },
                    AllowedCorsOrigins =     { "https://wordpress-paas.azurewebsites.net" },
                    EnableLocalLogin = false,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email
                    },
                    //AlwaysIncludeUserClaimsInIdToken = true,
                    //AlwaysSendClientClaims = true,
                    ClientSecrets = new List<Secret>() { new Secret("VUdPR5HIlKLe4sVmMe6JbZk8v/JMZC5qy8VY2Chdfrg=".Sha256()) }
                },
                new Client
                {
                    ClientId = "android-app",
                    ClientName = "Android Client",
                    AllowedGrantTypes = GrantTypes.Hybrid,
                    RedirectUris = { "freemason://callback" },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.OfflineAccess,
                        "api1" 
                    },
                    RequireClientSecret = false,
                    AllowOfflineAccess = true,
                    RequirePkce = true,
                    EnableLocalLogin = false,
                    RequireConsent = false,
                }
            };
        }
    }
}