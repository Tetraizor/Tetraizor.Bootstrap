using System.Collections;

namespace Tetraizor.Bootstrapper.Base
{
    public interface IPersistentSystem
    {
        public IEnumerator LoadSystem();
        public IEnumerator UnloadSystem();
        public string GetName();
    }
}