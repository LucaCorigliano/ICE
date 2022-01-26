using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml.Linq;
using Microsoft.Research.ICE.Stitching;

namespace Microsoft.Research.ICE.Helpers
{

	public static class StitchProjectFile
	{
		private static Version currentProjectFileVersion = new Version(2, 0);

		private static Dictionary<string, MapType> allProjections = new Dictionary<string, MapType>(StringComparer.InvariantCultureIgnoreCase)
	{
		{
			"unknown",
			MapType.Unknown
		},
		{
			"spherical",
			MapType.Spherical
		},
		{
			"mercator",
			MapType.Mercator
		},
		{
			"cylindrical",
			MapType.Cylindrical
		},
		{
			"transverseSpherical",
			MapType.TransverseSpherical
		},
		{
			"transverseMercator",
			MapType.TransverseMercator
		},
		{
			"transverseCylindrical",
			MapType.TransverseCylindrical
		},
		{
			"orthographic",
			MapType.Orthographic
		},
		{
			"fisheye",
			MapType.Fisheye
		},
		{
			"stereographic",
			MapType.Stereographic
		},
		{
			"perspective",
			MapType.Perspective
		}
	};

		private static Dictionary<string, MotionModel> allCameraMotions = new Dictionary<string, MotionModel>(StringComparer.InvariantCultureIgnoreCase)
	{
		{
			"automatic",
			MotionModel.Automatic
		},
		{
			"rigid",
			MotionModel.Rigid
		},
		{
			"rigidScale",
			MotionModel.RigidScale
		},
		{
			"rotation3D",
			MotionModel.Rotation3D
		},
		{
			"affine",
			MotionModel.Affine
		},
		{
			"translation",
			MotionModel.Translation
		},
		{
			"homography",
			MotionModel.Homography
		}
	};

		private static Dictionary<string, Orientation> allOrientations = new Dictionary<string, Orientation>(StringComparer.InvariantCultureIgnoreCase)
	{
		{
			"unknown",
			Orientation.Unknown
		},
		{
			"normal",
			Orientation.Normal
		},
		{
			"flipped",
			Orientation.Normal
		},
		{
			"rot90cw",
			Orientation.Rotated90Clock
		},
		{
			"rot90cwFlipped",
			Orientation.Rotated90ClockHFlipped
		},
		{
			"rot90ccw",
			Orientation.Rotated90AntiClock
		},
		{
			"rot90ccwFlipped",
			Orientation.Rotated90AntiClockHFlipped
		},
		{
			"rot180",
			Orientation.Rotated180
		},
		{
			"rot180Flipped",
			Orientation.Rotated180HFlipped
		}
	};

		private static Dictionary<string, AngleUnit> allAngleUnits = new Dictionary<string, AngleUnit>(StringComparer.InvariantCultureIgnoreCase)
	{
		{
			"degrees",
			AngleUnit.Degrees
		},
		{
			"radians",
			AngleUnit.Radians
		}
	};

		public static StitchProjectInfo Load(string filePath)
		{
			XElement xElement = XElement.Load(filePath);
			Version version = xElement?.GetVersionAttribute("version");
			if (xElement == null || xElement.Name.LocalName != "stitchProject" || version == null)
			{
				throw new FileFormatException("Invalid stitch project file format.");
			}
			if (version > currentProjectFileVersion)
			{
				throw new FileFormatException(string.Format(CultureInfo.CurrentCulture, "Unable to read version {0} file format.", new object[1] { version }));
			}
			StitchProjectInfo stitchProjectInfo = null;
			stitchProjectInfo = ((version.Major >= 2) ? LoadCurrentFormat(xElement) : LoadOldFormat(xElement));
			if (stitchProjectInfo.SourceImages.Count == 0 && stitchProjectInfo.SourceVideos.Count == 0)
			{
				throw new FileFormatException("Project contains no images or videos.");
			}
			string directoryName = Path.GetDirectoryName(filePath);
			foreach (ImageInfo sourceImage in stitchProjectInfo.SourceImages)
			{
				if (!Path.IsPathRooted(sourceImage.FilePath))
				{
					sourceImage.FilePath = Path.Combine(directoryName, sourceImage.FilePath);
				}
			}
			foreach (VideoInfo sourceVideo in stitchProjectInfo.SourceVideos)
			{
				if (!Path.IsPathRooted(sourceVideo.FilePath))
				{
					sourceVideo.FilePath = Path.Combine(directoryName, sourceVideo.FilePath);
				}
			}
			if (IsPlanarMotion(stitchProjectInfo.CameraMotion) || IsPlanarMotion(stitchProjectInfo.ComputedCameraMotion))
			{
				stitchProjectInfo.Projection = MapType.Orthographic;
			}
			return stitchProjectInfo;
		}

