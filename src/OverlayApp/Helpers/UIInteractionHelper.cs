namespace OverlayApp.Helpers
{
    public static class UIInteractionHelper
    {
        public static void AttachDrag(Canvas targetCanvas, Func<bool> isEditMode)
        {
            Point dragOffset = new();
            bool dragging = false;

            targetCanvas.MouseLeftButtonDown += (s, e) =>
            {
                if (!isEditMode()) return;
                dragging = true;
                dragOffset = e.GetPosition(targetCanvas);
                targetCanvas.CaptureMouse();
                e.Handled = true;
            };

            targetCanvas.MouseMove += (s, e) =>
            {
                if (!dragging || !isEditMode()) return;

                var parent = VisualTreeHelper.GetParent(targetCanvas) as Canvas;
                if (parent == null) return;

                var position = e.GetPosition(parent);
                Canvas.SetLeft(targetCanvas, position.X - dragOffset.X);
                Canvas.SetTop(targetCanvas, position.Y - dragOffset.Y);
                e.Handled = true;
            };

            targetCanvas.MouseLeftButtonUp += (s, e) =>
            {
                if (!dragging) return;
                dragging = false;
                targetCanvas.ReleaseMouseCapture();
                e.Handled = true;
            };
        }

        public static void AttachResize(Border handle, FrameworkElement target, Func<bool> isEditMode, double minW, double minH)
        {
            Point start = new();
            bool resizing = false;

            handle.MouseLeftButtonDown += (s, e) =>
            {
                if (!isEditMode()) return;
                resizing = true;
                start = e.GetPosition(target);
                handle.CaptureMouse();
                e.Handled = true;
            };

            handle.MouseMove += (s, e) =>
            {
                if (!resizing || !isEditMode()) return;

                var current = e.GetPosition(target);
                var delta = current - start;

                target.Width = Math.Max(minW, target.Width + delta.X);
                target.Height = Math.Max(minH, target.Height + delta.Y);

                start = current;
                e.Handled = true;
            };

            handle.MouseLeftButtonUp += (s, e) =>
            {
                resizing = false;
                handle.ReleaseMouseCapture();
                e.Handled = true;
            };
        }
    }
}