using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoPasser.Helper;

public static class EnumerablePredictionExtension
{
    private class TakeEmunerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly Predicate<T> _predicate;
        public TakeEmunerable(IEnumerable<T> source, Predicate<T> predicate)
        {
            _source = source;
            _predicate = predicate;
        }
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _source)
            {
                if (_predicate(item))
                    yield return item;
            }
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<Task<T>> _source;
        public AsyncEnumerable(IEnumerable<Task<T>> source)
        {
            _source = source;
        }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerator(_source.GetEnumerator());
        }
        private class AsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<Task<T>> _enumerator;
            public AsyncEnumerator(IEnumerator<Task<T>> enumerator)
            {
                _enumerator = enumerator;
            }
            public T Current { get; private set; }
            public async ValueTask<bool> MoveNextAsync()
            {
                if (_enumerator.MoveNext())
                {
                    Current = await _enumerator.Current;
                    return true;
                }
                return false;
            }
            public ValueTask DisposeAsync()
            {
                _enumerator.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    extension<TElement>(IEnumerable<TElement> enumerable)
    {
        public int IndexOf(Predicate<TElement> predicate)
        {
            int index = 0;
            foreach (var item in enumerable)
            {
                if (predicate(item))
                    return index;
                index++;
            }
            return -1;
        }

        public IEnumerable<TElement> Use(Predicate<TElement> predicate) => new TakeEmunerable<TElement>(enumerable, predicate);

        

        public bool Contains(Predicate<TElement> predicate)
        {
            foreach (var item in enumerable)
            {
                if (predicate(item))
                    return true;
            }
            return false;
        }
    }

    extension<TElement>(IEnumerable<Task<TElement>> enumerable)
    {
        public IAsyncEnumerable<TElement> EvalResults() => new AsyncEnumerable<TElement>(enumerable);
    }

    extension<TElement>(IAsyncEnumerable<TElement> enumerable)
    {
        public async Task<ObservableCollection<TElement>> AsObservableAsync()
        {
            var collection = new ObservableCollection<TElement>();
            await foreach (var item in enumerable)
            {
                collection.Add(item);
            }
            return collection;
        }
        public async Task<List<TElement>> ToListAsync()
        {
            var list = new List<TElement>();
            await foreach(var item in enumerable)
            {
                list.Add(item);
            }
            return list;
        }
    }
}
