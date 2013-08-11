using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly:   AssemblyTitle("SL7FIX")]
[assembly: AssemblyProduct("SL7FIX")]
[assembly: AssemblyVersion("1.2.0.0")]

//

namespace SL7FIX { 
	public static class Info {
		public enum Status { Unknown, Unlicensed, Licensed, Grace, Expired, Counterfeit, NoKey }
		public static String    KeyType;
		public static String    OS;
		public static String    Description;
		public static String    PartialKey;
		public static Status    LicenseStatus;
		public static String    StatusMessage;
		public static String    NotifyReason;
		public static Int32?    RearmCount;
		public static Int32?    MinutesLeft;
		public static DateTime? TrustedTime;
		public static DateTime? ExpireTime;
		public static DateTime  InfoTime = DateTime.Now;
	}
}

//

namespace SL7FIX { class AppMain { 
	[STAThread]
	static void Main (string[] args) {
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new WinForm());
	}
}}

//

namespace SL7FIX { static class About {
	public static String Title { get { return "SL7FIX"; } }
}}

//

namespace SL7FIX { static class Config {
	public static String Path { get { return Environment.GetEnvironmentVariable("SYSTEMDRIVE") + @"\SL7FIX"; } }

	public static Dictionary<String,String> Keys = new Dictionary<String,String> {
		{ "ULTIMATE", "D4F6K-QK3RD-TMVMJ-BBMRX-3MBMV" }
	};

	// Shortcut LNK File That Points To C:\SL7FIX\SL7FIX.EXE
	public static String LNK64 = "TAAAAAEUAgAAAAAAwAAAAAAAAEbbAAgAIAAAANv/fjtTls4B2/9+O1OWzgHt8i4RU5bOAQBkAAAsAAAAAQAAAAAAAAAAAAAAAAAAANsAFAAfUOBP0CDqOmkQotgIACswMJ0ZAC9DOlwAAAAAAAAAAAAAAAAAAAAAAAAAUAAxAAAAAAALQxcjEABTTDdGSVgAADoACAAEAO++C0MXIwtDFyMqAAAA0ZkBAAAAJAAAAAAAAAAAAAAAAAAAAFMATAA3AEYASQBYAAAAFgBcADIAAGQAAAtDEisgAFNMN0ZJWC5FWEUAAEIACAAEAO++C0M4KwtDOCsqAAAARe4BAAAACgAAAAAAAAAAAAAAAAAAAFMATAA3AEYASQBYAC4ARQBYAEUAAAAaAAAARQAAABwAAAABAAAAHAAAAC8AAAAAAAAARAAAABMAAAADAAAAHWYxihAAAABPUwBDOlxTTDdGSVhcU0w3RklYLkVYRQAADAAuAFwAUwBMADcARgBJAFgALgBFAFgARQAJAEMAOgBcAFMATAA3AEYASQBYACEAJQBTAHkAcwB0AGUAbQBSAG8AbwB0ACUAXABzAHkAcwB0AGUAbQAzADIAXABTAEgARQBMAEwAMwAyAC4AZABsAGwAVwEAAAkAAKCtAAAAMVNQUzDxJbfvRxoQpfECYIye66wpAAAACgAAAAAfAAAACwAAAFMATAA3AEYASQBYAC4ARQBYAEUAAAAAACkAAAAEAAAAAB8AAAAMAAAAQQBwAHAAbABpAGMAYQB0AGkAbwBuAAAAFQAAAA8AAAAAQAAAAACWZDxTls4BFQAAAAwAAAAAFQAAAABkAAAAAAAAFQAAAA4AAAAAQAAAAABCehFTls4BAAAAAEUAAAAxU1BT7TC92kMAiUen+NATpHNmIikAAABkAAAAAB8AAAAMAAAAUwBMADcARgBJAFgAIAAoAEMAOgApAAAAAAAAAFkAAAAxU1BTpmpjKD2V0hG11gDAT9kY0D0AAAAeAAAAAB8AAAAVAAAAQwA6AFwAUwBMADcARgBJAFgAXABTAEwANwBGAEkAWAAuAEUAWABFAAAAAAAAAAAAAAAAAGAAAAADAACgWAAAAAAAAABkZXZib3gAAAAAAAAAAAAAAMFUIBowVk2bCFEUJjGvf24UVx78AeMRjuAAAnIcVHgAwVQgGjBWTZsIURQmMa9/bhRXHvwB4xGO4AACchxUeAAAAAA=";
}}

