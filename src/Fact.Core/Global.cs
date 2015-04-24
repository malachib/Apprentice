// Keep parity with ApprenticeOS (this comment has yet to be copied to closed-source version)

#if MONODROID
#define TINYIOC
#endif
#if NETCORE
#define MSIOC
#endif

using System;
using System.Text;
#if !NETCORE
using System.Configuration;
#endif

#if MSIOC
using Microsoft.Framework.DependencyInjection;
#else
#if !TINYIOC
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Castle.DynamicProxy;
#endif
#endif

namespace Fact.Apprentice.Core
{
    public static class Global
    {
#if MSIOC
        public static readonly IServiceProvider Container;
#else
#if TINYIOC
        public static readonly TinyIoC.TinyIoCContainer Container;
#else // NO MSIOC and NO TINYIOC = Default of Windsor IoC
        public static readonly IWindsorContainer Container;

#if !MODULAR
        internal
#else
        // MODULAR variant externalizes many of the items which use proxy, so make it public (unfortunately)
        public
#endif
        static readonly ProxyGenerator Proxy = new ProxyGenerator();
#endif
#endif

#if !VNEXT
        static Configuration.IApprenticeConfiguration config;
        public static Configuration.IApprenticeConfiguration Config
        {
            get
            {
#if TINYIOC
                return Container.Resolve<Configuration.IApprenticeConfiguration>();
				throw new InvalidOperationException("Not yet supported.  Planned SharedPreferences interaction here");
#else
                if (config == null)
                    config = Configuration.AppConfigInterceptor.Get<Configuration.IApprenticeConfiguration>();
#endif

                return config;
            }
        }
#endif

        static Global()
        {
#if TINYIOC
            Container = new TinyIoC.TinyIoCContainer();
#else
#if MSIOC
            var serviceCollection = new ServiceCollection();
            //serviceCollection.BuildServiceProvider(); // Not yet available? TBD
            //Container = null; 
#else // If not TINYIOC or MSIOC, default to Windsor IoC
            try
            {
                // XmlInterpreter scans web/app.config for castle-related goodies
                var xml = new XmlInterpreter();
                Container = new WindsorContainer(xml);
            }
            // occurs when no castle XML section is present.  Since we tend to programatically register
            // our components, just create a new empty windsor container
            // FIX: try/catch is slow.  Find a better method of initializing Container
            catch (ConfigurationErrorsException)
            {
                Container = new WindsorContainer();
            }
#endif
#endif
        }
    }
}
