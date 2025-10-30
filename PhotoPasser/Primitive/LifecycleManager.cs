using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Primitive;

public class LifecycleManager
{
    private static Dictionary<int, LifecycleManager> _managers = new();

    public static LifecycleManager GetLifecycleManagerForCurrentObject(object obj)
    {
        int hash = obj.GetHashCode();
        if (!_managers.TryGetValue(hash, out LifecycleManager? value))
        {
            value = new LifecycleManager();
            _managers[hash] = value;
        }
        return value;
    }

    private List<EventHandler> _launchingHandlers = new();
    public event EventHandler? Launching
    {
        add
        {
            if (value != null)
            {
                _launchingHandlers.Add(value);
            }
        }
        remove
        {
            if (value != null)
            {
                _launchingHandlers.Remove(value);
            }
        }
    }
    public void RaiseLaunchingEvent()
    {
        lock (new object())
        {
            foreach (var handler in _launchingHandlers)
            {
                handler.Method?.Invoke(handler.Target, [this,EventArgs.Empty]);
            }
        }
    }

    private List<EventHandler> _destroyingHandlers = new();
    public event EventHandler? Destroying
    {
        add
        {
            if (value != null)
            {
                _destroyingHandlers.Add(value);
            }
        }
        remove
        {
            if (value != null)
            {
                _destroyingHandlers.Remove(value);
            }
        }
    }
    public void RaiseDestroyingEvent()
    {
        lock(new object())
        {
            foreach (var handler in _destroyingHandlers)
            {
                handler.Method?.Invoke(handler.Target, [this, EventArgs.Empty]);
            }
        }
    }
}
