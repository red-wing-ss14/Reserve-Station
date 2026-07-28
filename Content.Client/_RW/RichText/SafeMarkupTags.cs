using Content.Client.UserInterface.RichText;
using Content.Goobstation.UIKit.UserInterface.RichText;
using Robust.Client.UserInterface.RichText;

namespace Content.Client._RW.RichText;

public static class SafeMarkupTags
{
    /// <summary>
    /// Safe markups for everything!!!
    /// </summary>
    public static readonly Type[] Basic =
    {
        typeof(BoldItalicTag),
        typeof(BoldTag),
        typeof(BulletTag),
        typeof(ColorTag),
        typeof(HeadingTag),
        typeof(ItalicTag),
        typeof(MonoTag),
    };

    /// <summary>
    /// Safe markups only for news UIs
    /// Because... this should be.
    /// </summary>
    public static readonly Type[] NewsArticle = Basic;
}
