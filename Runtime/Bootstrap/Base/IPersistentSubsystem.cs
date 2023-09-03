using System;
using Tetraizor.Bootstrap.Base;

public interface IPersistentSubsystem
{
    public string GetSystemName();
    public void Init(IPersistentSystem system);
}
