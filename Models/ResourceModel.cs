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
            public string Context { get; set; }
            public string ResourceId { get; set; }
            public IEnumerable<string> Type { get; set; }
            public IEnumerable<string> SubType { get; set; }
            public IEnumerable<string> Title { get; set; }
            public IEnumerable<string> SubTitle { get; set; }
            public IEnumerable<string> Code { get; set; }
            public IEnumerable<string> Body { get; set; }
            public IEnumerable<string> Status { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<string[]> Classification { get; set; }
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
            public class EnheterResource : Resource { }

            public ResourceIndex()
            {
                AddMap<EnheterResource>(enheter =>
                    from enhet in enheter
                    select new Resource
                    {
                        Context = "Enheter",
                        ResourceId = enhet.ResourceId,
                        Type = enhet.Type,
                        SubType = enhet.SubType,
                        Title = enhet.Title,
                        SubTitle = enhet.SubTitle,
                        Code = enhet.Code,
                        Body = enhet.Body,
                        Status = enhet.Status,
                        Tags = enhet.Tags,
                        Classification = enhet.Classification,
                        Properties = enhet.Properties,
                        _ = new object[] { }
                    }
                );

                Reduce = results  =>
                    from result in results
                    group result by new { result.Context, result.ResourceId } into g
                    select new Resource
                    {
                        Context = g.Key.Context,
                        ResourceId = g.Key.ResourceId,
                        Type = g.SelectMany(resource => resource.Type),
                        SubType = g.SelectMany(resource => resource.SubType),
                        Title = g.SelectMany(resource => resource.Title),
                        SubTitle = g.SelectMany(resource => resource.SubTitle),
                        Code = g.SelectMany(resource => resource.Code),
                        Body = g.SelectMany(resource => resource.Body),
                        Status = g.SelectMany(resource => resource.Status),
                        Tags = g.SelectMany(resource => resource.Tags),
                        Classification = g.SelectMany(resource => resource.Classification),
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

                Index(r => r.Context, FieldIndexing.Exact);
                Index(r => r.Type, FieldIndexing.Exact);
                Index(r => r.SubType, FieldIndexing.Exact);
                Index(r => r.Code, FieldIndexing.Exact);
                Index(r => r.Status, FieldIndexing.Exact);
                Index(r => r.Tags, FieldIndexing.Exact);
                Index(r => r.Classification, FieldIndexing.Exact);

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
            new Facet { FieldName = "Context" },
            new Facet { FieldName = "Type" },
            new Facet { FieldName = "SubType" },
            new Facet { FieldName = "Tags" },
            new Facet { FieldName = "Status" },
            new Facet { FieldName = "Classification" }
        };

        public static IDocumentQuery<Resource> QueryByExample(IDocumentQuery<Resource> query, IEnumerable<Resource> examples)
        {
            foreach(var example in examples)
            {
                query.OrElse();
                query.OpenSubclause();

                var fields = new[] {
                    new { Name = "Context", Values = Enumerable.Repeat(example.Context, 1) },
                    new { Name = "Type", Values = example.Type },
                    new { Name = "SubType", Values = example.SubType },
                    new { Name = "Code", Values = example.Code },
                    new { Name = "Status", Values = example.Status },
                    new { Name = "Tags", Values = example.Tags },
                    new { Name = "Classification", Values = example.Classification.SelectMany(c => c) }
                };

                foreach(var field in fields)
                {
                    foreach (var value in field.Values ?? new string[] { })
                    {
                        if (value.StartsWith("-"))
                        {
                            query.WhereNotEquals(field.Name, value.TrimStart('-'));
                        }
                        else if (value.EndsWith("*"))
                        {
                            query.WhereStartsWith(field.Name, value.TrimEnd('*'));
                        }
                        else
                        {
                            query.WhereEquals(field.Name, value, exact: true);
                        }
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