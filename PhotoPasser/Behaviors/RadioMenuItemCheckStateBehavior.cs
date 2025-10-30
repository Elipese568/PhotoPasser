using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using PhotoPasser.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Behaviors;

public class RadioMenuItemCheckStateBehavior : Behavior<MenuFlyout>
{
    public static string GetCheckedValue(DependencyObject obj)
    {
        return (string)obj.GetValue(CheckedValueProperty);
    }

    public static void SetCheckedValue(DependencyObject obj, string value)
    {
        obj.SetValue(CheckedValueProperty, value);
    }

    // Using a DependencyProperty as the backing store for CheckedValue.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CheckedValueProperty =
        DependencyProperty.RegisterAttached("CheckedValue", typeof(string), typeof(RadioMenuItemCheckStateBehavior), new PropertyMetadata(string.Empty));

    public string ListeningGroupName
    {
        get { return (string)GetValue(ListeningGroupNameProperty); }
        set { SetValue(ListeningGroupNameProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ListeningGroupName.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ListeningGroupNameProperty =
        DependencyProperty.Register(nameof(ListeningGroupName), typeof(string), typeof(RadioMenuItemCheckStateBehavior), new PropertyMetadata(string.Empty));

    public object Value
    {
        get { return (object)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(RadioMenuItemCheckStateBehavior), new PropertyMetadata(null));

    private readonly ConvertingService _converter;

    private List<WeakReference<RadioMenuFlyoutItem>> _radioItemsRefs = new();
    private Dictionary<int, long> _unregisterTokens = new();

    public RadioMenuItemCheckStateBehavior()
    {
        _converter = App.GetService<ConvertingService>();
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Opened += AssociatedObject_Opened_Initialize;
    }

    private void AssociatedObject_Opened_Initialize(object? sender, object e)
    {
        foreach (var item in AssociatedObject.Items)
            _radioItemsRefs.AddRange(FindRadioItems(item, ListeningGroupName));

        foreach (var radioRef in _radioItemsRefs)
        {
            if (radioRef.TryGetTarget(out var radioItem))
            {
                _unregisterTokens.Add(radioItem.GetHashCode(), radioItem.RegisterPropertyChangedCallback(RadioMenuFlyoutItem.IsCheckedProperty, RadioIsCheckedChange));
            }
        }

        foreach(var radioRef in _radioItemsRefs)
        {
            if(radioRef.TryGetTarget(out var radioItem))
            {
                var checkedValue = GetCheckedValue(radioItem);
                if(_converter.Convert(checkedValue, Value.GetType()).Equals(Value))
                {
                    radioItem.IsChecked = true;
                }
            }
        }
        AssociatedObject.Opened -= AssociatedObject_Opened_Initialize;
        AssociatedObject.Opened += AssociatedObject_Opened_Update;
    }

    private void AssociatedObject_Opened_Update(object? sender, object e)
    {
        foreach (var radioRef in _radioItemsRefs)
        {
            if (radioRef.TryGetTarget(out var radioItem))
            {
                var checkedValue = GetCheckedValue(radioItem);
                if (_converter.Convert(checkedValue, Value.GetType()).Equals(Value))
                {
                    radioItem.IsChecked = true;
                }
            }
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        foreach(var radioRef in _radioItemsRefs)
        {
            if(radioRef.TryGetTarget(out var radioItem))
            {
                if(_unregisterTokens.TryGetValue(radioItem.GetHashCode(), out var token))
                {
                    radioItem.UnregisterPropertyChangedCallback(RadioMenuFlyoutItem.IsCheckedProperty, token);
                }
            }
        }

        _radioItemsRefs.Clear();

    }

    private void RadioIsCheckedChange(DependencyObject sender, DependencyProperty prop)
    {
        if ((bool)sender.GetValue(prop) == false)
            return;
        Value = _converter.Convert(GetCheckedValue(sender), Value.GetType());
        sender.SetValue(prop, true);
        foreach(var radioRef in _radioItemsRefs)
        {
            if(radioRef.TryGetTarget(out var radioItem))
            {
                if(radioItem.GetHashCode() != sender.GetHashCode())
                {
                    radioItem.IsChecked = false;
                }
            }
        }
    }

    private static List<WeakReference<RadioMenuFlyoutItem>> FindRadioItems(MenuFlyoutItemBase item, string groupName)
    {
        if(item is RadioMenuFlyoutItem radioItem && radioItem.GroupName == groupName)
        {
            return new List<WeakReference<RadioMenuFlyoutItem>> { new WeakReference<RadioMenuFlyoutItem>(radioItem) };
        }
        else if(item is MenuFlyoutSubItem subItem)
        {
            var list = new List<WeakReference<RadioMenuFlyoutItem>>();
            foreach(var sub in subItem.Items)
            {
                list.AddRange(FindRadioItems(sub, groupName));
            }
            return list;
        }

        return [];
    }
}
