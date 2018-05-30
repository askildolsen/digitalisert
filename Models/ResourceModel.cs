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
            public IEnumerable<string> Body { get; set; }
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
            public class EnhetsregisteretResource : Resource { }

            public ResourceIndex()
            {
                AddMap<EnhetsregisteretResource>(enheter =>
                    from enhet in enheter
                    select new Resource
                    {
                        ResourceId = enhet.ResourceId,
                        Type = enhet.Type,
                        SubType = enhet.SubType,
                        Title = enhet.Title,
                        SubTitle = enhet.SubTitle,
                        Code = enhet.Code,
                        Body = enhet.Body,
                        Status = enhet.Status,
                        Tags = enhet.Tags,
                        Properties = enhet.Properties,
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
                        SubTitle = g.SelectMany(resource => resource.SubTitle),
                        Code = g.SelectMany(resource => resource.Code),
                        Body = g.SelectMany(resource => resource.Body),
                        Status = g.SelectMany(resource => resource.Status),
                        Tags = g.SelectMany(resource => resource.Tags),
                        Properties = g.SelectMany(resource => resource.Properties),
                        _ =
                            (
                                from p in g.SelectMany(resource => resource.Properties)
                                select CreateField(p.Name, p.Value)
                            ).Union (
                                from p in g.SelectMany(resource => resource.Properties)
                                from r in p.Resources
                                from type in r.Type
                                select CreateField(p.Name + "_" + type + "_Code", r.Code)
                            )
                    };

                Index(r => r.Type, FieldIndexing.Exact);
                Index(r => r.SubType, FieldIndexing.Exact);
                Index(r => r.Code, FieldIndexing.Exact);
                Index(r => r.Status, FieldIndexing.Exact);
                Index(r => r.Tags, FieldIndexing.Exact);

                Index(r => r.Title, FieldIndexing.Search);
                Index(r => r.SubTitle, FieldIndexing.Search);
                Index(r => r.Body, FieldIndexing.Search);

                Index(r => r.Properties, FieldIndexing.No);
                Store(r => r.Properties, FieldStorage.Yes);

                Analyzers.Add(x => x.Title, "SimpleAnalyzer");
                Analyzers.Add(x => x.SubTitle, "SimpleAnalyzer");
                Analyzers.Add(x => x.Body, "SimpleAnalyzer");
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

                    foreach(var resource in property.Resources ?? new Resource[] { })
                    {
                        foreach(var type in resource.Type ?? new string[] { })
                        {
                            foreach(var code in resource.Code ?? new string[] { })
                            {
                                query.WhereEquals(property.Name + "_" + type + "_Code", code);
                            }
                        }
                    }
                }

                foreach(var title in example.Title ?? new string[] { })
                {
                    query.Search("Title", title, Raven.Client.Documents.Queries.SearchOperator.And);
                }

                foreach(var subTitle in example.SubTitle ?? new string[] { })
                {
                    query.Search("SubTitle", subTitle, Raven.Client.Documents.Queries.SearchOperator.And);
                }

                foreach(var body in example.Body ?? new string[] { })
                {
                    query.Search("Body", body, Raven.Client.Documents.Queries.SearchOperator.And);
                }

                query.CloseSubclause();
            }

            return query;
        }
    }
}