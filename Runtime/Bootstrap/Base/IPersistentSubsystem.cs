using Tetraizor.Bootstrap.Base;

public interface IPersistentSubsystem<T> where T : IPersistentSystem
{
    public void Init(T system);
}
