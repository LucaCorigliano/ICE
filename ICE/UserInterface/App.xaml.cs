
using Microsoft.Research.ICE.Properties;
using System;
using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Research.ICE. UserInterface
	{
		public partial class App : Application
		{

			static App()
			{
				TextOptions.TextFormattingModeProperty.OverrideMetadata(typeof(UIElement), new FrameworkPropertyMetadata((object)TextFormattingMode.Display));
				EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.ManipulationBoundaryFeedbackEvent, new EventHandler<ManipulationBoundaryFeedbackEventArgs>(ScrollViewer_ManipulationBoundaryFeedback));
			}

			public App()
			{
				Guid guid;
				if (CheckForValidSettings())
				{
					if (!Guid.TryParse(Settings.Default.UserId, out guid))
					{
						guid = Guid.NewGuid();
						Settings.Default.UserId = guid.ToString();
						Settings.Default.Save();
					}

					DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
					CheckForValidTempDirectory();
				}
			}

			private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
			{
				try
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendFormat("ICE Crash Report\r\n", new object[0]);
					stringBuilder.AppendFormat("----------------\r\n", new object[0]);
					stringBuilder.AppendFormat("ICE version: {0} ({1}-bit)\r\n", typeof(App).Assembly.GetName().Version, 8 * IntPtr.Size);
					stringBuilder.AppendFormat("Operating system version: {0}\r\n", Environment.OSVersion.VersionString);
					stringBuilder.AppendFormat("Processor count: {0}\r\n", Environment.ProcessorCount);
					stringBuilder.AppendFormat("Working set: {0}\r\n", Environment.WorkingSet);
					stringBuilder.AppendFormat("Managed memory consumption: {0}\r\n", GC.GetTotalMemory(false));
					stringBuilder.AppendFormat("UI culture: {0}\r\n", CultureInfo.CurrentUICulture.Name);
					stringBuilder.AppendFormat("Current culture: {0}", CultureInfo.CurrentCulture.Name);
					string str = "Exception";
					for (Exception i = e.Exception; i != null; i = i.InnerException)
					{
						object[] fullName = new object[] { str, i.GetType().FullName, i.Message, i.StackTrace };
						stringBuilder.AppendFormat("\r\n\r\n{0} type: {1}\r\n{0} message: {2}\r\nCall stack:\r\n{3}", fullName);
						str = "Inner exception";
					}
					string str1 = Path.Combine(Path.GetTempPath(), "Image Composite Editor");
					DateTime utcNow = DateTime.UtcNow;
					string str2 = Path.Combine(str1, string.Concat("CrashReport-", utcNow.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture), ".txt"));
					File.WriteAllText(str2, stringBuilder.ToString());
				}
				catch
				{
				}
			}

			private bool CheckForValidSettings()
			{
				bool flag;
				try
				{
					bool hasCheckedForImageCacheEnvironmentVariable = Settings.Default.HasCheckedForImageCacheEnvironmentVariable;
					flag = true;
				}
				catch (ConfigurationException configurationException)
				{
					ConfigurationException innerException = configurationException;
					while (innerException != null && innerException.Filename == null)
					{
						innerException = innerException.InnerException as ConfigurationException;
					}
					if (innerException == null || innerException.Filename == null || MessageBox.Show("ICE could not start because a configuration file is corrupted.\n\nDo you want ICE to delete the configuration file and try again?", "Corrupted configuration file", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
					{
						SuggestReinstall("Configuration error", "ICE could not start because of a configuration error.");
					}
					else
					{
						try
						{
							File.Delete(innerException.Filename);
						}
						catch
						{
							SuggestReinstall("Unable to delete configuration file", "ICE could not delete the corrupted configuration file.");
							flag = false;
							return flag;
						}
						Process.Start(Assembly.GetEntryAssembly().Location);
						Shutdown();
					}
					flag = false;
				}
				return flag;
			}

			private void CheckForValidTempDirectory()
			{
				bool flag = false;
				try
				{
					string str = ValidateTempPath(Path.GetTempPath());
					if (str == null)
					{
						string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
						str = ValidateTempPath(Path.Combine(folderPath, "Temp"));
						if (str != null)
						{
							Environment.SetEnvironmentVariable("TMP", str);
							flag = true;
						}
					}
					else
					{
						flag = true;
					}
				}
				catch
				{
				}
				if (!flag)
				{
					MessageBox.Show("ICE could not start because the TMP environment variable is not set to a valid directory.", "Invalid temporary disk path", MessageBoxButton.OK, MessageBoxImage.Hand);

					Shutdown();
				}
			}

			protected override void OnExit(ExitEventArgs e)
			{
				base.OnExit(e);

			}

			private static void ScrollViewer_ManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
			{
				e.Handled = true;
			}

			private void SuggestReinstall(string title, string message)
			{
				MessageBox.Show(string.Concat(message, "\n\nPlease uninstall Image Composite Editor, then reinstall it."), title, MessageBoxButton.OK);
				Process.Start("http://go.microsoft.com/fwlink/?LinkId=185909");
				Process.Start(Path.Combine(Environment.SystemDirectory, "appwiz.cpl"));
				Shutdown();
			}

			private static string ValidateTempPath(string tempPath)
			{
				try
				{
					tempPath = Path.GetFullPath(tempPath);
					Directory.CreateDirectory(tempPath);
					string str = Path.Combine(tempPath, Path.GetRandomFileName());
					File.WriteAllText(str, string.Empty);
					File.Delete(str);
				}
				catch
				{
					tempPath = null;
				}
				return tempPath;
			}
		}
	}
