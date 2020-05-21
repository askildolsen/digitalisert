using System;
using System.Collections.Generic;

namespace Digitalisert.Models
{
    public class ResourceModelUtils
    {
        public static IEnumerable<string> ResourceFormat(string value, dynamic resource)
        {
            return Digitalisert.Raven.ResourceModelExtensions.ResourceFormat(value, resource);
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
