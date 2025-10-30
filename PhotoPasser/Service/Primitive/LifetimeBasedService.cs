using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPasser.Service.Primitive;

public class LifetimeBasedService
{
    protected virtual void OnStart() { }
    protected virtual void OnExit() { }
    public LifetimeBasedService()
    {
        OnStart();
        App.Current.ExitProcess += (s, e) => OnExit();
    }
}
