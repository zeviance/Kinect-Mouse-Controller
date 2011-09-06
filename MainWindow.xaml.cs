// Chris Wong @ chris-wong.net
//original code acquired from http://codelaboratories.com/forums/viewthread/416/P15/  Author:L14M333
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;
using System.ComponentModel;
using CLNUIDeviceTest;
using System.Windows.Input;
using System.Windows.Forms;
using System.Drawing;

namespace CLNUIDeviceTest {
    /* WARING: VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY MESSY CODE HERE: I am almost ashamed to release it :(
     * Source altererd originally from: http://benxtan.com/temp/pmidickinect_v0.2.zip <--- i use Ben x Tans method of reading the colour of each pixel (from depth feed) what i realise is very long, cpu intense and rubbish way
     * This method probably has no potential but can be used for ideas ect
    */
    public partial class MainWindow : Window, INotifyPropertyChanged {
        #region ViewModelProperty: TheCharacter
        private string _theCharacter;
        public string TheCharacter {
            get { return _theCharacter;
            }
            set { _theCharacter = value;
                OnPropertyChanged("TheCharacter");
            }
        }
        #endregion 

        #region ViewModelProperty: FromLeft
        private int _fromLeft;
        public int FromLeft {
            get { return _fromLeft;
            }
            set {
                _fromLeft = value + 10;
                OnPropertyChanged("FromLeft");
            }
        }
        #endregion

        #region ViewModelProperty: FromTop
        private int _fromTop;
        public int FromTop {
            get { return _fromTop;
            }
            set { _fromTop = value + 10;
                OnPropertyChanged("FromTop");
            }
        }
        #endregion

        #region ViewModelProperty: TheFontSize
        private int _theFontSize;
        public int TheFontSize {
            get { return _theFontSize;
            }
            set {
                _theFontSize = value;
                OnPropertyChanged("TheFontSize");
            }
        }
        #endregion

        private int stepSize = 5;
        private IntPtr motor = IntPtr.Zero;
        private DispatcherTimer ClickWait;
        private bool isMute = false;
        private IntPtr camera = IntPtr.Zero;
        public bool canclick = true; //just a way to make click more normal (if didnt exist click would click every frame something is detected) - see the timer for more info
        private NUIImage colorImage;
        private NUIImage depthImage;
        private NUIImage realImage;
        private WriteableBitmap processedImage;
        private bool usemouse = false;
        private Thread captureThread;
        private bool running;
        private bool drag; 
        double xres = (double)System.Windows.SystemParameters.PrimaryScreenWidth; //Get screen reolution to doubble up the pixels from kinects 640 x 480 res  
        double yres = (double)System.Windows.SystemParameters.PrimaryScreenHeight; //Get screen reolution to doubble up the pixels from kinects 640 x 480 res  

        IntPtr hmidi = IntPtr.Zero;