		public static void Save(string filePath, StitchProjectInfo projectInfo)
		{
			XElement xElement = SaveCurrentFormat(projectInfo);
			XDocument xDocument = new XDocument(xElement);
			xDocument.Save(filePath);
		}

		private static bool IsPlanarMotion(MotionModel cameraMotion)
		{
			switch (cameraMotion)
			{
				case MotionModel.Translation:
				case MotionModel.Rigid:
				case MotionModel.RigidScale:
				case MotionModel.Affine:
				case MotionModel.Homography:
					return true;
				default:
					return false;
			}
		}

		private static StitchProjectInfo LoadOldFormat(XElement rootElement)
		{
			StitchProjectInfo stitchProjectInfo = new StitchProjectInfo();
			LoadOldStitchParams(stitchProjectInfo, rootElement.Element("stitchParams"));
			LoadOldSourceImages(stitchProjectInfo, rootElement.Element("sourceImages"));
			LoadOldSourceVideos(stitchProjectInfo, rootElement.Element("sourceVideos"));
			return stitchProjectInfo;
		}

		private static void LoadOldStitchParams(StitchProjectInfo projectInfo, XElement stitchParamsElement)
		{
			if (stitchParamsElement != null)
			{
				Dictionary<string, MapType> dictionary = new Dictionary<string, MapType>(StringComparer.InvariantCultureIgnoreCase);
				dictionary.Add("unknown", MapType.Unknown);
				dictionary.Add("perspective", MapType.Perspective);
				dictionary.Add("horizontalCylindrical", MapType.Cylindrical);
				dictionary.Add("verticalCylindrical", MapType.TransverseCylindrical);
				dictionary.Add("horizontalSpherical", MapType.Spherical);
				dictionary.Add("verticalSpherical", MapType.TransverseSpherical);
				Dictionary<string, MapType> values = dictionary;
				projectInfo.Projection = stitchParamsElement.GetAttribute("mapping", values, MapType.Unknown);
				Dictionary<string, MotionModel> dictionary2 = new Dictionary<string, MotionModel>(StringComparer.InvariantCultureIgnoreCase);
				dictionary2.Add("unknown", MotionModel.Automatic);
				dictionary2.Add("rigid", MotionModel.Rigid);
				dictionary2.Add("rigidScale", MotionModel.RigidScale);
				dictionary2.Add("rotation3D", MotionModel.Rotation3D);
				dictionary2.Add("affine", MotionModel.Affine);
				dictionary2.Add("translation", MotionModel.Translation);
				dictionary2.Add("homography", MotionModel.Homography);
				Dictionary<string, MotionModel> values2 = dictionary2;
				projectInfo.CameraMotion = stitchParamsElement.GetAttribute("motionModel", values2, MotionModel.Unknown);
				projectInfo.IsPoseApproximate = stitchParamsElement.GetBooleanAttribute("poseIsApprox");
				if (projectInfo.CameraMotion == MotionModel.Rotation3D)
				{
					projectInfo.ViewRotation = stitchParamsElement.GetOldRotationAttribute("view3D");
					return;
				}
				double oldAngleAttribute = stitchParamsElement.GetOldAngleAttribute("view2D");
				projectInfo.ViewRotation = new Quaternion(new Vector3D(0.0, 0.0, 1.0), oldAngleAttribute);
			}
		}

		private static void LoadOldImageDefaults(StitchProjectInfo projectInfo, XElement imageDefaultsElement)
		{
			if (imageDefaultsElement != null)
			{
				projectInfo.VignetteCorrection = LoadOldVignetteCorrection(imageDefaultsElement.Element("vignetteCorrection"));
			}
		}

		private static VignetteParams LoadOldVignetteCorrection(XElement vignetteCorrectionElement)
		{
			VignetteParams result = null;
			if (vignetteCorrectionElement != null)
			{
				float floatAttribute = vignetteCorrectionElement.GetFloatAttribute("FactorR");
				float floatAttribute2 = vignetteCorrectionElement.GetFloatAttribute("FactorG");
				float floatAttribute3 = vignetteCorrectionElement.GetFloatAttribute("FactorB");
				float floatAttribute4 = vignetteCorrectionElement.GetFloatAttribute("GlobalIntensityScale", 1f);
				result = new VignetteParams(floatAttribute, floatAttribute2, floatAttribute3, floatAttribute4);
			}
			return result;
		}

		private static void LoadOldSourceImages(StitchProjectInfo projectInfo, XElement sourceImagesElement)
		{
			if (sourceImagesElement == null)
			{
				return;
			}
			LoadOldImageDefaults(projectInfo, sourceImagesElement.Element("imageDefaults"));
			foreach (XElement item2 in sourceImagesElement.Elements("image"))
			{
				ImageInfo item = LoadOldImage(projectInfo, item2);
				projectInfo.SourceImages.Add(item);
			}
		}

