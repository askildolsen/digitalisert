using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Digitalisert.Models;

namespace Digitalisert.Controllers
{
    public class HomeController : Controller
    {

       private readonly IDocumentStore _store;

        public HomeController()
        {
            _store = DocumentStoreHolder.Store;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Resource([FromQuery] Models.ResourceModel.Resource[] resources)
        {
            using(var session = _store.OpenSession())
            {
                var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();

                query = ResourceModel.QueryByExample(query, resources);

                return View(query.Take(100).ToList());
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
