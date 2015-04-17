// Keep in parity with ApprenticeOS

using System;
using System.Collections.Generic;
#if !VNEXT
using System.Linq;
using System.Text;
using Fact.Apprentice.Core.Validation;
//using Fact.Apprentice.Core.Flow;
using System.Security.Permissions;
using Fact.Apprentice.Core;
#endif
using System.Collections;

namespace Fact.Apprentice.Collection
{
    /// <summary>
    /// Encapsulates a particular serialization strategy
    /// </summary>
    /// <remarks>TODO: Move this to Fact.Apprentice.Serialization, 
    /// providing it doesn't muck with MODULAR compilation</remarks>
    public interface ISerializationManager
	{
		void Serialize (System.IO.Stream output, object inputValue);
		object Deserialize (System.IO.Stream input, Type type);
	}

    public interface IParameterProviderCore
    {
        /// <summary>
        /// Retrieves the raw, ordered input parameters.  It is recommended that IParameterInfo be
        /// repeatable.  That is, if this is called twice, the same references are returned
        /// </summary>
        IParameterInfo[] InputParameters { get; }
    }

    /// <summary>
    /// Provider for IStateAccessors providing details of the parameters they must interact
    /// with
    /// </summary>
    public interface IParameterProvider : IParameterProviderCore
    {
        /// <summary>
        /// Retrieve native parameter info given the parameter name.  Case insensitve.
        /// Returns NULL if parameter not available
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>
        /// It is recommende that IParameterInfo reference be repeatable.  That is, if this
        /// is called twice with the same name, the same reference is returned
        /// </remarks>
        IParameterInfo GetParameterByName(string name);
    }


    public interface IParameterProviderSurface
    {
        IParameterProvider ParameterProvider { get; }
    }


    public interface IParameterProvider_MetaData
    {
        IDictionary<string, object> GetMetaData(IParameterInfo p);
    }


    public static class IParameterProvider_Extensions
    {
        /// <summary>
        /// Initializes IParameterProvider from a dictionary
        /// </summary>
        /// <param name="pp"></param>
        /// <param name="source">dictionary to pull keys and values from</param>
        /// <param name="onNullType">If a NULL is found in the dictionary, provide a type manually this way</param>
        /// <remarks>A bit sloppy taking SimpleParameterProvider, revise this when we consolidate IParameterInfo
        /// into ParameterInfo</remarks>
        public static void CopyFrom<T>(this T pp, IDictionary source, Func<string, Type> onNullType)
            where T: IParameterProvider, IParameterProvider_Malleable
        {
            int i = 0;
            foreach (string key in source.Keys)
            {
                object value = source[key];
                pp.Add(key, value == null ? onNullType(key) : value.GetType(), i);
                i++;
            }
        }
    }


	public static class ISerializationManager_Extensions
	{
		public static byte[] Serialize(this ISerializationManager serializationManager, object inputValue)
		{
			using(var ms = new System.IO.MemoryStream())
			{
				serializationManager.Serialize(ms, inputValue);
				ms.Flush();
				return ms.ToArray();
			}
		}


		public static T Deserialize<T>(this ISerializationManager serializationManager, byte[] inputValue)
		{
			using(var ms = new System.IO.MemoryStream(inputValue))
			{
				return (T) serializationManager.Deserialize(ms, typeof(T));
			}
		}
	}

    public interface IParameterProvider_Count
    {
        /// <summary>
        /// Returns current number of parameters registered
        /// </summary>
        int Count { get; }
    }

    public interface IParameterProvider_Malleable<TParameterInfo> : IParameterProvider_Count
    {
        void Add(TParameterInfo parameter);
    }

    public interface IParameterProvider_Malleable
    {
        void Add(string key, Type type, int pos);
    }

    /// <summary>
    /// Consider doing away with IParameterInfo and replace with actual base class ParameterInfo,
    /// to make event handling less clumsy
    /// </summary>
    public interface IParameterInfo
    {
        string Name { get; }
        Type ParameterType { get; }
        int Position { get; }

        /// <summary>
        /// SA-held value is updating
        /// 
        /// As is obvious one potentially can get updates from many different SA's
        /// for this one parameter.  Be aware
        /// </summary>
        event Action<IStateAccessorBase, IParameterInfo> Updating;
        /// <summary>
        /// SA-held value is updating
        /// 
        /// As is obvious one potentially can get updates from many different SA's
        /// for this one parameter.  Be aware
        /// </summary>
        event Action<IStateAccessorBase, IParameterInfo, object> Updated;

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="sa"></param>
        void DoUpdating(IStateAccessorBase sa);
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="sa"></param>
        void DoUpdated(IStateAccessorBase sa, object newValue);
    }

