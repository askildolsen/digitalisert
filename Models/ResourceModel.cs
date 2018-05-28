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
        }

        public class Property
        {
            public string Name { get; set; }
            public IEnumerable<string> Value { get; set; }
            public IEnumerable<string> Tags { get; set; }
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
                            }.Where(s => !String.IsNullOrEmpty(s))
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
                        Tags = g.SelectMany(resource => resource.Tags)
                    };

                Index(x => x.Type, FieldIndexing.Exact);
                Index(x => x.SubType, FieldIndexing.Exact);
                Index(x => x.Code, FieldIndexing.Exact);
                Index(x => x.Status, FieldIndexing.Exact);
                Index(x => x.Tags, FieldIndexing.Exact);

                Index(x => x.Title, FieldIndexing.Search);

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