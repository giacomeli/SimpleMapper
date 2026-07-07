using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleMapper.Net;

internal static class TreeConsole
{
    private const int MaxScalarLen = 80;

    public static void WriteNode(TextWriter w, string label, int depth, bool last, ConsoleColor? color = null)
    {
        var prefix = Prefix(depth, last);
        w.Write(prefix);
        // Colors only make sense on the real console; custom writers get plain text.
        if (color.HasValue && ReferenceEquals(w, Console.Out))
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            w.WriteLine(label);
            Console.ForegroundColor = prev;
        }
        else
        {
            w.WriteLine(label);
        }
    }

    public static string PrettyValue(object? v) =>
        v == null ? "null"
        : v is string s ? Cut(s)
        : v is IEnumerable e && v.GetType() != typeof(string) ? $"collection ({e.Cast<object>().Count()})"
        : v.ToString() ?? "null";

    private static string Cut(string s) =>
        s.Length <= MaxScalarLen ? $"\"{s}\"" : $"\"{s[..MaxScalarLen]}...\"";

    private static string Prefix(int depth, bool last)
    {
        if (depth == 0) return string.Empty;
        Span<char> buf = stackalloc char[depth * 2];
        for (int i = 0; i < depth - 1; i++) { buf[i * 2] = '│'; buf[i * 2 + 1] = ' '; }
        buf[(depth - 1) * 2] = last ? '└' : '├';
        buf[(depth - 1) * 2 + 1] = '─';
        return buf.ToString();
    }
}
