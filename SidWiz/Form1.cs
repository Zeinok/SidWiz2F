﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing.Imaging;
using AviFile;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;



namespace SidWiz {
    public partial class Form1 : Form
    {
        String masterFile = "";
        Form lay = new LayoutForm();
        bool outputWindow = true;
        int voices = 0;
        Form2 frm;
        bool flag = false;
        int ResX = 0;
        int ResY = 0;
        int prevVoice = 0;
        Color clr = new Color();
        public Form1()
        {
            InitializeComponent();
        }





        private void Start_Click(object sender, EventArgs e)
        {
            //Stopwatch Setup = new Stopwatch();
            //Stopwatch ReadFromDisk = new Stopwatch();
            //Stopwatch Synchronization = new Stopwatch();
            //Stopwatch SaveAvi = new Stopwatch();
            //Stopwatch Draw = new Stopwatch();
            

            //Setup.Start();

            Random rnd = new Random();
            bool cc = chkColorCycle.Checked;
            #region setup and catch errors
            bool block = chkBlock.Checked;
            int thickness = cmbThick.SelectedIndex + 2;
            float percentDone = 0;
            float prevDone = 0;
            if (button2.Text == "Master Audio File")
            {
                MessageBox.Show("No master file specified");
                return;
            }
            if (!File.Exists(masterFile))
            {
                MessageBox.Show("Master file does not exist");
                return;
            }
            voices = cmbVoices.SelectedIndex + 1;
            bool[] altSync = new bool[voices];

            Color[] colorList = new Color[voices];
            String[] pathList = new String[voices];
            if (pathList.Length == 0)
            {
                MessageBox.Show("There was no file selected for channel 1");
                return;
            }

            if (chkSm.Checked)
            {
                foreach (LayoutControl_s li in lay.Controls)
                {
                    pathList[li.TabIndex] = li.filePath;
                    if (li.filePath == "none")
                    {
                        MessageBox.Show("There was no file selected for channel " + li.label1.Text);
                        return;
                    }
                    if (!File.Exists(li.filePath))
                    {
                        MessageBox.Show("The file for channel " + li.label1.Text + " does not exist.");
                        return;
                    }
                    colorList[li.TabIndex] = li.button1.BackColor;
                    altSync[li.TabIndex] = li.chkAlt.Checked;
                }
            }
            else
            {
                foreach (LayoutControl li in lay.Controls)
                {
                    pathList[li.TabIndex] = li.filePath;
                    if (li.filePath == "none")
                    {
                        MessageBox.Show("There was no file selected for channel " + li.label1.Text);
                        return;
                    }
                    if (!File.Exists(li.filePath))
                    {
                        MessageBox.Show("The file for channel " + li.label1.Text + " does not exist.");
                        return;
                    }
                    colorList[li.TabIndex] = li.button1.BackColor;
                    altSync[li.TabIndex] = li.chkAlt.Checked;
                }
            }

            bool grid = chkGrid.Checked;
            int setamt = 0;
            if (grid) setamt = 3;
            int cols = comboColumns.SelectedIndex;
            int columns = cols + 1;
            int yMax, yMin;

            int triggerLevel = 135;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "AVI Files (.avi)|*.avi";
            sfd.ShowDialog();
            if (sfd.FileName == "")
            {
                return;
            }

            Start.Enabled = false;





            
            //string fileName = "song";

            //int frameCounter = 0;

            long frameTriggerOffset = 0;
            int oldY = 0;
            int newY = 0;
            int oldZ = 3;
            int channels = 0;




            WAVFile temp = new WAVFile();
            temp.Open(pathList[0], WAVFile.WAVFileMode.READ);
            channels = temp.NumChannels;
            int jumpAmount = (temp.SampleRateHz / 60)*channels;                                     //3200 is 1 frame at 30fps and 96kHz samplerate / stereo.  735 is 44100hz/60fps/mono.  hz / framerate
            long frameIndex = jumpAmount + 1;

            WAVFile[] voiceData = new WAVFile[voices];

            long sampleLenght = temp.NumSamples / temp.NumChannels;



            temp.Close();


            ResX = 0;
            ResY = 0;
            
            switch (cmbRes.SelectedIndex)
            {
                case 0:
                    ResX = 852;
                    ResY = 480;
                    break;
                case 1:
                    ResX = 1280;
                    ResY = 720;
                    break;
                case 2:
                    ResX = 1920;
                    ResY = 1080;
                    break;
                case 3:
                    ResX = 2560;
                    ResY = 1440;
                    break;
            }




            Bitmap bitmap = new Bitmap(ResX, ResY, PixelFormat.Format24bppRgb);
            //create a new AVI file

            //String tempavi = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 8) + ".avi";
            string avifile = sfd.FileName;

            if (File.Exists(avifile)) File.Delete(avifile);
            AviManager aviManager = new AviManager(avifile, false);
            //add a new video stream and one frame to the new file
            VideoStream aviStream = aviManager.AddVideoStream(true, 60, ResX * ResY * 3, ResX, ResY, PixelFormat.Format24bppRgb);
            AviManager soundManager;
            AudioStream audioStream;
            try {
                soundManager = new AviManager(masterFile, true);
                audioStream = soundManager.GetWaveStream();
                aviManager.AddAudioStream(audioStream, 0);
                soundManager.Close();
            } catch(Exception ex) {
                MessageBox.Show(ex.Message,"Failed to open master audio file!",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);
                return;
            }
            float resScaler = (float)Math.Ceiling((float)voices/(float)columns);
            int slotSize = ResY / (int)resScaler;

            frm = new Form2();
            frm.Show(this);
            SetDesktopLocation(0, Location.Y);
            frm.SetDesktopLocation(Location.X + 300, 0);
            frm.Width = ResX + 32;
            frm.Height = ResY + 80;
            frm.pictureBox1.Location = new Point(10, 10);
            frm.pictureBox1.Height = ResY;
            frm.pictureBox1.Width = ResX;
            #endregion



            #region Scale

            float scale = 2;
            float center = 0;



            switch (cmbScale.SelectedIndex)
            {
                                
                case 0:
                    scale = 0.125f;
                    center = 32;
                    break;
                case 1:
                    scale = 0.25f;
                    center = 16;
                    break;
                case 2:
                    scale = 0.5f;
                    center = 8;
                    break;
                case 3:
                    scale = 1;
                    center = 4;
                    break;
                case 4:
                    scale = 2;
                    center = 2;
                    break;
                case 5:
                    scale = 4;
                    center = 1;
                    break;
                case 6:
                    scale = 8;
                    center = 0.5f;
                    break;
                case 7:
                    scale = 16;
                    center = 0.25f;
                    break;

            }
#endregion

            //Setup.Stop();
            //ReadFromDisk.Start();


            #region Read data chunk, all channels
            //jua is the size of vData
            //vData is the small amount of data read, per channel, per frame

            int jua = ((jumpAmount * 5) / channels)*((int)scale+1);


            List<string> err = new List<string>();
            for (int i = 0; i < voices; i++)
            {
                if (!File.Exists(pathList[i]))
                {
                    MessageBox.Show("File " + pathList[i] + " does not exist!");
                    return;
                }
                voiceData[i] = new WAVFile();
                try {
                    voiceData[i].Open(pathList[i], WAVFile.WAVFileMode.READ);
                }catch(Exception ex) {
                    err.Add(pathList[i] + ": " + ex.Message);
                    
                }
            }
            if(err.Count > 0) {
                StringBuilder sb = new StringBuilder();
                foreach(string msg in err)
                    sb.AppendLine(msg);
                MessageBox.Show(sb.ToString(), "Failed to open file!");
                return; //exit function
            }
#endregion

            //ReadFromDisk.Stop();


            while (frameIndex < ((sampleLenght * channels) - (jua + 10)))
                {

                    #region draw/sync setup

                                    Bitmap framebuffer = new Bitmap(ResX, ResY, PixelFormat.Format24bppRgb);

                                    //Locking the bits and doing direct operations on the bitmap data is MUCH faster than using the "pixel()" command.
                                    //Doing this brought render-time down from 55ms to 30ms originally with a separate function, but the bits had to be locked and unlocked every time.
                                    //Dragging this function into the main one means that we can do all passes without having to lock/unlock the bits hundreds of times.
                                    //This made render time go from 30ms to about 5-7ms.  UI+AVI time takes 25ms so this is about double speed, effectively.

                                    BitmapData bitmapData = framebuffer.LockBits(new Rectangle(0, 0, framebuffer.Width, framebuffer.Height), ImageLockMode.ReadWrite, framebuffer.PixelFormat);

                                    byte[] vData = new byte[jua];





                                    int oldY2 = 0;
                                    int newY2 = 0;
                                    int z = 0;
                    
                                    for (int i = 0; i < voices; i++)                          //Program runs this for each voice.  
                                    {
                                        z = 0;
                                        oldZ = ((i % columns) * (ResX / columns))*3;
                                        oldY2 = 0;
                                        newY2 = 0;
                                        clr = colorList[i];
                                        byte B = clr.R;
                                        byte G = clr.G;
                                        byte R = clr.B;

                                        if (cc)
                                        {
                                            int v = rnd.Next(1, 7);
                                            //int vq = rnd.Next(1, 10);
                                            //if (vq == 1)
                                            //{
                                            if (v == 1 && (int)(R + G + B) < 600 && R < 245)
                                                R += 10;
                                            else if (v == 2 && (int)(R + G + B) < 600 && G < 245)
                                                G += 10;
                                            else if (v == 3 && (int)(R + G + B) < 600 && B < 245)
                                                B += 10;
                                            else if (v == 4 && (int)(R + G + B) > 200 && R > 10)
                                                R -= 10;
                                            else if (v == 5 && (int)(R + G + B) > 200 && G > 10)
                                                G -= 10;
                                            else if ((int)(R + G + B) > 200 && B > 10)
                                                B -= 10;
                                            colorList[i] = Color.FromArgb(B, G, R);
                                            //}
                                        }
                                        long b = (frameIndex - (long)(jumpAmount*2.5));
                                        if (b < 0)
                                            b = frameIndex - jumpAmount;

                                        voiceData[i].SeekToAudioSample(b);
                                        for (int ze = 0; ze < jua; ze++)
                                        {
                                            vData[ze] = voiceData[i].GetNextSampleAs8Bit();
                                            if (channels == 2)
                                            {
                                                vData[ze] = (byte)(((int)vData[ze] + (int)voiceData[i].GetNextSampleAs8Bit()) / 2);
                                            }
                                        }

                                        /*frameTriggerOffset = 0;                                 //syncronation
                                   while (voiceData[i, frameIndex + frameTriggerOffset] < 128 && frameTriggerOffset < 3000) frameTriggerOffset++;
                                   while (voiceData[i, frameIndex + frameTriggerOffset] >= 126 && frameTriggerOffset < 3000) frameTriggerOffset++;*/


                                        //jac is the search window
                                        yMax = 0;                                       // scan for peak values
                                        yMin = 255;
                                        int jac = (jumpAmount / channels);
                                        for (int h = (jua/2)-jac; h <= (jua/2)+jac; h++)
                                        {
                                            int value = vData[h];
                                            if (value > yMax)
                                            {
                                                yMax = value;
                                            }
                                            if (value < yMin) yMin = value;
                                        }


                                        triggerLevel = (yMin + yMax) / 2;   //the middle line of the waveform

                #endregion
                                        
                                        //Synchronization.Start();

                    #region syncronization
                                        //  while (vData[qx] >= (triggerLevel) && qx < jac * 2) qx++;
                                        //    while ((vData[qx] != yMax-1 || (yMin == yMax || yMin == yMax-1)) && qx < jac*2) qx++;

                                        if (altSync[i] == false)
                                        {
                                            frameTriggerOffset = jac;
                                            while (vData[frameTriggerOffset] < (triggerLevel + 1) && frameTriggerOffset < jac * 2) frameTriggerOffset++;
                                            while (vData[frameTriggerOffset] >= (triggerLevel - 1) && frameTriggerOffset < jac * 2) frameTriggerOffset++;
                                            if (frameTriggerOffset == jac * 2) frameTriggerOffset = 0;
                                        }
                                        else
                                        {



                                            List<List<int>> distances = new List<List<int>>();
                                            int qx = jac;
                                            while (vData[qx] >= (triggerLevel) && qx < jac * 2) qx++;
                                            int ctr;
                                            while (qx < jac * 2)
                                            {
                                                ctr = qx;
                                                List<int> data = new List<int>();
                                                bool isUp = false;
                                                //find point where crosses midline
                                                if (vData[qx] < triggerLevel)
                                                {
                                                    while (vData[qx] < (triggerLevel) && qx < jac * 2) qx++;
                                                    isUp = true;
                                                }
                                                else while (vData[qx] >= (triggerLevel) && qx < jac * 2) qx++;

                                                //add point to data
                                                if (!isUp)
                                                {
                                                    data.Add(qx - ctr); //difference
                                                    data.Add(qx); //position of the offset
                                                    distances.Add(data);
                                                }


                                            } 
                                            
                                            ctr = 0; //count of highest values
                                            List<int> highest = new List<int>(); //this will be the highest value
                                            highest.Add(0);
                                            highest.Add(0);
                                            foreach (List<int> data in distances)
                                            {
                                                if (data[0] > highest[0])
                                                {
                                                    highest.RemoveRange(0, highest.Count);

                                                    highest.Add(data[0]);
                                                    highest.Add(data[1]);
                                                    ctr = 1;
                                                }
                                                else if (data[0] == highest[0])
                                                {
                                                    highest.Add(data[1]);
                                                    ctr++;
                                                }
                                            }
                                            //at this point "highest" should be a list where the first value is the difference, and the rest are points in order where the difference occurred
                                            //ctr is the number of same values. if more than 95% it's probably a square wave 
                                            if (ctr != 1) ctr = (int)Math.Ceiling(ctr / 2.0);
                                            frameTriggerOffset = highest[ctr];

                                        }
                                        //}
                                        //else
                                        //{
                                        //    frameTriggerOffset = jac;
                                        //    while (vData[frameTriggerOffset] < (triggerLevel + 1) && frameTriggerOffset < jac * 2) frameTriggerOffset++;
                                        //    while (vData[frameTriggerOffset] >= (triggerLevel - 1) && frameTriggerOffset < jac * 2) frameTriggerOffset++;
                                        //    if (frameTriggerOffset == jac * 2) frameTriggerOffset = 0;

                                        //}



                                        /*
                                        if (altSync[i] == false)
                                        {
                                            //old synchronization. Finds a spot where the wave crosses the middle line from below, then from above and uses that as the point

                                        }
                                        else
                                        {
                                            //newer sync.  finds the maximum value, then finds the point where it crosses the middle line
                                            frameTriggerOffset = jac;
                                            //if (frameIndex < jac) frameTriggerOffset = jac;
                                            int max = 0;
                                            long ftoPos = jac;
                                            while (frameTriggerOffset < jac * 2)
                                            {
                                                if (max < vData[frameTriggerOffset])
                                                {
                                                    max = vData[frameTriggerOffset];
                                                    ftoPos = frameTriggerOffset;
                                                }
                                                frameTriggerOffset++;
                                            }
                                            frameTriggerOffset = ftoPos;
                                            while (vData[frameTriggerOffset] >= (max * 0.8) && frameTriggerOffset < (jac * 3) - 1) frameTriggerOffset++;
                                            if (frameTriggerOffset == jac * 2) frameTriggerOffset = ftoPos;
                                        }*/

                #endregion
                                    
                                        //Synchronization.Stop();

                    #region Flip waveform for drawing
                                        for(int j=0; j<vData.Length; j++)
                                            vData[j]=(byte)(255 - vData[j]);
                #endregion             
                                        
                    #region Draw Waveform Setup


                                        int scalar = 2;
                                        int divisor = 1;
                                        int offset = 0;
                                        float trueScale = (float)(ResY / 255.0) / (float)Math.Ceiling((float)voices / (float)columns);
                                        int halfpoint = (int)(((float)voices / columns) + .5);
                                        float multi = (float)halfpoint / (float)(voices - halfpoint);
                                        if (cols == 1) divisor = 2;
                                        if (divisor == 2) scalar = 1;






                                        int sectionCount = i % columns;
                                        int sectionSize = ResX / columns;
                                        float cond1 = (scale / scalar);
                                        int cond2 = (framebuffer.Width / divisor) - setamt;
                                        int bound = thickness + (sectionCount * sectionSize);

                                        for (int x = 0; x / (cond1) < (cond2); x++)
                                        {   //draw waveform         

                                            //----------------------calculate positions
                                            long vdPos = frameTriggerOffset + x - (int)(ResX / center);
                                            if (vdPos < 0) vdPos = 0;
                                            int vdSet = vData[vdPos] + offset;
                                            newY = (int)(vdSet * trueScale) + ((i / columns) * slotSize);
                                            int middle = (int)(127 * trueScale) + ((i / columns) * slotSize);
                                            //newY -= ResY / 20;

                                            if (oldY == 1)
                                            {
                                                oldY2 = newY;// (int)((float)newY * (float)(resScaler / (float)halfpoint));
                                            }
                                            else
                                            {
                                                oldY2 = oldY;// (int)((float)oldY * (float)(resScaler / (float)halfpoint));  //255 is max for 8-bit, this gives us scaling per voice.
                                            }

                                            newY2 = newY; //* (float)(resScaler / (float)halfpoint));

                                            if (oldY2 > ResY - 3) oldY2 = ResY - 3;
                                            if (newY2 > ResY - 3) newY2 = ResY - 3;
                                            if (newY2 < 1) newY2 = 1;
                                            if (oldY2 < 1) oldY2 = 1;



                                            float div = 1;
                                            div = 2 / (float)columns;

                                            z = (int)(((float)x / (scale / div)) + (sectionCount * sectionSize));
                                            
                                            //z -= i % columns;
                                            //----------------------setup for drawline
                                            if (z < (((i%columns)*(ResX/columns))+thickness)) z = ((i%columns)*(ResX/columns))+thickness-1;

                                            

                                            //if (z <= bound) continue;
                                            z *= 3;           //3 bytes per pixel
                                            int supY = oldY2;
                                            if (oldY2 > newY2) //waveform moved down
                                            {
                                                int tmp = oldY2;
                                                oldY2 = newY2;
                                                newY2 = tmp;
                                            }
                #endregion

                                            //Draw.Start();

                    #region DrawLine

                                            //-------------drawline-------------------  sucks but is immensely faster, 30 ms -> 5 ms
                                            
                                            unsafe
                                            {

                                                int bytesPerPixel = 3;
                                                int heightInPixels = bitmapData.Height;
                                                int widthInBytes = bitmapData.Width * bytesPerPixel;
                                                byte* ptrFirstPixel = (byte*)bitmapData.Scan0;

                                                //get pixel byte values for color.  Format is apparently BGR




                                                if (cond1 < 1) //if cond1 is a fraction we need to fill spots
                                                {
                                                    for (int u = oldZ; u < z; u += 3)
                                                    {
                                                        byte* currentLine = ptrFirstPixel + (supY * bitmapData.Stride);  //get current line of pixels
                                                        //if (u < 3) u = 3;

                                                        currentLine[u] = R;
                                                        currentLine[u + 1] = G;
                                                        currentLine[u + 2] = B;

                                                        if (currentLine[u + 3] == 0 && currentLine[u + 4] == 0 && currentLine[u + 5] == 0)
                                                        {
                                                            currentLine[u + 3] = R;
                                                            currentLine[u + 4] = G;
                                                            currentLine[u + 5] = B;
                                                        }

                                                        for (int r = 2; r < thickness; r++)
                                                        {
                                                            int pd = (3 + ((r - 2) * 3));
                                                            if (z - pd < 0) break;
                                                            if (currentLine[z - pd] == 0 && currentLine[z - (pd - 1)] == 0 && currentLine[z - (pd - 2)] == 0)
                                                            {
                                                                currentLine[z - pd] = R;
                                                                currentLine[z - (pd - 1)] = G;
                                                                currentLine[z - (pd - 2)] = B;
                                                            }
                                                        }
                                                        currentLine = ptrFirstPixel + ((supY + 1) * bitmapData.Stride); //move down a line
                                                        if (currentLine[u + 3] == 0 && currentLine[u + 4] == 0 && currentLine[u + 5] == 0)
                                                        {
                                                            currentLine[u + 3] = R;
                                                            currentLine[u + 4] = G;
                                                            currentLine[u + 5] = B;
                                                        }
                                                        for (int r = 2; r < thickness; r++)
                                                        {
                                                            int pd = (3 + ((r - 2) * 3));
                                                            if (z - pd < 0) break;
                                                            if (currentLine[z - pd] == 0 && currentLine[z - (pd - 1)] == 0 && currentLine[z - (pd - 2)] == 0)
                                                            {
                                                                currentLine[z - pd] = R;
                                                                currentLine[z - (pd - 1)] = G;
                                                                currentLine[z - (pd - 2)] = B;
                                                            }
                                                        }
                                                        currentLine[u] = R;
                                                        currentLine[u + 1] = G;
                                                        currentLine[u + 2] = B;
                                                        for (int q = 3; q <= thickness; q++)
                                                        {
                                                            currentLine = ptrFirstPixel + ((supY - 1) * bitmapData.Stride); //move up a line
                                                            if (currentLine[u + 3] == 0 && currentLine[u + 4] == 0 && currentLine[u + 5] == 0)
                                                            {
                                                                currentLine[u + 3] = R;
                                                                currentLine[u + 4] = G;
                                                                currentLine[u + 5] = B;
                                                            }
                                                            for (int r = 2; r < thickness; r++)
                                                            {
                                                                int pd = (3 + ((r - 2) * 3));
                                                                if (z - pd < 0) break;
                                                                if (currentLine[z - pd] == 0 && currentLine[z - (pd - 1)] == 0 && currentLine[z - (pd - 2)] == 0)
                                                                {
                                                                    currentLine[z - pd] = R;
                                                                    currentLine[z - (pd - 1)] = G;
                                                                    currentLine[z - (pd - 2)] = B;
                                                                }
                                                            }
                                                            if (currentLine[u] == 0 && currentLine[u + 1] == 0 && currentLine[u + 2] == 0)
                                                            {
                                                                currentLine[u] = R;
                                                                currentLine[u + 1] = G;
                                                                currentLine[u + 2] = B;
                                                            }
                                                        }
                                                        if (block == true)
                                                        {
                                                            int yx = supY;
                                                            currentLine = ptrFirstPixel + (supY * bitmapData.Stride);
                                                            while (yx > middle)
                                                            {
                                                                currentLine = ptrFirstPixel + ((yx) * bitmapData.Stride); //move down a line
                                                                currentLine[u] = R;
                                                                currentLine[u + 1] = G;
                                                                currentLine[u + 2] = B;
                                                                yx--;
                                                            }
                                                            yx = supY;
                                                            while (yx <= middle)
                                                            {
                                                                currentLine = ptrFirstPixel + ((yx) * bitmapData.Stride); //move down a line
                                                                currentLine[u] = R;
                                                                currentLine[u + 1] = G;
                                                                currentLine[u + 2] = B;
                                                                yx++;
                                                            }
                                                        }
                                                    }
                                                    oldZ = z;
                                                }


                                                if (block == true)
                                                {
                                                    for (int y = oldY2; y <= newY2; y++)
                                                    {
                                                        int yx = y;
                                                        byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);
                                                        while (yx > middle)
                                                        {
                                                            currentLine = ptrFirstPixel + ((yx) * bitmapData.Stride); //move down a line
                                                            currentLine[z] = R;
                                                            currentLine[z + 1] = G;
                                                            currentLine[z + 2] = B;
                                                            yx--;
                                                        }
                                                        yx = y;
                                                        while (yx <= middle)
                                                        {
                                                            currentLine = ptrFirstPixel + ((yx) * bitmapData.Stride); //move down a line
                                                            currentLine[z] = R;
                                                            currentLine[z + 1] = G;
                                                            currentLine[z + 2] = B;
                                                            yx++;
                                                        }
                                                    }
                                                }
                                                else  //normal drawing
                                                {

                                                        for (int y = oldY2; y <= newY2; y++)
                                                        {
                                                            byte* currentLine = ptrFirstPixel + (y * bitmapData.Stride);  //get current line of pixels

                                                            

                                                            currentLine[z] = R;
                                                            currentLine[z + 1] = G;
                                                            currentLine[z + 2] = B;

                                                            if (currentLine[z + 3] == 0 && currentLine[z + 4] == 0 && currentLine[z + 5] == 0)
                                                            {
                                                                currentLine[z + 3] = R;
                                                                currentLine[z + 4] = G;
                                                                currentLine[z + 5] = B;
                                                            }
                                                            currentLine = ptrFirstPixel + ((y + 1) * bitmapData.Stride); //move down a line

                                                            if (currentLine[z] == 0 && currentLine[z + 1] == 0 && currentLine[z + 2] == 0)
                                                            {
                                                                currentLine[z] = R;
                                                                currentLine[z + 1] = G;
                                                                currentLine[z + 2] = B;
                                                            }
                                                            if (currentLine[z + 3] == 0 && currentLine[z + 4] == 0 && currentLine[z + 5] == 0)
                                                            {
                                                                currentLine[z + 3] = R;
                                                                currentLine[z + 4] = G;
                                                                currentLine[z + 5] = B;
                                                            }
                                                            for (int r = 2; r < thickness; r++)
                                                            {
                                                                int pd = (3 + ((r - 2) * 3));
                                                                if (z - pd < 0) break;
                                                                if (currentLine[z - pd] == 0 && currentLine[z - (pd - 1)] == 0 && currentLine[z - (pd - 2)] == 0)
                                                                {
                                                                    currentLine[z - pd] = R;
                                                                    currentLine[z - (pd - 1)] = G;
                                                                    currentLine[z - (pd - 2)] = B;
                                                                }
                                                            }
                                                            int btmp = 0;
                                                            for (int q = 1; q <= thickness-2; q++)
                                                            {
                                                                if (supY - q < 0)
                                                                    btmp = 1;
                                                                else btmp = q;
                                                                currentLine = ptrFirstPixel + ((supY - btmp) * bitmapData.Stride); //move up a line
                                                                if (currentLine[z + 3] == 0 && currentLine[z + 4] == 0 && currentLine[z + 5] == 0)
                                                                {
                                                                    currentLine[z + 3] = R;
                                                                    currentLine[z + 4] = G;
                                                                    currentLine[z + 5] = B;
                                                                }
                                                                for (int r = 2; r < thickness; r++)
                                                                {
                                                                    int pd = (3 + ((r - 2) * 3));
                                                                    if (z - pd < 0) break;
                                                                    if (currentLine[z - pd] == 0 && currentLine[z - (pd - 1)] == 0 && currentLine[z - (pd - 2)] == 0)
                                                                    {
                                                                        currentLine[z - pd] = R;
                                                                        currentLine[z - (pd - 1)] = G;
                                                                        currentLine[z - (pd - 2)] = B;
                                                                    }
                                                                }
                                                                if (currentLine[z] == 0 && currentLine[z + 1] == 0 && currentLine[z + 2] == 0)
                                                                {
                                                                    currentLine[z] = R;
                                                                    currentLine[z + 1] = G;
                                                                    currentLine[z + 2] = B;
                                                                }
                                                            }

                                                        }

                                                }
                                            }
                                            oldY = newY;

                                        }
                                        oldY = 1;
                                    } // end of i / channels
                                    //draw grid lines

                                    framebuffer.UnlockBits(bitmapData);
                #endregion

                                    //Draw.Stop();

                    #region grid

                                    if (grid)
                                    {
                                        Pen blackPen = new Pen(Color.Gray, 1);
                                        Pen dark = new Pen(Color.Black, 1);
                                        int vces = voices; //temporary variable for voices being rounded up for grid heights
                                        if (vces % columns != 0)
                                          vces += 1;
                                        using (var graphics = Graphics.FromImage(framebuffer))
                                        {

                                            int wide = (framebuffer.Width / columns) - 1; //old was -2
                                            int tall = ((framebuffer.Height / vces) * (columns));
                                            int zx = wide;
                                            for (int j = 1; j < columns; j++)
                                            {
                                                //graphics.DrawLine(dark, zx, 0, zx, framebuffer.Height - 1);
                                                graphics.DrawLine(blackPen, zx - 1, 0, zx - 1, framebuffer.Height - 1);
                                                //graphics.DrawLine(dark, zx - 2, 0, zx - 2, framebuffer.Height - 1);
                                                zx += wide;
                                            }
                                            if (voices > 1)
                                            {
                                                for (int r = 1; r < vces / (columns); r++)
                                                {
                                                    //for(int s = 0; s < framebuffer.Width-1; s++){
                                                    //framebuffer.SetPixel(s,r*(framebuffer.Height/voices),Color.FromArgb(150,150,150));
                                                    graphics.DrawLine(blackPen, 0, r * tall, framebuffer.Width - 1, r * tall);
                                                    //}

                                                }
                                            }
                                        }
                                    }
                                    #endregion
                                    //SaveAvi.Start();

                    #region Add frame to AVI and display on screen

                                    //note that this code easily takes the longest time to execute at 25ms or so.  Look for ways to improve this.  Threading AVI output doesn't seem to work.

                    if (Application.OpenForms.Count == 0) break;  //if the program is closed, jump out of loop and exit gracefully.

                    //following code adds a frame of video data to the avistream

                    frameIndex += jumpAmount;

                    if (flag)
                    {
                        frm.SetDesktopLocation(Location.X + 300, 0);
                        frm.Width = ResX + 32;
                        frm.Height = ResY + 88;
                        flag = false;
                    }

                    try
                    {
                        if (outputWindow)
                        {
                            frm.pictureBox1.Image = framebuffer;
                            frm.pictureBox1.Refresh();

                            //get rid of red X
                            typeof(Control).InvokeMember("SetState", BindingFlags.NonPublic |
                          BindingFlags.InvokeMethod | BindingFlags.Instance, null,
                          frm.pictureBox1, new object[] { 0x400000, false });
                        }
                        else
                        {
                            frm.SetDesktopLocation(Location.X + Width, Location.Y);
                            frm.Width = 75;
                            frm.Height = 70;
                        }

                    }
                    catch { }
                    
                
                    aviStream.AddFrame(framebuffer);    //add frame to AVI
                    
                    Application.DoEvents(); //so the UI doesn't freeze

                    try
                    {
                        framebuffer.Dispose();
                    }
                    catch
                    {

                    }
                    percentDone = ((float)frameIndex / (float)((sampleLenght * channels) - (jua + 10)) * 100);  //percent counter at bottom
                    if (percentDone > prevDone + 0.1)
                    {
                        frm.toolStripStatusLabel2.Text = Math.Round(percentDone, 1).ToString("0.0") + "%";
                        prevDone = percentDone;
                    }

                    if (Start.Enabled == true) break;
                    #endregion
                    //SaveAvi.Stop();
                    //MessageBox.Show(Setup.ElapsedTicks.ToString() + " " + ReadFromDisk.ElapsedTicks.ToString() + " " + Synchronization.ElapsedTicks.ToString() + " " + Draw.ElapsedTicks.ToString() + " " + SaveAvi.ElapsedTicks.ToString());
                    //Setup.Reset();
                    //Draw.Reset();
                    //ReadFromDisk.Reset();
                    //Synchronization.Reset();
                    //SaveAvi.Reset();
                }

