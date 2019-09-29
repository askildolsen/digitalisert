using System.Collections.Generic;
using System.Linq;

namespace Digitalisert.Models
{
    public class ResourceModelUtils
    {
        public static bool PropertyResourceComparison(dynamic property, IEnumerable<dynamic> properties)
        {
            foreach(var criteriaProperty in property.Properties)
            {
                foreach(var compareProperty in properties.Where(p => ((dynamic)p).Name == criteriaProperty.Name))
                {
                    foreach(var criteriaResource in criteriaProperty.Resources)
                    {
                        foreach(var compareResource in ((dynamic)compareProperty).Resources)
                        {
                            if (CompareResource(criteriaResource, compareResource))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool CompareResource(dynamic criteriaResource, dynamic compareResource)
        {
            foreach(var criteria in criteriaResource)
            {
                if (criteria.Value.GetType().Name == "DynamicArray")
                {
                    object[] criteriaArray = criteria.Value.ToArray();
                    string[] compareArray = ((IEnumerable<object>)compareResource.GetType().GetProperty(criteria.Key).GetValue(compareResource)).Cast<object>().Select(v => v.ToString()).ToArray();

                    if (compareArray == null || criteriaArray.Select(v => v.ToString()).Intersect(compareArray).Count() == 0)
                    {
                        return false;
                    }
                }
                else
                {
                    string criteriaValue = criteria.Value;
                    string compareValue = compareResource.GetType().GetProperty(criteria.Key).GetValue(compareResource);
                    
                    if (compareValue == null || criteriaValue != compareValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static string ReadResourceFile(string filename)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(filename))
            {
                using (var reader = new System.IO.StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
