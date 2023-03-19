using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 项目集合模型
/// </summary>
[DebuggerDisplay("{Name},Count = {ItemsSource.Count}")]
public partial class ItemsCollectionVM<T> : ControlVMBase, IList<T>, IEnumerable<T>, IEnumerable
{
    /// <summary>
    /// 项目资源
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<T> _itemsSource = null!;

    /// <inheritdoc/>
    public T this[int index] { get => ItemsSource[index]; set => ItemsSource[index] = value; }

    /// <inheritdoc/>
    public int Count => ItemsSource.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => ((IList)ItemsSource).IsReadOnly;

    /// <inheritdoc/>
    public void Add(T item) => ItemsSource.Add(item);

    /// <inheritdoc/>
    public void Clear() => ItemsSource.Clear();

    /// <inheritdoc/>
    public bool Contains(T item) => ItemsSource.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(T[] array, int arrayIndex) => ItemsSource.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public int IndexOf(T item) => ItemsSource.IndexOf(item);

    /// <inheritdoc/>
    public void Insert(int index, T item) => ItemsSource.Insert(index, item);

    /// <inheritdoc/>
    public bool Remove(T item) => ItemsSource.Remove(item);

    /// <inheritdoc/>
    public void RemoveAt(int index) => ItemsSource.RemoveAt(index);

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => ItemsSource.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}