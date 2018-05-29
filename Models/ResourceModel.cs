using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Linq.Indexing;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Session;

namespace Digitalisert.Models
{
    public class ResourceModel
    {
        public class Resource
        {
            public Resource() { }
            public string ResourceId { get; set; }
            public IEnumerable<string> Type { get; set; }
            public IEnumerable<string> SubType { get; set; }
            public IEnumerable<string> Title { get; set; }
            public IEnumerable<string> SubTitle { get; set; }
            public IEnumerable<string> Code { get; set; }
            public IEnumerable<string> Status { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<Property> Properties { get; set; }
            public IEnumerable<object> _ { get; set; }
        }

        public class Property
        {
            public string Name { get; set; }
            public IEnumerable<string> Value { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<Resource> Resources { get; set; }
            public IEnumerable<Property> Properties { get; set; }
        }

        public class ResourceIndex : AbstractMultiMapIndexCreationTask<Resource>
        {
            public class Enhetsregisteret { }

            public ResourceIndex()
            {
                AddMap<Enhetsregisteret>(enheter =>
                    from e in enheter
                    let enhet = (IDictionary<string, string>)(object)e
                    //where new[] { "Organisasjonsledd", "Staten"}.Contains(enhet["orgform.beskrivelse"])
                    select new Resource
                    {
                        ResourceId =  enhet["organisasjonsnummer"],
                        Type = new[] { enhet["orgform.beskrivelse"] },
                        SubType = new [] { enhet["institusjonellSektorkode.beskrivelse"] }.Where(s => !String.IsNullOrEmpty(s)),
                        Title = new[] { enhet["navn"] },
                        Code =  new[] { enhet["organisasjonsnummer"] },
                        Status = 
                            from status in new[] { "konkurs", "underAvvikling", "underTvangsavviklingEllerTvangsopplosning" }
                            where enhet[status] == "J"
                            select status,
                        Tags =
                            new[] {
                                enhet["naeringskode1.beskrivelse"], enhet["naeringskode2.beskrivelse"], enhet["naeringskode3.beskrivelse"]
                            }.Where(s => !String.IsNullOrEmpty(s)),
                        Properties = new[] {
                            new Property
                            {
                                Name = "Postadresse",
                                Value = new[] { enhet["postadresse.adresse"], enhet["postadresse.postnummer"] + " " + enhet["postadresse.poststed"] },
                                Resources = new[] {
                                    new Resource { Type = new[] { "Poststed" }, Code = new[] { enhet["postadresse.postnummer"] }, Title = new[] { enhet["postadresse.poststed"] } },
                                    new Resource { Type = new[] { "Kommune" }, Code = new[] { enhet["postadresse.kommunenummer"] }, Title = new[] { enhet["postadresse.kommune"] } },
                                    new Resource { Type = new[] { "Land" }, Code = new[] { enhet["postadresse.landkode"] }, Title = new[] { enhet["postadresse.land"] } }
                                }
                            }
                        }.Where(p => p.Value.Any(v => !String.IsNullOrWhiteSpace(v))),
                        _ = new object[] { }
                    }
                );

                Reduce = results  =>
                    from result in results
                    group result by result.ResourceId into g
                    select new Resource
                    {
                        ResourceId = g.Key,
                        Type = g.SelectMany(resource => resource.Type),
                        SubType = g.SelectMany(resource => resource.SubType),
                        Title = g.SelectMany(resource => resource.Title),
                        Code = g.SelectMany(resource => resource.Code),
                        Status = g.SelectMany(resource => resource.Status),
                        Tags = g.SelectMany(resource => resource.Tags),
                        Properties = g.SelectMany(resource => resource.Properties),
                        _ =
                            from p in g.SelectMany(resource => resource.Properties)
                            select CreateField(p.Name, p.Value)
                    };

                Index(r => r.Type, FieldIndexing.Exact);
                Index(r => r.SubType, FieldIndexing.Exact);
                Index(r => r.Code, FieldIndexing.Exact);
                Index(r => r.Status, FieldIndexing.Exact);
                Index(r => r.Tags, FieldIndexing.Exact);

                Index(r => r.Title, FieldIndexing.Search);

                Index(r => r.Properties, FieldIndexing.No);
                Store(r => r.Properties, FieldStorage.Yes);

                Analyzers.Add(x => x.Title, "SimpleAnalyzer");
            }
        }

        public static List<Facet> Facets = new List<Facet>
        {
            new Facet { FieldName = "Type" },
            new Facet { FieldName = "SubType" },
            new Facet { FieldName = "Tags" },
            new Facet { FieldName = "Status" }
        };

        public static IDocumentQuery<Resource> QueryByExample(IDocumentQuery<Resource> query, IEnumerable<Resource> examples)
        {
            foreach(var example in examples)
            {
                query.OrElse();
                query.OpenSubclause();

                foreach(var type in example.Type ?? new string[] { })
                {
                    query.WhereEquals("Type", type, exact: true);
                }
                foreach(var subType in example.SubType ?? new string[] { })
                {
                    query.WhereEquals("SubType", subType, exact: true);
                }
                foreach(var tags in example.Tags ?? new string[] { })
                {
                    query.WhereEquals("Tags", tags, exact: true);
                }
                foreach(var status in example.Status ?? new string[] { })
                {
                    query.WhereEquals("Status", status, exact: true);
                }

                foreach(var code in example.Code ?? new string[] { })
                {
                    if (code.EndsWith("*"))
                    {
                        query.WhereStartsWith("Code", code.TrimEnd('*'));
                    }
                    else
                    {
                        query.WhereEquals("Code", code, exact: true);
                    }
                }

                foreach(var property in example.Properties ?? new Property[] { })
                {
                    foreach(var value in property.Value ?? new string[] { })
                    {
                        query.WhereEquals(property.Name, value, false);
                    }
                }

                foreach(var title in example.Title ?? new string[] { })
                {
                    query.Search("Title", title, Raven.Client.Documents.Queries.SearchOperator.And);
                }

                query.CloseSubclause();
            }

            return query;
        }
    }
}