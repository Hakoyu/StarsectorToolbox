using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace HKW.Extension;

/// <summary>
/// 集合拓展
/// </summary>
public static class SetExtension
{
    /// <summary>
    /// 只读哈希集合
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    [DebuggerDisplay("Count= {Count}")]
    public class ReadOnlySet<T> : IReadOnlySet<T>
        where T : notnull
    {
        private readonly ISet<T> _set;

        /// <summary>
        /// 初始化只读集合
        /// </summary>
        /// <param name="set">集合</param>
        public ReadOnlySet(HashSet<T> set)
        {
            ArgumentNullException.ThrowIfNull(set);
            _set = set;
        }

        /// <inheritdoc/>
        public int Count => _set.Count;

        /// <inheritdoc/>
        public bool Contains(T item) => _set.Contains(item);

        /// <inheritdoc/>
        public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

        /// <inheritdoc/>
        public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

        /// <inheritdoc/>
        public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

        /// <inheritdoc/>
        public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

        /// <inheritdoc/>
        public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

        /// <inheritdoc/>
        public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// 将普通集合转换为只读集合
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="this">此集合</param>
    /// <returns>只读集合</returns>
    public static ReadOnlySet<T> AsReadOnly<T>(this HashSet<T> @this)
        where T : notnull
    {
        return new ReadOnlySet<T>(@this);
    }
}