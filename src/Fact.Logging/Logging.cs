#define DEBUG2

#if MONODROID
using Android.Util;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Diagnostics;
using System.Transactions;
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
        internal static System.Reflection.MethodBase GetMethod(int frameIndex = 2)
        {
            var stack = new System.Diagnostics.StackTrace();
            var frame = stack.GetFrame(frameIndex);
            var method = frame.GetMethod();
            return method;
        }


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
#if MONODROID
                return Global.Container.Resolve<Castle.Core.Logging.ILoggerFactory>();
#else
                var factory = Global.Container.TryResolve<Castle.Core.Logging.ILoggerFactory>();
                if (factory == null)
                    return new Castle.Core.Logging.NullLogFactory();
                return factory;
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
        /// Same as ILogger.Error, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void E(this Castle.Core.Logging.ILogger logger, string message)
        {
            var method = LogManager.GetMethod();
            logger.Error(method.Name + ": " + message);
        }

        /// <summary>
        /// Same as ILogger.Error, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void E(this Castle.Core.Logging.ILogger logger, string message, Exception exception)
        {
            var method = LogManager.GetMethod();
            logger.Error(method.Name + ": " + message, exception);
        }


        /// <summary>
        /// Same as ILogger.Fatal, except this automatically prepends calling method's name
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public static void F(this Castle.Core.Logging.ILogger logger, string message, Exception exception)
        {
            var method = LogManager.GetMethod();
            logger.Fatal(method.Name + ": " + message, exception);
        }
    }
}


namespace Fact.Apprentice.Core.SI
{
    using Fact.Apprentice.Core.DAL;
    using Fact.Apprentice.Core.Validation;

    /// <summary>
    /// MSEL-style log record. 
    /// </summary>
    /// <remarks>
    /// TODO: Eventually refactor this out of Fact.Apprentice.Core
    /// </remarks>
#if !OSS
    [SourceTable("Log")]
#endif
    public class LogRecord
    {
        [Identity]
        public int LogID { get; set; }
        public int? EventID { get; set; }
        public int Priority { get; set; }

        [Length(32)]
        [Required]
        public string Severity { get; set; }

        [Length(256)]
        [Required]
        public string Title { get; set; }

        public DateTime Timestamp { get; set; }

        [Length(32)]
        [Required]
        public string MachineName { get; set; }

        [Length(512)]
        [Required]
        public string AppDomainName { get; set; }

        [Length(256)]
        [Required]
        public string ProcessID { get; set; }

        [Length(512)]
        [Required]
        public string ProcessName { get; set; }

        [Length(512)]
        public string ThreadName { get; set; }

        [Length(512)]
        public string Win32ThreadId { get; set; }

        [Length(1500)]
        public string Message { get; set; }

        [CLOB]
        public string FormattedMessage { get; set; }
    }
}