            aviManager.Close();     //file is done being written, close the stream

            


            #region process with MKVMerge

            //Process p = new Process();
            //// Redirect the output stream of the child process.
            //p.StartInfo.UseShellExecute = false;
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.FileName = "mkvmerge.exe";
            //p.StartInfo.Arguments = "-o \"" + sfd.FileName + "\" " + tempavi +" \"" + masterFile + "\"";
            //p.Start();
            //String strT = "";
            //strT = p.StandardOutput.ReadToEnd();
            //p.WaitForExit();
            ////MessageBox.Show(strT);

            //if (File.Exists(tempavi)) File.Delete(tempavi);





            Start.Enabled = true;   //you can click start again. this was causing some fun problems ;)
            frm.Hide();
            frm.Close();
            frm.Dispose();
            #endregion

            #region close streams
            for (int i = 0; i < voiceData.Length; i++)
            {
                voiceData[i].Close();
            }
            #endregion
        } //------end of start button click


        //populates cmbClr boxes with a list of every color - sorts by hue and sat/bright 
        private void Form1_Load(object sender, EventArgs e)
        {
            chkBlock.SendToBack();
            label8.SendToBack();
            cmbThick.SelectedIndex = 1;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            cmbScale.SelectedIndex = 4;
            cmbRes.SelectedIndex = 1;
            cmbVoices.SelectedIndex = 0;
            comboColumns.SelectedIndex = 0;
            lay.ShowInTaskbar = false;
            lay.Show(this);
            lay.Location = new Point(this.Location.X + this.Width-10, this.Location.Y);
            //ArrayList ColorList = new ArrayList();
            Type colorType = typeof(System.Drawing.Color);
            PropertyInfo[] propInfoList = colorType.GetProperties(BindingFlags.Static |
                                          BindingFlags.DeclaredOnly | BindingFlags.Public);
            List<Color> list = new List<Color>();
            foreach(PropertyInfo c in propInfoList)
            {
                list.Add(Color.FromName(c.Name));
            }
            List<Color> SortedList = list.OrderBy(o => (o.GetHue() + (o.GetSaturation() * o.GetBrightness() ))).ToList();

        }
        

