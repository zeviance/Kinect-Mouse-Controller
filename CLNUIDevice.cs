using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace CLNUIDeviceTest
{
    public class CLNUIDevice
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // CLNUIMotor  API
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport("CLNUIDevice.dll", EntryPoint = "CreateNUIMotor", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateMotor();
        [DllImport("CLNUIDevice.dll", EntryPoint = "DestroyNUIMotor", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DestroyMotor(IntPtr motor);
        [DllImport("CLNUIDevice.dll", EntryPoint = "GetNUIMotorSerial", CallingConvention = CallingConvention.Cdecl)]
        public static extern string GetMotorSerial(IntPtr motor);
        [DllImport("CLNUIDevice.dll", EntryPoint = "NUIMotorMove", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool MotorMove(IntPtr motor, short position);
        [DllImport("CLNUIDevice.dll", EntryPoint = "GetNUIMotorAccelerometer", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetMotorAccelerometer(IntPtr motor, ref short x, ref short y, ref short z);
        [DllImport("CLNUIDevice.dll", EntryPoint = "SetNUIMotorLED", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SetMotorLED(IntPtr motor, byte mode);
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // CLNUICamera API
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport("CLNUIDevice.dll", EntryPoint = "CreateNUICamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateCamera();
        [DllImport("CLNUIDevice.dll", EntryPoint = "DestroyNUICamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool DestroyCamera(IntPtr camera);
        [DllImport("CLNUIDevice.dll", EntryPoint = "StartNUICamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool StartCamera(IntPtr camera);
        [DllImport("CLNUIDevice.dll", EntryPoint = "StopNUICamera", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool StopCamera(IntPtr camera);

        [DllImport("CLNUIDevice.dll", EntryPoint = "GetNUICameraColorFrameRAW", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetCameraColorFrameRAW(IntPtr camera, IntPtr data, int timeout);
        [DllImport("CLNUIDevice.dll", EntryPoint = "GetNUICameraColorFrameRGB24", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetCameraColorFrameRGB24(IntPtr camera, IntPtr data, int timeout);
        [DllImport("CLNUIDevice.dll", EntryPoint = "GetNUICameraColorFrameRGB32", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetCameraColorFrameRGB32(IntPtr camera, IntPtr data, int timeout);

        [DllImport("CLNUIDevice.dll", EntryPoint = "GetNUICameraDepthFrameRAW", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetCameraDepthFrameRAW(IntPtr camera, IntPtr data, int timeout);
        [DllImport("CLNUIDevice.dll", EntryPoint = "GetNUICameraDepthFrameRGB32", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetCameraDepthFrameRGB32(IntPtr camera, IntPtr data, int timeout);
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    }
}
