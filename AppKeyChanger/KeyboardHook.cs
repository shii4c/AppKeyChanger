using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace AppKeyChanger
{
    class KeyboardHook
    {
        public static int WH_KEYBOARD_LL = 13;
        public static int WM_KEYDOWN = 0x0100;
        public static int WM_KEYUP = 0x0101;
        public static int WM_SYSKEYDOWN = 0x0104;
        public static int WM_SYSKEYUP = 0x0105;


        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public Int32 vkCode;
            public Int32 scanCode;
            public Int32 flags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }

        private delegate int LowLevelKeyboardProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT kbdHookInfo);
        public delegate int KeyboardHookProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT kbdHookInfo, ref bool isCancel);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(IntPtr hhk, int nCode, int wParam, ref KBDLLHOOKSTRUCT kbdHookInfo);


        public event KeyboardHookProc LowLevelKeyboardEvent;
        private LowLevelKeyboardProc keyboardProc_;
        private IntPtr hhk_;

        public KeyboardHook()
        {
            keyboardProc_ = keyboardProc;
            hhk_ = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc_, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
        }

        public void Dispose()
        {
            if (hhk_ == null) { return; }
            UnhookWindowsHookEx(hhk_);
            hhk_ = IntPtr.Zero;
        }

        ~KeyboardHook()
        {
            Dispose();
        }

        private int keyboardProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT kbdHookInfo)
        {
            bool isCancel = false;
            if (LowLevelKeyboardEvent != null) { LowLevelKeyboardEvent(nCode, wParam, ref kbdHookInfo, ref isCancel); }
            if (isCancel) { return -1; }
            return CallNextHookEx(hhk_, nCode, wParam, ref kbdHookInfo);
        }
    }
}