//

namespace SL7FIX { class Log {
	public static void MSG (String msg)  { Console.WriteLine(DateTime.Now.ToString() + "\t" + msg); }
	public static void EX (Exception ex) { Log.MSG(ex.Message+"\n\n"+ex.StackTrace); } 
}}

//

namespace SL7FIX { class WinForm : Form {

	private IContainer Components = null;
	private NotifyIcon TrayIcon;

	public WinForm () { Init(); }

	private void Init () {
		Log.MSG("WinForm->Init");

		FormBorderStyle = FormBorderStyle.SizableToolWindow;
		ShowInTaskbar = false;		

		if (Assembly.GetEntryAssembly().Location==Config.Path+@"\SL7FIX.EXE") { Commands.InstallStartupLink(); }

		Commands.SetInfo();

		Load += new EventHandler(Form_Load);

		Components = new Container();
		TrayIcon = GetTrayIcon(Components);
	}

	private void Form_Load (Object o, EventArgs e) { 
		HideForm(); 

		if (Info.LicenseStatus==Info.Status.Expired) {
			if (Info.RearmCount>0) { if (AskQuestion("Your windows trial has expired."+"\n"+"Would you like to rearm the trial?")) { RearmTrial(); } }
			else { MsgBox("Your windows trial has expired and the rearm counter is 0."+"\n"+"Select a reset method from the trayicon to restore your rearm count."); }
		}

		if (Info.LicenseStatus==Info.Status.NoKey) {
			if (!InstallKey()) { MsgBox("SL7FIX Needs A Windows Key To Run, Exiting Now"); }
			Exit(null,null);
		}
	}

	private void Exit       (Object o, EventArgs e) { Application.Exit(); }
	private void ShowInfo   (Object o, EventArgs e) { Commands.ShowInfo(); }
	private void Tray_Click (Object o, EventArgs e) { TrayIcon.ContextMenuStrip.Show(Cursor.Position.X,Cursor.Position.Y); }

	private void HideForm () { Hide(); Location = new Point(-9999,-9999); }

	private void MsgBox (String msg) { MsgBox(msg,About.Title); }
	private void MsgBox (String msg, String title) { MessageBox.Show(msg,title); }

	private Boolean AskQuestion (String q) { 
		var result = MessageBox.Show(q,About.Title,MessageBoxButtons.YesNo,MessageBoxIcon.Question);
		if (result.ToString()=="Yes") { return true; } else { return false; }
	}

	//

	private Boolean InstallKey () { return InstallKey(null,null); }
	private Boolean InstallKey (Object o, EventArgs e) {
		if (AskQuestion("Windows product key is missing, would you like to install one?")) { 
			if (Commands.InstallKey()) {
				MsgBox("Windows product key was sucessfully installed");
				Process.Start(Assembly.GetEntryAssembly().Location);
				return true;
			} else {
				 MsgBox("Key Install Operation Failed");
			}
		} else { MsgBox("Key Install Operation Canceled"); }

		return false;
	}

	//

	private void RearmTrial () { RearmTrial(null,null); }
	private void RearmTrial (Object o, EventArgs e) {
		if (AskQuestion("Are you sure you want to rearm the windows trial?\nThis operation equires a system reboot!")) { 
			if (Commands.RearmTrial()) {
				TrayIcon.ShowBalloonTip(9999,About.Title,"Rebooting to rearm trial...",ToolTipIcon.Info);
			} else {
				 MsgBox("Rearm Trial Operation Failed");
			}
		} else { MsgBox("Rearm Trial Operation Canceled"); }
	}

	//

	private void ResetCounter_DELWPA () { ResetCounter_DELWPA(null,null); }
	private void ResetCounter_DELWPA (Object o, EventArgs e) {
		if (AskQuestion("Are you sure you want to reset the counter?\nThis operation requires a system reboot!")) { 
			if (Commands.ResetCounter_DELWPA()) {
				TrayIcon.ShowBalloonTip(9999,About.Title,"Rebooting to run DELWPA...",ToolTipIcon.Info);
			} else {
				 MsgBox("Reset Counter Operation Failed");
			}
		} else { MsgBox("Reset Counter Operation Canceled"); }
	}

	//

