using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nancy;
using Newtonsoft.Json;
using Raven.Abstractions;

namespace Raven.Client.NancyIntegration
{
    public class RavenProfilerModule
        : NancyModule
    {
        public RavenProfilerModule()
            : base(RavenProfiler.ModulePath)
        {
            Get["/"] 
                = p =>
                    {
                        var ids = GetIdsFromContext(Context).ToArray();
                        var results = (from storeAndFilterer in RavenProfiler.StoresToFilterers
                                       let documentStore = storeAndFilterer.Key
                                       let filterer = storeAndFilterer.Value
                                       from id in ids
                                       let profilingInformation = documentStore.GetProfilingInformationFor(id)
                                       where profilingInformation != null
                                       select filterer.Filter(profilingInformation)).ToArray();

                        var jsonSerializer = CreateJsonSerializer();
                        return new Response
                            {
                                Contents = stream =>
                                    {
                                        var streamWriter = new StreamWriter(stream);
                                        jsonSerializer.Serialize(streamWriter, results);
                                        streamWriter.Flush();
                                    },
                                ContentType = "application/json"
                            };
                    };
        }

        private static JsonSerializer CreateJsonSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            foreach (var jsonConverter in Default.Converters)
            {
                jsonSerializer.Converters.Add(jsonConverter);
            }
            return jsonSerializer;
        }

        private static IEnumerable<Guid> GetIdsFromContext(NancyContext context)
        {
            var query = context.Request.Url.ParseQueryString();

            var ids = query.GetValues("id")
                      ?? query.GetValues("id[]")
                      ?? Enumerable.Empty<String>();

            return ids.Select(Guid.Parse);
        }
    }
}