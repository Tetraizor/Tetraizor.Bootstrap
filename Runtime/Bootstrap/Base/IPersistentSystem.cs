using System.Collections;

namespace Tetraizor.Bootstrap.Base
{
    public interface IPersistentSystem
    {
        public IEnumerator LoadSystem();
        public IEnumerator UnloadSystem();
        public string GetName();
    }
}