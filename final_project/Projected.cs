//----------------------------------------------------------------------------
//  Copyright (C) 2004-2013 by EMGU. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace gesture_viewer.cs
{
   public partial class Projected : Form
   {
	   public const float JitterThreshold = 0.05f;
	   public const float ScrollPercentagePerFrame = 0.5f;
	   public const int ScaleMult = 1;
	   public const float MaxScrollPixelAmount = -850;
	   public const int TicksToObjectTimeout = 3000; //ms

	   public float scrollbarPercentage;

	   public static uint TimeSinceLastSeenPuck = 0;

	   public static uint TimeSinceLastSeenOrange = 0;
	   public static uint TimeSinceLastSeenOnion = 0;
	   public static uint TimeElapsed = 2000; //ms

	   private static Projected instance;
	   public static Projected Instance
	   {
		   get
		   {
			   if (instance == null)
				   instance = new Projected();

			   return instance;
		   }
	   }

	   public bool Projecting = true;



	   private System.Windows.Forms.Timer timer1; 

	  public Projected()
      {
         InitializeComponent();
		 Thread.Sleep(2000);
		 InitTimer();

		 this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
		 this.WindowState = FormWindowState.Maximized;

		 ((PictureBox)this.panel1.Controls[0]).SizeMode = PictureBoxSizeMode.Zoom;
      }

	  public void InitTimer()
	  {
		  timer1 = new System.Windows.Forms.Timer();
		  timer1.Tick += new EventHandler(timer1_Tick);
		  timer1.Interval = 100; // in miliseconds
		  timer1.Start();
	  }

	private void timer1_Tick(object sender, EventArgs e)
	{
		TimeElapsed += 100; //ms
        
		if ((GestureRecognition.RecognizedObjects.Contains("orange") || TimeElapsed - TimeSinceLastSeenOrange < TicksToObjectTimeout) && (GestureRecognition.RecognizedObjects.Contains("onion") || TimeElapsed - TimeSinceLastSeenOnion < TicksToObjectTimeout))
		{ //both
			if (pictureBox.ImageLocation !=  "Pictures\\both_info.png")
                pictureBox.ImageLocation = "Pictures\\both_info.png";
			if (GestureRecognition.RecognizedObjects.Contains("orange"))
			{
				//TimeSinceLastSeenOrange = TimeElapsed;
			}
			if (GestureRecognition.RecognizedObjects.Contains("onion"))
			{
				//TimeSinceLastSeenOnion = TimeElapsed;
			}
		}
		else if (TimeElapsed - TimeSinceLastSeenOrange < TicksToObjectTimeout || GestureRecognition.RecognizedObjects.Contains("orange"))
		{
            if (pictureBox.ImageLocation != "Pictures\\orange_info.png")
                pictureBox.ImageLocation = "Pictures\\orange_info.png";
			if (GestureRecognition.RecognizedObjects.Contains("orange"))
			{
				//TimeSinceLastSeenOrange = TimeElapsed;
			}
		}
		else if (TimeElapsed - TimeSinceLastSeenOnion < TicksToObjectTimeout || GestureRecognition.RecognizedObjects.Contains("onion"))
		{
            if (pictureBox.ImageLocation != "Pictures\\lime_info.png")
                pictureBox.ImageLocation = "Pictures\\lime_info.png";
			if (GestureRecognition.RecognizedObjects.Contains("onion"))
			{
				//TimeSinceLastSeenOnion = TimeElapsed;
			}
		}
		else //if (GestureRecognition.RecognizedObjects.Count == 0)
		{ //none
            if (pictureBox.ImageLocation != "Pictures\\none_info.png")
                pictureBox.ImageLocation = "Pictures\\none_info.png";

			//TimeSinceLastSeenPuck = TimeElapsed;
		}
        
		pictureBox.Refresh();

        if (GestureRecognition.PuckY != -1 || (TimeElapsed - TimeSinceLastSeenPuck < TicksToObjectTimeout)) //-1 means there is no puck being recognized
		{
			float desiredPercentage = GestureRecognition.PuckY / GestureRecognition.CameraHeight;

			float actualMappedPositionPercentage = PixelToPercentage(panel1.Controls[0].Top);

			float mappedDesiredPercentage = (desiredPercentage - .2f) / (.8f - .2f);
			float desiredPosition = PercentageToPixel(mappedDesiredPercentage);

			float difference = mappedDesiredPercentage - actualMappedPositionPercentage;
			float differenceAdd = difference * ScrollPercentagePerFrame;

			if (Program.ReversedScrolling)
			{
				differenceAdd *= -1; //reverse the scrolling
			}

            if ((GestureRecognition.RecognizedObjects.Contains("orange") || TimeElapsed - TimeSinceLastSeenOrange < TicksToObjectTimeout) || (GestureRecognition.RecognizedObjects.Contains("onion") || TimeElapsed - TimeSinceLastSeenOnion < TicksToObjectTimeout)) //if none, no content
			{
				panel1.Controls[0].Top = (int)PercentageToPixel(actualMappedPositionPercentage + differenceAdd);
			}
			else
			{
				if (TimeElapsed - TimeSinceLastSeenPuck < TicksToObjectTimeout)
					panel1.Controls[0].Top = 0;
			}

			label1.Text = (actualMappedPositionPercentage * 100).ToString();
            label2.Text = panel1.Controls[0].Top.ToString();//(mappedDesiredPercentage * 100).ToString();

			if (Projecting)
			{
				label3.Text = "ON";
			}
			else
			{
				label3.Text = "OFF";
			}


			//panel1.Controls[0].Top = -1 * (int)(percentangeOfHeight * ((PictureBox)panel1.Controls[0]).Height);
			//label1.Text = panel1.Controls[0].Top.ToString();
			/*
			((VScrollBar)imageListView1.Controls[1]).Value = (int)(percentangeOfHeight * (float)((VScrollBar)imageListView1.Controls[1]).Maximum);
			((VScrollBar)imageListView1.Controls[1]).Value = (int)(percentangeOfHeight * (float)((VScrollBar)imageListView1.Controls[1]).Maximum);
			imageListView1.PerformLayout();
			((VScrollBar)imageListView1.Controls[1]).PerformLayout();
			*/
		}

		panel1.Visible = Projecting;
	}

	private float PercentageToPixel(float percent)
	{
		return percent * MaxScrollPixelAmount * ScaleMult;
	}

	private float PixelToPercentage(float pixel)
	{
		return pixel / (MaxScrollPixelAmount * ScaleMult);
	}

      private void ProcessFrame(object sender, EventArgs arg)
      {

      }

      private void ReleaseData()
      {
      }
   }
}
