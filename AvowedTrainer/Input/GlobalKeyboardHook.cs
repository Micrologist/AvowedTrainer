﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace AvowedTrainer.Input
{
	/// <summary>
	/// A class that manages a global low level keyboard hook
	/// </summary>
	class GlobalKeyboardHook
	{
		#region Constant, Structure and Delegate Definitions
		/// <summary>
		/// defines the callback type for the hook
		/// </summary>
		public delegate int keyboardHookProc(int code, int wParam, ref keyboardHookStruct lParam);
		private readonly keyboardHookProc hookProcDelegate;

		public struct keyboardHookStruct
		{
			public int vkCode;
			public int scanCode;
			public int flags;
			public int time;
			public int dwExtraInfo;
		}

		public const int dc = 999;
		const int WH_KEYBOARD_LL = 13;
		const int WM_KEYDOWN = 0x100;
		const int WM_KEYUP = 0x101;
		const int WM_SYSKEYDOWN = 0x104;
		const int WM_SYSKEYUP = 0x105;
		#endregion

		#region Instance Variables
		/// <summary>
		/// The collections of keys to watch for
		/// </summary>
		public List<Key> HookedKeys = new();
        /// <summary>
        /// Handle to the hook, need this to unhook and call the next hook
        /// </summary>
        nint hhook = nint.Zero;
		#endregion

		#region Events
		/// <summary>
		/// Occurs when one of the hooked keys is pressed
		/// </summary>
		public event KeyEventHandler KeyDown;
		/// <summary>
		/// Occurs when one of the hooked keys is released
		/// </summary>
		public event KeyEventHandler KeyUp;
		#endregion

		#region Constructors and Destructors
		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalKeyboardHook"/> class and installs the keyboard hook.
		/// </summary>
		public GlobalKeyboardHook()
		{
			hookProcDelegate = hookProc;
			hook();
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="GlobalKeyboardHook"/> is reclaimed by garbage collection and uninstalls the keyboard hook.
		/// </summary>
		~GlobalKeyboardHook()
		{
			unhook();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Installs the global hook
		/// </summary>
		public void hook()
		{
            nint hInstance = LoadLibrary("User32");
			hhook = SetWindowsHookEx(WH_KEYBOARD_LL, hookProcDelegate, hInstance, 0);
		}

		/// <summary>
		/// Uninstalls the global hook
		/// </summary>
		public void unhook()
		{
			UnhookWindowsHookEx(hhook);
		}

		/// <summary>
		/// The callback for the keyboard hook
		/// </summary>
		/// <param name="code">The hook code, if it isn't >= 0, the function shouldn't do anyting</param>
		/// <param name="wParam">The event type</param>
		/// <param name="lParam">The keyhook event information</param>
		/// <returns></returns>
		    public int hookProc(int code, int wParam, ref keyboardHookStruct lParam)
        {
            if (code >= 0)
            {
                Key key = KeyInterop.KeyFromVirtualKey(lParam.vkCode);
                if (HookedKeys.Contains(key))
                {
                    var inputSource = Keyboard.PrimaryDevice.ActiveSource;
                    
                    KeyEventArgs kea = new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        inputSource ?? new HwndSource(0, 0, 0, 0, 0, "", IntPtr.Zero),
                        0,
                        key
                    );

                    if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && KeyDown != null)
                    {
                        KeyDown(this, kea);
                    }
                    else if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && KeyUp != null)
                    {
                        KeyUp(this, kea);
                    }
                    if (kea.Handled)
                        return 1;
                    
                }
            }
            return CallNextHookEx(hhook, code, wParam, ref lParam);
        }
		#endregion

		#region DLL imports
		/// <summary>
		/// Sets the windows hook, do the desired event, one of hInstance or threadId must be non-null
		/// </summary>
		/// <param name="idHook">The id of the event you want to hook</param>
		/// <param name="callback">The callback.</param>
		/// <param name="hInstance">The handle you want to attach the event to, can be null</param>
		/// <param name="threadId">The thread you want to attach the event to, can be null</param>
		/// <returns>a handle to the desired hook</returns>
		[DllImport("user32.dll")]
		static extern nint SetWindowsHookEx(int idHook, keyboardHookProc callback, nint hInstance, uint threadId);

		/// <summary>
		/// Unhooks the windows hook.
		/// </summary>
		/// <param name="hInstance">The hook handle that was returned from SetWindowsHookEx</param>
		/// <returns>True if successful, false otherwise</returns>
		[DllImport("user32.dll")]
		static extern bool UnhookWindowsHookEx(nint hInstance);

		/// <summary>
		/// Calls the next hook.
		/// </summary>
		/// <param name="idHook">The hook id</param>
		/// <param name="nCode">The hook code</param>
		/// <param name="wParam">The wparam.</param>
		/// <param name="lParam">The lparam.</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		static extern int CallNextHookEx(nint idHook, int nCode, int wParam, ref keyboardHookStruct lParam);

		/// <summary>
		/// Loads the library.
		/// </summary>
		/// <param name="lpFileName">Name of the library</param>
		/// <returns>A handle to the library</returns>
		[DllImport("kernel32.dll")]
		static extern nint LoadLibrary(string lpFileName);
		#endregion
	}
}
