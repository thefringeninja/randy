using System;
using Nancy;
using Nancy.Bootstrapper;
using Raven.Client.Document;
using TinyIoC;

namespace Raven.Client.NancyIntegration.Sample
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            Store = BuildDocumentStore();
            RavenProfiler.Enable(pipelines, Store);

            pipelines.BeforeRequest.InsertAfter(RavenProfiler.PipelineItemName, context =>
                {
                    context.Items[RavenSessionKey] = Store.OpenSession();
                    return null;
                });

            pipelines.AfterRequest.InsertAfter(RavenProfiler.PipelineItemName, context =>
                {
                    if (false == context.Items.ContainsKey(RavenSessionKey)) return;
                    ((IDisposable)context.Items[RavenSessionKey]).Dispose();
                });
        }

        public const string RavenSessionKey = "ravendb.session";

        protected IDocumentStore Store;

        private IDocumentStore BuildDocumentStore()
        {
            return new DocumentStore
                {
                    Url = "http://localhost:8080/"
                }.Initialize();
        }
    }
}
