using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UrbanLegend.Arg
{
    [Serializable] public class PageLink { public string label; public string target; }
    [Serializable] public class WebPage
    {
        public string id, address, title, source, body, image;
        public string[] terms;
        public PageLink[] links;
        public bool restricted;
    }
    [Serializable] public class ArtDefinition
    {
        public string id, title, resource, caption, overlay;
    }
    [Serializable] public class ContentData
    {
        public WebPage[] pages;
        public ArtDefinition[] art;
        public WebPage Page(string id) => pages.FirstOrDefault(p => p.id == id);
        public ArtDefinition Art(string id) => art.First(a => a.id == id);
        public WebPage[] Search(string query)
        {
            var words = (query ?? "").Split(new[] {' ', '\t', '　'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(Normalize).Where(w => w.Length > 0).ToArray();
            if (words.Length == 0) return Array.Empty<WebPage>();
            return pages.Where(p => !p.restricted && words.All(w => Normalize(
                p.title + p.source + p.body + string.Join(" ", p.terms ?? Array.Empty<string>())).Contains(w))).ToArray();
        }
        public static string Normalize(string value)
        {
            var result = new StringBuilder();
            foreach (char c in (value ?? "").ToLowerInvariant())
                if (char.IsLetterOrDigit(c) || c == '_') result.Append(c);
            return result.ToString();
        }
    }
}
