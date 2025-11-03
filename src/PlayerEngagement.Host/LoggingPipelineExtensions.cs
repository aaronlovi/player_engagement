using Microsoft.AspNetCore.Builder;

namespace PlayerEngagement.Host;

internal static class LoggingPipelineExtensions {
    internal static IApplicationBuilder UseRequestPipelineLogging(this IApplicationBuilder app) {
        return app.UseHttpLogging();
    }
}