		private static ImageInfo LoadOldImage(StitchProjectInfo projectInfo, XElement imageElement)
		{
			string stringAttribute = imageElement.GetStringAttribute("src");
			Dictionary<string, Orientation> dictionary = new Dictionary<string, Orientation>(StringComparer.InvariantCultureIgnoreCase);
			dictionary.Add("unknown", Orientation.Unknown);
			dictionary.Add("normal", Orientation.Normal);
			dictionary.Add("flipped", Orientation.Normal);
			dictionary.Add("rot90cw", Orientation.Rotated90Clock);
			dictionary.Add("rot90cwFlipped", Orientation.Rotated90ClockHFlipped);
			dictionary.Add("rot90ccw", Orientation.Rotated90AntiClock);
			dictionary.Add("rot90ccwFlipped", Orientation.Rotated90AntiClockHFlipped);
			dictionary.Add("rot180", Orientation.Rotated180);
			dictionary.Add("rot180Flipped", Orientation.Rotated180HFlipped);
			Dictionary<string, Orientation> values = dictionary;
			imageElement.GetAttribute("orientation", values, Orientation.Unknown);
			ImagePose imagePose = LoadOldPose2D(imageElement.Element("camOrient2D")) ?? LoadOldPose3D(imageElement.Element("camOrient3D")) ?? LoadOldPoseMatrix(projectInfo, imageElement.Element("camOrientMatrix"));
			float[] array = LoadOldRadialDistortion(imageElement.Element("radialDistortion"));
			if (imagePose != null && array != null)
			{
				imagePose.LensDistortion[0] = array[0];
				imagePose.LensDistortion[1] = array[1];
				imagePose.LensDistortion[2] = array[2];
				imagePose.LensDistortion[3] = array[3];
			}
			return new ImageInfo(stringAttribute, imagePose);
		}

		private static void LoadOldSourceVideos(StitchProjectInfo projectInfo, XElement sourceVideosElement)
		{
			if (sourceVideosElement == null)
			{
				return;
			}
			foreach (XElement item2 in sourceVideosElement.Elements("video"))
			{
				VideoInfo item = LoadOldVideo(projectInfo, item2);
				projectInfo.SourceVideos.Add(item);
			}
		}

		private static VideoInfo LoadOldVideo(StitchProjectInfo projectInfo, XElement videoElement)
		{
			string stringAttribute = videoElement.GetStringAttribute("src");
			double doubleAttribute = videoElement.GetDoubleAttribute("startTime");
			double doubleAttribute2 = videoElement.GetDoubleAttribute("endTime");
			int integerAttribute = videoElement.GetIntegerAttribute("rotation");
			List<VideoFrameInfo> list = new List<VideoFrameInfo>();
			foreach (XElement item2 in videoElement.Elements("frame"))
			{
				VideoFrameInfo item = LoadOldFrame(projectInfo, item2, integerAttribute);
				list.Add(item);
			}
			return new VideoInfo(stringAttribute, doubleAttribute, doubleAttribute2, integerAttribute, 0, list);
		}

		private static VideoFrameInfo LoadOldFrame(StitchProjectInfo projectInfo, XElement frameElement, int quarterRotationCount)
		{
			double doubleAttribute = frameElement.GetDoubleAttribute("time");
			int num = frameElement.GetIntegerAttribute("width");
			int num2 = frameElement.GetIntegerAttribute("height");
			if (((uint)quarterRotationCount & (true ? 1u : 0u)) != 0)
			{
				int num3 = num;
				num = num2;
				num2 = num3;
			}
			float[] array = LoadOldRadialDistortion(frameElement.Element("radialDistortion"));
			ImagePose imagePose = LoadOldPose2D(frameElement.Element("camOrient2D")) ?? LoadOldPose3D(frameElement.Element("camOrient3D")) ?? LoadOldPoseMatrix(projectInfo, frameElement.Element("camOrientMatrix"));
			if (imagePose != null && array != null)
			{
				imagePose.LensDistortion[0] = array[0];
				imagePose.LensDistortion[1] = array[1];
				imagePose.LensDistortion[2] = array[2];
				imagePose.LensDistortion[3] = array[3];
			}
			VideoFrameInfo videoFrameInfo = new VideoFrameInfo(doubleAttribute, num, num2, isAutoSelected: false);
			videoFrameInfo.Pose = imagePose;
			foreach (XElement item in frameElement.Elements("rectangle"))
			{
				int integerAttribute = item.GetIntegerAttribute("left");
				int integerAttribute2 = item.GetIntegerAttribute("top");
				int integerAttribute3 = item.GetIntegerAttribute("right");
				int integerAttribute4 = item.GetIntegerAttribute("bottom");
				videoFrameInfo.Rectangles.Add(new Int32Rect(integerAttribute, integerAttribute2, integerAttribute3 - integerAttribute, integerAttribute4 - integerAttribute2));
			}
			videoFrameInfo.IsAutoSelected = videoFrameInfo.Rectangles.Count == 0;
			return videoFrameInfo;
		}