	private void ResetCounter_TOKENS () { ResetCounter_TOKENS(null,null); }
	private void ResetCounter_TOKENS (Object o, EventArgs e) {
		if (AskQuestion("Are you sure you want to reset the counter?\nThis operation requires a system reboot!")) { 
			if (Commands.ResetCounter_TOKENS()) {
				TrayIcon.ShowBalloonTip(9999,About.Title,"Rebooting to reset counter...",ToolTipIcon.Info);
			} else {
				 MsgBox("Reset Counter Operation Failed");
			}
		} else { MsgBox("Reset Counter Operation Canceled"); }
	}

	//

	private NotifyIcon GetTrayIcon (IContainer container) {
		String tip = "";
		tip += "Status: " + Info.LicenseStatus + "\n";
		if (Info.LicenseStatus==Info.Status.Grace) { 
			var days = (Info.ExpireTime.Value-DateTime.Now).Days;			
			tip += days + " day" + (days==1?"":"s") + " remaining" + "\n"; 
		}
		if (Info.RearmCount!=null) { tip += Info.RearmCount+" rearm"+(Info.RearmCount==1?"":"s")+" left"; }

		var tray = new NotifyIcon(container);
		tray.Icon = Icon.ExtractAssociatedIcon(@"C:\WINDOWS\SYSTEM32\CREDWIZ.EXE");
		tray.Text = About.Title;
		tray.Visible = true;
		tray.ShowBalloonTip(9,About.Title,tip,ToolTipIcon.Info);
		tray.ContextMenuStrip = GetMenu();
		// tray.Click += new EventHandler(Tray_Click); // TODO: fix this so it will disappear like normal context menu
		return tray;
	}

	//

	private ContextMenuStrip GetMenu () {
		ToolStripItem item = null;
		List<ToolStripItem> items = new List<ToolStripItem> ();

		item = new ToolStripMenuItem(About.Title);
		item.Enabled = false;
		items.Add(item);

		/*
		items.Add(new ToolStripSeparator());
		 
		item = new ToolStripMenuItem("Auto Rearm Trial");
		item.Checked = true;
		items.Add(item);

		item = new ToolStripMenuItem("Auto Reset Counter");
		item.Checked = true;
		items.Add(item);
		*/

		items.Add(new ToolStripSeparator());

		item = new ToolStripMenuItem("Show Info", null, new EventHandler(ShowInfo));
		items.Add(item);

		items.Add(new ToolStripSeparator());

		item = new ToolStripMenuItem("Rearm Trial", null, new EventHandler(RearmTrial));
		if ((Info.LicenseStatus==Info.Status.Expired || Info.LicenseStatus==Info.Status.Grace) && Info.RearmCount>0) { item.Enabled = true; } else { item.Enabled = false; }
		items.Add(item);

		items.Add(new ToolStripSeparator());

		item = new ToolStripMenuItem("Reset Counter Using DELWPA", null, new EventHandler(ResetCounter_DELWPA));
		//if (Info.LicenseStatus!=Info.Status.Unknown && Info.RearmCount==0) { item.Enabled = true; } else { item.Enabled = false; }
		//if (Info.RearmCount!=0) { item.Enabled = false; }
		//if (Info.LicenseStatus==Info.Status.Unknown) { item.Enabled = false; }
		items.Add(item);

		item = new ToolStripMenuItem("Reset Counter Using TOKENS", null, new EventHandler(ResetCounter_TOKENS));
		//if (Info.LicenseStatus!=Info.Status.Unknown && Info.RearmCount==0) { item.Enabled = true; } else { item.Enabled = false; }
		//if (Info.RearmCount!=0) { item.Enabled = false; }
		//if (Info.LicenseStatus==Info.Status.Unknown) { item.Enabled = false; }
		items.Add(item);

		items.Add(new ToolStripSeparator());

		item = new ToolStripMenuItem("Exit", null, new EventHandler(Exit));
		items.Add(item);

		ContextMenuStrip menu = new ContextMenuStrip();
		foreach (var x in items) { menu.Items.Add(x); }
		return menu;
	}

}}

//

namespace SL7FIX { static class Commands {

