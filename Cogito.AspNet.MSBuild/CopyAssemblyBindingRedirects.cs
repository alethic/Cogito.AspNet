using System;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Framework;

namespace Cogito.AspNet.MSBuild
{

    /// <summary>
    /// Copies the assembly bindings redirects from one config file to another.
    /// </summary>
    public class CopyAssemblyBindingRedirects : Microsoft.Build.Utilities.Task
    {

        static readonly XNamespace asmv1 = "urn:schemas-microsoft-com:asm.v1";

        /// <summary>
        /// File from which to obtain binding redirects.
        /// </summary>
        [Required]
        public ITaskItem SourceFile { get; set; }

        /// <summary>
        /// File to copy binding redirects.
        /// </summary>
        [Required]
        public ITaskItem TargetFile { get; set; }

        public override bool Execute()
        {
            var source = XDocument.Load(SourceFile.ItemSpec);
            var target = XDocument.Load(TargetFile.ItemSpec);

            // load new assembly bindings
            var items = source.Root.Element("runtime")?.Elements(asmv1 + "assemblyBinding") ?? Enumerable.Empty<XElement>();
            items = items.OrderBy(i => (string)i.Elements(asmv1 + "dependentAssembly").Elements(asmv1 + "assemblyIdentity").Attributes("name").FirstOrDefault() ?? "");

            // reorder attributes
            foreach (var element in items.DescendantsAndSelf())
            {
                static Tuple<int, string> Comparable(XAttribute s) => s.Name.LocalName switch
                {
                    "name" => Tuple.Create(0, s.Name.LocalName),
                    "publicKeyToken" => Tuple.Create(1, s.Name.LocalName),
                    "culture" => Tuple.Create(2, s.Name.LocalName),
                    "oldVersion" => Tuple.Create(0, s.Name.LocalName),
                    "newVersion" => Tuple.Create(1, s.Name.LocalName),
                    _ => Tuple.Create(int.MaxValue, s.Name.LocalName),
                };

                var attr = element.Attributes().OrderBy(i => Comparable(i)).ToList();
                element.ReplaceAttributes(attr);
            }

            // ensure output runtime element exists
            if (target.Root.Element("runtime") == null)
                target.Root.Add(new XElement("runtime"));

            // remove existing binding elements
            target.Root.Element("runtime").Elements(asmv1 + "assemblyBinding").Remove();
            target.Root.Element("runtime").Add(items);

            // save new file
            target.Save(TargetFile.ItemSpec);

            return true;
        }

    }

}
