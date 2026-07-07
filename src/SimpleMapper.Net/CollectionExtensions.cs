using System;
using System.Collections.Generic;

namespace SimpleMapper.Net;

/// <summary>
/// Collection helpers used inside SimpleMapper lambda expressions.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Navigates into the items of a collection inside SimpleMapper lambda expressions.
    /// This method exists only for expression tree parsing — it is never invoked at runtime.
    /// </summary>
    /// <example>
    /// user.Map&lt;User&gt;()
    ///     .Ignore(x => x.Articles.Each().Media.Thumbnails)
    ///     .To&lt;UserDto&gt;();
    /// </example>
    public static T Each<T>(this IEnumerable<T> source)
        => throw new InvalidOperationException(
            "Each() is only for use in SimpleMapper lambda expressions and should never be called directly.");
}
