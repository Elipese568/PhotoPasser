using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Helper;

public static class ObjectExtensions
{
    extension<T>(T obj)
    {
        public T With(Action<T> action)
        {
            action(obj);
            return obj;
        }
        public T Let(Func<T, T> action)
        {
            return action(obj);
        }
    }
}
