using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace digitalisert.Controllers
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
        public IEnumerable<object> Get()
        {
            using(var session = _store.OpenSession())
            {
                return session
                    .Query<Models.ResourceModel.Resource, Models.ResourceModel.ResourceIndex>()
                    //.Where(r => r.Title.Contains("Xangeli AS"))
                    .Take(100)
                    .ToList();
            }
        }
    }
}
