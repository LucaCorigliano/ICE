using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Research.ICE.Stitching;

namespace Microsoft.Research.ICE.ViewModels
{

	public sealed class TilesetExportViewModel : ExportViewModel
	{
		private const bool DefaultUseZipArchive = false;

		private const ViewerChoice DefaultViewer = ViewerChoice.Automatic;

		private const bool DefaultUseEntireWebPage = true;

		private const int DefaultViewerWidth = 800;

		private const int DefaultViewerHeight = 600;

		private const int MinViewerSize = 250;

		private const int MaxViewerSize = 2048;

		private const bool DefaultOpenAfterExport = true;

		private bool useZipArchive;

		private NamedValue<string> templateDirectory;

		private ViewerChoice viewer;

		private bool useEntireWebPage;

		private int viewerWidth;

		private int viewerHeight;

		private bool openAfterExport;

		public override string FileFilter => "Deep Zoom Image File (*.xml)|*.xml";

		public override string DefaultFileExtension => "xml";

		public IEnumerable<NamedValue<string>> PossibleTemplateDirectories { get; private set; }

		public CompressionQualityViewModel Quality { get; private set; }

		public bool UseZipArchive
		{
			get
			{
				return useZipArchive;
			}
			set
			{
				if (SetProperty(ref useZipArchive, value, "UseZipArchive"))
				{
					NotifyPropertyChanged("OpenAfterExport");
					NotifyPropertyChanged("CanOpenAfterExport");
				}
			}
		}

		public ZipArchiveSizeViewModel ZipArchiveSize { get; private set; }

		public NamedValue<string> TemplateDirectory
		{
			get
			{
				return templateDirectory;
			}
			set
			{
				SetProperty(ref templateDirectory, value, "TemplateDirectory");
			}
		}

		public ViewerChoice Viewer
		{
			get
			{
				return viewer;
			}
			set
			{
				if (SetProperty(ref viewer, value, "Viewer"))
				{
					NotifyPropertyChanged("OpenAfterExport");
					NotifyPropertyChanged("CanOpenAfterExport");
				}
			}
		}

		public bool UseEntireWebPage
		{
			get
			{
				return useEntireWebPage;
			}
			set
			{
				SetProperty(ref useEntireWebPage, value, "UseEntireWebPage");
			}
		}

		public int ViewerWidth
		{
			get
			{
				return viewerWidth;
			}
			set
			{
				value = Math.Max(250, Math.Min(value, 2048));
				SetProperty(ref viewerWidth, value, "ViewerWidth");
			}
		}

		public int ViewerHeight
		{
			get
			{
				return viewerHeight;
			}
			set
			{
				value = Math.Max(250, Math.Min(value, 2048));
				SetProperty(ref viewerHeight, value, "ViewerHeight");
			}
		}

		public bool OpenAfterExport
		{
			get
			{
				if (openAfterExport)
				{
					return CanOpenAfterExport;
				}
				return false;
			}
			set
			{
				SetProperty(ref openAfterExport, value, "OpenAfterExport");
			}
		}

		public bool CanOpenAfterExport
		{
			get
			{
				if (UseZipArchive)
				{
					return Viewer != ViewerChoice.HDViewSL;
				}
				return true;
			}
		}

		public TilesetExportViewModel()
		{
			Quality = new CompressionQualityViewModel(supportsLossless: true);
			useZipArchive = false;
			ZipArchiveSize = new ZipArchiveSizeViewModel();
			PossibleTemplateDirectories = GetTemplateDirectories();
			templateDirectory = PossibleTemplateDirectories.FirstOrDefault();
			viewer = ViewerChoice.Automatic;
			useEntireWebPage = true;
			viewerWidth = 800;
			viewerHeight = 600;
			openAfterExport = true;
		}

		public override OutputOptions CreateOutputOptions()
		{
			return new OutputOptions(ExportFormat.HDView, Quality.Value, UseZipArchive, ZipArchiveSize.Value, TemplateDirectory.Value, Viewer, UseEntireWebPage, ViewerWidth, ViewerHeight, OpenAfterExport);
		}

		private static IEnumerable<NamedValue<string>> GetTemplateDirectories()
		{
			List<NamedValue<string>> list = new List<NamedValue<string>>();
			string[] array = new string[2]
			{
			Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Image Composite Editor")
			};
			string[] array2 = array;
			foreach (string path in array2)
			{
				string path2 = Path.Combine(path, "Templates");
				if (Directory.Exists(path2))
				{
					string[] directories = Directory.GetDirectories(path2);
					foreach (string text in directories)
					{
						list.Add(new NamedValue<string>(Path.GetFileName(text), text));
					}
				}
			}
			return list;
		}
	}

}