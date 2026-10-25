#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Paramore.Darker
{
    /// <summary>
    /// The single path through which a handler's pipeline is read by reflection: method lookup, decorator
    /// attributes in builder order, and decorator-type closing. It throws nothing itself; each failure is
    /// returned, typed by the operation that failed, so the pipeline builder can rethrow it unchanged.
    /// </summary>
    internal static class PipelineAttributeReader
    {
        private const string EXECUTE_METHOD_NAME = "Execute";
        private const string EXECUTE_ASYNC_METHOD_NAME = "ExecuteAsync";

        /// <summary>Finds the handler's sync <c>Execute</c> method.</summary>
        public static ExecuteMethodLookup FindExecute(Type handlerType)
            => Lookup(() => handlerType.GetMethod(EXECUTE_METHOD_NAME));

        /// <summary>Finds the handler's <c>ExecuteAsync</c> method.</summary>
        public static ExecuteMethodLookup FindExecuteAsync(Type handlerType)
            => Lookup(() => handlerType.GetMethod(EXECUTE_ASYNC_METHOD_NAME));

        /// <summary>Finds a stream handler's <c>ExecuteAsync(TQuery, CancellationToken)</c> by signature.</summary>
        public static ExecuteMethodLookup FindStreamExecuteAsync(Type handlerType, Type queryType)
            => Lookup(() => handlerType.GetMethod(EXECUTE_ASYNC_METHOD_NAME, new[] { queryType, typeof(CancellationToken) }));

        /// <summary>
        /// Reads the <typeparamref name="TAttribute"/> attributes of <paramref name="method"/>, orders them by
        /// descending <paramref name="stepOf"/>, and closes each decorator type over <paramref name="queryType"/> and <paramref name="resultType"/>.
        /// </summary>
        /// <remarks>
        /// Entries are returned even when a later one fails to close, so a caller that creates decorators in order
        /// still creates (and tracks) the earlier ones before it reaches the failing entry.
        /// </remarks>
        public static DecoratorEntries<TAttribute> ReadDecorators<TAttribute>(
            MemberInfo method,
            Type queryType,
            Type resultType,
            Func<TAttribute, int> stepOf,
            Func<TAttribute, Type> decoratorTypeOf)
            where TAttribute : Attribute
        {
            List<TAttribute> attributes;
            try
            {
                attributes = method.GetCustomAttributes(typeof(TAttribute), true)
                    .Cast<TAttribute>()
                    .OrderByDescending(stepOf)
                    .ToList();
            }
            catch (Exception ex)
            {
                return new DecoratorEntries<TAttribute>([], new AttributeReadFailure(AttributeReadFailureKind.AttributeRead, ex));
            }

            return new DecoratorEntries<TAttribute>(attributes.Select(attribute => Close(attribute, queryType, resultType, decoratorTypeOf)).ToList(), null);
        }

        /// <summary>
        /// Checks that <paramref name="method"/> carries no decorator attributes belonging to another path, in the
        /// order the builder has always checked them, and returns the message for the first wrong kind found.
        /// </summary>
        public static WrongKindCheck CheckAttributeKinds(MemberInfo? method, HandlerPath path)
        {
            try
            {
                foreach (var (wrongAttributeType, message) in WrongKindsFor(path))
                {
                    var found = method!.GetCustomAttributes(wrongAttributeType, true);
                    if (found.Length > 0)
                        return new WrongKindCheck(message, found.Select(attribute => attribute.GetType()).ToList(), null);
                }

                return new WrongKindCheck(null, [], null);
            }
            catch (Exception ex)
            {
                return new WrongKindCheck(null, [], new AttributeReadFailure(AttributeReadFailureKind.AttributeRead, ex));
            }
        }

        private static IEnumerable<(Type WrongAttributeType, string Message)> WrongKindsFor(HandlerPath path)
        {
            switch (path)
            {
                case HandlerPath.Sync:
                    yield return (typeof(QueryHandlerAttributeAsync),
                        "Sync handler has async attribute(s) on Execute. Use sync attributes (e.g. QueryHandlerAttribute) for sync handlers, or switch to an async handler with ExecuteAsync.");
                    yield return (typeof(StreamQueryHandlerAttribute),
                        "Sync handler has stream attribute(s) on Execute. Use StreamQueryHandlerAttribute only on stream handlers implementing IStreamQueryHandler.");
                    break;
                case HandlerPath.Async:
                    yield return (typeof(QueryHandlerAttribute),
                        "Async handler has sync attribute(s) on ExecuteAsync. Use async attributes (e.g. QueryHandlerAttributeAsync) for async handlers, or switch to a sync handler with Execute.");
                    yield return (typeof(StreamQueryHandlerAttribute),
                        "Async handler has stream attribute(s) on ExecuteAsync. Use StreamQueryHandlerAttribute only on stream handlers implementing IStreamQueryHandler.");
                    break;
                case HandlerPath.Stream:
                    yield return (typeof(QueryHandlerAttribute),
                        "Stream handler has sync attribute(s) on ExecuteAsync. Use StreamQueryHandlerAttribute for stream handlers.");
                    yield return (typeof(QueryHandlerAttributeAsync),
                        "Stream handler has async attribute(s) on ExecuteAsync. Use StreamQueryHandlerAttribute for stream handlers.");
                    break;
            }
        }

        private static ExecuteMethodLookup Lookup(Func<MethodInfo?> find)
        {
            try
            {
                return new ExecuteMethodLookup(find(), null);
            }
            catch (Exception ex)
            {
                return new ExecuteMethodLookup(null, new AttributeReadFailure(AttributeReadFailureKind.MethodLookup, ex));
            }
        }

        private static DecoratorEntry<TAttribute> Close<TAttribute>(
            TAttribute attribute,
            Type queryType,
            Type resultType,
            Func<TAttribute, Type> decoratorTypeOf)
            where TAttribute : Attribute
        {
            Type? openType;
            try
            {
                openType = decoratorTypeOf(attribute);
            }
            catch (Exception ex)
            {
                return new DecoratorEntry<TAttribute>(attribute, null, new AttributeReadFailure(AttributeReadFailureKind.DecoratorType, ex));
            }

            if (openType == null)
                return new DecoratorEntry<TAttribute>(attribute, null,
                    new AttributeReadFailure(AttributeReadFailureKind.DecoratorType, new NullReferenceException()));

            try
            {
                return new DecoratorEntry<TAttribute>(attribute, openType.MakeGenericType(queryType, resultType), null);
            }
            catch (Exception ex)
            {
                return new DecoratorEntry<TAttribute>(attribute, null, new AttributeReadFailure(AttributeReadFailureKind.DecoratorClosing, ex));
            }
        }
    }
}
