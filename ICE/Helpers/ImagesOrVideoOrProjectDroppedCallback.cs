using System;
using System.Collections.Generic;

namespace Microsoft.Research.ICE.Helpers
{
	public delegate void ImagesOrVideoOrProjectDroppedCallback(IEnumerable<string> imageFiles, string videoFile, string projectFile, string source);
}