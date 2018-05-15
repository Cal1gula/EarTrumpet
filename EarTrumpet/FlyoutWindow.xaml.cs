﻿using EarTrumpet.DataModel;
using EarTrumpet.Extensions;
using EarTrumpet.Services;
using EarTrumpet.UserControls;
using EarTrumpet.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace EarTrumpet
{
    public partial class FlyoutWindow
    {
        private readonly MainViewModel _mainViewModel;
        private readonly FlyoutViewModel _viewModel;
        private VolumeControlPopup _popup;

        public FlyoutWindow(MainViewModel mainViewModel, IAudioDeviceManager manager)
        {
            _mainViewModel = mainViewModel;

            InitializeComponent();

            _viewModel = new FlyoutViewModel(mainViewModel, manager);
            _viewModel.StateChanged += OnStateChanged;
            _viewModel.AppExpanded += OnAppExpanded;
            _viewModel.AppCollapsed += OnAppCollapsed;
            _viewModel.WindowSizeInvalidated += (_, __) => UpdateWindowBounds();

            DataContext = _viewModel;

            _popup = (VolumeControlPopup)Resources["AppPopup"];
            _popup.Closed += (_, __) => _viewModel.OnAppCollapsed();

            Deactivated += (_, __) => _viewModel.BeginClose();

            SourceInitialized += (s, e) =>
            {
                this.Cloak();

                UpdateTheme();

                ThemeService.RegisterForThemeChanges(new WindowInteropHelper(this).Handle);
            };

            ThemeService.ThemeChanged += () => UpdateTheme();

            // Ensure the Win32 and WPF windows are created to fix first show issues with DPI Scaling
            Show();
            Hide();
            this.Cloak(false);

            _viewModel.ChangeState(FlyoutViewModel.ViewState.Hidden);
        }

        private void OnAppCollapsed(object sender, object e)
        {
            LayoutRoot.Children.Remove(_popup);
            _popup.IsOpen = false;
        }

        private void OnAppExpanded(object sender, AppExpandedEventArgs e)
        {
            var selectedApp = e.ViewModel;

            _popup.DataContext = selectedApp;
           LayoutRoot.Children.Add(_popup);

            Point relativeLocation = e.Container.TranslatePoint(new Point(0, 0), this);

            double HEADER_SIZE = (double)App.Current.Resources["DeviceTitleCellHeight"];
            double ITEM_SIZE = (double)App.Current.Resources["AppItemCellHeight"];
            Thickness volumeListMargin = (Thickness)App.Current.Resources["VolumeAppListMargin"];

            // TODO: can't figure out where this 6px is from
            relativeLocation.Y -= HEADER_SIZE + 6;

            var popupHeight = HEADER_SIZE + (selectedApp.ChildApps.Count * ITEM_SIZE) + volumeListMargin.Bottom + volumeListMargin.Top;

            // TODO: Cap top as well as bottom
            if (relativeLocation.Y + popupHeight > ActualHeight)
            {
                relativeLocation.Y = ActualHeight - popupHeight;
            }

            _popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
            _popup.HorizontalOffset = this.PointToScreen(new Point(0, 0)).X;
            _popup.VerticalOffset = this.PointToScreen(new Point(0, 0)).Y + relativeLocation.Y;

            _popup.Width = ActualWidth;
            _popup.Height = popupHeight;

            _popup.ShowWithAnimation();
        }

        private void OnStateChanged(object sender, FlyoutViewModel.ViewState e)
        {
            switch (e)
            {
                case FlyoutViewModel.ViewState.Opening:

                    UpdateWindowBounds();

                    this.ShowwithAnimation(() => _viewModel.ChangeState(FlyoutViewModel.ViewState.Open));
                    break;

                case FlyoutViewModel.ViewState.Closing:
                    this.Visibility = Visibility.Hidden;
                    _viewModel.ChangeState(FlyoutViewModel.ViewState.Hidden);
                    break;
            }
        }

        public void OpenAsFlyout()
        {
            switch (_viewModel.State)
            {
                case FlyoutViewModel.ViewState.Hidden:
                    _viewModel.BeginOpen();
                    break;
                case FlyoutViewModel.ViewState.Open:
                    _viewModel.BeginClose();
                    break;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _viewModel.BeginClose();
            }
        }

        private void UpdateTheme()
        {
            ThemeService.LoadCurrentTheme();

            if (ThemeService.IsWindowTransparencyEnabled && !SystemParameters.HighContrast)
            {
                this.EnableBlur();
            }
            else
            {
                this.DisableBlur();
            }
        }

        private void UpdateWindowBounds()
        {
            var taskbarState = TaskbarService.GetWinTaskbarState();

            double newHeight = 0;
            if (_viewModel.IsEmpty)
            {
                var NoItemsPaneHeight = (double)App.Current.Resources["NoItemsPaneHeight"];
                var NoItemsPaneMargin = (Thickness)App.Current.Resources["NoItemsPaneMargin"];

                newHeight = NoItemsPaneHeight + NoItemsPaneMargin.Bottom + NoItemsPaneMargin.Top;
            }
            else
            {
                var DeviceItemCellHeight = (double)App.Current.Resources["DeviceItemCellHeight"];
                var DeviceTitleCellHeight = (double)App.Current.Resources["DeviceTitleCellHeight"];
                var AppItemCellHeight = (double)App.Current.Resources["AppItemCellHeight"];
                
                var VolumeAppListMargin = (Thickness)App.Current.Resources["VolumeAppListMargin"];
                foreach (var device in _viewModel.Devices)
                {
                    newHeight += DeviceTitleCellHeight + DeviceItemCellHeight;
                    
                    if (device.Apps.Count > 0)
                    {
                        newHeight += VolumeAppListMargin.Bottom + VolumeAppListMargin.Top;
                    }

                    foreach(var app in device.Apps)
                    {
                        newHeight += AppItemCellHeight;
                    }
                }
            }

            bool isOverflowing = false;
            if (newHeight > taskbarState.TaskbarScreen.WorkingArea.Height)
            {
                newHeight = taskbarState.TaskbarScreen.WorkingArea.Height;
                isOverflowing = true;
            }

            BaseVisual.VerticalScrollBarVisibility = isOverflowing ? System.Windows.Controls.ScrollBarVisibility.Visible : System.Windows.Controls.ScrollBarVisibility.Hidden;

            double newTop = 0;
            double newLeft = 0;
            switch(taskbarState.TaskbarPosition)
            {
                case TaskbarPosition.Left:
                    newLeft = (taskbarState.TaskbarSize.Right / this.DpiWidthFactor());
                    newTop = (taskbarState.TaskbarSize.Bottom / this.DpiHeightFactor()) - newHeight;
                    break;
                case TaskbarPosition.Right:
                    newLeft = (taskbarState.TaskbarSize.Left / this.DpiWidthFactor()) - Width;
                    newTop = (taskbarState.TaskbarSize.Bottom / this.DpiHeightFactor()) - newHeight;
                    break;
                case TaskbarPosition.Top:
                    newLeft = (taskbarState.TaskbarSize.Right / this.DpiWidthFactor()) - Width;
                    newTop = (taskbarState.TaskbarSize.Bottom / this.DpiHeightFactor());
                    break;
                case TaskbarPosition.Bottom:
                    newLeft = (taskbarState.TaskbarSize.Right / this.DpiWidthFactor()) - Width;
                    newTop = (taskbarState.TaskbarSize.Top / this.DpiHeightFactor()) - newHeight;
                    break;
            }

            this.Move(newTop, newLeft, newHeight, Width);
        }

        private void ExpandCollapse_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DoExpandCollapse();
        }

        private void ExpandCollapse_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                // Top of window - don't wrap around.
                e.Handled = true;
            }
        }

        private void LightDismissBorder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _popup.HideWithAnimation();
            e.Handled = true;
        }

        private void DeviceAndAppsControl_AppExpanded(object sender, AppVolumeControlExpandedEventArgs e)
        {
            _viewModel.OnAppExpanded(e.ViewModel, e.Container);
        }
    }
}