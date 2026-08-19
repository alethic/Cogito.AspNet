using System.Linq;
using System.Xml.Linq;

namespace Cogito.AspNet.MSBuild
{

    /// <summary>
    /// Helpers for editing config XML while preserving the existing formatting of the file.
    /// </summary>
    static class ConfigXml
    {

        /// <summary>
        /// Gets the newline sequence in use by the text.
        /// </summary>
        public static string DetectNewline(string text)
        {
            return text.Contains("\r\n") ? "\r\n" : "\n";
        }

        /// <summary>
        /// Gets the leading whitespace of the line the element starts on.
        /// </summary>
        public static string IndentOf(XElement element)
        {
            if (element.PreviousNode is XText text && string.IsNullOrWhiteSpace(text.Value))
            {
                var value = text.Value;
                var index = value.LastIndexOf('\n');
                return index >= 0 ? value.Substring(index + 1) : value;
            }

            return "";
        }

        /// <summary>
        /// Gets the indent unit of the document, derived from the root's first element child.
        /// </summary>
        public static string DetectIndentUnit(XElement root)
        {
            var first = root.Elements().FirstOrDefault();
            var indent = first != null ? IndentOf(first) : "";
            return indent.Length > 0 ? indent : "  ";
        }

        /// <summary>
        /// Removes the element along with the whitespace that precedes it.
        /// </summary>
        public static void RemoveWithLeadingWhitespace(XElement element)
        {
            if (element.PreviousNode is XText text && string.IsNullOrWhiteSpace(text.Value))
                text.Remove();

            element.Remove();
        }

        /// <summary>
        /// Recursively inserts indentation whitespace into the element. Only element content is expected.
        /// </summary>
        public static void Indent(XElement element, string indent, string unit, string newline)
        {
            if (element.HasElements == false)
                return;

            var children = element.Elements().ToList();
            element.RemoveNodes();

            foreach (var child in children)
            {
                element.Add(new XText(newline + indent + unit));
                element.Add(child);
                Indent(child, indent + unit, unit, newline);
            }

            element.Add(new XText(newline + indent));
        }

        /// <summary>
        /// Appends the newline to the file if the original content ended with one and the current content does not.
        /// </summary>
        public static void RestoreTrailingNewline(string path, string original, string newline)
        {
            if (original.EndsWith("\n") == false)
                return;

            var text = System.IO.File.ReadAllText(path);
            if (text.EndsWith("\n") == false)
                System.IO.File.AppendAllText(path, newline);
        }

    }

}
