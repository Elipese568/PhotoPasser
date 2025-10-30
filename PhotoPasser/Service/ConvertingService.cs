using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Service;

public class ConvertingService
{
    private Dictionary<Tuple<Type, Type>, Converter<object,object>> _converters = new();

    public ConvertingService Register<TInput,TOutput>(Converter<TInput,TOutput> converter)
    {
        var key = Tuple.Create(typeof(TInput), typeof(TOutput));
        _converters.TryAdd(key, input => converter((TInput)input));
        return this;
    }

    public TOutput Convert<TInput,TOutput>(TInput input)
    {
        return (TOutput)Convert(input, typeof(TInput), typeof(TOutput));
    }

    public object Convert(object value, Type convertTo, Type convertFrom = null)
    {
        var key = Tuple.Create(convertFrom??value.GetType(), convertTo);
        if (_converters.TryGetValue(key, out var converter))
        {
            return converter(value);
        }
        throw new InvalidOperationException($"No converter registered for {convertFrom} to {convertTo}");
    }
}
