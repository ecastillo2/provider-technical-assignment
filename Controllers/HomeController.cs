using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProviderAssignmentStarter.Models;

namespace ProviderAssignmentStarter.Controllers
{
    /// <summary>
    /// Hosts the home / landing page, the About (formerly "Privacy") page,
    /// and the generic Error page.
    /// </summary>
    /// <remarks>
    /// Functionality lives entirely in the views — there is no business
    /// logic in the home flow. The Error action exists so the global
    /// exception handler in <c>Program.cs</c> has a route to redirect to
    /// in non-development environments.
    /// </remarks>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>GET / — landing page.</summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// GET /Home/Privacy — kept under the "Privacy" name for routing
        /// stability, but actually rendered as an About page (see
        /// <c>Views/Home/Privacy.cshtml</c>). The nav link reads "About".
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// GET /Home/Error — generic error landing. Surfaces the request
        /// id so server logs can be correlated with whatever the user
        /// saw on screen.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
