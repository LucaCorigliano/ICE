using Microsoft.Research.VisionTools.Toolkit;

namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class TaskViewModel : Notifier
    {
        private double progress;

        private TaskState taskState;

        public TaskPurpose TaskPurpose { get; private set; }

        public string Message { get; private set; }

        public bool IsProgressIndeterminate { get; private set; }

        public double Progress
        {
            get
            {
                return progress;
            }
            set
            {
                SetProperty(ref progress, value, "Progress");
            }
        }

        public TaskState TaskState
        {
            get
            {
                return taskState;
            }
            set
            {
                SetProperty(ref taskState, value, "TaskState");
            }
        }

        public TaskViewModel(TaskPurpose taskPurpose)
        {
            TaskPurpose = taskPurpose;
            IsProgressIndeterminate = false;
            switch (taskPurpose)
            {
                case TaskPurpose.CheckForUpgrade:
                    Message = "Checking for updates";
                    IsProgressIndeterminate = true;
                    break;
                case TaskPurpose.SelectVideoFrames:
                    Message = "Analyzing video";
                    break;
                case TaskPurpose.Align:
                    Message = "Aligning images";
                    break;
                case TaskPurpose.Composite:
                    Message = "Compositing images";
                    break;
                case TaskPurpose.Project:
                    Message = "Projecting panorama";
                    break;
                case TaskPurpose.Complete:
                    Message = "Completing panorama";
                    break;
                case TaskPurpose.Reproject:
                    Message = "Reprojecting panorama";
                    break;
                case TaskPurpose.Export:
                    Message = "Exporting panorama";
                    break;
            }
        }
    }
}