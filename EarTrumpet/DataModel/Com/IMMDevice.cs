﻿using System;
using System.Runtime.InteropServices;

namespace EarTrumpet.DataModel.Com
{
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice
    {
        void Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.Interface)] out object ppInterface);
        void OpenPropertyStore(uint stgmAccess, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppProperties);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        void GetState(out uint pdwState);
    }
}