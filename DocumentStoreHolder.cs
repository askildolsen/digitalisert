using System;
using Raven.Client.Documents;
using Newtonsoft.Json;

public class DocumentStoreHolder
{
	private static Lazy<IDocumentStore> store = new Lazy<IDocumentStore>(CreateStore);

	public static IDocumentStore Store => store.Value;

    private static IDocumentStore CreateStore()
	{
		IDocumentStore store = new DocumentStore()
		{
		    Urls = new[] { "http://localhost:8080" },
			Database = "Digitalisert",
		};

		store.Conventions.FindCollectionName = t => t.Name;
		//store.Conventions.CustomizeJsonSerializer = s => s.NullValueHandling = NullValueHandling.Ignore;

		return store.Initialize();
	}
}
