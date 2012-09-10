using System.Collections.Specialized;
using Nancy;
using Nancy.Helpers;

namespace Raven.Client.NancyIntegration
{
    public static class Extensions
    {
        public static NameValueCollection ParseQueryString(this Url url)
        {
            // http://stackoverflow.com/questions/68624/how-to-parse-a-query-string-into-a-namevaluecollection-in-net/68803#68803
            var queryParameters = new NameValueCollection();
            var querySegments = url.Query.Split('&');
            foreach (var segment in querySegments)
            {
                var parts = segment.Split('=');
                if (parts.Length <= 0) continue;
                var key = parts[0].Trim(new[] { '?', ' ' });
                var val = parts[1].Trim();
                
                queryParameters.Add(HttpUtility.UrlDecode(key), HttpUtility.UrlDecode(val));
            }

            return queryParameters;
        }
    }
}