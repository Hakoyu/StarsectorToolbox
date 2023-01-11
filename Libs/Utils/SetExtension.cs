using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>
    /// 集合拓展
    /// </summary>
    public static class SetExtension
    {
        /// <summary>
        /// 外部只读集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        [DebuggerDisplay("Count= {Count}")]
        public class ReadOnlySet<T> : IReadOnlySet<T>
            where T : notnull
        {
            private readonly ISet<T> set;

            /// <summary>
            /// 初始化只读集合
            /// </summary>
            /// <param name="set">集合</param>
            public ReadOnlySet(ISet<T> set)
            {
                ArgumentNullException.ThrowIfNull(set);
                this.set = set;
            }
            /// <inheritdoc/>
            public int Count => set.Count;
            /// <inheritdoc/>
            public bool Contains(T item) => set.Contains(item);
            /// <inheritdoc/>
            public bool IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);
            /// <inheritdoc/>
            public bool IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);
            /// <inheritdoc/>
            public bool IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);
            /// <inheritdoc/>
            public bool IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);
            /// <inheritdoc/>
            public bool Overlaps(IEnumerable<T> other) => set.Overlaps(other);
            /// <inheritdoc/>
            public bool SetEquals(IEnumerable<T> other) => set.SetEquals(other);
            /// <inheritdoc/>
            public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        /// <summary>
        /// 将普通集合转换为只读集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="this">此集合</param>
        /// <returns>只读集合</returns>
        public static ReadOnlySet<T> AsReadOnly<T>(this ISet<T> @this)
            where T : notnull
        {
            return new ReadOnlySet<T>(@this);
        }
    }
}
