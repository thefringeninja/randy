using System.Linq;
using Nancy;
using Raven.Json.Linq;

namespace Raven.Client.NancyIntegration.Sample
{
    public class DefaultModule
        : NancyModule
    {
        public DefaultModule()
        {
            Get["/"] = _ =>
                {
                    var docs = DocumentSession.Query<RavenJObject>("Raven/DocumentsByEntityName").Take(20);

                    return View[new HomeViewModel(docs)];
                };
        }

        protected IDocumentSession DocumentSession
        {
            get { return (IDocumentSession) Context.Items[Bootstrapper.RavenSessionKey]; }
        }
    }
}