	private static String Run (String cmd, String args) {
		var proc = new Process { StartInfo = new ProcessStartInfo {
			FileName = cmd,
			Arguments = args,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			CreateNoWindow = true
		}};

		proc.Start();
		proc.WaitForExit();

		var output = proc.StandardOutput.ReadToEnd().TrimEnd(new char[] {'\n','\r'})+"\n";

		Log.MSG("Run: "+cmd+" "+args+"\n\n"+output+"\n");
		// MsgBox(output);

		return output;
	}

	//

	private static Boolean RunAs (String cmd, String args) {
		var proc = new Process { StartInfo = new ProcessStartInfo {
			FileName = cmd,
			Arguments = args,
			UseShellExecute = true,
			CreateNoWindow = true,
			Verb = "runas"
		}};

		Log.MSG("RunAs: "+cmd+" "+args);

		try { 
			proc.Start();
			proc.WaitForExit();
		} catch (Exception ex) { 
			Log.EX(ex);
			MessageBox.Show("RunAs Operation Canceled By User",About.Title);
			return false;
		}

		return true;
	}

	//

	private static void Reboot (Int32 seconds) { Reboot(null,seconds); } 
	private static void Reboot (String reason) { Reboot(reason,9); }
	private static void Reboot (String reason, Int32 seconds) { 
		var cmd  = @"C:\WINDOWS\SYSTEM32\SHUTDOWN.EXE";
		var args = "-r -t "+seconds+" -c \""+reason+"\"";
		Run(cmd,args);
	}

	//

	private static String GetDLV () {
		String output = Run(@"C:\WINDOWS\SYSTEM32\CSCRIPT.EXE",@"C:\WINDOWS\SYSTEM32\SLMGR.VBS /DLV");
		Log.MSG("SLMGR.VBS /DLV"+"\n\n"+output+"\n");
		return output;
	}

	private static String GetExpiredDLV () {
		return @"
			Name: Windows(R) 7, Ultimate edition
			Description: Windows Operating System - Windows(R) 7, RETAIL channel
			Partial Product Key: 3MBMV
			License Status: Notification
			Notification Reason: 0xC004F009 (grace time expired).
			Remaining Windows rearm count: 0
			Trusted time: 8/9/2013 9:45:52 PM
		";
	}

	private static String GetGraceDLV () {
		return @"
			Name: Windows(R) 7, Ultimate edition
			Description: Windows Operating System - Windows(R) 7, RETAIL channel
			Partial Product Key: 3MBMV
			License Status: Initial grace period
			Time remaining: 43200 minute(s) (30 day(s))
			Remaining Windows rearm count: 1
			Trusted time: 8/9/2013 10:03:31 PM		
		";
	}

	//

	public static Boolean InstallKey () {
		var result = false;
		result = RunAs("CSCRIPT.EXE",@"C:\WINDOWS\SYSTEM32\SLMGR.VBS -IPK "+Config.Keys[Info.KeyType]);
		return result;
	}

	public static Boolean InstallStartupLink () {
		if (!File.Exists(Config.Path+@"\SL7FIX.EXE")) { return false; }

		var lnk = Config.Path+@"\SL7FIX.LNK";
		if (!File.Exists(lnk)) { File.WriteAllBytes(lnk,Convert.FromBase64String(Config.LNK64)); }
		
		var menuLink = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)+@"\PROGRAMS\STARTUP"+@"\SL7FIX.LNK";
		if (!File.Exists(menuLink)) { File.Copy(lnk,menuLink); }

		return true;
	}

	//

	private static Int32? GetInt (String str) {
		try { return Convert.ToInt32(str); } catch (Exception ex) { Log.MSG(ex.Message + "\n" + ex.StackTrace); }
		return null;
	}

	public static void MsgBox (String msg) { MsgBox(msg,null); }
	public static void MsgBox (String msgraw, String title) {
		var msgz = msgraw.TrimStart(new char[] {'\n','\r'}).Split('\n');
		var msg = "";
		foreach (var line in msgz) { msg += line.TrimStart(new char[] {'\t'}); }
		MessageBox.Show(msg,title);
	}
	
	//

