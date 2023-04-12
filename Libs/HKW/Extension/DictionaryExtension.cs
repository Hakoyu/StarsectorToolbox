using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace HKW.Extension;

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
    [DebuggerDisplay("Count = {Count}")]
    public class ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue>
        : IDictionary<TKey, TReadOnlyValue>
        where TKey : notnull
        where TValue : TReadOnlyValue
    {
        private readonly IDictionary<TKey, TValue> r_dictionary;

        /// <summary>
        /// 构造只读字典
        /// </summary>
        /// <param name="dictionary">字典</param>
        /// <exception cref="ArgumentNullException">输入为null</exception>
        public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
        {
            r_dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        /// <inheritdoc/>
        public int Count => r_dictionary.Count;

        /// <inheritdoc/>
        ICollection<TKey> IDictionary<TKey, TReadOnlyValue>.Keys => r_dictionary.Keys;

        /// <inheritdoc/>
        ICollection<TReadOnlyValue> IDictionary<TKey, TReadOnlyValue>.Values =>
            r_dictionary.Values.Cast<TReadOnlyValue>().ToList();

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        TReadOnlyValue IDictionary<TKey, TReadOnlyValue>.this[TKey key]
        {
            get => r_dictionary[key];
            set => throw new Exception("Is ReadOnly");
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TReadOnlyValue>> GetEnumerator() =>
            r_dictionary
                .Select(x => new KeyValuePair<TKey, TReadOnlyValue>(x.Key, x.Value))
                .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public void Add(TKey key, TReadOnlyValue value) => throw new Exception("Is ReadOnly");

        /// <inheritdoc/>
        public bool Remove(TKey key) => throw new Exception("Is ReadOnly");

        /// <inheritdoc/>
        public void Add(KeyValuePair<TKey, TReadOnlyValue> item) =>
            throw new Exception("Is ReadOnly");

        /// <inheritdoc/>
        public void Clear() => throw new Exception("Is ReadOnly");

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<TKey, TReadOnlyValue> item) =>
            throw new Exception("Is ReadOnly");

        /// <inheritdoc/>
        public bool ContainsKey(TKey key) => r_dictionary.ContainsKey(key);

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<TKey, TReadOnlyValue> item) =>
            r_dictionary.ContainsKey(item.Key);

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TReadOnlyValue value)
        {
            var r = r_dictionary.TryGetValue(key, out var v);
            value = v!;
            return r;
        }

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TReadOnlyValue>[] array, int arrayIndex) =>
            array = r_dictionary
                .ToDictionary(kv => kv.Key, kv => (TReadOnlyValue)kv.Value)
                .ToArray()[arrayIndex..];
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
    public static ReadOnlyDictionary<TKey, TReadOnlyValue> AsReadOnly<TKey, TValue, TReadOnlyValue>(
        this IDictionary<TKey, TValue> @this
    )
        where TKey : notnull
        where TValue : TReadOnlyValue
    {
        return new(new ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue>(@this));
    }
}
