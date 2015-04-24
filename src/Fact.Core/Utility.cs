// Keep in parity between ApprenticeOpen and Apprentice

#if MONODROID
#define TINYIOC
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
#if !NETCORE
using Castle.Core.Logging;
#endif

namespace Fact.Apprentice.Core
{
    public static partial class Utility
    {
#if !VNEXT
        readonly static ILogger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Null strings get included 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="delim"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static StringBuilder Concat(StringBuilder sb, string delim, IEnumerable<string> s)
        {
            var enumerator = s.GetEnumerator();

            enumerator.MoveNext();
            sb.Append(enumerator.Current);

            while (enumerator.MoveNext())
            {
                sb.Append(delim);
                sb.Append(enumerator.Current);
            }

            return sb;
        }

        /// <summary>
        /// Note that null strings don't get included
        /// </summary>
        /// <param name="delim"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static string Concat(string delim, IEnumerable<string> s)
        {
            string result = null;

            foreach (var _s in s)
            {
                if (!string.IsNullOrEmpty(_s))
                {
                    if (result != null)
                        result += delim + _s;
                    else
                        result = _s;
                }
            }

            return result;
        }

        public static string Concat(string delim, params string[] s)
        {
            return Concat(delim, (IEnumerable<string>)s);
        }


        /// <summary>
        /// Compares two objects, and for each property difference fires off a delegate call
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="differing"></param>
        public static void Compare(object source, object target, Action<PropertyInfo> differing)
        {
            Compare(source, target, (p, s, t) => differing(p));
        }

        /// <summary>
        /// Compares two objects, and for each property difference fires off a delegate call
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="differing"></param>
        public static void Compare(object source, object target, Action<PropertyInfo, object, object> differing)
        {
            // should really use the GoLightweight flavor-- shrink down the code
            //Walker.GoLightweight(source.GetType(), source, target, 
            var type = source.GetType();

            foreach (var prop in type.GetProperties())
            {
                var attribs = prop.GetCustomAttributes(true);

                if (!attribs.Occurrs<WalkerSkipAttribute>())
                {
                    var sourceValue = prop.GetValue(source, null);
                    var targetValue = prop.GetValue(target, null);

                    if (sourceValue == null || targetValue == null)
                    {
                        if (sourceValue != targetValue)
                            differing(prop, sourceValue, targetValue);
                    }
                    else if (!sourceValue.Equals(targetValue))
                        differing(prop, sourceValue, targetValue);
                }
            }
        }


        public static IEnumerable<Walker.Tuple> Differing(Type type, object source, object target, IEnumerable<string> includeFields, IEnumerable<string> excludeFields)
        {
            if (excludeFields != null)
                excludeFields = excludeFields.AsArray();

            if (includeFields != null)
                includeFields = includeFields.AsArray();

#if DEBUG
            // perf hit, but useful for debugging
            ReportInvalidProperties(type, excludeFields);
            ReportInvalidProperties(type, includeFields);
#endif

            var differing = Walker.GoLightweight(type, source, target, x =>
            {
                if (!x.CanWrite) return false;

                if (includeFields != null && !includeFields.Contains(x.Name))
                    return false;

                if (excludeFields != null && excludeFields.Contains(x.Name))
                    return false;

                return true;
            });

            foreach (var i in differing)
            {
                var s = i.sourceValue;
                var t = i.targetValue;

                if (s == t) continue;
                else if (s == null || t == null) yield return i;
                else if (!s.Equals(t)) yield return i;
            }
        }

        /// <summary>
        /// Compares to objects, accounting for NULL
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <returns></returns>
        [Obsolete("Use Object.Equals instead")]
        public static bool Equal(object source, object dest)
        {
            // Handles scenarios null == null, null == not null
            if (source == null) return dest == null;

            // Handles scenarios not null == null
            if (dest == null) return false;

            // Handles all other scenarios
            return source.Equals(dest);
        }


        static void ReportInvalidProperties(Type type, IEnumerable<string> properties)
        {
            if (properties != null)
            {
                var checkIncludeExists = from n in properties
                                         let p = type.GetProperty(n)
                                         where p == null
                                         select n;

                foreach (var item in checkIncludeExists)
                {
                    throw new KeyNotFoundException("Property '" + item + "' not present in type " + type);
                }
            }

        }



        public delegate void NotifyDelegate(string message, System.Diagnostics.TraceEventType level);

        /// <summary>
        /// Much like its HTTP counterpart, this maps ~/ or ~\ to the currently running process path (AppDomain.BaseDirectory)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string MapVirtualPath(string path)
        {
            if (path.StartsWith("~\\") || path.StartsWith("~/"))
                path = AppDomain.CurrentDomain.BaseDirectory + path.Substring(1);

            return path;
        }

        public static long ToUnixStamp(this DateTime dt)
        {
            return DateTimeToUnixStamp(dt);
        }

        public static long DateTimeToUnixStamp(DateTime dt)
        {
            dt = dt.ToUniversalTime();
            var diff = dt.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            return (long)diff.TotalSeconds;
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            //var dtTest = new DateTime(
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }


#endif // !VNEXT
#if !TINYIOC
        /// <summary>
        /// Acquires component registered in windsor container, or returns NULL if
        /// it can't find it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T TryResolve<T>(this Castle.Windsor.IWindsorContainer container, string key = null)
        {
            var handlers = container.Kernel.GetHandlers(typeof(T));
            var handler = key == null ?
                handlers.FirstOrDefault() :
                handlers.FirstOrDefault(x => x.ComponentModel.Name == key);

            if (handler == null)
                return default(T);

            var context = Castle.MicroKernel.Context.CreationContext.CreateEmpty();

            return (T)handler.Resolve(context);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="t">Which service to filter names by</param>
        /// <returns></returns>
        public static IEnumerable<string> GetServiceNames(this Castle.Windsor.IWindsorContainer container, Type t)
        {
            var handlers = container.Kernel.GetHandlers(t);

            return handlers.Select(x => x.ComponentModel.Name);
        }
#else // TINYIOC
        [Obsolete("Update to proper native TinyIoC TryResolve code.  This exists only temporarily as we transition code for vNext")]
        public static T TryResolve<T>(this TinyIoC.TinyIoCContainer container, string key = null)
            where T : class
        {
            T resolved;

            container.TryResolve(key, out resolved);

            return resolved;
        }
#endif // TINYIOC
    }

#if OSS
    // stubs to placehold for non-OSS flavor of Apprentice
    // TODO: Split Commerical & OSS source code out so that OSS can get to actual versions of these
    public interface IResettable
    {
        /// <summary>
        /// Event fired when Reset() operation begins
        /// </summary>
        event Action<IResettable> Resetting;

        /// <summary>
        /// Bring object back to its default/freshly initialized state
        /// </summary>
        void Reset();
    }

    namespace Validation
    {
        public class LengthAttribute : Attribute
        {
            public LengthAttribute(int length) { }

            public int Min { get; set; }
            public int Max { get; set; }
        }

        public class Required : Attribute { }
    }
#endif

    public interface IKeyed<T>
    {
        T Key { get; }
    }

    /// <summary>
    /// Be mindful for explicit interfaces you'll need two setters
    /// </summary>
    /// <remarks>></remarks>
    public interface IKeyed_Setter<T> : IKeyed<T>
    {
        new T Key { get; set; }
    }

    public interface INamed
    {
        string Name { get; }
    }
}
