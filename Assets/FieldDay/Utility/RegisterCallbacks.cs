using System.Runtime.CompilerServices;

namespace FieldDay {
    /// <summary>
    /// Interface for OnRegister and OnDeregister callbacks.
    /// </summary>
    public interface IRegistrationCallbacks {
        void OnRegister();
        void OnDeregister();
    }

    /// <summary>
    /// IRegistrationCallbacks utilities.
    /// </summary>
    static public class RegistrationCallbacks
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void InvokeRegister(object obj) {
            (obj as IRegistrationCallbacks)?.OnRegister();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void InvokeDeregister(object obj) {
            (obj as IRegistrationCallbacks)?.OnDeregister();
        }
    }
}