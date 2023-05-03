using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Has2BeSameNameSpace
{
    public partial class Form1 : Form
    {
        private VideoCapture _capture;
        private LoCoMoCo CommandL2Bot; //created var from LoCoMoCo class
        private Thread _captureThread;
        private int _threshold = 150;
        private int threshold_redHmin = 0, threshold_redHmax = 255, threshold_redSmin = 0, threshold_redSmax = 255, threshold_redVmin = 0, threshold_redVmax = 255;
        private int threshold_yellowHmin = 0, threshold_yellowHmax = 255, threshold_yellowSmin = 0, threshold_yellowSmax = 255, threshold_yellowVmin = 0, threshold_yellowVmax = 255;

        bool Movement = false;

        public Form1()
        {
            InitializeComponent();
        }

       //initalizing camera processing
        private void Form1_Load(object sender, EventArgs e) 
        {
            _capture = new VideoCapture(0); //0 for laptop camera, 1 for webcam 
            _captureThread = new Thread(DisplayWebcam);
            _captureThread.Start();

            thresholdTrackbar.Value = _threshold;
            CommandL2Bot = new LoCoMoCo("COM9"); //serial communication port (device manager)
        }


        private void DisplayWebcam() //processing image data
        {
            while (_capture.IsOpened)
            {
                Mat frame = _capture.QueryFrame(); //frame maintenance

                //resize to PictureBox aspect ratio
                int newHeight = (frame.Size.Height * emguPictureBox.Size.Width) / frame.Size.Width;
                Size newSize = new Size(emguPictureBox.Size.Width, newHeight);
                CvInvoke.Resize(frame, frame, newSize);

                // display the image in the raw PictureBox
                emguPictureBox.Image = frame.ToBitmap();

                //convert the image to a binary image:
                Mat grayFrame = new Mat();

                //generate the thresholded image
                CvInvoke.CvtColor(frame, grayFrame, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                CvInvoke.Threshold(grayFrame, grayFrame, _threshold, 255, Emgu.CV.CvEnum.ThresholdType.Binary);
                emguBinary.Image = grayFrame.ToBitmap();

                //Convert the image to an HSV image:
                Mat hsvFrame = new Mat();
                CvInvoke.CvtColor(frame, hsvFrame, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);

                //split the HSV image into an array of Mat objects, one per channel:
                Mat[] hsvChannels = hsvFrame.Split();

                //filter out all but "the color you want"?
                Mat hueFilter = new Mat();
                CvInvoke.InRange(hsvChannels[0], new ScalarArray(threshold_yellowHmin), new ScalarArray(threshold_yellowHmax), hueFilter);
                Invoke(new Action(() => { hPictureBox.Image = hueFilter.ToBitmap(); }));

                //use the saturation channel to filter out all but certain saturations
                Mat saturationFilter = new Mat();
                CvInvoke.InRange(hsvChannels[1], new ScalarArray(threshold_yellowSmin), new ScalarArray(threshold_yellowSmax), saturationFilter);
                Invoke(new Action(() => { sPictureBox.Image = saturationFilter.ToBitmap(); }));

                //use the value channel to filter out all but brighter colors
                Mat valueFilter = new Mat();
                CvInvoke.InRange(hsvChannels[2], new ScalarArray(threshold_yellowVmin), new ScalarArray(threshold_yellowVmax), valueFilter);
                Invoke(new Action(() => { vPictureBox.Image = valueFilter.ToBitmap(); }));

                //now combine the filters together:
                Mat mergedImage = new Mat();
                CvInvoke.BitwiseAnd(hueFilter, saturationFilter, mergedImage);
                CvInvoke.BitwiseAnd(mergedImage, valueFilter, mergedImage);
                Invoke(new Action(() => { mergedPictureBox.Image = mergedImage.ToBitmap(); }));




                //Convert the image to an HSV image:
                Mat hsvFrameY = new Mat();
                CvInvoke.CvtColor(frame, hsvFrameY, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);

                //split the HSV image into an array of Mat objects, one per channel:
                Mat[] hsvChannelsY = hsvFrameY.Split();


                //filter out all but "the color you want"?
                Mat hueFilterY = new Mat();
                CvInvoke.InRange(hsvChannelsY[0], new ScalarArray(threshold_redHmin), new ScalarArray(threshold_redHmax), hueFilterY);
                Invoke(new Action(() => { hPictureBoxY.Image = hueFilterY.ToBitmap(); }));

                //use the saturation channel to filter out all but certain saturations
                Mat saturationFilterY = new Mat();
                CvInvoke.InRange(hsvChannelsY[1], new ScalarArray(threshold_redSmin), new ScalarArray(threshold_redSmax), saturationFilterY);
                Invoke(new Action(() => { sPictureBoxY.Image = saturationFilterY.ToBitmap(); }));

                //use the value channel to filter out all but brighter colors
                Mat valueFilterY = new Mat();
                CvInvoke.InRange(hsvChannelsY[2], new ScalarArray(threshold_redVmin), new ScalarArray(threshold_redVmax), valueFilterY);
                Invoke(new Action(() => { vPictureBoxY.Image = valueFilterY.ToBitmap(); }));

                //now combine the filters together:
                Mat mergedImageY = new Mat();
                CvInvoke.BitwiseAnd(hueFilterY, saturationFilterY, mergedImageY);
                CvInvoke.BitwiseAnd(mergedImageY, valueFilterY, mergedImageY);
                Invoke(new Action(() => { mergedPictureBoxY.Image = mergedImageY.ToBitmap(); }));


                Image<Gray, byte> img2 = mergedImage.ToImage<Gray, byte>();
                Image<Gray, byte> imgYellow = mergedImageY.ToImage<Gray, byte>();


                //initalize 5 slice locations
                int sliceWidth = grayFrame.Width / 5; //5 slices
                int whitePixelsLeft = 0;
                int whitePixelsLeftCenter = 0;
                int whitePixelsCenter = 0;
                int whitePixelsRightCenter = 0;
                int whitePixelsRight = 0;
                int redPixels = 0;

                //counter for pixel slices
                //slice 1 (Left)
                for (int x = 0; x < sliceWidth; x++)
                {
                    for (int y = 0; y < grayFrame.Height; y++)
                    {
                        if (img2.Data[y, x, 0] == 255) whitePixelsLeft++;


                    }
                }
                Invoke(new Action(() =>
                {
                    Slice1.Text = $"{whitePixelsLeft} white pixels";

                }));


               //slice 2 (Left Center)
                for (int x = sliceWidth; x < 2 * (sliceWidth); x++)
                {
                    for (int y = 0; y < grayFrame.Height; y++)
                    {
                        if (img2.Data[y, x, 0] == 255) whitePixelsLeftCenter++;
                    }
                }
                Invoke(new Action(() =>
                {
                    Slice2.Text = $"{whitePixelsLeftCenter} white pixels";
                }));


                //slice 3 (Center)
                for (int x = 2 * (sliceWidth); x < 3 * (sliceWidth); x++)
                {
                    for (int y = 0; y < grayFrame.Height; y++)
                    {
                        if (img2.Data[y, x, 0] == 255) whitePixelsCenter++;
                    }
                }
                Invoke(new Action(() =>
                {
                    Slice3.Text = $"{whitePixelsCenter} white pixels";
                }));


                //slice 4 (Center Right)
                for (int x = 3 * (sliceWidth); x < 4 * (sliceWidth); x++)
                {
                    for (int y = 0; y < grayFrame.Height; y++)
                    {
                        if (img2.Data[y, x, 0] == 255) whitePixelsRightCenter++;
                    }
                }
                Invoke(new Action(() =>
                {
                    Slice4.Text = $"{whitePixelsRightCenter} white pixels";
                }));


                //slice 5 (Right)
                for (int x = 4 * (sliceWidth); x < grayFrame.Width; x++)
                {
                    for (int y = 0; y < grayFrame.Height; y++)
                    {
                        if (img2.Data[y, x, 0] == 255) whitePixelsRight++;
                    }
                }
                Invoke(new Action(() =>
                {
                    Slice5.Text = $"{whitePixelsRight} white pixels";
                }));

                //red slice
                for (int x = 0; x < hsvFrameY.Width; x++)
                {
                    for (int y = 0; y < hsvFrameY.Height; y++)
                    {
                        if (imgYellow.Data[y, x, 0] == 255) ++redPixels;
                    }
                }


                //sliceArray parameters, slice locations
                int[] sliceArray = { whitePixelsCenter, whitePixelsLeftCenter, whitePixelsRightCenter, whitePixelsLeft, whitePixelsRight };
                int biggestSlice = sliceArray[0];
                int state = 0;

                for (int i = 1; i < sliceArray.Length; i++)
                {
                    if (sliceArray[i] > biggestSlice)
                    {
                        biggestSlice = sliceArray[i];
                        state = i;

                    }
                    if (redPixels > whitePixelsCenter + whitePixelsLeftCenter + whitePixelsRightCenter + whitePixelsLeft + whitePixelsRight)
                    {
                        state = 5; //red pixels = STOP
                    }
                }

                //movement according to sliceArray parameters
                if (Movement)
                {
                    switch (state) //using WASD movement + 'Z'=softLeft, 'X'=softRight
                    {
                        //must match sliceArray parameters in order.....Center = Forward.....Left Center = softLeft...etc.
                        case 0:
                            CommandL2Bot.Move('w'); //forward
                            break;
                        case 1:
                            CommandL2Bot.Move('z'); //softLeft
                            break;
                        case 2:
                            CommandL2Bot.Move('x'); //softRight
                            break;
                        case 3:
                            CommandL2Bot.Move('a'); //left
                            break;
                        case 4:
                            CommandL2Bot.Move('d'); //right
                            break;
                        case 5:
                            CommandL2Bot.Move('s'); //stop
                            break;

                    }


                }

                //or else default to STOP
                else CommandL2Bot.Move('s'); 



                //10 frames a seconds. Chip does 10 frames while camera does 29. adds slight delay
                Thread.Sleep(10);  
            }
        }
        //important feature, create and link formClosing event listenr on applic form, when event fires, terminate capture thread
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _captureThread.Abort();
        }

        //designer
        private void thresholdTrackbar_ValueChanged(object sender, EventArgs e)
        {
            _threshold = thresholdTrackbar.Value;
        }

        private void trackBarYellowH_Scroll(object sender, EventArgs e)
        {
            threshold_yellowHmin = trackBarYellowH.Value;
            yellowHmin.Text = $"{threshold_yellowHmin}";
        }

        private void trackBarYellowH2_Scroll(object sender, EventArgs e)
        {
            threshold_yellowHmax = trackBarYellowH2.Value;
            yellowHmax.Text = $"{threshold_yellowHmax}";
        }

        private void trackBarYellowS1_Scroll(object sender, EventArgs e)
        {
            threshold_yellowSmin = trackBarYellowS1.Value;
            yellowSmin.Text = $"{threshold_yellowSmin}";
        }

        private void trackBarYellowS2_Scroll(object sender, EventArgs e)
        {
            threshold_yellowSmax = trackBarYellowS2.Value;
            yellowSmax.Text = $"{threshold_yellowSmax}";
        }

        private void trackBarYellowV1_Scroll(object sender, EventArgs e)
        {
            threshold_yellowVmin = trackBarYellowV1.Value;
            yellowVmin.Text = $"{threshold_yellowVmin}";
        }


        private void trackBarYellowV2_Scroll(object sender, EventArgs e)
        {
            threshold_yellowVmax = trackBarYellowV2.Value;
            yellowVmax.Text = $"{threshold_yellowVmax}";
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            Movement = true;
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            Movement = false;
        }

        private void trackBarRedH1_Scroll(object sender, EventArgs e)
        {
            threshold_redHmin = trackBarRedH1.Value;
            redHmin.Text = $"{threshold_redHmin}";
        }

        private void trackBarRedH2_Scroll(object sender, EventArgs e)
        {
            threshold_redHmax = trackBarRedH2.Value;
            redHmax.Text = $"{threshold_redHmax}";
        }

        private void trackBarRedS1_Scroll(object sender, EventArgs e)
        {
            threshold_redSmin = trackBarRedS1.Value;
            redSmin.Text = $"{threshold_redSmin}";
        }

        private void trackBarRedS2_Scroll(object sender, EventArgs e)
        {
            threshold_redSmax = trackBarRedS2.Value;
            redSmax.Text = $"{threshold_redSmax}";
        }

        private void trackBarRedV1_Scroll(object sender, EventArgs e)
        {
            threshold_redVmin = trackBarRedV1.Value;
            redVmin.Text = $"{threshold_redVmin}";
        }

        private void trackBarRedV2_Scroll(object sender, EventArgs e)
        {
            threshold_redVmax = trackBarRedV2.Value;
            redVmax.Text = $"{threshold_redVmax}";
        }


    }
}