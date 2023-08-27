using System.Collections;
using System.Collections.Generic;

namespace Tetraizor.SystemManager.Base
{
    public interface IPersistentSystem
    {
        public IEnumerator LoadSystem();
        public IEnumerator UnloadSystem();
        public string GetName();
    }
}