	//public static void SetInfo () { SetInfo(GetExpiredDLV()); }
	//public static void SetInfo () { SetInfo(GetGraceDLV()); }
	public static void SetInfo () { SetInfo(GetDLV()); }
	public static Boolean SetInfo (String dlv) {
		var dlvz = dlv.Split('\n');
		if (dlvz.Length==3) { return false; }
		if (dlv.Contains("Error: product key not found")) { Info.LicenseStatus = Info.Status.NoKey; Info.KeyType="ULTIMATE"; return false; } // TODO: Use WMIC OS GET CAPTION to get KeyType
		String q = "";
		foreach (String lineraw in dlvz) { 
			String line = lineraw.TrimStart(new char[] {'\t'});
			
			q = "Name: ";
			if (line.StartsWith(q)) { Info.OS = line.Substring(q.Length); }

			q = "Description: ";
			if (line.StartsWith(q)) { Info.Description = line.Substring(q.Length); }

			q = "Partial Product Key: ";
			if (line.StartsWith(q)) { Info.PartialKey = line.Substring(q.Length); }

			q = "License Status: ";
			if (line.StartsWith(q)) { Info.StatusMessage = line.Substring(q.Length); }

			q = "Notification Reason: ";
			if (line.StartsWith(q)) { Info.NotifyReason = line.Substring(q.Length); }			
			
			q = "Remaining Windows rearm count: ";
			if (line.StartsWith(q)) { Info.RearmCount = GetInt(line.Substring(q.Length)); }

			q = "Trusted time: ";
			if (line.StartsWith(q)) { Info.TrustedTime = DateTime.Parse( line.Substring(q.Length) ); }

			q = "Time remaining: ";
			if (line.StartsWith(q)) { 
				var x = line.Substring(q.Length);
				var m = x.Substring(0,x.IndexOf(" minute(s)"));
				Info.MinutesLeft = GetInt(m);
			}
		}
	
		if (Info.OS.Contains("Ultimate edition")) { Info.KeyType = "ULTIMATE"; }

		if (Info.StatusMessage.Contains("Unlicensed")) { 
			Info.LicenseStatus = Info.Status.Unlicensed; 
		} else if (Info.StatusMessage.Contains("Licensed")) { 
			Info.LicenseStatus = Info.Status.Licensed;
		} else if (Info.StatusMessage.Contains("Initial grace period")) { 
			Info.LicenseStatus = Info.Status.Grace;
		} else if (Info.StatusMessage.Contains("Notification") && Info.NotifyReason.Contains("grace time expired")) { 
			Info.LicenseStatus = Info.Status.Expired; 
		} else if (Info.NotifyReason.Contains("C004FE00")) {
			Info.LicenseStatus = Info.Status.Counterfeit;
		} else { 
			Info.LicenseStatus = Info.Status.Unknown; 
		}

		if (Info.MinutesLeft.HasValue) {
			if (Info.TrustedTime.HasValue) { Info.ExpireTime = Info.TrustedTime.Value.AddMinutes(Info.MinutesLeft.Value); }			
		} else {
			Info.MinutesLeft = 0;
		}

		return true;
	}

	//

	public static void ShowInfo () {
		String msg = "";
		msg += "OS:            " + Info.OS + "\n";
		msg += "Description:   " + Info.Description + "\n";
		msg += "PartialKey:    " + Info.PartialKey + "\n";
		msg += "LicenseStatus: " + Info.LicenseStatus + "\n";
		msg += "StatusMessage: " + Info.StatusMessage + "\n";
		if (!String.IsNullOrEmpty(Info.NotifyReason)) { msg += "NotifyReason:  " + Info.NotifyReason + "\n"; }
		msg += "RearmCount:    " + Info.RearmCount + "\n";
		msg += "InfoTime:      " + Info.InfoTime + "\n";
		msg += "TrustedTime:   " + Info.TrustedTime + "\n";
		msg += "MinutesLeft:   " + Info.MinutesLeft + "\n";
		if (Info.ExpireTime!=null) { msg += "ExpireTime:    " + Info.ExpireTime + "\n"; }
		MessageBox.Show(msg,About.Title);
	}

	//

	public static Boolean RearmTrial () {
		Log.MSG("CMD: RearmTrial");

		var result = RunAs("CSCRIPT.EXE",@"C:\WINDOWS\SYSTEM32\SLMGR.VBS -REARM");

		if (result) { Reboot("SL7FIX: Rearming Trial"); }
		return result;
	}

	//

	public static Boolean ResetCounter () { return ResetCounter_DELWPA(); }

	//

