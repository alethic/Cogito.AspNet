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
            var original = System.IO.File.ReadAllText(TargetFile.ItemSpec);
            var newline = ConfigXml.DetectNewline(original);

            var source = XDocument.Load(SourceFile.ItemSpec);
            var target = XDocument.Load(TargetFile.ItemSpec, LoadOptions.PreserveWhitespace);

            // load new assembly bindings
            var items = (source.Root.Element("runtime")?.Elements(asmv1 + "assemblyBinding") ?? Enumerable.Empty<XElement>())
                .OrderBy(i => (string)i.Elements(asmv1 + "dependentAssembly").Elements(asmv1 + "assemblyIdentity").Attributes("name").FirstOrDefault() ?? "")
                .ToList();

            // reorder attributes
            foreach (var element in items.DescendantsAndSelf())
            {
                Tuple<int, string> Comparable(XAttribute s)
                {
                    switch (s.Name.LocalName)
                    {
                        case "name":
                            return Tuple.Create(0, s.Name.LocalName);
                        case "publicKeyToken":
                            return Tuple.Create(1, s.Name.LocalName);
                        case "culture":
                            return Tuple.Create(2, s.Name.LocalName);
                        case "oldVersion":
                            return Tuple.Create(0, s.Name.LocalName);
                        case "newVersion":
                            return Tuple.Create(1, s.Name.LocalName);
                        default:
                            return Tuple.Create(int.MaxValue, s.Name.LocalName);
                    }
                }

                var attr = element.Attributes().OrderBy(i => Comparable(i)).ToList();
                element.ReplaceAttributes(attr);
            }

            var unit = ConfigXml.DetectIndentUnit(target.Root);

            // ensure output runtime element exists
            var runtime = target.Root.Element("runtime");
            if (runtime == null)
            {
                runtime = new XElement("runtime");
                if (target.Root.LastNode is XText tail && string.IsNullOrWhiteSpace(tail.Value))
                    tail.AddBeforeSelf(new XText(newline + unit), runtime);
                else
                    target.Root.Add(new XText(newline + unit), runtime, new XText(newline));
            }

            // remove existing binding elements along with their indentation
            foreach (var element in runtime.Elements(asmv1 + "assemblyBinding").ToList())
                ConfigXml.RemoveWithLeadingWhitespace(element);

            // insert the new bindings, indented to match the document
            var indent = ConfigXml.IndentOf(runtime) + unit;
            foreach (var item in items)
                ConfigXml.Indent(item, indent, unit, newline);
            if (runtime.LastNode is XText close && string.IsNullOrWhiteSpace(close.Value))
            {
                foreach (var item in items)
                    close.AddBeforeSelf(new XText(newline + indent), item);
            }
            else if (items.Count > 0)
            {
                foreach (var item in items)
                    runtime.Add(new XText(newline + indent), item);
                runtime.Add(new XText(newline + ConfigXml.IndentOf(runtime)));
            }

            // save new file, leaving the untouched content formatted as it was
            target.Save(TargetFile.ItemSpec, SaveOptions.DisableFormatting);
            ConfigXml.RestoreTrailingNewline(TargetFile.ItemSpec, original, newline);

            return true;
        }

    }

}