		private static ImagePose LoadOldPose2D(XElement camOrient2DElement)
		{
			ImagePose result = null;
			if (camOrient2DElement != null)
			{
				double oldAngleAttribute = camOrient2DElement.GetOldAngleAttribute("rotation");
				double doubleAttribute = camOrient2DElement.GetDoubleAttribute("scale");
				Vector oldVectorAttribute = camOrient2DElement.GetOldVectorAttribute("translation");
				float floatAttribute = camOrient2DElement.GetFloatAttribute("tolerance");
				result = ImagePose.CreateImagePose(oldAngleAttribute.ToRadians(), doubleAttribute, oldVectorAttribute.X, oldVectorAttribute.Y, floatAttribute);
			}
			return result;
		}

		private static ImagePose LoadOldPose3D(XElement camOrient3DElement)
		{
			ImagePose result = null;
			if (camOrient3DElement != null)
			{
				Quaternion oldRotationAttribute = camOrient3DElement.GetOldRotationAttribute("eulerAngles");
				double doubleAttribute = camOrient3DElement.GetDoubleAttribute("focalLength", 1.0);
				float floatAttribute = camOrient3DElement.GetFloatAttribute("tolerance");
				result = ImagePose.CreateImagePose(oldRotationAttribute, doubleAttribute, floatAttribute);
			}
			return result;
		}

		private static ImagePose LoadOldPoseMatrix(StitchProjectInfo projectInfo, XElement camOrientMatrixElement)
		{
			ImagePose result = null;
			if (camOrientMatrixElement != null)
			{
				double[] doubleArrayValue = camOrientMatrixElement.GetDoubleArrayValue(9);
				float floatAttribute = camOrientMatrixElement.GetFloatAttribute("tolerance");
				result = ImagePose.CreateImagePose(matrix: new Matrix3D(doubleArrayValue[0], doubleArrayValue[3], doubleArrayValue[6], 0.0, doubleArrayValue[1], doubleArrayValue[4], doubleArrayValue[7], 0.0, doubleArrayValue[2], doubleArrayValue[5], doubleArrayValue[8], 0.0, 0.0, 0.0, 0.0, 1.0), motionModel: projectInfo.CameraMotion, tolerance: floatAttribute);
			}
			return result;
		}

		private static float[] LoadOldRadialDistortion(XElement radialDistortionElement)
		{
			float[] result = null;
			if (radialDistortionElement != null)
			{
				result = radialDistortionElement.GetFloatArrayValue(4);
			}
			return result;
		}

		private static StitchProjectInfo LoadCurrentFormat(XElement rootElement)
		{
			StitchProjectInfo stitchProjectInfo = new StitchProjectInfo();
			stitchProjectInfo.CameraMotion = rootElement.GetAttribute("cameraMotion", allCameraMotions, MotionModel.Automatic);
			stitchProjectInfo.ComputedCameraMotion = rootElement.GetAttribute("computedCameraMotion", allCameraMotions, MotionModel.Unknown);
			stitchProjectInfo.Projection = rootElement.GetAttribute("projection", allProjections, MapType.Unknown);
			bool isRotatingMotion = stitchProjectInfo.CameraMotion == MotionModel.Rotation3D || stitchProjectInfo.ComputedCameraMotion == MotionModel.Rotation3D;
			stitchProjectInfo.ViewRotation = LoadViewRotation(rootElement.Element("viewRotation"), isRotatingMotion);
			stitchProjectInfo.VignetteCorrection = LoadVignetteCorrection(rootElement.Element("vignetteCorrection"));
			stitchProjectInfo.StructuredPanoramaSettings = LoadStructuredPanoramaSettings(rootElement.Element("structuredPanoramaSettings"));
			LoadSourceImages(stitchProjectInfo, rootElement.Element("sourceImages"));
			LoadSourceVideos(stitchProjectInfo, rootElement.Element("sourceVideos"));
			return stitchProjectInfo;
		}

