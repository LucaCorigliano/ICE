using System;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Research.ICE.Controls
{
    public sealed class GridLineRenderer : FrameworkElement
    {
        private const double gridFractionOfView = 0.8;

        private const double gridLineSpacing = 50.0;

        private const double gridLineThickness = 1.0;

        private const float gridLineAlpha = 0.3f;

        private const double gridShadowThickness = 3.0;

        private const float gridShadowAlpha = 0.15f;

        private const double diagonalLineThickness = 1.0;

        private const float diagonalLineAlpha = 0.5f;

        private const double diagonalShadowThickness = 3.0;

        private const float diagonalShadowAlpha = 0.25f;

        private const double quadrantLineThickness = 1.0;

        private const float quadrantLineAlpha = 1f;

        private const double quadrantShadowThickness = 3.0;

        private const float quadrantShadowAlpha = 0.5f;

        private Brush gridLineBrush;

        private Brush gridShadowBrush;

        private Pen diagonalLinePen;

        private Pen diagonalShadowPen;

        private Pen quadrantLinePen;

        private Pen quadrantShadowPen;

        public static readonly DependencyProperty ShowDiagonalsProperty = DependencyProperty.Register("ShowDiagonals", typeof(bool), typeof(GridLineRenderer), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        private static readonly DependencyPropertyKey DeviceScalePropertyKey = DependencyProperty.RegisterReadOnly("DeviceScale", typeof(double), typeof(GridLineRenderer), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DeviceScaleProperty = DeviceScalePropertyKey.DependencyProperty;

        public bool ShowDiagonals
        {
            get
            {
                return (bool)GetValue(ShowDiagonalsProperty);
            }
            set
            {
                SetValue(ShowDiagonalsProperty, value);
            }
        }

        public double DeviceScale
        {
            get
            {
                return (double)GetValue(DeviceScaleProperty);
            }
            private set
            {
                SetValue(DeviceScalePropertyKey, value);
            }
        }

        public GridLineRenderer()
        {
            gridLineBrush = new SolidColorBrush(Color.FromScRgb(0.3f, 1f, 1f, 1f));
            gridLineBrush.Freeze();
            gridShadowBrush = new SolidColorBrush(Color.FromScRgb(0.15f, 0f, 0f, 0f));
            gridShadowBrush.Freeze();
            diagonalLinePen = new Pen(new SolidColorBrush(Color.FromScRgb(0.5f, 1f, 1f, 1f)), 1.0)
            {
                StartLineCap = PenLineCap.Flat,
                EndLineCap = PenLineCap.Flat
            };
            diagonalShadowPen = new Pen(new SolidColorBrush(Color.FromScRgb(0.25f, 0f, 0f, 0f)), 3.0)
            {
                StartLineCap = PenLineCap.Flat,
                EndLineCap = PenLineCap.Flat
            };
            quadrantLinePen = new Pen(new SolidColorBrush(Color.FromScRgb(1f, 1f, 1f, 1f)), 1.0)
            {
                StartLineCap = PenLineCap.Flat,
                EndLineCap = PenLineCap.Flat
            };
            quadrantShadowPen = new Pen(new SolidColorBrush(Color.FromScRgb(0.5f, 0f, 0f, 0f)), 3.0)
            {
                StartLineCap = PenLineCap.Flat,
                EndLineCap = PenLineCap.Flat
            };
            PresentationSource.AddSourceChangedHandler(this, SourceChanged);
        }

        protected override void OnRender(DrawingContext context)
        {
            double actualWidth = ActualWidth;
            double actualHeight = ActualHeight;
            double deviceScale = DeviceScale;
            double num = Math.Round(0.8 * actualWidth * deviceScale) / deviceScale;
            double num2 = Math.Round(0.8 * actualHeight * deviceScale) / deviceScale;
            double x = Math.Round((actualWidth - num) / 2.0 * deviceScale) / deviceScale;
            double y = Math.Round((actualHeight - num2) / 2.0 * deviceScale) / deviceScale;
            Rect rect = new Rect(x, y, num, num2);
            if (ShowDiagonals)
            {
                context.DrawLine(diagonalShadowPen, rect.TopLeft, rect.BottomRight);
                context.DrawLine(diagonalShadowPen, rect.BottomLeft, rect.TopRight);
                context.DrawLine(diagonalLinePen, rect.TopLeft, rect.BottomRight);
                context.DrawLine(diagonalLinePen, rect.BottomLeft, rect.TopRight);
            }
            DrawVerticalLines(context, rect, deviceScale, gridShadowBrush, 3.0);
            DrawHorizontalLines(context, rect, deviceScale, gridShadowBrush, 3.0);
            DrawVerticalLines(context, rect, deviceScale, gridLineBrush, 1.0);
            DrawHorizontalLines(context, rect, deviceScale, gridLineBrush, 1.0);
            Rect rectangle = rect;
            rectangle.Inflate(0.5 / deviceScale, 0.5 / deviceScale);
            double x2 = rect.X + (Math.Round(num / 2.0 * deviceScale) + 0.5) / deviceScale;
            double y2 = rect.Y + (Math.Round(num2 / 2.0 * deviceScale) + 0.5) / deviceScale;
            context.DrawRectangle(null, quadrantShadowPen, rectangle);
            context.DrawLine(quadrantShadowPen, new Point(x2, rect.Top), new Point(x2, rect.Bottom));
            context.DrawLine(quadrantShadowPen, new Point(rect.Left, y2), new Point(rect.Right, y2));
            context.DrawRectangle(null, quadrantLinePen, rectangle);
            context.DrawLine(quadrantLinePen, new Point(x2, rect.Top), new Point(x2, rect.Bottom));
            context.DrawLine(quadrantLinePen, new Point(rect.Left, y2), new Point(rect.Right, y2));
        }

        private void SourceChanged(object sender, SourceChangedEventArgs e)
        {
            double num = 1.0;
            if (e.NewSource != null && e.NewSource.CompositionTarget != null)
            {
                num = e.NewSource.CompositionTarget.TransformToDevice.M11;
            }
            DeviceScale = num;
            diagonalLinePen.Thickness = 1.0 / num;
            diagonalShadowPen.Thickness = 3.0 / num;
            quadrantLinePen.Thickness = 1.0 / num;
            quadrantShadowPen.Thickness = 3.0 / num;
        }

        private static void DrawVerticalLines(DrawingContext context, Rect gridRect, double deviceScale, Brush brush, double thickness)
        {
            double num = gridRect.Width / 2.0;
            int num2 = (int)Math.Floor(num / 50.0);
            for (int i = 1; i <= num2; i++)
            {
                double x = gridRect.X + (Math.Round((num - (double)i * 50.0) * deviceScale) - (thickness - 1.0) / 2.0) / deviceScale;
                Rect rectangle = new Rect(x, gridRect.Y, thickness / deviceScale, gridRect.Height);
                context.DrawRectangle(brush, null, rectangle);
                x = gridRect.X + (Math.Round((num + (double)i * 50.0) * deviceScale) - (thickness - 1.0) / 2.0) / deviceScale;
                rectangle = new Rect(x, gridRect.Y, thickness / deviceScale, gridRect.Height);
                context.DrawRectangle(brush, null, rectangle);
            }
        }

        private static void DrawHorizontalLines(DrawingContext context, Rect gridRect, double deviceScale, Brush brush, double thickness)
        {
            double num = gridRect.Height / 2.0;
            int num2 = (int)Math.Floor(num / 50.0);
            for (int i = 1; i <= num2; i++)
            {
                double y = gridRect.Y + (Math.Round((num - (double)i * 50.0) * deviceScale) - (thickness - 1.0) / 2.0) / deviceScale;
                Rect rectangle = new Rect(gridRect.X, y, gridRect.Width, thickness / deviceScale);
                context.DrawRectangle(brush, null, rectangle);
                y = gridRect.Y + (Math.Round((num + (double)i * 50.0) * deviceScale) - (thickness - 1.0) / 2.0) / deviceScale;
                rectangle = new Rect(gridRect.X, y, gridRect.Width, thickness / deviceScale);
                context.DrawRectangle(brush, null, rectangle);
            }
        }
    }
}