using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;

namespace CustomCharInfo.server.Filters
{
    // Clears the anonymous public-read cache after any successful write, so a change never waits out the cache lifetime.
    // Registered globally; the cached endpoints carry [OutputCache(PolicyName = "Public")], whose policy is tagged with Tag.
    public class PublicCacheEvictionFilter : IAsyncActionFilter
    {
        public const string Tag = "public";

        private readonly IOutputCacheStore _store;

        public PublicCacheEvictionFilter(IOutputCacheStore store)
        {
            _store = store;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();

            var method = context.HttpContext.Request.Method;
            if (method != HttpMethods.Post && method != HttpMethods.Put
                && method != HttpMethods.Patch && method != HttpMethods.Delete)
                return;

            if (executed.Exception != null && !executed.ExceptionHandled)
                return;

            // The result has not run yet, so read the status from the result object rather than the response.
            if (executed.Result is ForbidResult or ChallengeResult or UnauthorizedResult)
                return;
            if (executed.Result is IStatusCodeActionResult { StatusCode: >= 400 })
                return;

            await _store.EvictByTagAsync(Tag, context.HttpContext.RequestAborted);
        }
    }
}