		private static Quaternion LoadViewRotation(XElement viewRotationElement, bool isRotatingMotion)
		{
			Quaternion result = Quaternion.Identity;
			if (viewRotationElement != null)
			{
				AngleUnit attribute = viewRotationElement.GetAttribute("angleUnit", allAngleUnits, AngleUnit.Radians);
				double y = 0.0;
				double x = 0.0;
				if (isRotatingMotion)
				{
					y = viewRotationElement.GetAngleAttribute("yaw", attribute);
					x = viewRotationElement.GetAngleAttribute("pitch", attribute);
				}
				double angleAttribute = viewRotationElement.GetAngleAttribute("roll", attribute);
				Vector3D eulerAngles = new Vector3D(x, y, angleAttribute);
				result = eulerAngles.ToQuaternion();
			}
			return result;
		}

		private static VignetteParams LoadVignetteCorrection(XElement vignetteCorrectionElement)
		{
			VignetteParams result = null;
			if (vignetteCorrectionElement != null)
			{
				float floatAttribute = vignetteCorrectionElement.GetFloatAttribute("redFactor");
				float floatAttribute2 = vignetteCorrectionElement.GetFloatAttribute("greenFactor");
				float floatAttribute3 = vignetteCorrectionElement.GetFloatAttribute("blueFactor");
				float floatAttribute4 = vignetteCorrectionElement.GetFloatAttribute("globalIntensityScale", 1f);
				result = new VignetteParams(floatAttribute, floatAttribute2, floatAttribute3, floatAttribute4);
			}
			return result;
		}

		private static StructuredPanoramaInfo LoadStructuredPanoramaSettings(XElement structuredPanoramaSettingsElement)
		{
			StructuredPanoramaInfo result = null;
			if (structuredPanoramaSettingsElement != null)
			{
				StartingCorner attribute = structuredPanoramaSettingsElement.GetAttribute<StartingCorner>("startingCorner");
				PrimaryDirection attribute2 = structuredPanoramaSettingsElement.GetAttribute<PrimaryDirection>("primaryDirection");
				int integerAttribute = structuredPanoramaSettingsElement.GetIntegerAttribute("primaryDirectionImageCount");
				MovementMethod attribute3 = structuredPanoramaSettingsElement.GetAttribute<MovementMethod>("movementMethod");
				AngularRange attribute4 = structuredPanoramaSettingsElement.GetAttribute<AngularRange>("angularRange");
				double doubleAttribute = structuredPanoramaSettingsElement.GetDoubleAttribute("horizontalOverlap");
				double doubleAttribute2 = structuredPanoramaSettingsElement.GetDoubleAttribute("verticalOverlap");
				double doubleAttribute3 = structuredPanoramaSettingsElement.GetDoubleAttribute("seamOverlap");
				double doubleAttribute4 = structuredPanoramaSettingsElement.GetDoubleAttribute("featureMatchingSearchRadius");
				StructuredPanoramaInfo structuredPanoramaInfo = new StructuredPanoramaInfo();
				structuredPanoramaInfo.StartingCorner = attribute;
				structuredPanoramaInfo.PrimaryDirection = attribute2;
				structuredPanoramaInfo.PrimaryDirectionImageCount = integerAttribute;
				structuredPanoramaInfo.MovementMethod = attribute3;
				structuredPanoramaInfo.AngularRange = attribute4;
				structuredPanoramaInfo.HorizontalOverlap = doubleAttribute;
				structuredPanoramaInfo.VerticalOverlap = doubleAttribute2;
				structuredPanoramaInfo.SeamOverlap = doubleAttribute3;
				structuredPanoramaInfo.FeatureMatchingSearchRadius = doubleAttribute4;
				result = structuredPanoramaInfo;
			}
			return result;
		}

		private static void LoadSourceImages(StitchProjectInfo projectInfo, XElement sourceImagesElement)
		{
			if (sourceImagesElement == null)
			{
				return;
			}
			foreach (XElement item2 in sourceImagesElement.Elements("sourceImage"))
			{
				ImageInfo item = LoadSourceImage(projectInfo, item2);
				projectInfo.SourceImages.Add(item);
			}
		}

		private static ImageInfo LoadSourceImage(StitchProjectInfo projectInfo, XElement imageElement)
		{
			string stringAttribute = imageElement.GetStringAttribute("filePath");
			imageElement.GetAttribute("orientation", allOrientations, Orientation.Unknown);
			ImagePose imagePose = LoadCameraPose(projectInfo, imageElement.Element("cameraPose"));
			float[] array = LoadRadialDistortion(imageElement.Element("radialDistortion"));
			if (imagePose != null && array != null)
			{
				imagePose.LensDistortion[0] = array[0];
				imagePose.LensDistortion[1] = array[1];
				imagePose.LensDistortion[2] = array[2];
				imagePose.LensDistortion[3] = array[3];
			}
			return new ImageInfo(stringAttribute, imagePose);
		}

		private static void LoadSourceVideos(StitchProjectInfo projectInfo, XElement sourceVideosElement)
		{
			if (sourceVideosElement == null)
			{
				return;
			}
			foreach (XElement item2 in sourceVideosElement.Elements("sourceVideo"))
			{
				VideoInfo item = LoadSourceVideo(projectInfo, item2);
				projectInfo.SourceVideos.Add(item);
			}
		}

