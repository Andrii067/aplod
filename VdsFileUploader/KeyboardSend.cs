using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VdsFileUploader
{
	internal static class KeyboardSend
	{
		private const int KEYEVENTF_EXTENDEDKEY = 1;

		private const int KEYEVENTF_KEYUP = 2;

		private const byte VK_LWIN = 91;

		private const int WM_KEYDOWN = 256;

		private const int WM_KEYUP = 257;

		[DllImport("user32.dll")]
		private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

		[DllImport("user32.dll")]
		private static extern bool PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint MapVirtualKey(uint uCode, uint uMapType);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern ushort VkKeyScan(char ch);

		public static void SendText(IntPtr window, string text)
		{
			for (int i = 0; i < text.Length; i++)
			{
				ushort num = VkKeyScan(text[i]);
				if ((ushort)(num & 0x100) == 256)
				{
					KeyDown(window, (Keys)160, special: false);
				}
				KeyDown(window, (Keys)(byte)VkKeyScan(text[i]), special: false);
				KeyUp(window, (Keys)(byte)VkKeyScan(text[i]), special: false);
				if ((ushort)(num & 0x100) == 256)
				{
					KeyUp(window, (Keys)160, special: false);
				}
			}
		}

		public static void KeyDown(IntPtr window, Keys vKey, bool special)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Expected I4, but got Unknown
			uint num = 16777216u;
			if (!special)
			{
				num = 0u;
			}
			PostMessage(window, 256u, (byte)vKey, (MapVirtualKey((uint)(int)vKey, 0u) << 16) | 1u | num);
		}

		public static void KeyUp(IntPtr window, Keys vKey, bool special)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Expected I4, but got Unknown
			uint num = 16777216u;
			if (!special)
			{
				num = 0u;
			}
			PostMessage(window, 257u, (byte)vKey, (MapVirtualKey((uint)(int)vKey, 0u) << 16) | 0xC0000001u | num);
		}
	}
}
