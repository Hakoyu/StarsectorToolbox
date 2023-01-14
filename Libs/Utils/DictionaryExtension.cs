using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>
    /// 提供用于任何字典的方法 <see cref="AsReadOnly{TKey, TValue, TReadOnlyValue}(IDictionary{TKey, TValue})"/>
    /// </summary>
    public static class DictionaryExtension
    {
        /// <summary>
        /// 只读字典包装器
        /// </summary>
        /// <typeparam name="TKey">键</typeparam>
        /// <typeparam name="TValue">值</typeparam>
        /// <typeparam name="TReadOnlyValue">只读值</typeparam>
        [DebuggerDisplay("Count= {Count}")]
        public class ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue> : IReadOnlyDictionary<TKey, TReadOnlyValue>
                where TKey : notnull
                where TValue : TReadOnlyValue
        {
            private IDictionary<TKey, TValue> _dictionary;

            /// <summary>
            /// 构造只读字典
            /// </summary>
            /// <param name="dictionary">字典</param>
            /// <exception cref="ArgumentNullException">输入为null</exception>
            public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            }
            /// <inheritdoc/>
            public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

            /// <inheritdoc/>
            public IEnumerable<TKey> Keys => _dictionary.Keys;

            /// <inheritdoc/>
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TReadOnlyValue value)
            {
                var r = _dictionary.TryGetValue(key, out var v);
                value = v!;
                return r;
            }

            /// <inheritdoc/>
            public IEnumerable<TReadOnlyValue> Values => _dictionary.Values.Cast<TReadOnlyValue>();

            /// <inheritdoc/>
            public TReadOnlyValue this[TKey key] => _dictionary[key];

            /// <inheritdoc/>
            public int Count => _dictionary.Count;

            /// <inheritdoc/>
            public IEnumerator<KeyValuePair<TKey, TReadOnlyValue>> GetEnumerator() => _dictionary.Select(x => new KeyValuePair<TKey, TReadOnlyValue>(x.Key, x.Value)).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// 创建一个只读字典,可手动转换字典中的值为只读模式
        /// <para>示例:
        /// <code lang="csharp">
        /// <![CDATA[
        /// Dictionary<int, List<int>> dic = new();
        /// var readOnlyDic = dic.AsReadOnly<int, List<int>, IReadOnlyCollection<int>>();
        /// 
        /// Dictionary<int, HashSet<int>> dic = new();
        /// var readOnlyDic = dic.AsReadOnly<int, HashSet<int>, IReadOnlySet<int>>();
        /// 
        /// Dictionary<int, Dictionary<int,int>> dic = new();
        /// var readOnlyDic = dic.AsReadOnly<int, Dictionary<int,int>, IReadOnlyDictionary<int,int>>();
        /// ]]>
        /// </code>
        /// </para>
        /// </summary>
        /// <typeparam name="TKey">键</typeparam>
        /// <typeparam name="TValue">值</typeparam>
        /// <typeparam name="TReadOnlyValue">只读值</typeparam>
        /// <param name="this">此字典</param>
        /// <returns>只读字典</returns>
        public static IReadOnlyDictionary<TKey, TReadOnlyValue> AsReadOnly<TKey, TValue, TReadOnlyValue>(this IDictionary<TKey, TValue> @this)
            where TKey : notnull
            where TValue : TReadOnlyValue
        {
            return new ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue>(@this);
        }
    }
}