		private static VideoInfo LoadSourceVideo(StitchProjectInfo projectInfo, XElement sourceVideoElement)
		{
			string stringAttribute = sourceVideoElement.GetStringAttribute("filePath");
			double doubleAttribute = sourceVideoElement.GetDoubleAttribute("startTime");
			double doubleAttribute2 = sourceVideoElement.GetDoubleAttribute("endTime");
			int integerAttribute = sourceVideoElement.GetIntegerAttribute("rotation");
			List<VideoFrameInfo> list = new List<VideoFrameInfo>();
			foreach (XElement item2 in sourceVideoElement.Elements("videoFrame"))
			{
				VideoFrameInfo item = LoadVideoFrame(projectInfo, item2);
				list.Add(item);
			}
			return new VideoInfo(stringAttribute, doubleAttribute, doubleAttribute2, integerAttribute, 0, list);
		}

		private static VideoFrameInfo LoadVideoFrame(StitchProjectInfo projectInfo, XElement videoFrameElement)
		{
			double doubleAttribute = videoFrameElement.GetDoubleAttribute("time");
			int integerAttribute = videoFrameElement.GetIntegerAttribute("width");
			int integerAttribute2 = videoFrameElement.GetIntegerAttribute("height");
			bool booleanAttribute = videoFrameElement.GetBooleanAttribute("isAutoSelected");
			float[] array = LoadRadialDistortion(videoFrameElement.Element("radialDistortion"));
			ImagePose imagePose = LoadCameraPose(projectInfo, videoFrameElement.Element("cameraPose"));
			if (imagePose != null && array != null)
			{
				imagePose.LensDistortion[0] = array[0];
				imagePose.LensDistortion[1] = array[1];
				imagePose.LensDistortion[2] = array[2];
				imagePose.LensDistortion[3] = array[3];
			}
			VideoFrameInfo videoFrameInfo = new VideoFrameInfo(doubleAttribute, integerAttribute, integerAttribute2, booleanAttribute);
			videoFrameInfo.Pose = imagePose;
			foreach (XElement item in videoFrameElement.Elements("frameRectangle"))
			{
				int integerAttribute3 = item.GetIntegerAttribute("left");
				int integerAttribute4 = item.GetIntegerAttribute("top");
				int integerAttribute5 = item.GetIntegerAttribute("right");
				int integerAttribute6 = item.GetIntegerAttribute("bottom");
				videoFrameInfo.Rectangles.Add(new Int32Rect(integerAttribute3, integerAttribute4, integerAttribute5 - integerAttribute3, integerAttribute6 - integerAttribute4));
			}
			return videoFrameInfo;
		}

		private static float[] LoadRadialDistortion(XElement radialDistortionElement)
		{
			float[] result = null;
			if (radialDistortionElement != null)
			{
				float floatAttribute = radialDistortionElement.GetFloatAttribute("k1");
				float floatAttribute2 = radialDistortionElement.GetFloatAttribute("k2");
				float floatAttribute3 = radialDistortionElement.GetFloatAttribute("k3");
				float floatAttribute4 = radialDistortionElement.GetFloatAttribute("k4");
				result = new float[4] { floatAttribute, floatAttribute2, floatAttribute3, floatAttribute4 };
			}
			return result;
		}

