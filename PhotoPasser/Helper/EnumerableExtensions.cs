using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Helper;

static class EnumerableExtensions
{
    extension<TElement>(IEnumerable<TElement> enumerable)
    {
        public ObservableCollection<TElement> AsObservable()
        {
            return new ObservableCollection<TElement>(enumerable);
        }
    }
}
