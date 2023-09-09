using System.Collections;

namespace Tetraizor.Bootstrap.Base
{
    public interface IPersistentSubsystem
    {
        public IEnumerator LoadSubsystem(IPersistentSystem system);
        public string GetSystemName();
    }
}