		private static ImagePose LoadCameraPose(StitchProjectInfo projectInfo, XElement cameraPoseElement)
		{
			ImagePose result = null;
			if (cameraPoseElement != null)
			{
				MotionModel motionModel = projectInfo.ComputedCameraMotion;
				if (motionModel == MotionModel.Unknown || motionModel == MotionModel.Automatic)
				{
					motionModel = projectInfo.CameraMotion;
				}
				if (projectInfo.ComputedCameraMotion == MotionModel.Unknown || projectInfo.ComputedCameraMotion == MotionModel.Automatic)
				{
					motionModel = ((cameraPoseElement.Attribute("translationX") != null || cameraPoseElement.Attribute("translationY") != null) ? MotionModel.RigidScale : ((cameraPoseElement.Attribute("focalLength") == null) ? MotionModel.Homography : MotionModel.Rotation3D));
				}
				switch (motionModel)
				{
					case MotionModel.Translation:
					case MotionModel.Rigid:
					case MotionModel.RigidScale:
						{
							double value = 0.0;
							double scale = 1.0;
							if (projectInfo.CameraMotion != 0)
							{
								AngleUnit attribute2 = cameraPoseElement.GetAttribute("angleUnit", allAngleUnits, AngleUnit.Radians);
								value = cameraPoseElement.GetAngleAttribute("rotation", attribute2);
							}
							if (projectInfo.CameraMotion == MotionModel.RigidScale)
							{
								scale = cameraPoseElement.GetDoubleAttribute("scale", 1.0);
							}
							double doubleAttribute2 = cameraPoseElement.GetDoubleAttribute("translationX");
							double doubleAttribute3 = cameraPoseElement.GetDoubleAttribute("translationY");
							float floatAttribute3 = cameraPoseElement.GetFloatAttribute("tolerance");
							result = ImagePose.CreateImagePose(value.ToRadians(), scale, doubleAttribute2, doubleAttribute3, floatAttribute3);
							break;
						}
					case MotionModel.Rotation3D:
						{
							AngleUnit attribute = cameraPoseElement.GetAttribute("angleUnit", allAngleUnits, AngleUnit.Radians);
							double angleAttribute = cameraPoseElement.GetAngleAttribute("yaw", attribute);
							double angleAttribute2 = cameraPoseElement.GetAngleAttribute("pitch", attribute);
							double angleAttribute3 = cameraPoseElement.GetAngleAttribute("roll", attribute);
							double doubleAttribute = cameraPoseElement.GetDoubleAttribute("focalLength");
							float floatAttribute2 = cameraPoseElement.GetFloatAttribute("tolerance");
							Vector3D eulerAngles = new Vector3D(angleAttribute2, angleAttribute, angleAttribute3);
							Quaternion cameraRotation = eulerAngles.ToQuaternion();
							result = ImagePose.CreateImagePose(cameraRotation, doubleAttribute, floatAttribute2);
							break;
						}
					case MotionModel.Affine:
					case MotionModel.Homography:
						{
							Matrix3D matrixAttribute = cameraPoseElement.GetMatrixAttribute("matrix");
							float floatAttribute = cameraPoseElement.GetFloatAttribute("tolerance");
							result = ImagePose.CreateImagePose(motionModel, matrixAttribute, floatAttribute);
							break;
						}
				}
			}
			return result;
		}

		private static XElement SaveCurrentFormat(StitchProjectInfo projectInfo)
		{
			XElement xElement = new XElement("stitchProject", new XAttribute("version", currentProjectFileVersion.ToString(2)));
			xElement.SetAttribute("cameraMotion", projectInfo.CameraMotion, allCameraMotions, MotionModel.Unknown);
			if (projectInfo.ComputedCameraMotion != projectInfo.CameraMotion)
			{
				xElement.SetAttribute("computedCameraMotion", projectInfo.ComputedCameraMotion, allCameraMotions, MotionModel.Unknown);
			}
			xElement.SetAttribute("projection", projectInfo.Projection, allProjections, MapType.Unknown);
			if (projectInfo.ViewRotation != Quaternion.Identity)
			{
				bool flag = projectInfo.CameraMotion == MotionModel.Rotation3D || projectInfo.ComputedCameraMotion == MotionModel.Rotation3D;
				Vector3D vector3D = projectInfo.ViewRotation.ToEulerAngles();
				xElement.Add(new XElement("viewRotation", flag ? new XAttribute("yaw", vector3D.Y) : null, flag ? new XAttribute("pitch", vector3D.X) : null, new XAttribute("roll", vector3D.Z), new XAttribute("angleUnit", "degrees")));
			}
			if (projectInfo.VignetteCorrection != null)
			{
				xElement.Add(new XElement("vignetteCorrection", new XAttribute("redFactor", projectInfo.VignetteCorrection.RedFactor), new XAttribute("greenFactor", projectInfo.VignetteCorrection.GreenFactor), new XAttribute("blueFactor", projectInfo.VignetteCorrection.BlueFactor), new XAttribute("globalIntensityScale", projectInfo.VignetteCorrection.GlobalIntensityScale)));
			}
			StructuredPanoramaInfo structuredPanoramaSettings = projectInfo.StructuredPanoramaSettings;
			if (structuredPanoramaSettings != null)
			{
				xElement.Add(new XElement("structuredPanoramaSettings", new XAttribute("startingCorner", structuredPanoramaSettings.StartingCorner), new XAttribute("primaryDirection", structuredPanoramaSettings.PrimaryDirection), new XAttribute("primaryDirectionImageCount", structuredPanoramaSettings.PrimaryDirectionImageCount), new XAttribute("movementMethod", structuredPanoramaSettings.MovementMethod), new XAttribute("angularRange", structuredPanoramaSettings.AngularRange), new XAttribute("horizontalOverlap", structuredPanoramaSettings.HorizontalOverlap), new XAttribute("verticalOverlap", structuredPanoramaSettings.VerticalOverlap), new XAttribute("seamOverlap", structuredPanoramaSettings.SeamOverlap), new XAttribute("featureMatchingSearchRadius", structuredPanoramaSettings.FeatureMatchingSearchRadius)));
			}
			if (projectInfo.SourceImages.Count > 0)
			{
				IEnumerable<XElement> content = projectInfo.SourceImages.Select(SaveSourceImage);
				xElement.Add(new XElement("sourceImages", content));
			}
			if (projectInfo.SourceVideos.Count > 0)
			{
				IEnumerable<XElement> content2 = projectInfo.SourceVideos.Select(SaveSourceVideo);
				xElement.Add(new XElement("sourceVideos", content2));
			}
			return xElement;
		}

