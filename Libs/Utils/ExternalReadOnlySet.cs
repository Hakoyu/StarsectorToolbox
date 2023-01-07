using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>
    /// 外部只读集合
    /// </summary>
    public class ExternalReadOnlySet<T> : IReadOnlySet<T>
    {
        private readonly ISet<T> set;
        public ExternalReadOnlySet()
        {
            set = new HashSet<T>();
        }

        public ExternalReadOnlySet(ISet<T> set)
        {
            ArgumentNullException.ThrowIfNull(set);
            this.set = set;
        }
        public int Count => set.Count;
        internal bool Add(T item) => set.Add(item);
        internal bool Remove(T item) => set.Remove(item);
        internal void Clear() => set.Clear();
        public bool Contains(T item) => set.Contains(item);
        public bool IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => set.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => set.SetEquals(other);
        public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
