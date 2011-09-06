using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;

namespace CLNUIDeviceTest
{
    public class NUIImage : IDisposable
    {
        #region [ Native ]
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool UnmapViewOfFile(IntPtr hMap);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hHandle);
        #endregion

        private IntPtr _map = IntPtr.Zero;
        private IntPtr _section = IntPtr.Zero;

        public InteropBitmap BitmapSource { get; private set; }
        public IntPtr ImageData { get { return _map; } }

        public NUIImage(int width, int height)
        {
            uint imageSize = (uint)width * (uint)height * 4;
            // create memory section and map
            _section = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, imageSize, null);
            _map = MapViewOfFile(_section, 0xF001F, 0, 0, imageSize);
            BitmapSource = Imaging.CreateBitmapSourceFromMemorySection(_section, width, height, PixelFormats.Bgr32, width * 4, 0) as InteropBitmap;
        }

        public void Invalidate()
        {
            BitmapSource.Invalidate();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }
            // free native resources if there are any.
            if (_map != IntPtr.Zero)
            {
                UnmapViewOfFile(_map);
                _map = IntPtr.Zero;
            }
            if (_section != IntPtr.Zero)
            {
                CloseHandle(_section);
                _section = IntPtr.Zero;
            }
        }
    }
}