		private static XElement SaveSourceImage(ImageInfo sourceImage)
		{
			return new XElement("sourceImage", new XAttribute("filePath", sourceImage.FilePath), SaveRadialDistortion(sourceImage.Pose), SaveCameraPose(sourceImage.Pose));
		}

		private static XElement SaveSourceVideo(VideoInfo sourceVideo)
		{
			IEnumerable<XElement> enumerable = sourceVideo.RequiredFrames.Select(SaveVideoFrame);
			return new XElement("sourceVideo", new XAttribute("filePath", sourceVideo.FilePath), new XAttribute("startTime", sourceVideo.StartFrameTime), new XAttribute("endTime", sourceVideo.EndFrameTime), (sourceVideo.QuarterRotationCount != 0) ? new XAttribute("rotation", sourceVideo.QuarterRotationCount) : null, enumerable);
		}

		private static XElement SaveVideoFrame(VideoFrameInfo videoFrame)
		{
			IEnumerable<XElement> enumerable = videoFrame.Rectangles.Select(SaveFrameRectangle);
			return new XElement("videoFrame", new XAttribute("time", videoFrame.FrameTime), new XAttribute("width", videoFrame.FrameWidth), new XAttribute("height", videoFrame.FrameHeight), videoFrame.IsAutoSelected ? new XAttribute("isAutoSelected", videoFrame.IsAutoSelected) : null, SaveRadialDistortion(videoFrame.Pose), SaveCameraPose(videoFrame.Pose), enumerable);
		}

		private static XElement SaveFrameRectangle(Int32Rect frameRectangle)
		{
			return new XElement("frameRectangle", new XAttribute("left", frameRectangle.X), new XAttribute("top", frameRectangle.Y), new XAttribute("right", frameRectangle.X + frameRectangle.Width), new XAttribute("bottom", frameRectangle.Y + frameRectangle.Height));
		}

		private static XElement SaveCameraPose(ImagePose pose)
		{
			XElement xElement = null;
			if (pose != null && pose.IsValid)
			{
				xElement = new XElement("cameraPose");
				switch (pose.MotionModel)
				{
					case MotionModel.Translation:
					case MotionModel.Rigid:
					case MotionModel.RigidScale:
						if (pose.MotionModel != 0)
						{
							xElement.Add(new XAttribute("rotation", pose.Rotation.ToDegrees()));
							xElement.Add(new XAttribute("angleUnit", "degrees"));
						}
						if (pose.MotionModel == MotionModel.RigidScale)
						{
							xElement.Add(new XAttribute("scale", pose.Scale));
						}
						xElement.Add(new XAttribute("translationX", pose.Translation.X));
						xElement.Add(new XAttribute("translationY", pose.Translation.Y));
						break;
					case MotionModel.Rotation3D:
						{
							Vector3D vector3D = pose.CameraRotation.ToEulerAngles();
							xElement.Add(new XAttribute("yaw", vector3D.Y));
							xElement.Add(new XAttribute("pitch", vector3D.X));
							xElement.Add(new XAttribute("roll", vector3D.Z));
							xElement.Add(new XAttribute("angleUnit", "degrees"));
							xElement.Add(new XAttribute("focalLength", pose.CameraFocalLength));
							break;
						}
					case MotionModel.Affine:
					case MotionModel.Homography:
						xElement.SetMatrixAttribute("matrix", pose.Matrix);
						break;
				}
				if (pose.Tolerance > 0f)
				{
					xElement.Add(new XAttribute("tolerance", pose.Tolerance));
				}
			}
			return xElement;
		}

		private static XElement SaveRadialDistortion(ImagePose pose)
		{
			XElement result = null;
			if (pose != null && pose.IsValid)
			{
				float[] lensDistortion = pose.LensDistortion;
				if (lensDistortion != null && pose.LensDistortion.Any((float r) => r != 0f))
				{
					result = new XElement("radialDistortion", new XAttribute("k1", lensDistortion[0]), new XAttribute("k2", lensDistortion[1]), new XAttribute("k3", lensDistortion[2]), new XAttribute("k4", lensDistortion[3]));
				}
			}
			return result;
		}
	}

}