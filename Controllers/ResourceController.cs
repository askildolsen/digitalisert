using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries.Facets;
using Digitalisert.Models;

namespace Digitalisert.Controllers
{
    [Route("api/[controller]")]
    public class ResourceController : Controller
    {
        private readonly IDocumentStore _store;

        public ResourceController()
        {
            _store = DocumentStoreHolder.Store;
        }

        [HttpGet]
        public IEnumerable<object> Get([FromQuery] Models.ResourceModel.Resource[] resources)
        {
            using(var session = _store.OpenSession())
            {
                var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();

                query = ResourceModel.QueryByExample(query, resources);

                return query.Take(100).ToList();
            }
        }

        [HttpGet("Facet")]
        public Dictionary<string, FacetResult> Facet([FromQuery] Models.ResourceModel.Resource[] resources)
        {
            using(var session = _store.OpenSession())
            {
                var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();

                query = ResourceModel.QueryByExample(query, resources);

                return query
                    .AggregateBy(ResourceModel.Facets)
                    .Execute();
            }
        }

        [HttpGet("Deploy")]
        public void Deploy()
        {
            new ResourceModel.ResourceIndex().Execute(DocumentStoreHolder.Store);
        }
    }
}
