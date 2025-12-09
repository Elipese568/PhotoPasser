using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Helper;

public class SelfReference : DependencyObject
{


    public object Value
    {
        get { return (object)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(SelfReference), new PropertyMetadata(0));


    public SelfReference() { }
}
