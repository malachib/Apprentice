// Keep in parity with ApprenticeOS
#define DEBUG2
#if NETCORE
#define MSIOC
#endif

#if MONODROID
#define TINYIOC
using Android.Util;
#endif

using System;
using Castle.Core.Logging;

namespace Fact.Apprentice.Core
{
    /// <summary>
    /// Assist class to ease acquisition of Castle ILogger facades
    /// </summary>
    public class LogManager
    {
        static LogManager()
        {
            FullName = true;
        }

        /// <summary>
        /// When true, utilizes the namespace + class name during a GetCurrentClassLogger
        /// When false, just utilizes the classname
        /// </summary>
        /// <remarks>defaults to True</remarks>
        public static bool FullName { get; set; }
        /// <summary>
        /// NLog-style log acquisition mechanism
        /// Create logger with name of instance's class who is calling GetCurrentClassLogger
        /// </summary>
        /// <returns></returns>
        public static ILogger GetCurrentClassLogger()
        {
            var method = GetMethod();
            var declaringType = method.DeclaringType;
            var name = FullName ? declaringType.FullName : declaringType.Name;
            return GetLogger(name);
        }


        /// <summary>
        /// Acquire an active method info on the stack
        /// </summary>
        /// <param name="frameIndex">defaults to one frame ABOVE the immediate caller</param>
        /// <returns></returns>
#if !NETCORE
        internal static System.Reflection.MethodBase GetMethod(int frameIndex = 2)
        {
            var stack = new System.Diagnostics.StackTrace();
            var frame = stack.GetFrame(frameIndex);
            var method = frame.GetMethod();
            return method;
        }
#else
        
        // Kluding this since NETCORE doesn't allow direct stack frame access,
        // and therefore hard for us to pull proper MethodBase
        internal class _MethodBase
        {
            internal Type DeclaringType;
            internal string Name;
        }

        internal static _MethodBase GetMethod(int frameIndex = 2)
        {
            var stack = Environment.StackTrace;
            var stackLines = stack.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var frame = stackLines[frameIndex - 1];
            var method = frame;

            return new _MethodBase() { DeclaringType = typeof(Nullable), Name = method };
        }
#endif


        /// <summary>
        /// NLog-style log acquisition mechanism
        /// Create logger with name of instance's class who is calling GetCurrentClassLogger
        /// </summary>
        /// <param name="fullName">
        /// Overrides FullName property see <see cref="LogManager.FullName"/>
        /// </param>
        public static ILogger GetCurrentClassLogger(bool fullName)
        {
            var method = GetMethod();
            var declaringType = method.DeclaringType;
            var name = fullName ? declaringType.FullName : declaringType.Name;
            return GetLogger(name);
        }


        /// <summary>
        /// NLog-style log acquisition mechanism
        /// </summary>
        public static ILogger GetLogger(string name)
        {
            return Factory.Create(name);
        }


        public static Castle.Core.Logging.ILoggerFactory Factory
        {
            get
            {
                // MonoDroid uses TinyIoC and is hard-wired to the specialized CastleMonodroidLogger
                // so we confidently resolve directly every time
#if TINYIOC
                return Global.Container.Resolve<Castle.Core.Logging.ILoggerFactory>();
#else
#if MSIOC
                return (ILoggerFactory) Global.Container.GetService(typeof(ILoggerFactory));
#else
                var factory = Global.Container.TryResolve<Castle.Core.Logging.ILoggerFactory>();
                if (factory == null)
                    return new Castle.Core.Logging.NullLogFactory();
                return factory;
#endif
#endif
            }
        }
    }

    /// <summary>
    /// Android-style log extension methods - each of them prepend calling method's name
    /// automatically through stack reflection.  Useful, but don't use them in high 
    /// performance situations
    /// </summary>
    public static class ILogger_Extensions
    {
        /// <summary>
        /// Same as ILogger.Info, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void I(this Castle.Core.Logging.ILogger logger, string message)
        {
            var method = LogManager.GetMethod();
            logger.Info(method.Name + ": " + message);
        }

        /// <summary>
        /// Same as ILogger.Warn, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void W(this Castle.Core.Logging.ILogger logger, string message)
        {
            var method = LogManager.GetMethod();
            logger.Warn(method.Name + ": " + message);
        }


        /// <summary>
        /// Same as ILogger.Warn, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void W(this Castle.Core.Logging.ILogger logger, string message, Exception exception)
        {
            var method = LogManager.GetMethod();
            logger.Warn(method.Name + ": " + message, exception);
        }


        /// <summary>
        /// Same as ILogger.Debug, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void D(this Castle.Core.Logging.ILogger logger, string message)
        {
            var method = LogManager.GetMethod();
            logger.Debug(method.Name + ": " + message);
        }

        /// <summary>
        /// Same as ILogger.Debug, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void D(this ILogger logger, string message, Exception exception)
        {
            var method = LogManager.GetMethod();
            logger.Debug(method.Name + ": " + message, exception);
        }

        /// <summary>
        /// Same as ILogger.Error, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void E(this ILogger logger, string message)
        {
            var method = LogManager.GetMethod();
            logger.Error(method.Name + ": " + message);
        }

        /// <summary>
        /// Same as ILogger.Error, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void E(this ILogger logger, string message, Exception exception)
        {
            var method = LogManager.GetMethod();
            logger.Error(method.Name + ": " + message, exception);
        }


        /// <summary>
        /// Same as ILogger.Fatal, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void F(this ILogger logger, string message, Exception exception)
        {
            var method = LogManager.GetMethod();
            logger.Fatal(method.Name + ": " + message, exception);
        }
    }
}