        public MainWindow() {
            InitializeComponent();
            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            DataContext = this;
            TheCharacter = "█";
            FromLeft = 0;
            FromTop = 0;
            TheFontSize = 9;
            //xp.Minimum = 0; // set the progress bar min and max
            //xp.Maximum = 640;
            //yp.Minimum = 0;
            //yp.Maximum = 480;
            try {
                motor = CLNUIDevice.CreateMotor();
                camera = CLNUIDevice.CreateCamera();
                CLNUIDevice.SetMotorLED(motor, 3);
            }
            catch (System.Exception ex) {
                System.Windows.MessageBox.Show(ex.ToString());
                this.Close(); // no point opening the program if it cant find kinect
            }
            ClickWait = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Normal, (EventHandler)delegate(object sender, EventArgs e)  {
                canclick = true;
                ClickWait.Stop();
            }, Dispatcher);
            //create the images
            colorImage = new NUIImage(640, 480);
            color.Source = colorImage.BitmapSource;
            depthImage = new NUIImage(640, 480);
            realImage = new NUIImage(640, 480);
            real.Source = realImage.BitmapSource;
            //depth.Source = depthImage.BitmapSource;
            processedImage = new WriteableBitmap(depthImage.BitmapSource);
            depth.Source = processedImage;
            // Create camera capture thread
            running = true;
            captureThread = new Thread(delegate() {
                CLNUIDevice.StartCamera(camera);
                int numLoops = 0;
                int pixelStep = 3;
                int loopStep = 1;
                while (running) {
                    CLNUIDevice.GetCameraDepthFrameRGB32(camera, colorImage.ImageData, 500); //normal feed (top)
                    CLNUIDevice.GetCameraDepthFrameRGB32(camera, depthImage.ImageData, 0); //to be altered feed (bottom)
                    CLNUIDevice.GetCameraColorFrameRGB32(camera, realImage.ImageData, 0);
                    int red = 255;
                    int green = 255;
                    int blue = 255;
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate() {
                        if (numLoops == 0 || numLoops == loopStep) {                                                                
                            int height = (int)depthImage.BitmapSource.Height;
                            int width = (int)depthImage.BitmapSource.Width;
                            int stride = (depthImage.BitmapSource.PixelWidth * depthImage.BitmapSource.Format.BitsPerPixel + 7) / 8;
                            byte[] newPixels = new byte[height * width * processedImage.Format.BitsPerPixel / 8];
                            bool found = false;
                            // Loop through the all the pixels row by row                            
                            bool foundclick = false;
                            bool draw = false;
                            for (int row = 0; row < height; row = row + pixelStep) {//for each row of pixels 
                                for (int col = 0; col < width; col = col + pixelStep) {//for each collumn of pixels
                                    // Disregard top 10% and lower 10% to speed up the app
                                    if ( row < height* 0.1 || row > height * 0.9) {
                                        break;
                                    }
                                    
                                    // Get current pixel
                                    //========================================
                                    //Overview of below code:
                                    //-----------------------
                                    //basically it goes through each pixel.. if it is a certian colour (set via depth) it then changes is colour to white/red/green - i did warn you, its ugly
                                    //================================-=======
                                    byte[] currentPixel = new byte[4];
                                    depthImage.BitmapSource.CopyPixels(new Int32Rect((int)col, (int)row, 1, 1), currentPixel, stride, 0);
                                    /*
                                    // Blue Green Red Alpha
                                    //Console.WriteLine("Test: " + currentPixel[0] + "," + currentPixel[1] + "," + currentPixel[2] + "," + currentPixel[3]);
                                    */
                                    int index = (row * stride) + (col * 4);
                                    //IF STATEMENT BELOW: 
                                    // currentPixel[0] = Blue
                                    // currentPixel[1] = Green
                                    // currentPixel[2] = Red
                                    // currentPixel[3] = Alpha - we dont actually need to use this 
                                    if (currentPixel[0] == 255 
                                        && (currentPixel[1] <= 45 && currentPixel[1] >= 15)
                                        && (currentPixel[2] <= 45 && currentPixel[2] >= 15))   // set this to your bacground colour - This is the colour the paper in my demo shows up as 
                                    {
                                            red = 255;
                                            green = 255;
                                            blue = 255;
                                        // if it is found it changes the pixels to white with the above code
                                    }
                                    //else if (currentPixel[0] == 255
                                    else if ((currentPixel[1] == 255)
                                        && (currentPixel[0] == 0 )
                                        && (currentPixel[2] >= 40 && currentPixel[2] <= 150))
                                        //&& (currentPixel[1] <= 50 && currentPixel[1] >= 46)
                                        //&& (currentPixel[2] <= 50 && currentPixel[2] >= 46))   // set this to your finger tip touching the background colour 
                                    {
                                        if (col <= 80 && row <= 60) //this if statement checks if the pixel is found in the top right of the screen it will be used as the clicker (thats how i set it up)
                                        {
                                            red = 0;
                                            green = 255; //sets it to come up green
                                            blue = 0;
                                            if (usemouse == true) // if i have clicked enable mouse
                                            {
                                                if (canclick == true) // this is the 200 ms gap between clicks i made, if it wasnt there it would click each frame my finger is detected
                                                {
                                                    SendDoubleClick(); // sends single click command (although its named doubble)
                                                    canclick = false; // disables can click
                                                    ClickWait.Start(); //starts 200ms wait timer
                                                }
                                            }
                                        }
                                        else // if its not in the 'click corner' then it will be used to set the point
                                        {
                                            red = 255; //sets the fingertip to red
                                            green = 0;
                                            blue = 0;
                                            if (!found)  {
                                                RawDat(col, row); //Update RawDat and to see the code executed when it finds the finger
                                                found = true;
                                            }
                                        }
                                    }
                                    else if (currentPixel[0] == 255 // just to make it a bit more tidy, it makes the rest of the hand white instead of black 
                                        && currentPixel[1] >= 47
                                        && currentPixel[2] >= 47)
                                    {
                                        red = 255;
                                        green = 255;
                                        blue = 255;
                                    }
                                    else // anything else is set to black
                                    {
                                        /*
                                        red = currentPixel[2];
                                        green = currentPixel[1];
                                        blue = currentPixel[0];
                                         */
                                        red = 0;
                                        green = 0;
                                        blue = 0;
                                    }
                                    // Set pixel with NEW colour
                                    newPixels[index] = (byte)blue;
                                    newPixels[index + 1] = (byte)green;
                                    newPixels[index + 2] = (byte)red;
                                    newPixels[index + 3] = 255;

                                    // Draw more pixels
                                    // Left pixel
                                    if (col >= 4)
                                    {
                                        newPixels[index - 4] = (byte)blue;
                                        newPixels[index - 4 + 1] = (byte)green;
                                        newPixels[index - 4 + 2] = (byte)red;
                                        newPixels[index - 4 + 3] = 255;
                                    }
                                    // Top pixel and left to pixel
                                    if (row > 0)
                                    {
                                        newPixels[index - (stride)] = (byte)blue;
                                        newPixels[index - (stride) + 1] = (byte)green;
                                        newPixels[index - (stride) + 2] = (byte)red;
                                        newPixels[index - (stride) + 3] = 255;

                                        newPixels[index - (stride) - 4] = (byte)blue;
                                        newPixels[index - (stride) - 4 + 1] = (byte)green;
                                        newPixels[index - (stride) - 4 + 2] = (byte)red;
                                        newPixels[index - (stride) - 4 + 3] = 255;
                                    }
                                }
                            }

                            // Draw the entire image
                            stride = (width * processedImage.Format.BitsPerPixel + 7) / 8;
                            processedImage.WritePixels(new Int32Rect(0, 0, width, height), newPixels, stride, 0);

                            numLoops = 0;
                        }
                        colorImage.Invalidate();
                        realImage.Invalidate();
                        //depthImage.Invalidate();
                        numLoops++;
                    });
                }
                CLNUIDevice.StopCamera(camera);
            });
            captureThread.IsBackground = true;
            captureThread.Start();
        }

        void RawDat(int _x, int _y) {
            //x.Content = _x; //sets the x lable 
            //xp.Value = _x; //sets the x progress bar val
            //y.Content = _y; //sets the y label
            //yp.Value = _y;//sets the y progress bar val
            FromLeft = _x / 2; //Sets the sqares location (the graph on my window)
            FromTop = _y / 2; //Sets the sqares location (the graph on my window)
            if (usemouse == true) {//if mouse enabled button pressed            
                int coorX = (int)(xres - (xres * (double)(_x / 640.0)));//(xres-_x * (xres / 640));
                int coorY = (int)(yres * (double)(_y / 480.0));//(_y * (yres / 480));
                TransitionMouseTo(coorX, coorY, 0.01);
                //SetMouseCursor = new System.Drawing.Point(coorX,coorY); //sets the cursor to kinects sensor location ajusted to meet your resolution 
                //SetMouseCursor = new System.Drawing.Point((int)(xres* (_x / 640)), (int)(yres * (_y / 480))); 
                //SetMouseCursor = new System.Drawing.Point(200, 100); 
            }
        }

        //  Function for calling the keyboard keys based on your kinect orientation
        void OutputKeys(int _x, int _y) {


        }

        void TransitionMouseTo(int x, int y, double durationSecs) {
            int frames = 25;
            PointF vector = new PointF();
            vector.X = (x - System.Windows.Forms.Cursor.Position.X) / frames;
            vector.Y = (y - System.Windows.Forms.Cursor.Position.Y) / frames;
            for (int i = 0; i < frames; i++) {
                System.Drawing.Point pos = System.Windows.Forms.Cursor.Position;
                pos.X += (int)vector.X;
                pos.Y += (int)vector.Y;
                System.Windows.Forms.Cursor.Position = pos;
                Thread.Sleep((int) (durationSecs / frames) * 100);
            }

        }


        int map(int value, int low1, int high1, int low2, int high2) {
            decimal newValue = (value - low1) / (decimal)(high1 - low1);   // normalise between 0-1
            newValue *= (high2 - low2);   // scale to new range
            newValue += low2;   // add lowest value of new range
            return (int) Math.Floor(newValue);
        }


        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            //accelerometerTimer.Stop();
            CLNUIDevice.SetMotorLED(motor, 0);
            if (motor != IntPtr.Zero)
                CLNUIDevice.DestroyMotor(motor);
            running = false;
            captureThread.Join();
            if (camera != IntPtr.Zero)
                CLNUIDevice.DestroyCamera(camera);
        }
        
        #region INotifiedProperty Block
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private void button1_Click(object sender, RoutedEventArgs e) {
            if (usemouse == false) {
                usemouse = true;
                button1.Content = "Disable Mouse";
            }
            else {
                usemouse = false;
                button1.Content = "Enable Mouse";
            }
        }
        // set the mouse cursor to where the wiimote is pointing to (using the midRawX and midRawY properties of the wiimote)
        System.Drawing.Point SetMouseCursor {
            set { System.Windows.Forms.Cursor.Position = value; }
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(UInt32 dwFlags, UInt32 dx, UInt32 dy, UInt32 dwData, IntPtr dwExtraInfo);
        private const UInt32 MouseEventLeftDown = 0x0002;
        private const UInt32 MouseEventLeftUp = 0x0004;

        public void SendDoubleClick() {
            mouse_event(MouseEventLeftDown, 0, 0, 0, new System.IntPtr());
            mouse_event(MouseEventLeftUp, 0, 0, 0, new System.IntPtr());
        }
        public void PressDown() {
            mouse_event(MouseEventLeftDown, 0, 0, 0, new System.IntPtr());
        }
        public void PressUp() {
            mouse_event(MouseEventLeftUp, 0, 0, 0, new System.IntPtr());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

    }
}