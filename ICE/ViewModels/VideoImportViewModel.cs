using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Research.ICE.Stitching;
using Microsoft.Research.VisionTools.Toolkit;


namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class VideoImportViewModel : Notifier, IDisposable
    {
        private VideoWrapper videoWrapper;

        private string mediaFilename;

        private string errorMessage;

        private int rawWidth;

        private int rawHeight;

        private TimeSpan duration;

        private double framesPerSecond;

        private TimeSpan startTime;

        private TimeSpan endTime;

        private TimeSpan currentTime;

        private double rotationAngle;

        private VideoWrapper VideoWrapper
        {
            get
            {
                return videoWrapper;
            }
            set
            {
                if (videoWrapper != value)
                {
                    if (videoWrapper != null)
                    {
                        videoWrapper.Dispose();
                    }
                    videoWrapper = value;
                }
            }
        }

        public string MediaFilename
        {
            get
            {
                return mediaFilename;
            }
            private set
            {
                SetProperty(ref mediaFilename, value, "MediaFilename");
            }
        }

        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
            set
            {
                SetProperty(ref errorMessage, value, "ErrorMessage");
            }
        }

        public int RawWidth
        {
            get
            {
                return rawWidth;
            }
            private set
            {
                SetProperty(ref rawWidth, value, "RawWidth");
            }
        }

        public int RawHeight
        {
            get
            {
                return rawHeight;
            }
            private set
            {
                SetProperty(ref rawHeight, value, "RawHeight");
            }
        }

        public int RotatedWidth
        {
            get
            {
                if (((uint)QuarterRotationCount & (true ? 1u : 0u)) != 0)
                {
                    return RawHeight;
                }
                return RawWidth;
            }
        }

        public int RotatedHeight
        {
            get
            {
                if (((uint)QuarterRotationCount & (true ? 1u : 0u)) != 0)
                {
                    return RawWidth;
                }
                return RawHeight;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return duration;
            }
            private set
            {
                value = Clamp(value, TimeSpan.Zero, TimeSpan.MaxValue);
                if (SetProperty(ref duration, value, "Duration"))
                {
                    EndTime = Clamp(EndTime, TimeSpan.Zero, Duration);
                    StartTime = Clamp(StartTime, TimeSpan.Zero, EndTime);
                }
            }
        }

        public double FramesPerSecond
        {
            get
            {
                return framesPerSecond;
            }
            private set
            {
                SetProperty(ref framesPerSecond, value, "FramesPerSecond");
            }
        }

        public TimeSpan StartTime
        {
            get
            {
                return startTime;
            }
            set
            {
                value = Clamp(value, TimeSpan.Zero, EndTime);
                if (SetProperty(ref startTime, value, "StartTime"))
                {
                    ClearAutoSelectedFrames();
                    NotifyPropertyChanged("IsTimeRangeValid");
                    OnSettingsChanged(DirtyFlags.VideoFrameSelectionAndBeyond);
                }
            }
        }

        public TimeSpan EndTime
        {
            get
            {
                return endTime;
            }
            set
            {
                value = Clamp(value, StartTime, Duration);
                if (SetProperty(ref endTime, value, "EndTime"))
                {
                    ClearAutoSelectedFrames();
                    NotifyPropertyChanged("IsTimeRangeValid");
                    OnSettingsChanged(DirtyFlags.VideoFrameSelectionAndBeyond);
                }
            }
        }

        public TimeSpan CurrentTime
        {
            get
            {
                return currentTime;
            }
            set
            {
                if (!SetProperty(ref currentTime, value, "CurrentTime"))
                {
                    return;
                }
                foreach (VideoRectangleViewModel videoRectangle in VideoRectangles)
                {
                    if (videoRectangle.IsSelected && videoRectangle.Time != currentTime)
                    {
                        videoRectangle.IsSelected = false;
                    }
                }
                NotifyPropertyChanged("VideoRectanglesAtCurrentTime");
                NotifyPropertyChanged("IsCurrentTimeSelected");
            }
        }

        public double RotationAngle
        {
            get
            {
                return rotationAngle;
            }
            set
            {
                if (SetProperty(ref rotationAngle, value, "RotationAngle"))
                {
                    ClearAutoSelectedFrames();
                    NotifyPropertyChanged("QuarterRotationCount");
                    OnSettingsChanged(DirtyFlags.VideoFrameSelectionAndBeyond);
                }
            }
        }

        public int QuarterRotationCount => (int)Math.Round(RotationAngle / 90.0) & 3;

        public int SystemQuarterRotationCount { get; private set; }

        public ObservableCollection<VideoRectangleViewModel> VideoRectangles { get; private set; }

        public IEnumerable<VideoRectangleViewModel> SelectedVideoRectangles => VideoRectangles.Where((VideoRectangleViewModel r) => r.IsSelected);

        public IEnumerable<VideoRectangleViewModel> VideoRectanglesAtCurrentTime => VideoRectangles.Where((VideoRectangleViewModel r) => r.Time == CurrentTime);

        public ObservableCollection<TimeSpan> SelectedTimes { get; private set; }

        public bool IsCurrentTimeSelected
        {
            get
            {
                return SelectedTimes.Contains(CurrentTime);
            }
            set
            {
                if (IsCurrentTimeSelected != value)
                {
                    if (value)
                    {
                        SelectedTimes.Add(CurrentTime);
                    }
                    else
                    {
                        SelectedTimes.Remove(CurrentTime);
                    }
                    NotifyPropertyChanged("IsCurrentTimeSelected");
                }
            }
        }

        public bool IsTimeRangeValid => EndTime > StartTime;

        private List<VideoFrameInfo> RequiredFrames { get; set; }

        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        public VideoImportViewModel()
        {
            VideoRectangles = new ObservableCollection<VideoRectangleViewModel>();
            SelectedTimes = new ObservableCollection<TimeSpan>();
            RequiredFrames = new List<VideoFrameInfo>();
        }

        ~VideoImportViewModel()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                VideoWrapper = null;
            }
        }

        public void ImportVideo(VideoInfo videoInfo, bool isNewProject)
        {
            CurrentTime = TimeSpan.Zero;
            VideoRectangles.Clear();
            SelectedTimes.Clear();
            RequiredFrames.Clear();
            MediaFilename = videoInfo.FilePath;
            VideoWrapper = new VideoWrapper(videoInfo.FilePath);
            ErrorMessage = VideoWrapper.ErrorMessage;
            if (ErrorMessage == null)
            {
                StartTime = TimeSpan.Zero;
                TimeSpan timeSpan3 = (EndTime = (Duration = TimeSpan.FromSeconds(VideoWrapper.DurationInSeconds)));
                FramesPerSecond = VideoWrapper.FramesPerSecond;
                int num = VideoWrapper.Rotation / 90;
                RotationAngle = 90 * (isNewProject ? num : videoInfo.QuarterRotationCount);
                if (VideoWrapper.DecoderAutoRotates)
                {
                    SystemQuarterRotationCount = num;
                }

                if ((SystemQuarterRotationCount & 1) == 0)
                {
                    RawWidth = VideoWrapper.Width;
                    RawHeight = VideoWrapper.Height;
                }
                else
                {
                    RawWidth = VideoWrapper.Height;
                    RawHeight = VideoWrapper.Width;
                }
                if (videoInfo.StartFrameTime >= 0.0)
                {
                    StartTime = TimeSpan.FromSeconds(videoInfo.StartFrameTime);
                }
                if (videoInfo.EndFrameTime >= 0.0)
                {
                    EndTime = TimeSpan.FromSeconds(videoInfo.EndFrameTime);
                }
                if (videoInfo.RequiredFrames != null)
                {
                    Matrix rotationMatrix = GetRotationMatrix(-QuarterRotationCount, RotatedWidth, RotatedHeight);
                    foreach (VideoFrameInfo item in videoInfo.RequiredFrames.OrderBy((VideoFrameInfo f) => f.FrameTime))
                    {
                        if (item.Rectangles.Count <= 0)
                        {
                            continue;
                        }
                        TimeSpan timeSpan4 = TimeSpan.FromSeconds(item.FrameTime);
                        foreach (Int32Rect rectangle in item.Rectangles)
                        {
                            Int32Rect int32Rect = TransformRect(rectangle, rotationMatrix);
                            VideoRectangleViewModel videoRectangle = new VideoRectangleViewModel
                            {
                                Time = timeSpan4,
                                Left = int32Rect.X,
                                Top = int32Rect.Y,
                                Right = int32Rect.X + int32Rect.Width,
                                Bottom = int32Rect.Y + int32Rect.Height
                            };
                            int index = VideoRectangles.Count((VideoRectangleViewModel r) => r.Time <= videoRectangle.Time);
                            VideoRectangles.Insert(index, videoRectangle);
                            UpdateVideoRectangleImage(videoRectangle, DirtyFlags.None);
                        }
                        SelectedTimes.Add(timeSpan4);
                    }
                }
                StoreRequiredFrames(videoInfo);
            }
            else
            {

            }
        }

        public void StoreRequiredFrames(VideoInfo videoInfo)
        {
            RequiredFrames.Clear();
            if (videoInfo.RequiredFrames != null)
            {
                RequiredFrames.AddRange(videoInfo.RequiredFrames.OrderBy((VideoFrameInfo frame) => frame.FrameTime));
            }
        }

        public void AddVideoRectangle(VideoRectangleViewModel videoRectangle)
        {
            foreach (VideoRectangleViewModel videoRectangle2 in VideoRectangles)
            {
                videoRectangle2.IsSelected = false;
            }
            int index = VideoRectangles.Count((VideoRectangleViewModel r) => r.Time <= videoRectangle.Time);
            VideoRectangles.Insert(index, videoRectangle);
            DirtyFlags dirtyFlags = DirtyFlags.CompositingAndBeyond;
            if (!SelectedTimes.Contains(videoRectangle.Time))
            {
                SelectedTimes.Add(videoRectangle.Time);
                dirtyFlags = DirtyFlags.AlignmentAndBeyond;
            }
            NotifyPropertyChanged("IsCurrentTimeSelected");
            NotifyPropertyChanged("VideoRectanglesAtCurrentTime");
            OnSettingsChanged(dirtyFlags);
        }

        public void RemoveSelectedVideoRectangles()
        {
            VideoRectangleViewModel[] array = SelectedVideoRectangles.ToArray();
            VideoRectangleViewModel[] array2 = array;
            foreach (VideoRectangleViewModel videoRectangle in array2)
            {
                RemoveVideoRectangle(videoRectangle);
            }
            OnSettingsChanged(DirtyFlags.CompositingAndBeyond);
        }

        public void UpdateVideoRectangleImage(VideoRectangleViewModel videoRectangle, DirtyFlags dirtyFlags)
        {
            using (ImageWrapper imageWrapper = VideoWrapper.GetImage(videoRectangle.Time.TotalSeconds))
            {
                if (imageWrapper != null)
                {
                    imageWrapper.Rotate(-SystemQuarterRotationCount);
                    try
                    {
                        Int32Rect int32Rect = new Int32Rect(videoRectangle.Left, videoRectangle.Top, videoRectangle.Right - videoRectangle.Left, videoRectangle.Bottom - videoRectangle.Top);
                        imageWrapper.Crop(int32Rect);
                    }
                    catch 
                    {
                        
                    }
                    int width = imageWrapper.Width;
                    int height = imageWrapper.Height;
                    if (width > 80 || height > 80)
                    {
                        double val = 80.0 / (double)width;
                        double val2 = 80.0 / (double)height;
                        double num = Math.Min(val, val2);
                        width = (int)Math.Round(num * (double)width);
                        height = (int)Math.Round(num * (double)height);
                        imageWrapper.Resize(width, height);
                    }
                    videoRectangle.Image = imageWrapper.CreateBitmapSource();
                }
            }
            OnSettingsChanged(dirtyFlags);
        }

        public void UpdateProject(StitchProjectInfo projectInfo)
        {
            Matrix rotationMatrix = GetRotationMatrix(QuarterRotationCount, RawWidth, RawHeight);
            List<VideoFrameInfo> list = new List<VideoFrameInfo>();
            foreach (VideoRectangleViewModel videoRectangle in VideoRectangles)
            {
                Int32Rect rect = videoRectangle.Rect;
                Int32Rect item = TransformRect(rect, rotationMatrix);
                VideoFrameInfo videoFrameInfo = list.LastOrDefault();
                if (videoFrameInfo != null && videoFrameInfo.FrameTime == videoRectangle.Time.TotalSeconds)
                {
                    videoFrameInfo.Rectangles.Add(item);
                    continue;
                }
                VideoFrameInfo frame2 = new VideoFrameInfo(videoRectangle.Time.TotalSeconds, RotatedWidth, RotatedHeight, isAutoSelected: false);
                frame2.Rectangles.Add(item);
                VideoFrameInfo videoFrameInfo2 = RequiredFrames.FirstOrDefault((VideoFrameInfo f) => f.FrameTime == frame2.FrameTime);
                if (videoFrameInfo2 != null)
                {
                    frame2.Pose = videoFrameInfo2.Pose;
                }
                list.Add(frame2);
            }
            foreach (VideoFrameInfo frame in RequiredFrames.Where((VideoFrameInfo f) => f.IsAutoSelected))
            {
                Func<VideoFrameInfo, bool> predicate = (VideoFrameInfo f) => f.FrameTime < frame.FrameTime;
                int num = list.Count(predicate);
                if (num < list.Count && list[num].FrameTime == frame.FrameTime)
                {
                    list[num].IsAutoSelected = true;
                    continue;
                }
                list.Insert(num, new VideoFrameInfo(frame.FrameTime, frame.FrameWidth, frame.FrameHeight, isAutoSelected: true)
                {
                    Pose = frame.Pose
                });
            }
            VideoInfo item2 = new VideoInfo(MediaFilename, StartTime.TotalSeconds, EndTime.TotalSeconds, QuarterRotationCount, SystemQuarterRotationCount, list);
            projectInfo.SourceVideos.Add(item2);
        }

        private void ClearAutoSelectedFrames()
        {
            foreach (VideoFrameInfo requiredFrame in RequiredFrames)
            {
                requiredFrame.IsAutoSelected = false;
                requiredFrame.Pose = new ImagePose();
            }
            VideoFrameInfo[] array = RequiredFrames.Where((VideoFrameInfo f) => f.Rectangles.Count == 0).ToArray();
            foreach (VideoFrameInfo item in array)
            {
                RequiredFrames.Remove(item);
            }
        }

        private void RemoveVideoRectangle(VideoRectangleViewModel videoRectangle)
        {
            if (VideoRectangles.Remove(videoRectangle))
            {
                if (!VideoRectangles.Any((VideoRectangleViewModel vr) => vr.Time == videoRectangle.Time))
                {
                    SelectedTimes.Remove(videoRectangle.Time);
                }
                NotifyPropertyChanged("VideoRectanglesAtCurrentTime");
            }
        }

        private void OnSettingsChanged(DirtyFlags dirtyFlags)
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(dirtyFlags));
        }

        private static TimeSpan Clamp(TimeSpan time, TimeSpan minTime, TimeSpan maxTime)
        {
            if (!(time < minTime))
            {
                if (!(time > maxTime))
                {
                    return time;
                }
                return maxTime;
            }
            return minTime;
        }

        private static Matrix GetRotationMatrix(int quarterRotationCount, int width, int height)
        {
            switch (quarterRotationCount & 3)
            {
                case 1: return new Matrix(0.0, 1.0, -1.0, 0.0, height, 0.0);
                case 2: return new Matrix(-1.0, 0.0, 0.0, -1.0, width, height);
                case 3: return new Matrix(0.0, -1.0, 1.0, 0.0, 0.0, width);

            };
            return Matrix.Identity;
        }

        private static Int32Rect TransformRect(Int32Rect inputRect, Matrix matrix)
        {
            Point point = matrix.Transform(new Point(inputRect.X, inputRect.Y));
            Point point2 = matrix.Transform(new Point(inputRect.X + inputRect.Width, inputRect.Y + inputRect.Height));
            Rect rect = new Rect(point, point2);
            return new Int32Rect((int)rect.Left, (int)rect.Top, (int)(rect.Right - rect.Left), (int)(rect.Bottom - rect.Top));
        }
    }
}