﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Threading;

namespace AuraPhotoViewer.Styles.Behaviors
{
    public class ZoomingPanningBorderBehavior : Behavior<Border>
    {
        private Point start;
        private bool isRightSnap;
        private bool isLeftSnap;
        private bool isDownSnap;
        private bool isTopSnap;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseWheel += OnMouseWheel;
            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
            Application.Current.MainWindow.StateChanged += MainWindowOnStateChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseWheel -= OnMouseWheel;
            AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            Application.Current.MainWindow.StateChanged -= MainWindowOnStateChanged;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs mouseWheelEventArgs)
        {
            double zoom = mouseWheelEventArgs.Delta > 0 ? .2 : -.2;
            ScaleTransform scale = (ScaleTransform) ((TransformGroup) AssociatedObject.RenderTransform).Children[0];
            TranslateTransform translate =
                (TranslateTransform) ((TransformGroup) AssociatedObject.RenderTransform).Children[1];
            if (zoom < 0 && scale.ScaleX <= 1 && scale.ScaleY <= 1)
            {
                translate.X = 0;
                translate.Y = 0;
                isRightSnap = isLeftSnap = isDownSnap = isTopSnap = false;
                return;
            }
            //Point position = mouseWheelEventArgs.GetPosition(AssociatedObject);
            //AssociatedObject.RenderTransformOrigin = new Point(position.X / AssociatedObject.ActualWidth, position.Y / AssociatedObject.ActualHeight);
            scale.ScaleX += zoom;
            scale.ScaleY += zoom;
            AutoCorrectAfterScale(translate);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (AssociatedObject.IsMouseCaptured) return;
            start = mouseButtonEventArgs.GetPosition(AssociatedObject);
            AssociatedObject.CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!AssociatedObject.IsMouseCaptured) return;
            TranslateTransform translate =
                (TranslateTransform) ((TransformGroup) AssociatedObject.RenderTransform).Children[1];
            Point p = mouseEventArgs.GetPosition(AssociatedObject);
            Rect? bounds = GetImageBoundsRelativeToBorder();
            if (!bounds.HasValue)
            {
                return;
            }
            Rect boundsValue = bounds.Value;
            double offsetX = p.X - start.X;
            double offsetY = p.Y - start.Y;
            // Check if the bounds are located inside the Border control.
            if (AssociatedObject.ActualWidth < boundsValue.Width)
            {
                if (offsetX < 0 && boundsValue.Right > AssociatedObject.ActualWidth) // move left
                {
                    translate.X += offsetX;
                }
                if (offsetX > 0 && boundsValue.Left < 0) // move right
                {
                    translate.X += offsetX;
                }
            }
            if (AssociatedObject.ActualHeight < boundsValue.Height)
            {
                if (offsetY < 0 && boundsValue.Bottom > AssociatedObject.ActualHeight) // move up
                {
                    translate.Y += offsetY;
                }
                if (offsetY > 0 && boundsValue.Top < 0) // move down
                {
                    translate.Y += offsetY;
                }
            }
            isRightSnap = isLeftSnap = isDownSnap = isTopSnap = false;
            AutoCorrectAfterTranslate(translate, offsetX, offsetY);
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            AssociatedObject.ReleaseMouseCapture();
        }

        private Rect? GetImageBoundsRelativeToBorder()
        {
            DependencyObject child = FindVisualChild<Image>(AssociatedObject);
            if (child == null)
            {
                return null;
            }
            Image image = (Image) child;
            // The bounds of the transformed Image control in coordinates relative to the Border control.
            Rect rect = new Rect(new Size(image.ActualWidth, image.ActualHeight));
            return image.TransformToAncestor((Border) AssociatedObject.Parent).TransformBounds(rect);
        }

        private void AutoCorrectAfterTranslate(TranslateTransform translate, double offsetX, double offsetY)
        {
            Rect? bounds = GetImageBoundsRelativeToBorder();
            if (!bounds.HasValue)
            {
                return;
            }
            Rect boundsValue = bounds.Value;
            if (AssociatedObject.ActualWidth < boundsValue.Width)
            {
                if (offsetX <= 0 && boundsValue.Right <= AssociatedObject.ActualWidth) // move left
                {
                    translate.X += AssociatedObject.ActualWidth - boundsValue.Right; // move right
                    isRightSnap = true;
                }
                if (offsetX >= 0 && boundsValue.Left >= 0) // move right
                {
                    translate.X += -boundsValue.Left; // move left
                    isLeftSnap = true;
                }
            }
            if (AssociatedObject.ActualHeight < boundsValue.Height)
            {
                if (offsetY <= 0 && boundsValue.Bottom <= AssociatedObject.ActualHeight) // move up
                {
                    translate.Y += AssociatedObject.ActualHeight - boundsValue.Bottom; // move down
                    isDownSnap = true;
                }
                if (offsetY >= 0 && boundsValue.Top >= 0) // move down
                {
                    translate.Y += -boundsValue.Top; // move up
                    isTopSnap = true;
                }
            }
        }

        private void AutoCorrectAfterScale(TranslateTransform translate)
        {
            Rect? bounds = GetImageBoundsRelativeToBorder();
            if (!bounds.HasValue)
            {
                return;
            }
            Rect boundsValue = bounds.Value;
            if (isRightSnap && boundsValue.Right < AssociatedObject.ActualWidth)
            {
                translate.X += AssociatedObject.ActualWidth - boundsValue.Right; // move right
            }
            if (isLeftSnap && boundsValue.Left > 0)
            {
                translate.X += -boundsValue.Left; // move left
            }
            if (isDownSnap && boundsValue.Bottom < AssociatedObject.ActualHeight)
            {
                translate.Y += AssociatedObject.ActualHeight - boundsValue.Bottom; // move down
            }
            if (isTopSnap && boundsValue.Top > 0)
            {
                translate.Y += -boundsValue.Top; // move up
            }
        }

        private void MainWindowOnStateChanged(object sender, EventArgs e)
        {
            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() =>
                {
                    TranslateTransform translate =
                        (TranslateTransform) ((TransformGroup) AssociatedObject.RenderTransform).Children[1];
                    AutoCorrectAfterScale(translate);
                }));
        }

        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                T visualChild = child as T;
                if (visualChild != null)
                {
                    return visualChild;
                }
                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
}