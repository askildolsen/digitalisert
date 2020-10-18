using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Queries.Facets;
using Digitalisert.Models;

namespace Digitalisert.Controllers
{
    public class HomeController : Controller
    {
        [ViewData]
        public string ResourceSearch { get; set; }
        [ViewData]
        public Dictionary<string, FacetResult> ResourceFacet { get; set; }
        private readonly IDocumentStore _store;

        public HomeController()
        {
            _store = DocumentStoreHolder.Store;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Resource([FromQuery] Models.ResourceModel.Resource[] resources = null, string search = null)
        {
            using(var session = _store.OpenSession())
            {
                var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();

                query = ResourceModel.QueryByExample(query, resources);

                if (!String.IsNullOrWhiteSpace(search))
                {
                    query.Search("Search", search, @operator: SearchOperator.And);
                    ResourceSearch = search;
                }

                var result = query.ToQueryable().ProjectInto<ResourceModel.Resource>().Take(100).ToList();

                ResourceFacet = query.AggregateBy(ResourceModel.Facets).Execute();

                return View(result);
            }
        }

        [Route("Home/Resource/{context}/{*id}")]
        public IActionResult Resource(string context, string id)
        {
            using(var session = _store.OpenSession())
            {
                var query = session
                    .Query<ResourceModel.Resource, ResourceModel.ResourceIndex>()
                    .Include<ResourceModel.Resource>(r => r.Properties.SelectMany(p => p.Resources).SelectMany(re => re.Source))
                    .Where(r => r.Context == context && r.ResourceId == id);

                ResourceFacet = session.Query<ResourceModel.Resource, ResourceModel.ResourceIndex>().AggregateBy(ResourceModel.Facets).Execute();

                return View(query.ProjectInto<ResourceModel.Resource>().ToList());
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
