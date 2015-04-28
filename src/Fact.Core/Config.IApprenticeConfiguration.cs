// Keep parity with Apprentice & ApprenticeOpen

using System;
using Fact.Apprentice.Core;

namespace Fact.Apprentice.Configuration
{
    // One may use MS-provided System.ComponentModel.DefaultValue also if that is preferred
    public class DefaultValueAttribute : Attribute
    {
        public object Default { get; private set; }

        public DefaultValueAttribute(object value)
        {
            Default = value;
        }
    }


    public class AliasAttribute : Attribute, INamed
    {
        public AliasAttribute(string name) { Name = name; }

        public string Name
        {
            get;
            set;
        }
    }


    [Alias("fact.apprentice.")]
    public interface IApprenticeConfiguration
    {
        [Alias("::")]
        string unit_storedresults_dir { get; }
        [Alias("workflow.ro-mapper.enabled")]
        bool IsROMapperEnabled { get; }
        [Alias("core.pluginDirectory")]
        string PluginDirectory { get; }
        [Alias("::logging.level")]
        string LoggingLevel { get; }
        [Alias("::transitioner.autoinstall")]
        bool? TransitionerAutoinstall { get; }
        [Alias("workflow.manager.autoStart")]
        [DefaultValue(true)]
        bool WorkflowManagerAutoStart { get; }
        [Alias("core.dal.orm.Prefix")]
        string ORMPrefix { get; }
        [Alias("core.dal.orm.default")]
        [DefaultValue("default")]
        string ORMDefault { get; }

        /// <summary>
        /// debug switch to globally enable APR-237 mode (reflected step templates)
        /// regardless of APR-237 attribute
        /// </summary>
        [Alias("core.apr-237")]
        bool APR_237 { get; }

        /// <summary>
        /// EarlyInit mode is for situations where logging is needed earlier, and the fast, graceful 
        /// async log initialization doesn't complete soon enough.
        /// 
        /// Note that setting this to true will slow down startup times, and also does not work with
        /// IoC container logging
        /// </summary>
        [Alias("core.logging.earlyInit")]
        bool LoggingEarlyInit { get; }

        [Alias("::logging.directory")]
        string LoggingDir { get; }

    }
}
