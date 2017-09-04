using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Test;
using Microsoft.Extensions.Logging;

namespace identity_server
{
	public class ProfileService : IProfileService
	{
		private readonly ILogger<DefaultProfileService> _logger;
		private readonly TestUserStore _users;
		public ProfileService(ILogger<DefaultProfileService> logger, TestUserStore users)
		{
			_logger = logger;
			_users = users;
		}

		public Task GetProfileDataAsync(ProfileDataRequestContext context)
		{
			_logger.LogDebug("Get profile called for {subject} from {client} with {claimTypes} because {caller}",
                context.Subject.GetSubjectId(),
                context.Client.ClientName,
                context.RequestedClaimTypes,
                context.Caller);

            if (context.RequestedClaimTypes.Any())
            {				
				TestUser user = _users.FindBySubjectId(context.Subject.GetSubjectId());
				string email = GetEmail(user.Claims);

				if(!string.IsNullOrWhiteSpace(email))
				{
					user.Claims.Add(new Claim("email", email));
				}

				//TODO: Implement an IEqualityComparer
				List<Claim> allClaims = new List<Claim>();

				foreach(var c in user.Claims)
				{
					if(!allClaims.Any(claim => claim.Type == c.Type))
					{
						allClaims.Add(c);
					}
				}
				context.AddFilteredClaims(allClaims);
			}

			return Task.FromResult(0);
		}


		//Method that gets the email from the claim, noticed that claims received by Azure AD was sending name twice and one refers
		//to the email address.
		private string GetEmail(ICollection<Claim> claims)
		{
			string email = 
				claims
				.Where(claim => IsValidEmail(claim.Value))
				.Select(claim => claim.Value)
				.FirstOrDefault();
			
			return email;
		}

		private bool IsValidEmail(string email)
		{
			bool result = 
				Regex.IsMatch(email,
					@"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
					@"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
					RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
			return result;
		}

		public Task IsActiveAsync(IsActiveContext context)
		{
			//TODO: Verify if user is active or not
			context.IsActive = true;
			return Task.FromResult(0);
		}
	}
}
