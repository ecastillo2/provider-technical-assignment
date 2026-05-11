namespace ProviderAssignmentStarter.Models
{
    /// <summary>
    /// Backs the shared error page. Populated by
    /// <see cref="Controllers.HomeController.Error(int?)"/> for both
    /// unhandled exceptions (via <c>UseExceptionHandler</c>) and non-success
    /// status codes (via <c>UseStatusCodePagesWithReExecute</c>).
    /// </summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public int? StatusCode { get; set; }

        public string Title { get; set; } = "Something went wrong";

        public string Message { get; set; } =
            "An unexpected error occurred while processing your request.";

        /// <summary>Original path the user was trying to reach, when known.</summary>
        public string? OriginalPath { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
