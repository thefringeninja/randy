using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Json.Linq;

namespace Raven.Client.NancyIntegration.Sample
{
    public class HomeViewModel
    {
        public IEnumerable<String> Docs { get; private set; }
        public HomeViewModel(IEnumerable<RavenJObject> docs)
        {
            docs = docs.ToArray();
            Docs = from doc in docs
                   select doc.ToString();
        }
    }
}