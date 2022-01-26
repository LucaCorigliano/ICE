using System;

namespace Microsoft.Research.ICE.ViewModels
{
    [Flags]
    public enum DirtyFlags
    {
        None = 0,
        Initialization = 1,
        VideoFrameSelection = 2,
        Alignment = 4,
        Compositing = 8,
        Projection = 0x10,
        Completion = 0x20,
        Reprojection = 0x40,
        InitializationAndBeyond = 0x7F,
        VideoFrameSelectionAndBeyond = 0x7E,
        AlignmentAndBeyond = 0x7C,
        CompositingAndBeyond = 0x78,
        ProjectionAndBeyond = 0x70,
        CompletionAndBeyond = 0x60,
        ReprojectionAndBeyond = 0x40
    }
}