    /// <summary>
    /// Experimental
    /// </summary>
    public interface IParameterInfo_GetterSetter
    {
        object GetValue(object instance);
        void SetValue(object instance, object value);
    }

#if !VNEXT
    public interface IParameterInfo_PropInfo
    {
        Walker.PropInfo Property { get; }
    }
#endif

    /// <summary>
    /// Indexer reusable base interface
    /// Hides base IAccessor - so if implementing explicitly, will need two "gets"
    /// since .NET can't mesh the get from one interface and the set from the other
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IIndexer<TKey, TValue> : IAccessor<TKey, TValue>
    {
        new TValue this[TKey key] { get; set; }
    }

    /// <summary>
    /// Accessor like IIndexer, but for read-only operations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IAccessor<TKey, TValue>
    {
        TValue this[TKey key] { get; }
    }


    public interface IAccessor_Keys<TKey>
    {
        IEnumerable<TKey> Keys { get; }
    }

    /// <summary>
    /// The most basic IStateAccessor, get/set parameters directly by IParameterInfo
    /// </summary>
    public interface IStateAccessorBase : IIndexer<IParameterInfo, object>
    {
        /// <summary>
        /// Fired when a parameter is reassigned
        /// </summary>
        event Action<IParameterInfo, object> ParameterUpdated;
    }

    /// <summary>
    /// Reusable template for standard read-only indexer operations whose lookup key is a string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// Be mindful to mainly use this on implementing classes, not
    /// so much on consumers due to the way IIndexer and IAccessor mesh 
    /// together
    /// </remarks>
    public interface INamedAccessor<TValue> : IAccessor<string, TValue> { }

    /// <summary>
    /// Reusable template for stock standard indexer operations
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public interface INamedIndexer<TValue> : IIndexer<string, TValue>
    {
    }

    /// <summary>
    /// Reusable template for stock standard indexer operations
    /// </summary>
    public interface INamedIndexer : INamedIndexer<object> { }

    public interface IStateAccessor : 
        INamedIndexer<object>,
        IIndexer<int, object>,
        IStateAccessorBase, IParameterProvider//, IResettable
    {
        /// <summary>
        /// Retrieves whether parameters have been updated.
        /// Note 'set' is temporary, in the long run no external processes should be able to change dirty status
        /// </summary>
        /// <remarks>
        /// TODO: Fully Implement ISyncStatus for this, which should smooth out the operations using CopyFrom as well
        /// TODO: Externally manage IsDirty, since having ParameterUpdate events can
        /// make any external entity aware of dirty/touched state
        /// </remarks>
        bool IsDirty { get; set; }
    }


#if !VNEXT
    public static class IStateAccessor_Extensions
    {
        /// <summary>
        /// Copies and clones the contents of one state accessor to another
        /// </summary>
        /// <param name="This"></param>
        /// <param name="destination"></param>
        public static void CloneFrom(this IStateAccessor This, IStateAccessor source)
        {
            for (int i = 0; i < source.InputParameters.Length; i++)
                This[i] = source[i].Clone2();
        }
        /// <summary>
        /// Copies the contents of one state accessor to another
        /// </summary>
        /// <param name="This"></param>
        /// <param name="destination"></param>
        public static void CopyFrom(this IStateAccessor This, IStateAccessor source)
        {
            for (int i = 0; i < source.InputParameters.Length; i++)
                This[i] = source[i];
        }


        /// <summary>
        /// Copy only changed items into the SA
        /// </summary>
        /// <param name="sa"></param>
        /// <param name="getSourceViaKey"></param>
        /// <remarks>TODO: Move this out of the IWorkflow area, it's more reusable than that</remarks>
        public static void CopyFrom(this IStateAccessor sa,
            Func<string, object> getSourceViaKey)
        {
            foreach (var p in sa.InputParameters)
            {
                var original = sa[p];
                var source = getSourceViaKey(p.Name);

                if (!Utility.Equal(source, original))
                {
                    sa[p] = source;
                    /*
                    if (wrapper == null)
                        sa[p] = source;
                    else
                        wrapper(() => sa[p] = source);*/
                }
            }
        }
    }
#endif
}
