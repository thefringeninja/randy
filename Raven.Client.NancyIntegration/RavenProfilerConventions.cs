using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Conventions;
using Nancy.Responses;

namespace Raven.Client.NancyIntegration
{
    public class RavenProfilerConventions : IConvention
    {
        public void Initialise(NancyConventions conventions)
        {
            conventions.StaticContentsConventions = new List<Func<NancyContext, string, Response>>
                {
                    (context, _) =>
                        {
                            var assetPath = RavenProfiler.ModulePath + "/assets";
                            var requestPath = context.Request.Url.Path;
                            if (false == requestPath.StartsWith(assetPath)) return null;

                            var fileName = requestPath.Split(new[] {assetPath}, StringSplitOptions.RemoveEmptyEntries)
                                .LastOrDefault();

                            if (string.IsNullOrEmpty(fileName)) return null;

                            var name = "Assets" + fileName.Replace('/', '.');

                            return new EmbeddedFileResponse(
                                typeof(RavenProfilerConventions).Assembly,
                                typeof(RavenProfiler).Namespace,
                                name);
                        }
                };
        }

        public Tuple<bool, string> Validate(NancyConventions conventions)
        {
            return Tuple.Create(true, string.Empty);
        }
    }
}