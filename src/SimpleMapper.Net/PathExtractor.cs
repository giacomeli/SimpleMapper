using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SimpleMapper.Net;

internal static class PathExtractor
{
    /// <summary>
    /// Extracts the property path from a lambda expression.
    /// x => x.Account.Password            -> ["Account", "Password"]
    /// x => x.Articles.Each().Media       -> ["Articles", "*", "Media"]
    /// </summary>
    public static string[] Extract<T>(Expression<Func<T, object>> expression)
    {
        var path = new List<string>();
        var node = expression.Body;

        // Unwrap Convert (boxing of value types)
        while (node is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            node = unary.Operand;

        Traverse(node, path);

        path.Reverse();
        return path.ToArray();
    }

    private static void Traverse(Expression node, List<string> path)
    {
        switch (node)
        {
            case MemberExpression member:
                path.Add(member.Member.Name);
                Traverse(member.Expression!, path);
                break;

            case MethodCallExpression call when IsEachMethod(call):
                path.Add("*");
                Traverse(call.Arguments[0], path);
                break;

            case ParameterExpression:
                break;

            default:
                throw new ArgumentException(
                    $"Unsupported expression: '{node.NodeType}'. " +
                    "Only property access and Each() are allowed.");
        }
    }

    private static bool IsEachMethod(MethodCallExpression call)
        => call.Method.Name == nameof(CollectionExtensions.Each)
        && call.Method.DeclaringType == typeof(CollectionExtensions);
}