	public static Boolean ResetCounter_DELWPA () {
		Log.MSG("CMD: ResetCounter_DELWPA");

		Directory.CreateDirectory(Config.Path);

		var delwpacmd = Config.Path+@"\DELWPA.CMD";
		var delwpastr = @"
			REG LOAD HKLM\MY_SYSTEM "+'"'+@"%~dp0..\WINDOWS\SYSTEM32\CONFIG\SYSTEM"+'"'+@"
			REG DELETE HKLM\MY_SYSTEM\WPA /F
			REG UNLOAD HKLM\MY_SYSTEM
			EXIT
		";

		var result = true;
		try { 
			Directory.CreateDirectory(Config.Path);
			File.WriteAllText(delwpacmd,delwpastr);
		} catch (Exception ex) { Log.MSG(ex.Message+"\n\n"+ex.StackTrace); result = false; }

		if (result) { 
			MsgBox(@"
				* Write these directions down or snap them with your phone

				* After closing this message, your PC will reboot
				* Hit F8 as soon as your PC starts booting to open the boot menu

				* Choose 'Repair My Computer', login and then select 'Command Prompt'
				* Your SYSTEMDRIVE is mapped randomly, so you need to locate it...
				* Enter the following commands until the window closes:

				* C:\SL7FIX\DELWPA.CMD
				* D:\SL7FIX\DELWPA.CMD
				* E:\SL7FIX\DELWPA.CMD
				* ...
				* Z:\SL7FIX\DELWPA.CMD

				* After it closes, select 'Restart' and allow your PC to boot normally
				* Windows will ask for a product key, just click 'Cancel'
				* When SL7FIX runs it will prompt you to restore the key
			","DELWPA Instructions");

			Reboot("SL7FIX: Rebooting Into WinRE to use DELWPA"); 
		}

		return result;
	}

	//

	public static Boolean ResetCounter_TOKENS () {
		Log.MSG("CMD: ResetCounter_TOKENS");

		Directory.CreateDirectory(Config.Path);
		// Directory.CreateDirectory(Config.Path+@"\BACKUP");

		var bak = DateTime.Now.ToString("yyyyMMdd_HHmmss");

		var resetcmd = Config.Path+@"\RESET.CMD";
		var resetstr = @"
			@ECHO ON
			SET DIR="+Config.Path+@"
			SET BAK="+bak+@"
			SET KEY="+Config.Keys[Info.KeyType]+@"
			SET WAIT10=PING 1.1.1.1 -n 1 -w 9999 > NUL
			SET W32=%WINDIR%\SYSTEM32
			SET SPP=%WINDIR%\ServiceProfiles\NetworkService\AppData\Roaming\Microsoft\SoftwareProtectionPlatform
			SET X1=7B296FB0-376B-497E-B012-9C450E1B7327-5P-0.C7483456-A289-439D-8115-601632D005A0
			SET X2=7B296FB0-376B-497E-B012-9C450E1B7327-5P-1.C7483456-A289-439D-8115-601632D005A0
			SET SLMGR=%W32%\CSCRIPT.EXE %W32%\SLMGR.VBS
			%SYSTEMDRIVE%
			MD %DIR%
			CD %DIR%
			MD BACKUP
			CD BACKUP
			MD %BAK%
			CD %BAK%
			NET STOP SPPSVC
			%WAIT10% > NUL
			MOVE %SPP%\TOKENS.DAT .
			ATTRIB -H %W32%\%X1%
			ATTRIB -H %W32%\%X2%
			TAKEOWN -F %W32%\%X1%
			TAKEOWN -F %W32%\%X2%
			MOVE %W32%\%X1% .
			MOVE %W32%\%X2% .
			NET START SPPSVC
			%WAIT10% > NUL
			%SLMGR% -DLV
			%WAIT10% > NUL
			NET STOP SPPSVC
			%WAIT10% > NUL
			COPY %X1% %W32%
			COPY %X2% %W32%
			ATTRIB +H %W32%\%X1%
			ATTRIB +H %W32%\%X2%
			COPY TOKENS.DAT %SPP%
			REM NET START SPPSVC
			%SLMGR% -IPK %KEY%
		";

		var result = false;
		try { 
			Directory.CreateDirectory(Config.Path);
			File.WriteAllText(resetcmd,resetstr);
			result = RunAs(resetcmd,"");
		} catch (Exception ex) { Log.MSG(ex.Message+"\n\n"+ex.StackTrace); result = false; }

		if (result) { Reboot("SL7FIX: Resetting Counter Using TOKENS"); }
		return result;
	}

}}
