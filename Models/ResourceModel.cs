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
            public IEnumerable<string> Source { get; set; }
            public IEnumerable<object> _ { get; set; }
        }

        public class Property
        {
            public string Name { get; set; }
            public IEnumerable<string> Value { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<Resource> Resources { get; set; }
            public IEnumerable<Property> Properties { get; set; }
            public IEnumerable<string> Source { get; set; }
        }

        public class ResourceIndex : AbstractMultiMapIndexCreationTask<Resource>
        {
            public ResourceIndex()
            {
                AddMap<Resource>(resources =>
                    from resource in resources
                    let source = LoadDocument<Resource>(resource.Source.Where(s => !s.StartsWith("Resource")).Distinct()).Where(r => r != null)
                    let properties = resource.Properties.Union(source.SelectMany(s => s.Properties))
                    select new Resource
                    {
                        Context = resource.Context,
                        ResourceId = resource.ResourceId,
                        Type = source.SelectMany(r => r.Type).Distinct(),
                        SubType = source.SelectMany(r => r.SubType).Distinct(),
                        Title = source.SelectMany(r => r.Title).Distinct(),
                        SubTitle = source.SelectMany(r => r.SubTitle).Distinct(),
                        Code = source.SelectMany(r => r.Code).Distinct(),
                        Body = source.SelectMany(r => r.Body).Distinct(),
                        Status = source.SelectMany(r => r.Status).Distinct(),
                        Tags = source.SelectMany(r => r.Tags).Distinct(),
                        Classification = source.SelectMany(r => r.Classification).Distinct(),
                        Properties = properties,
                        _ = (
                            from p in properties
                            group p by p.Name into pg
                            select CreateField(
                                pg.Key,
                                pg.SelectMany(pv => pv.Value).Union(
                                    from r in pg.SelectMany(pr => pr.Resources)
                                    select r.ResourceId
                                ).Distinct()
                            )
                        )
                    }
                );

                Index(r => r.Context, FieldIndexing.Exact);
                Index(r => r.Type, FieldIndexing.Exact);
                Index(r => r.SubType, FieldIndexing.Exact);
                Index(r => r.Code, FieldIndexing.Exact);
                Index(r => r.Status, FieldIndexing.Exact);
                Index(r => r.Tags, FieldIndexing.Exact);
                Index(r => r.Classification, FieldIndexing.Exact);
                Index(r => r.Properties, FieldIndexing.No);

                Index(r => r.Title, FieldIndexing.Search);
                Index(r => r.SubTitle, FieldIndexing.Search);
                Index(r => r.Body, FieldIndexing.Search);

                Store(r => r.Context, FieldStorage.Yes);
                Store(r => r.Type, FieldStorage.Yes);
                Store(r => r.SubType, FieldStorage.Yes);
                Store(r => r.Title, FieldStorage.Yes);
                Store(r => r.SubTitle, FieldStorage.Yes);
                Store(r => r.Code, FieldStorage.Yes);
                Store(r => r.Status, FieldStorage.Yes);
                Store(r => r.Tags, FieldStorage.Yes);
                Store(r => r.Classification, FieldStorage.Yes);
                Store(r => r.Properties, FieldStorage.Yes);

                Analyzers.Add(x => x.Title, "SimpleAnalyzer");
                Analyzers.Add(x => x.SubTitle, "SimpleAnalyzer");
                Analyzers.Add(x => x.Body, "SimpleAnalyzer");
            }

            public override IndexDefinition CreateIndexDefinition()
            {
                var indexDefinition = base.CreateIndexDefinition();
                indexDefinition.Configuration = new IndexConfiguration { { "Indexing.MapTimeoutInSec", "60"} };

                return indexDefinition;
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
                    new { Name = "Context", Values = Enumerable.Repeat(example.Context, 1).Where(v => v != null) },
                    new { Name = "Type", Values = example.Type },
                    new { Name = "SubType", Values = example.SubType },
                    new { Name = "Code", Values = example.Code },
                    new { Name = "Status", Values = example.Status },
                    new { Name = "Tags", Values = example.Tags },
                    new { Name = "Classification", Values = (example.Classification ?? new string[][] {}).SelectMany(c => c) }
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


        public static IEnumerable<ResourceModel.Resource> LoadSource(IQueryable<ResourceModel.Resource> query, Raven.Client.Documents.Session.IDocumentSession session)
        {
            foreach(var resource in query)
            {
                foreach(var property in resource.Properties.Where(p => p.Resources != null && p.Resources.Any(r => r.Source.Any())))
                {
                    property.Resources = property.Resources.SelectMany(r => r.Source).Select(s => session.Load<Resource>(s));
                }

                yield return resource;
            }
        }
    }
}