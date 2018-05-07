using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using digitalisert.Models;

namespace digitalisert.Controllers
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

        public IActionResult Resource()
        {
            using(var session = _store.OpenSession())
            {
                return View(session
                    .Query<Models.ResourceModel.Resource, Models.ResourceModel.ResourceIndex>()
                    //.Where(r => r.Title.Contains("Xangeli AS"))
                    .Take(100)
                    .ToList());
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
