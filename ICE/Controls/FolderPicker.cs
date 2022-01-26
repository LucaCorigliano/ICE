using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.Research.ICE.Controls
{

	public static class FolderPicker
	{
		private class FolderBrowserDialog
		{
			[Flags]
			private enum BrowseFlags
			{
				BIF_DEFAULT = 0,
				BIF_RETURNONLYFSDIRS = 1,
				BIF_DONTGOBELOWDOMAIN = 2,
				BIF_STATUSTEXT = 4,
				BIF_RETURNFSANCESTORS = 8,
				BIF_EDITBOX = 0x10,
				BIF_VALIDATE = 0x20,
				BIF_NEWDIALOGSTYLE = 0x40,
				BIF_BROWSEINCLUDEURLS = 0x80,
				BIF_UAHINT = 0x100,
				BIF_NONEWFOLDERBUTTON = 0x200,
				BIF_NOTRANSLATETARGETS = 0x400,
				BIF_BROWSEFORCOMPUTER = 0x1000,
				BIF_BROWSEFORPRINTER = 0x2000,
				BIF_BROWSEINCLUDEFILES = 0x4000,
				BIF_SHAREABLE = 0x8000,
				BIF_BROWSEFILEJUNCTIONS = 0x10000
			}

			private sealed class NativeMethods
			{
				public delegate int BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lParam, IntPtr lpData);

				[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 8)]
				public struct BROWSEINFO
				{
					public IntPtr hwndOwner;

					public IntPtr pidlRoot;

					public IntPtr pszDisplayName;

					public string lpszTitle;

					public int ulFlags;

					public BrowseCallbackProc lpfn;

					public IntPtr lParam;

					public int iImage;
				}

				public static readonly int BFFM_SETSELECTION;

				static NativeMethods()
				{
					if (Marshal.SystemDefaultCharSize == 1)
					{
						BFFM_SETSELECTION = 1126;
					}
					else
					{
						BFFM_SETSELECTION = 1127;
					}
				}

				[DllImport("user32.dll", CharSet = CharSet.Auto)]
				public static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, string lParam);

				[DllImport("user32.dll", CharSet = CharSet.Auto)]
				public static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, int lParam);

				[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
				public static extern IntPtr GetActiveWindow();

				[DllImport("shell32.dll", CharSet = CharSet.Auto)]
				public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);

				[DllImport("shell32.dll", CharSet = CharSet.Auto)]
				public static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

				[DllImport("shell32.dll", CharSet = CharSet.Auto)]
				public static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder path);

				[DllImport("shell32.dll", CharSet = CharSet.Auto)]
				public static extern int SHGetSpecialFolderLocation(IntPtr hwnd, int csidl, ref IntPtr ppidl);

				[DllImport("ole32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
				internal static extern void CoTaskMemFree(IntPtr pv);
			}

			private const int MAX_PATH = 260;

			private NativeMethods.BrowseCallbackProc callback;

			public Window Owner { get; set; }

			public string Title { get; set; }

			public Environment.SpecialFolder RootFolder { get; set; }

			public bool IncludeTextBox
			{
				get
				{
					return (Flags & BrowseFlags.BIF_EDITBOX) != 0;
				}
				set
				{
					SetFlag(BrowseFlags.BIF_EDITBOX, value);
				}
			}

			public string SelectedPath { get; set; }

			public string DisplayName { get; private set; }

			private BrowseFlags Flags { get; set; }

			public FolderBrowserDialog()
			{
				RootFolder = Environment.SpecialFolder.Desktop;
				Flags = BrowseFlags.BIF_EDITBOX | BrowseFlags.BIF_NEWDIALOGSTYLE;
			}

			public bool ShowDialog()
			{
				callback = FolderBrowserDialog_BrowseCallbackProc;
				IntPtr intPtr;
				if (Owner != null)
				{
					WindowInteropHelper windowInteropHelper = new WindowInteropHelper(Owner);
					intPtr = windowInteropHelper.Handle;
				}
				else
				{
					intPtr = NativeMethods.GetActiveWindow();
				}
				IntPtr ppidl = IntPtr.Zero;
				NativeMethods.SHGetSpecialFolderLocation(intPtr, (int)RootFolder, ref ppidl);
				if (ppidl == IntPtr.Zero)
				{
					return false;
				}
				IntPtr displayName_ptr = IntPtr.Zero;
				IntPtr selectedFolder_ptr = IntPtr.Zero;
				bool result = false;
				try
				{
					displayName_ptr = Marshal.AllocHGlobal(260 * Marshal.SystemDefaultCharSize);
					NativeMethods.BROWSEINFO bi = default(NativeMethods.BROWSEINFO);
					bi.hwndOwner = intPtr;
					bi.pszDisplayName = IntPtr.Zero;
					bi.pidlRoot = ppidl;
					bi.lpfn = callback;
					bi.ulFlags = (int)Flags;
					bi.lParam = IntPtr.Zero;
					bi.lpszTitle = ((!string.IsNullOrEmpty(Title)) ? Title : "Select Folder");
					bi.pszDisplayName = displayName_ptr;
					selectedFolder_ptr = NativeMethods.SHBrowseForFolder(ref bi);
					if (selectedFolder_ptr != IntPtr.Zero)
					{
						StringBuilder stringBuilder = new StringBuilder(260);
						NativeMethods.SHGetPathFromIDList(selectedFolder_ptr, stringBuilder);
						SelectedPath = stringBuilder.ToString();
						DisplayName = Marshal.PtrToStringAuto(bi.pszDisplayName);
						return true;
					}
					return result;
				}
				finally
				{
					if (ppidl != IntPtr.Zero)
					{
						NativeMethods.CoTaskMemFree(ppidl);
					}
					if (selectedFolder_ptr != IntPtr.Zero)
					{
						NativeMethods.CoTaskMemFree(selectedFolder_ptr);
					}
					if (displayName_ptr != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(displayName_ptr);
					}
					callback = null;
				}
			}

			private int FolderBrowserDialog_BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lParam, IntPtr lpData)
			{
				switch (msg)
				{
					case 1:
						if (!string.IsNullOrEmpty(SelectedPath))
						{
							NativeMethods.SendMessage(new HandleRef(null, hwnd), NativeMethods.BFFM_SETSELECTION, 1, SelectedPath);
						}
						break;
					case 2:
						if (lParam != IntPtr.Zero)
						{
							IntPtr intPtr = Marshal.AllocHGlobal(260 * Marshal.SystemDefaultCharSize);
							bool flag = NativeMethods.SHGetPathFromIDList(lParam, intPtr);
							Marshal.FreeHGlobal(intPtr);
							NativeMethods.SendMessage(new HandleRef(null, hwnd), 1125, 0, flag ? 1 : 0);
						}
						break;
				}
				return 0;
			}

			private void SetFlag(BrowseFlags flag, bool value)
			{
				if (value)
				{
					Flags |= flag;
				}
				else
				{
					Flags &= ~flag;
				}
			}
		}

		public static string ChooseFolder(Window owner, string title, string initialFolder, string favoriteFolder)
		{
			if (CommonFileDialog.IsPlatformSupported)
			{
				CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog();
				commonOpenFileDialog.IsFolderPicker = true;
				commonOpenFileDialog.Title = title;
				CommonOpenFileDialog commonOpenFileDialog2 = commonOpenFileDialog;
				if (initialFolder != null)
				{
					commonOpenFileDialog2.InitialDirectory = initialFolder;
				}
				else
				{
					commonOpenFileDialog2.InitialDirectoryShellContainer = (ShellContainer)ShellObject.FromParsingName(KnownFolders.Computer.ParsingName);
				}
				if (!string.IsNullOrEmpty(favoriteFolder) && Directory.Exists(favoriteFolder))
				{
					commonOpenFileDialog2.AddPlace(favoriteFolder, FileDialogAddPlaceLocation.Top);
				}
				if (commonOpenFileDialog2.ShowDialog((Window)(object)owner) == CommonFileDialogResult.Ok)
				{
					return commonOpenFileDialog2.FileName;
				}
				return null;
			}
			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			folderBrowserDialog.Owner = owner;
			folderBrowserDialog.Title = title;
			folderBrowserDialog.SelectedPath = initialFolder;
			FolderBrowserDialog folderBrowserDialog2 = folderBrowserDialog;
			if (folderBrowserDialog2.ShowDialog())
			{
				return folderBrowserDialog2.SelectedPath;
			}
			return null;
		}
	}

}