using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Framework;

namespace Cogito.AspNet.MSBuild
{

    /// <summary>
    /// Removes the assembly binding redirects from the given file.
    /// </summary>
    public class RemoveAssemblyBindingRedirects : Microsoft.Build.Utilities.Task
    {

        static readonly XNamespace asmv1 = "urn:schemas-microsoft-com:asm.v1";

        /// <summary>
        /// File from which to remove binding redirects.
        /// </summary>
        [Required]
        public ITaskItem File { get; set; }

        public override bool Execute()
        {
            var original = System.IO.File.ReadAllText(File.ItemSpec);
            var newline = ConfigXml.DetectNewline(original);

            var file = XDocument.Load(File.ItemSpec, LoadOptions.PreserveWhitespace);

            var items = file.Root.Element("runtime")?.Elements(asmv1 + "assemblyBinding") ?? Enumerable.Empty<XElement>();
            foreach (var element in items.ToList())
                ConfigXml.RemoveWithLeadingWhitespace(element);

            file.Save(File.ItemSpec, SaveOptions.DisableFormatting);
            ConfigXml.RestoreTrailingNewline(File.ItemSpec, original, newline);

            return true;
        }

    }

}
