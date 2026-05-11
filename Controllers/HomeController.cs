using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
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
    /// logic in the home flow. The Error action is the single landing
    /// point for both unhandled exceptions (UseExceptionHandler) and
    /// non-success status codes (UseStatusCodePagesWithReExecute).
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
        /// GET /Home/Error or /Home/Error/{statusCode} — generic error
        /// landing.
        ///
        /// Pulls the original exception (when present, set by
        /// <c>UseExceptionHandler</c>) and the re-executed path (set by
        /// <c>UseStatusCodePagesWithReExecute</c>) and surfaces a friendly
        /// page plus a request id for log correlation. The exception
        /// itself is logged so an operator can find it without trusting
        /// the user to copy a request id.
        /// </summary>
        [Route("Home/Error/{statusCode:int?}")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            var exFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var reFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            var (title, message) = ResolveCopy(statusCode, exFeature is not null);
            var originalPath = exFeature?.Path ?? reFeature?.OriginalPath;

            if (exFeature?.Error is { } error)
            {
                _logger.LogError(
                    error,
                    "Unhandled exception on {Path}. RequestId={RequestId}",
                    originalPath ?? "(unknown)",
                    requestId);
            }
            else if (statusCode is int sc and >= 400)
            {
                _logger.LogWarning(
                    "Status {StatusCode} returned for {Path}. RequestId={RequestId}",
                    sc,
                    originalPath ?? "(unknown)",
                    requestId);

                Response.StatusCode = sc;
            }

            return View(new ErrorViewModel
            {
                RequestId    = requestId,
                StatusCode   = statusCode,
                Title        = title,
                Message      = message,
                OriginalPath = originalPath,
            });
        }

        private static (string Title, string Message) ResolveCopy(int? statusCode, bool hadException)
        {
            if (hadException)
            {
                return (
                    "Something went wrong",
                    "An unexpected error occurred while processing your request. Our team has been notified.");
            }

            return statusCode switch
            {
                400 => ("Bad request",     "The request was malformed or missing required information."),
                401 => ("Sign-in required","You need to sign in to view that page."),
                403 => ("Access denied",   "You do not have permission to view that page."),
                404 => ("Page not found",  "The page you’re looking for doesn’t exist or was moved."),
                408 => ("Request timed out","The server took too long to respond. Please try again."),
                500 => ("Server error",    "The server ran into a problem. Please try again in a moment."),
                502 => ("Bad gateway",     "An upstream service responded incorrectly."),
                503 => ("Service unavailable","The service is temporarily unavailable. Please try again shortly."),
                _   => ("Something went wrong",
                        "An unexpected error occurred while processing your request."),
            };
        }
    }
}
