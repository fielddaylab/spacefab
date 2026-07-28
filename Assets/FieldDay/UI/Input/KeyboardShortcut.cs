using FieldDay.Components;
using FieldDay.HID;
using System;
using UnityEngine;

namespace FieldDay.UI {
    public sealed class KeyboardShortcut : BatchedComponent {
        [Flags]
        public enum Flags {
            AllowDuringTextEdit = 0x01
        }
        
        public KeyCode KeyCode;
        public ModifierKeyCode Modifiers;
        public Flags Settings;

        // TODO: Handle alternate combos?
        // TODO: Handle clicking
    }
}