﻿using EarTrumpet.DataModel;
using EarTrumpet.Extensions;
using EarTrumpet.DataModel.Com;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace EarTrumpet.ViewModels
{
    public class DeviceViewModel : BindableBase
    {
        public AudioSessionViewModel Device { get; private set; }
        public ObservableCollection<AppItemViewModel> Apps { get; private set; }

        IAudioDevice _device;
        IAudioDeviceManager _deviceService;

        public DeviceViewModel(IAudioDeviceManager deviceService, IAudioDevice device)
        {
            _deviceService = deviceService;
            _device = device;

            Device = new AudioSessionViewModel(device);
            Apps = new ObservableCollection<AppItemViewModel>();

            _device.Sessions.CollectionChanged += Sessions_CollectionChanged;

            Apps.Clear();
            foreach (var session in _device.Sessions)
            {
                Apps.AddSorted(new AppItemViewModel(session), AppItemViewModelComparer.Instance);
            }
        }

        ~DeviceViewModel()
        {
            _device.Sessions.CollectionChanged -= Sessions_CollectionChanged;
        }

        public void TriggerPeakCheck()
        {
            Device.TriggerPeakCheck();

            foreach (var app in Apps) app.TriggerPeakCheck();
        }

        private void Sessions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1);
                    var newSession = new AppItemViewModel((IAudioDeviceSession)e.NewItems[0]);
                    Apps.AddSorted(newSession, AppItemViewModelComparer.Instance);
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1);
                    Apps.Remove(Apps.First(x => x.Id == ((IAudioDeviceSession)e.OldItems[0]).Id));
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    throw new NotImplementedException();

                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException();

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
            }
        }

        internal void TakeExternalSession(AudioSessionViewModel vm)
        {
            _device.TakeSessionFromOtherDevice(vm.Session);
        }
    }
}