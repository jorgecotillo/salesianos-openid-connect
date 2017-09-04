using System.Threading.Tasks;
using IdentityServer4.Services;
using Julio.Francisco.De.Iriarte.IdentityServer.CustomAttributes;
using Julio.Francisco.De.Iriarte.IdentityServer.Models.Home;
using Microsoft.AspNetCore.Mvc;

namespace Julio.Francisco.De.Iriarte.IdentityServer.Controllers
{
    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;

        public HomeController(IIdentityServerInteractionService interaction)
        {
            _interaction = interaction;
        }

        public IActionResult Index()
        {
            return View();
        }
   
        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;
            }

            return View("Error", vm);
}
    }
}