using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Microsoft.Research.D3DCore;
using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.PanoViewing;
using Microsoft.Research.ICE.Stitching;


namespace Microsoft.Research.ICE.Controls
{
    public partial class PanoOrientationEditor
    {
        private enum DragState
        {
            None,
            Yaw,
            Pitch,
            Roll
        }

        private const double MouseWheelDeltaIncrement = 120.0;

        private const double ZoomSpeed = 0.15;

        private DragState dragState;

        private Point mousePosition;

        public static readonly DependencyProperty CameraMotionProperty = DependencyProperty.Register("CameraMotion", typeof(MotionModel), typeof(PanoOrientationEditor), new PropertyMetadata(MotionModel.Unknown, CameraMotionPropertyChanged));

       

        public MotionModel CameraMotion
        {
            get
            {
                return (MotionModel)GetValue(CameraMotionProperty);
            }
            set
            {
                SetValue(CameraMotionProperty, value);
            }
        }

        public PanoOrientationEditor()
        {
            InitializeComponent();
        }

        private static void CameraMotionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PanoOrientationEditor panoOrientationEditor = (PanoOrientationEditor)d;
            bool flag = panoOrientationEditor.CameraMotion == MotionModel.Rotation3D;
            panoOrientationEditor.gridLineRenderer.ShowDiagonals = flag;
            Polygon polygon = panoOrientationEditor.pitchArea;
            Visibility visibility2 = (panoOrientationEditor.yawArea.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible));
            polygon.Visibility = visibility2;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point point = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            Point position = e.GetPosition(this);
            if (dragState != 0)
            {
                Vector vector = position - mousePosition;
                Vector3D axisOfRotation;
                double angleInDegrees;
                switch (dragState)
                {
                    case DragState.Yaw:
                        axisOfRotation = new Vector3D(0.0, 1.0, 0.0);
                        angleInDegrees = vector.X / ActualWidth * EffectiveFieldOfView.ToDegrees();
                        break;
                    case DragState.Pitch:
                        axisOfRotation = new Vector3D(1.0, 0.0, 0.0);
                        angleInDegrees = vector.Y / ActualWidth * EffectiveFieldOfView.ToDegrees();
                        break;
                    default:
                        axisOfRotation = new Vector3D(0.0, 0.0, 1.0);
                        angleInDegrees = Vector.AngleBetween(position - point, mousePosition - point);
                        SetRotationCursor(position, point);
                        break;
                }
                Orientation = new Quaternion(axisOfRotation, angleInDegrees) * Orientation;
                mousePosition = position;
            }
            else
            {
                SetRotationCursor(position, point);
            }
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            e.Handled = true;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (dragState != 0)
            {
                
            }
            dragState = DragState.None;
            Cursor = null;
            e.Handled = true;
        }

        protected override void ZoomBy(double zoomFactor, Point position)
        {
            Zoom *= zoomFactor;
        }

        private void YawPitchRollArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                mousePosition = e.GetPosition(this);
                if (sender == yawArea)
                {
                    dragState = DragState.Yaw;
                    Cursor = yawArea.Cursor;
                }
                else if (sender == pitchArea)
                {
                    dragState = DragState.Pitch;
                    Cursor = pitchArea.Cursor;
                }
                else
                {
                    dragState = DragState.Roll;
                    SetRotationCursor(center: new Point(ActualWidth / 2.0, ActualHeight / 2.0), position: mousePosition);
                }
            }
            e.Handled = true;
        }

        private void SetRotationCursor(Point position, Point center)
        {
            string key = ((position.Y < center.Y) ? "Top" : "Bottom") + ((position.X < center.X) ? "Left" : "Right") + "Cursor";
            Cursor = (Cursor)Resources[key];
        }

       
    }
}