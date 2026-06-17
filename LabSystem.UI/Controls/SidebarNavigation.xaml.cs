using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace LabSystem.UI.Controls
{
    public partial class SidebarNavigation : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(SidebarNavigation),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsSidebarPinnedProperty =
            DependencyProperty.Register("IsSidebarPinned", typeof(bool), typeof(SidebarNavigation),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsPinnedChanged));

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        public bool IsSidebarPinned
        {
            get { return (bool)GetValue(IsSidebarPinnedProperty); }
            set { SetValue(IsSidebarPinnedProperty, value); OnPropertyChanged("IsSidebarPinned"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SidebarNavigation()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                SidebarBorder.Width = IsSidebarPinned ? 220 : 60;
            };
        }

        private static void OnIsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SidebarNavigation;
            if (control != null && control.IsLoaded)
            {
                control.AnimateSidebarWidth(control.IsSidebarPinned ? 220 : 60);
            }
        }

        private void SidebarBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsSidebarPinned) return;
            AnimateSidebarWidth(220);
        }

        private void SidebarBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (IsSidebarPinned) return;
            AnimateSidebarWidth(60);
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            IsSidebarPinned = !IsSidebarPinned;
        }

        private void AnimateSidebarWidth(double targetWidth)
        {
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromSeconds(0.2),
                DecelerationRatio = 0.9
            };
            SidebarBorder.BeginAnimation(WidthProperty, animation);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
