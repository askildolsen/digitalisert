using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Session;
using static Digitalisert.Models.ResourceModelUtils;

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
                    let properties = 
                        from property in resource.Properties.Union(source.SelectMany(s => s.Properties))
                        group property by property.Name into propertyG
                        select new Property {
                            Name = propertyG.Key,
                            Value = propertyG.SelectMany(p => p.Value).Distinct(),
                            Tags = propertyG.SelectMany(p => p.Tags).Distinct(),
                            Resources =
                                from propertyresource in propertyG.SelectMany(p => p.Resources)
                                group propertyresource by new { Context = propertyresource.Context ?? resource.Context, ResourceId = propertyresource.ResourceId } into propertyresourceG
                                let propertyresourcesource = LoadDocument<Resource>(propertyresourceG.SelectMany(r => r.Source).Where(s => !s.StartsWith("Resource")).Distinct()).Where(r => r != null)
                                select new Resource {
                                    Context = propertyresourceG.Key.Context,
                                    ResourceId = propertyresourceG.Key.ResourceId,
                                    Type = propertyresourcesource.SelectMany(r => r.Type).Distinct(),
                                    SubType = propertyresourcesource.SelectMany(r => r.SubType).Distinct(),
                                    Title = propertyresourcesource.SelectMany(r => r.Title).Distinct(),
                                    Code = propertyresourcesource.SelectMany(r => r.Code).Distinct()
                                }
                        }
                    select new Resource
                    {
                        Context = resource.Context,
                        ResourceId = resource.ResourceId,
                        Type = source.SelectMany(r => r.Type).Union(resource.Type).Distinct(),
                        SubType = source.SelectMany(r => r.SubType).Union(resource.SubType).Distinct(),
                        Title = source.SelectMany(r => r.Title).Union(resource.Title).Distinct(),
                        SubTitle = source.SelectMany(r => r.SubTitle).Union(resource.SubTitle).Distinct(),
                        Code = source.SelectMany(r => r.Code).Union(resource.Code).Distinct(),
                        Body = source.SelectMany(r => r.Body).Union(properties.Where(p => p.Name == "@body").SelectMany(p => p.Value)).Select(v => ResourceFormat(v, resource)).Distinct(),
                        Status = source.SelectMany(r => r.Status).Union(resource.Status).Distinct(),
                        Tags = source.SelectMany(r => r.Tags).Union(resource.Tags).Distinct(),
                        Classification = source.SelectMany(r => r.Classification).Distinct(),
                        Properties = properties.Where(p => !p.Name.StartsWith("@")),
                        _ = (
                            from property in properties
                            group property by property.Name into propertyG
                            select CreateField(
                                propertyG.Key,
                                propertyG.SelectMany(p => p.Value).Union(
                                    from propertyresource in propertyG.SelectMany(p => p.Resources)
                                    from fieldvalue in new[] { propertyresource.ResourceId }.Union(propertyresource.Code).Union(propertyresource.Title)
                                    select fieldvalue
                                ).Where(v => !String.IsNullOrWhiteSpace(v)).Distinct()
                            )
                        ).Union(
                            from property in properties
                            group property by property.Name into propertyG
                            from resourcetype in propertyG.SelectMany(p => p.Resources).SelectMany(r => r.Type).Distinct()
                            select CreateField(
                                propertyG.Key + "." + resourcetype,
                                (
                                    from propertyresource in propertyG.SelectMany(p => p.Resources).Where(r => r.Type.Contains(resourcetype))
                                    from fieldvalue in new[] { propertyresource.ResourceId }.Union(propertyresource.Code).Union(propertyresource.Title)
                                    select fieldvalue
                                ).Where(v => !String.IsNullOrWhiteSpace(v)).Distinct()
                            )
                        ).Union(
                            new object[] {
                                CreateField(
                                    "Properties",
                                    properties.Select(p => p.Name).Where(n => !n.StartsWith("@")).Distinct(),
                                    new CreateFieldOptions { Indexing = FieldIndexing.Exact }
                                )
                            }
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
                Store(r => r.Body, FieldStorage.Yes);
                Store(r => r.Status, FieldStorage.Yes);
                Store(r => r.Tags, FieldStorage.Yes);
                Store(r => r.Classification, FieldStorage.Yes);
                Store(r => r.Properties, FieldStorage.Yes);

                Analyzers.Add(x => x.Title, "SimpleAnalyzer");
                Analyzers.Add(x => x.SubTitle, "SimpleAnalyzer");
                Analyzers.Add(x => x.Body, "SimpleAnalyzer");

                AdditionalSources = new Dictionary<string, string>
                {
                    {
                        "ResourceModel",
                        ReadResourceFile("digitalisert.Models.ResourceModelUtils.cs")
                    }
                };
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
            new Facet { FieldName = "Properties" }
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
                    new { Name = "Properties", Values = (example.Properties ?? new Property[] { }).Select(p => p.Name) }
                };

                foreach(var field in fields)
                {
                    foreach (var value in (field.Values ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
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
                    foreach(var value in (property.Value ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                    {
                        query.WhereEquals(property.Name, value, false);
                    }

                    foreach(var resource in property.Resources ?? new Resource[] { })
                    {
                        var resourcevalues = (new [] { resource.ResourceId ?? ""} )
                            .Union(resource.Code ?? new string[] { })
                            .Union(resource.Title ?? new string[] { });

                        foreach(var value in resourcevalues.Where(v => !String.IsNullOrWhiteSpace(v)))
                        {
                            query.WhereEquals(property.Name, value);
                        }
                    }
                }

                foreach(var title in (example.Title ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                {
                    query.Search("Title", title, SearchOperator.And);
                }

                foreach(var subTitle in (example.SubTitle ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                {
                    query.Search("SubTitle", subTitle, SearchOperator.And);
                }

                foreach(var body in (example.Body ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                {
                    query.Search("Body", body, SearchOperator.And);
                }

                query.CloseSubclause();
            }

            return query;
        }
    }
}