        private void Stop_Click(object sender, EventArgs e)
        {
            Start.Enabled = true;
        }

        private void comboColumns_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            if (chkSm.Checked)
            {
                foreach (LayoutControl_s li in lay.Controls)
                {
                    lay.ClientSize = new System.Drawing.Size((comboColumns.SelectedIndex + 1) * 55, (int)Math.Ceiling((((float)cmbVoices.SelectedIndex + 1) / ((float)comboColumns.SelectedIndex + 1))) * 50);
                    li.Location = new Point((li.TabIndex % (comboColumns.SelectedIndex + 1)) * 55, (int)Math.Ceiling((((float)li.TabIndex + 1) / ((float)comboColumns.SelectedIndex + 1)) - 1) * 50);
                }
            }else{
                foreach (LayoutControl li in lay.Controls)
                {
                    lay.ClientSize = new System.Drawing.Size((comboColumns.SelectedIndex + 1) * 125, (int)Math.Ceiling((((float)cmbVoices.SelectedIndex + 1) / ((float)comboColumns.SelectedIndex + 1))) * 65);
                    li.Location = new Point((li.TabIndex % (comboColumns.SelectedIndex + 1)) * 125, (int)Math.Ceiling((((float)li.TabIndex + 1) / ((float)comboColumns.SelectedIndex + 1)) - 1) * 65);
                }
            }
        }
        private void cmbVoices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkSm.Checked)
            {
                lay.ClientSize = new System.Drawing.Size((comboColumns.SelectedIndex + 1) * 55, (int)Math.Ceiling((((float)cmbVoices.SelectedIndex + 1) / ((float)comboColumns.SelectedIndex + 1))) * 50);

            }
            else
            {
                lay.ClientSize = new System.Drawing.Size((comboColumns.SelectedIndex + 1) * 125, (int)Math.Ceiling((((float)cmbVoices.SelectedIndex + 1) / ((float)comboColumns.SelectedIndex + 1))) * 65);

            }
            if (cmbVoices.SelectedIndex > prevVoice)
            {
                int dmb = prevVoice+1;
                while (dmb <= cmbVoices.SelectedIndex)
                {
                    LayoutControl li;
                    LayoutControl_s lis;
                    if (chkSm.Checked)
                    {
                        lis = new LayoutControl_s();
                        lay.Controls.Add(lis);
                        lis.label1.Text = (lis.TabIndex + 1).ToString().PadLeft(2, '0');
                        lis.Location = new Point((lis.TabIndex % (comboColumns.SelectedIndex + 1)) * 55, (int)Math.Ceiling((((float)dmb + 1) / ((float)comboColumns.SelectedIndex + 1)) - 1) * 50);
                    }
                    else
                    {
                        li = new LayoutControl();
                        lay.Controls.Add(li);
                        li.label1.Text = (li.TabIndex + 1).ToString().PadLeft(2, '0');
                        li.Location = new Point((li.TabIndex % (comboColumns.SelectedIndex + 1)) * 125, (int)Math.Ceiling((((float)dmb + 1) / ((float)comboColumns.SelectedIndex + 1)) - 1) * 65);
                    }

                    
                    dmb += 1;
                }
                            }
            else if(cmbVoices.SelectedIndex < prevVoice)
            {
                int dmb = prevVoice;
                while (dmb >= cmbVoices.SelectedIndex)
                {
                    if (chkSm.Checked)
                    {
                        foreach (LayoutControl_s lis in lay.Controls)
                        {
                            if (lis.TabIndex == dmb + 1)
                            {
                                lay.Controls.Remove(lis);
                            }
                        }
                    }
                    else
                    {
                        foreach (LayoutControl lis in lay.Controls)
                        {
                            if (lis.TabIndex == dmb + 1)
                            {
                                lay.Controls.Remove(lis);
                            }
                        }
                    }

                    dmb -= 1;
                }

                
                
            }
            prevVoice = cmbVoices.SelectedIndex;
        }
       

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("SidWiz 1.0 by Rolf R Bakke \r\nSidWiz 2 by RushJet1\r\nAVIFile Wrapper by Corinna John\r\nWAVFile class by CalicoSkies\r\n\r\nRelease 13.1");
        }


        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBox2.Checked)
            {
                outputWindow = true;
                flag = true;
            }
            else
            {
                outputWindow = false;
            }
            
        }

        
        static void LCLP(byte[] Input, double Frequency, double Q, int SampleRate, int NumSamples)
        {
            double O = 2.0 * Math.PI * Frequency / SampleRate;
            double C = Q / O;
            double L = 1 / Q / O;
            double V = 0, I = 0, T;
            for (int s = 0; s < NumSamples; s++)
            {
                T = (I - V) / C;
                I += (Input[s] * O - V) / L;
                V += T;
                Input[s] = (byte)(V / O);
            }
            
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Audio Files|*.wav;*.mp3;*.ogg;*.flac";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) == ".wav" || Path.GetExtension(ofd.FileName) == ".mp3" || Path.GetExtension(ofd.FileName) == ".ogg" || Path.GetExtension(ofd.FileName) == ".flac")
            {
                button2.Text = System.IO.Path.GetFileName(ofd.FileName);
                masterFile = ofd.FileName;
            }
            else
            {
                MessageBox.Show("Unsupported format or no file was selected.");
                return;
            }
        }

        private void location_Changed(object sender, EventArgs e){
            lay.Location = new Point(this.Location.X + this.Width - 10, this.Location.Y);
        }

        private void focus_Changed(object sender, EventArgs e)
        {
            try
            {
                lay.BringToFront();
            }
            catch { }
        }

        private void button4_Click(object sender, EventArgs e) //save template
        {
            saveData data = new saveData();
            data.voices = cmbVoices.SelectedIndex;
            data.columns = comboColumns.SelectedIndex;
            data.scale = cmbScale.SelectedIndex;
            data.thickness = cmbThick.SelectedIndex;
            data.resolution = cmbRes.SelectedIndex;
            data.block = chkBlock.Checked;
            data.showOutput = checkBox2.Checked;
            data.grid = chkGrid.Checked;

            data.master = button2.Text;
            data.voiceFiles = new String[cmbVoices.SelectedIndex + 1];
            data.colors = new Color[cmbVoices.SelectedIndex + 1];
            data.altSync = new bool[cmbVoices.SelectedIndex + 1];

            int i = 0;
            if (chkSm.Checked)
            {
                foreach (LayoutControl_s li in lay.Controls)
                {
                    data.altSync[i] = li.chkAlt.Checked;
                    data.voiceFiles[i] = li.label2.Text;
                    data.colors[i] = li.button1.BackColor;
                    if (i == 0)
                    {
                        data.folder = li.filePath;
                    }
                    i += 1;
                }
            }
            else
            {
                foreach (LayoutControl li in lay.Controls)
                {
                    data.altSync[i] = li.chkAlt.Checked;
                    data.voiceFiles[i] = li.label2.Text;
                    data.colors[i] = li.button1.BackColor;
                    if (i == 0)
                    {
                        data.folder = li.filePath;
                    }
                    i += 1;
                }
            }

            data.folder = Directory.GetParent(data.folder).FullName;

            BinaryFormatter formatter = new BinaryFormatter();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "SidWiz2 Template (.sdt)|*.sdt";
            sfd.ShowDialog();
            if (sfd.FileName == "")
            {
                return;
            }
            using (FileStream stream = File.OpenWrite(sfd.FileName))
            {
                formatter.Serialize(stream, data);
            }
        }




        [Serializable]
        class saveData      //for saving templates
        {
            //these are all indices of combo boxes
            public int voices;
            public int columns;
            public int scale;
            public int thickness;
            public int resolution;
            public bool block;
            public bool[] altSync;

            public bool grid;
            public bool showOutput;

            //files
            public String master;
            public String[] voiceFiles;
            public Color[] colors;
            public String folder;
                       
        }

        private void button3_Click(object sender, EventArgs e)      //load template
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "SidWiz2 Template (.sdt)|*.sdt";
            ofd.ShowDialog();
            if (Path.GetExtension(ofd.FileName) != ".sdt")
            {
                MessageBox.Show("Unsupported format or no file was selected.");
                return;
            }

            FileStream inStr = new FileStream(ofd.FileName, FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter();
            saveData data = bf.Deserialize(inStr) as saveData;

            try { chkBlock.Checked = data.block; }
            catch { }
            cmbVoices.SelectedIndex = data.voices;
            comboColumns.SelectedIndex = data.columns;
            cmbScale.SelectedIndex = data.scale;
            cmbThick.SelectedIndex = data.thickness;
            cmbRes.SelectedIndex = data.resolution;

            checkBox2.Checked = data.showOutput;
            chkGrid.Checked = data.grid;

            button2.Text = data.master;

            //data.voiceFiles = new String[voices];
            //data.colors = new Color[voices];

            int i = 0;
            if (chkSm.Checked)
            {
                foreach (LayoutControl_s li in lay.Controls)
                {
                    li.label2.Text = data.voiceFiles[i];
                    li.button1.BackColor = data.colors[i];
                    try { li.chkAlt.Checked = data.altSync[i]; }
                    catch { }
                    i += 1;
                }
            }
            else
            {
                foreach (LayoutControl li in lay.Controls)
                {
                    li.label2.Text = data.voiceFiles[i];
                    li.button1.BackColor = data.colors[i];
                    try { li.chkAlt.Checked = data.altSync[i]; }
                    catch { }
                    i += 1;
                }
            }

            //apply template to folder
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = data.folder;

            DialogResult result = fbd.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                if (chkSm.Checked)
                {
                    foreach (LayoutControl_s li in lay.Controls)
                    {
                        li.filePath = fbd.SelectedPath + "\\" + li.label2.Text;
                    }
                }
                else
                {
                    foreach (LayoutControl li in lay.Controls)
                    {
                        li.filePath = fbd.SelectedPath + "\\" + li.label2.Text;
                    }
                }

                masterFile = fbd.SelectedPath + "\\" + data.master;
            }
            inStr.Close();
        }

        private void chkSm_CheckedChanged(object sender, EventArgs e)
        {
            List<Control> s = new List<Control>();
            if(chkSm.Checked){
                lay.ClientSize = new System.Drawing.Size((comboColumns.SelectedIndex + 1) * 55, (int)Math.Ceiling((((float)cmbVoices.SelectedIndex + 1) / ((float)comboColumns.SelectedIndex + 1))) * 50);
                for (int x = 0; x < lay.Controls.Count; x++)
                {
                    LayoutControl li = (LayoutControl)lay.Controls[x];
                    LayoutControl_s lis = new LayoutControl_s();
                    lis.label1.Text = li.label1.Text;
                    lis.label2.Text = li.label2.Text;
                    lis.chkAlt.Checked = li.chkAlt.Checked;
                    lis.button1.BackColor = li.button1.BackColor;
                    lis.filePath = li.filePath;
                    lis.Location = new Point((li.TabIndex % (comboColumns.SelectedIndex + 1)) * 55, (int)Math.Ceiling((((float)x + 1) / ((float)comboColumns.SelectedIndex + 1)) - 1) * 50);
                    s.Add(lis);
                }
            }
            else
            {
                lay.ClientSize = new System.Drawing.Size((comboColumns.SelectedIndex + 1) * 125, (int)Math.Ceiling((((float)cmbVoices.SelectedIndex + 1) / ((float)comboColumns.SelectedIndex + 1))) * 65);
                for (int x = 0; x < lay.Controls.Count; x++)
                {
                    LayoutControl_s li = (LayoutControl_s)lay.Controls[x];
                    LayoutControl lis = new LayoutControl();
                    lis.label1.Text = li.label1.Text;
                    lis.label2.Text = li.label2.Text;
                    lis.chkAlt.Checked = li.chkAlt.Checked;
                    lis.button1.BackColor = li.button1.BackColor;
                    lis.filePath = li.filePath;
                    lis.Location = new Point((li.TabIndex % (comboColumns.SelectedIndex + 1)) * 125, (int)Math.Ceiling((((float)x + 1) / ((float)comboColumns.SelectedIndex + 1)) - 1) * 65);
                    s.Add(lis);
                }
            }
            while (lay.Controls.Count > 0)
            {
                lay.Controls.Remove(lay.Controls[0]);
            }
            foreach (Control li in s)
            {
                lay.Controls.Add(li);
            }

        }



    }
}
