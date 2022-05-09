using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

public static class HtmlExtensions
{
    public static IEnumerable<HtmlNode> DescendantsWithClass(this HtmlNode node, string className)
    {
        return node.Descendants().Where(d => d.HasClass(className));
    }

    public static IEnumerable<HtmlNode> DescendantsOfType(this HtmlNode node, string type)
    {
        return node.Descendants().Where(d => d.Name == type);
    }
}