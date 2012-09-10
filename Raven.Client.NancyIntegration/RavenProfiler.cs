using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.ViewEngines;
using Raven.Client.Connection.Profiling;
using Raven.Client.Document;

namespace Raven.Client.NancyIntegration
{
    public class RavenProfiler
    {
        private const string SessionsByContext = "RavenDB.Client.NancyIntegration.SessionList";
        public const string PipelineItemName = "RavenProfiler";

        private static readonly ConcurrentDictionary<DocumentStore, JsonFormatterAndFieldsFilterer> Stores 
            = new ConcurrentDictionary<DocumentStore, JsonFormatterAndFieldsFilterer>();

        private static readonly ConcurrentDictionary<NancyContext, Tuple<Action<InMemoryDocumentSessionOperations>, Action<ProfilingInformation>>> Operations 
            = new ConcurrentDictionary<NancyContext, Tuple<Action<InMemoryDocumentSessionOperations>, Action<ProfilingInformation>>>();

        public static IEnumerable<DocumentStore> DocumentStores
        {
            get { return StoresToFilterers.Keys; }
        }

        public static ConcurrentDictionary<DocumentStore, JsonFormatterAndFieldsFilterer> StoresToFilterers
        {
            get { return Stores; }
        }

        /// <summary>
        /// Generates some script tags that you insert into your view.
        /// You should probably call this from a helper depending on your view engine.
        /// </summary>
        /// <param name="renderContext">The current Nancy Render Context</param>
        /// <returns></returns>

        public static string CurrentRequestSessions(IRenderContext renderContext)
        {
            var rootUrl = renderContext.ParsePath("~" + ModulePath) + "/";
            var sessionIdList = GetCurrentSessionIdList(renderContext.Context);

            return CurrentRequestSessions(rootUrl, sessionIdList);
        }

        private static string CurrentRequestSessions(string rootUrl, IEnumerable<Guid> sessionIdList)
        {
            using (
                var stream = typeof (RavenProfiler).Assembly.GetManifestResourceStream(typeof (RavenProfiler),
                                                                                       "Assets.index.html"))
            {
                return new StreamReader(stream).ReadToEnd()
                    .Replace("{|id|}", string.Join(",", sessionIdList.Select(guid => "'" + guid + "'")))
                    .Replace("{|rootUrl|}", rootUrl);
            }
        }

        public static string ModulePath = "/ravendb/profiling";

        public static void Enable(IPipelines pipelines, IDocumentStore store, params string[] filter)
        {
            var documentStore = store as DocumentStore;
            if (documentStore == null || documentStore.WasDisposed) return;

            JsonFormatterAndFieldsFilterer _;

            documentStore.AfterDispose += (sender, args) => StoresToFilterers.TryRemove(documentStore, out _);

            StoresToFilterers.TryAdd(documentStore, new JsonFormatterAndFieldsFilterer(new HashSet<string>(filter)));

            pipelines.BeforeRequest.AddItemToStartOfPipeline(
                new PipelineItem<Func<NancyContext, Response>>(
                    PipelineItemName, context =>
                        {
                            if (documentStore.WasDisposed) return null;

                            Action<InMemoryDocumentSessionOperations> onSessionCreated =
                                operation => SessionCreated(operation, context);


                            Action<ProfilingInformation> onInformationCreated =
                                information => InformationCreated(information, context);


                            if (false ==
                                Operations.TryAdd(context, Tuple.Create(onSessionCreated, onInformationCreated)))
                                return null;

                            documentStore.SessionCreatedInternal += onSessionCreated;

                            return null;
                        }));

            pipelines.AfterRequest.AddItemToEndOfPipeline(
                new PipelineItem<Action<NancyContext>>(
                    PipelineItemName, context =>
                        {
                            if (documentStore.WasDisposed) return;

                            Tuple<Action<InMemoryDocumentSessionOperations>, Action<ProfilingInformation>> stuff;

                            if (false == Operations.TryGetValue(context, out stuff)) return;

                            documentStore.SessionCreatedInternal -= stuff.Item1;
                            ProfilingInformation.OnContextCreated -= stuff.Item2;
                        }));
        }

        private static void InformationCreated(ProfilingInformation information, NancyContext context)
        {
            //not sure what to do with this
        }

        private static void SessionCreated(InMemoryDocumentSessionOperations operations, NancyContext context)
        {
            GetCurrentSessionIdList(context).Add(operations.Id);
            //context.Response.WithHeader("X-RavenDb-Profiling-Id", operations.Id.ToString());
        }

        public static IList<Guid> GetCurrentSessionIdList(NancyContext context)
        {
            var sessions = false == context.Items.ContainsKey(SessionsByContext)
                                 ? (IList<Guid>) (context.Items[SessionsByContext] = new List<Guid>())
                                 : (IList<Guid>) context.Items[SessionsByContext];

            return sessions;
        }
    }
}