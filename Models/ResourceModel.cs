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
            public string Id { get; set; }
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

        public class ResourceReferences {
            public string[] ReduceOutputs { get; set; }
        }

        public class ResourceIndex : AbstractMultiMapIndexCreationTask<Resource>
        {
            public ResourceIndex()
            {
                AddMap<Resource>(resources =>
                    from resource in resources
                    let source = LoadDocument<Resource>(resource.Source.Where(s => !s.StartsWith("Resource")).Distinct()).Where(r => r != null)
                    let properties = 
                        from property in resource.Properties
                        select new Property {
                            Name = property.Name,
                            Value = property.Value,
                            Tags = property.Tags,
                            Resources = (
                                from propertyresource in property.Resources
                                where propertyresource.ResourceId == null
                                select propertyresource
                            ).Union(
                                from propertyresource in property.Resources
                                where propertyresource.ResourceId != null
                                let propertyresourcereduceoutputs = LoadDocument<ResourceReferences>("ResourceReferences/" + propertyresource.Context + "/" + propertyresource.ResourceId).ReduceOutputs
                                let propertyresourceoutputs = LoadDocument<Resource>(propertyresourcereduceoutputs)
                                select new Resource {
                                    Context = propertyresource.Context,
                                    ResourceId = propertyresource.ResourceId,
                                    Type = propertyresourceoutputs.SelectMany(r => r.Type).Distinct(),
                                    SubType = propertyresourceoutputs.SelectMany(r => r.SubType).Distinct(),
                                    Title = propertyresourceoutputs.SelectMany(r => r.Title).Distinct(),
                                    SubTitle = propertyresourceoutputs.SelectMany(r => r.SubTitle).Distinct(),
                                    Code = propertyresourceoutputs.SelectMany(r => r.Code).Distinct(),
                                    Status = propertyresourceoutputs.SelectMany(r => r.Status).Distinct(),
                                    Tags = propertyresourceoutputs.SelectMany(r => r.Tags).Distinct()
                                }
                            )
                        }
                    select new Resource
                    {
                        Context = resource.Context,
                        ResourceId = resource.ResourceId,
                        Type = resource.Type,
                        SubType = resource.SubType,
                        Title = resource.Title,
                        SubTitle = resource.SubTitle,
                        Code = resource.Code,
                        Body = source.SelectMany(r => r.Body).Union(properties.Where(p => p.Name == "@body").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource))).Distinct(),
                        Status = resource.Status,
                        Tags = resource.Tags,
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
                        ).Union(
                            new object[] {
                                CreateField(
                                    "Search",
                                    resource.Title.Union(resource.Code).Distinct(),
                                    new CreateFieldOptions { Indexing = FieldIndexing.Search, Storage = FieldStorage.Yes, TermVector = FieldTermVector.WithPositionsAndOffsets }
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