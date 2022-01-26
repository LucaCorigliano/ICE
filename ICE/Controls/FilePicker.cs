using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Microsoft.Research.ICE.Controls
{

	public sealed class FilePicker
	{
		private Window owner;

		private FileDialog fileDialog;

		public string FileName => fileDialog.FileName;

		public string[] FileNames => fileDialog.FileNames;

		public static FilePicker GetOpenFilePicker(Window owner, string title, string filter, bool multiselect)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Multiselect = multiselect;
			return new FilePicker(owner, openFileDialog, title, filter);
		}

		public static FilePicker GetSaveFilePicker(Window owner, string title, string filter, string defaultFileName)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			if (defaultFileName != null)
			{
				saveFileDialog.InitialDirectory = Path.GetDirectoryName(defaultFileName);
				saveFileDialog.FileName = Path.GetFileName(defaultFileName);
			}
			return new FilePicker(owner, saveFileDialog, title, filter);
		}

		public bool ShowDialog()
		{
			return fileDialog.ShowDialog(owner).GetValueOrDefault();
		}

		private FilePicker(Window owner, FileDialog fileDialog, string title, string filter)
		{
			this.owner = owner;
			this.fileDialog = fileDialog;
			this.fileDialog.Title = title;
			this.fileDialog.Filter = filter;
			this.fileDialog.RestoreDirectory = true;
		}
	}

}