using Nancy.ViewEngines.Razor;

namespace Raven.Client.NancyIntegration.Sample
{
    public static class HelperExtensions
    {
        public static IHtmlString RavenProfiler<TModel>(this HtmlHelpers<TModel> html)
        {
            return
                new HelperResult(
                    writer =>
                    writer.WriteLine(NancyIntegration.RavenProfiler.CurrentRequestSessions(html.RenderContext)));
        }
    }
}