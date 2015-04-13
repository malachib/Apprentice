using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;
//using Fact.Apprentice.Core;
using System.IO;
using Castle.Core.Logging;
//using Fact.Apprentice.Collection;

namespace Fact.Text
{
	public abstract class KeywordExpander
	{
		static readonly ILogger logger = LogManager.GetCurrentClassLogger();

		enum Mode
		{
			/// <summary>
			/// Default is regular "plaintext" mode
			/// </summary>
			Default,
			/// <summary>
			/// Keyword is when we believe we may have a keyword to process
			/// </summary>
			Keyword,
			/// <summary>
			/// Escaped is to embed characters which normally we cannot (at this time
			/// only \{ and \\ are supported)
			/// </summary>
			Escaped
		}

		Mode mode;

		/// <summary>
		/// For internal use only.  Public so that AggregateKeywordExpander may access it
		/// </summary>
		/// <param name="keyword"></param>
		/// <returns></returns>
		public abstract IEnumerable<char> ExpandKeyword(string keyword);

		public abstract bool CanExpandKeyword(string keyword);

		public abstract IEnumerable<string> KnownKeywords { get; }

		public IEnumerable<char> Expand(IEnumerable<char> input)
		{
			string keyword = null;

			foreach (var c in input)
			{
				switch (mode)
				{
					case Mode.Escaped:
						mode = Mode.Default;
						yield return c;
						break;

					case Mode.Default:

						switch (c)
						{
							case '\\':
								mode = Mode.Escaped;
								break;

							case '{':
								mode = Mode.Keyword;
								keyword = "";
								break;

							default:
								yield return c;
								break;
						}
						break;

					case Mode.Keyword:

						switch (c)
						{
							// this is an escaped '{' so add it in directly
							case '{':
								mode = Mode.Default;
								yield return c;
								break;

							case '}':
								{
									IEnumerable<char> expandedKeyword;
									try
									{
										expandedKeyword = ExpandKeyword(keyword);
									}
									catch (Exception e)
									{
										logger.Error("Expand Unable to expand keyword: " + keyword + " due to exception " + e.Message);
										logger.Error("Expand stack trace: " + e.StackTrace, e);
										throw;
									}
									finally { }
									foreach (var _c in expandedKeyword) yield return _c;
									mode = Mode.Default;
									break;
								}

							default:
								keyword += c;
								break;

						}
						break;
				}
			}
		}
	}

	public class PropertyKeywordExpander : KeywordExpander
	{
		object source;
		Type t;

		public PropertyKeywordExpander(object source)
		{
			this.source = source;
			t = source.GetType();
		}

		public override IEnumerable<string> KnownKeywords
		{
			get { return t.GetProperties(System.Reflection.BindingFlags.Public).Select(x => x.Name); }
		}

		public override IEnumerable<char> ExpandKeyword(string keyword)
		{
			var prop = t.GetProperty(keyword);
			return prop.GetValue(source, null).ToString();
		}

		public override bool CanExpandKeyword(string keyword)
		{
			return t.GetProperty(keyword) != null;
		}
	}


	public class AccessorKeywordExpander : KeywordExpander
	{
		readonly IAccessor<string, string> keywordExpansions;

		public AccessorKeywordExpander(IAccessor<string, string> keywordExpansions)
		{
			this.keywordExpansions = keywordExpansions;
		}

		public override IEnumerable<char> ExpandKeyword(string keyword)
		{
			return keywordExpansions[keyword];
		}

		public override bool CanExpandKeyword(string keyword)
		{
			return keywordExpansions[keyword] != null;
		}

		public override IEnumerable<string> KnownKeywords
		{
			get 
			{
				var k = keywordExpansions as IAccessor_Keys<string>;

				if (k != null)
					return k.Keys;
				else
					throw new InvalidOperationException();
			}
		}
	}


	/// <summary>
	/// The most basic keyword expander, provide it a property bag (NameValueCollection)
	/// and it will resolve based on that bag's contents
	/// </summary>
	/// <remarks>
	/// TODO: Replace with AccessorKeywordExpander
	/// </remarks>
	public class SimpleKeywordExpander : KeywordExpander
	{
		NameValueCollection keywordExpansions;

		public SimpleKeywordExpander(NameValueCollection c)
		{
			keywordExpansions = c;
		}

		public override IEnumerable<char> ExpandKeyword(string keyword)
		{
			return keywordExpansions[keyword];
		}

		public override bool CanExpandKeyword(string keyword)
		{
			return keywordExpansions[keyword] != null;
		}

		public override IEnumerable<string> KnownKeywords
		{
			get { return keywordExpansions.Keys.Cast<string>(); }
		}
	}



	public class AggregateKeywordExpander : KeywordExpander
	{
		Dictionary<string, KeywordExpander> expanders = new Dictionary<string, KeywordExpander>();
		Dictionary<string, KeywordExpander> keywordToExpanderMapping = null;

		/// <summary>
		/// Add a sub-keyword expander to this aggregate keyword expander, qualified by a keyword prefix
		/// </summary>
		/// <param name="e">Expander to additionally add to this aggregator</param>
		/// <param name="prefix">prefix to qualify this particular expander by</param>
		public void Add(KeywordExpander e, string prefix)
		{
			expanders.Add(prefix, e);
		}

		public override IEnumerable<string> KnownKeywords
		{
			get 
			{
				foreach (var e in expanders)
				{
					foreach (var keyword in e.Value.KnownKeywords)
						yield return e.Key + "." + keyword;
				}
			}
		}

		public override IEnumerable<char> ExpandKeyword(string keyword)
		{
			KeywordExpander expander = null;
			var seperator = keyword.IndexOf('.');
			string prefix = null;

			if (seperator != -1)
			{
				prefix = keyword.Substring(0, seperator);
				keyword = keyword.Substring(seperator + 1);
			}

			// First, see if we have a hard override for a particular keyword to come from
			// a particular expander
			if(keywordToExpanderMapping != null)
				keywordToExpanderMapping.TryGetValue(keyword, out expander);

			// Then, see if we have a prefix to specify a particular expander i.e. [prefix].[keyword]
			if (prefix != null)
				expander = expanders[prefix];

			// Finally, if no expander has been found, then iterate through each in order of Add-ition
			// looking for the specified keyword
			if (expander == null)
			{
				foreach (var e in expanders.Values)
				{
					if (e.CanExpandKeyword(keyword))
						return e.ExpandKeyword(keyword);
				}
			}
			else
				// Alternatively, if we did find an appropriate expander candidate, then 
				// expand using it here
				return expander.ExpandKeyword(keyword);

			throw new KeyNotFoundException("Cannot expand keyword: " + keyword);
		}

		public override bool CanExpandKeyword(string keyword)
		{
			bool canExpand = false;

			foreach (var e in expanders)
			{
				canExpand |= e.Value.CanExpandKeyword(keyword);
				if (canExpand)
					return true;
			}

			return false;
		}
	}


	public static class KeywordExpander_Extensions
	{
		static IEnumerable<char> Read(TextReader input)
		{
			for (int ch = input.Read(); ch != -1; ch = input.Read())
				yield return (char)ch;
		}

		public static void Expand(this KeywordExpander keywordExpander, TextReader input, TextWriter output)
		{
			var _input = Read(input);

			var _output = keywordExpander.Expand(_input);

			foreach (var c in _output)
				output.Write(c);
		}

		public static string ExpandString(this KeywordExpander keywordExpander, IEnumerable<char> input)
		{
			var sb = new StringBuilder();

			foreach (var c in keywordExpander.Expand(input))
				sb.Append(c);

			return sb.ToString();
		}
	}}

