using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace gesture_viewer.cs
{
    class GestureRecognition
    {

		public int[] PuckRGBLower = { 110, 110, 110 };
		public int[] PuckRGBUpper = { 255, 255, 255 };

        //Orange (or mango?)
		public int[] Object1RGBLower = { 140, 50, 30 };
		public int[] Object1RGBUpper = { 200, 110, 90 };

        //Now known as eggplant, formally an onion
		public int[] Object2RGBLower = { 30, 40, 20 };
		public int[] Object2RGBUpper = { 90, 110, 80 };



        private MainForm form;
        private bool disconnected = false;
        private PXCMGesture.GeoNode[][] nodes = new PXCMGesture.GeoNode[2][] { new PXCMGesture.GeoNode[11], new PXCMGesture.GeoNode[11] };
        private PXCMGesture.Gesture[] gestures = new PXCMGesture.Gesture[2];


	  private static float puckY;
		public static float PuckY
		{
			get { return puckY; }
			set { puckY = value; }
		}

		private static List<string> recognizedObjects = new List<string>();
		public static List<string> RecognizedObjects
		{
			get { return recognizedObjects; }
			set { recognizedObjects = value; }
		}

		public static float CameraHeight
		{
			get
			{
				return 296;//Image.Size.Height;
			}
		}

        public GestureRecognition(MainForm form)
        {
            this.form = form;
        }

        private bool DisplayDeviceConnection(bool state)
        {
            if (state)
            {
                if (!disconnected) form.UpdateStatus("Device Disconnected");
                disconnected = true;
            }
            else
            {
                if (disconnected) form.UpdateStatus("Device Reconnected");
                disconnected = false;
            }
            return disconnected;
        }

        private void DisplayPicture(PXCMImage depth, PXCMGesture gesture)
        {
            PXCMImage image = depth;
            bool dispose = false;
            if (form.GetLabelmapState())
            {
                if (gesture.QueryBlobImage(PXCMGesture.Blob.Label.LABEL_SCENE,0,out image)<pxcmStatus.PXCM_STATUS_NO_ERROR) return;
                dispose = true;
            }

            PXCMImage.ImageData data;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_RGB32, out data) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                form.DisplayBitmap(data.ToBitmap(image.info.width,image.info.height));
                image.ReleaseAccess(ref data);
            }

            if (dispose) image.Dispose();
        }

        private void DisplayGeoNodes(PXCMGesture gesture)
        {
            if (form.GetGeoNodeState())
            {
                gesture.QueryNodeData(0, PXCMGesture.GeoNode.Label.LABEL_BODY_HAND_PRIMARY, nodes[0]);
                gesture.QueryNodeData(0, PXCMGesture.GeoNode.Label.LABEL_BODY_HAND_SECONDARY, nodes[1]);
                gesture.QueryNodeData(0, PXCMGesture.GeoNode.Label.LABEL_BODY_ELBOW_PRIMARY, out nodes[0][nodes.Length-1]);
                gesture.QueryNodeData(0, PXCMGesture.GeoNode.Label.LABEL_BODY_ELBOW_SECONDARY, out nodes[1][nodes.Length-1]);
                form.DisplayGeoNodes(nodes);
            }
            else
            {
                form.DisplayGeoNodes(null);
            }
        }

        private void DisplayGesture(PXCMGesture gesture) {
            gesture.QueryGestureData(0, PXCMGesture.GeoNode.Label.LABEL_BODY_HAND_PRIMARY, 0, out gestures[0]);
            gesture.QueryGestureData(0, PXCMGesture.GeoNode.Label.LABEL_BODY_HAND_SECONDARY, 0, out gestures[1]);
            form.DisplayGestures(gestures);
        }

        public void SimplePipeline()
        {
            bool sts = true;
            UtilMPipeline pp = null;
            disconnected = false;

            /* Set Source */
            if (form.GetRecordState()) {
                pp = new UtilMPipeline(form.GetRecordFile(), true);
                pp.QueryCapture().SetFilter(form.GetCheckedDevice());
            }
            else if (form.GetPlaybackState())
            {
                pp = new UtilMPipeline(form.GetPlaybackFile(), false);
            }
            else
            {
                pp = new UtilMPipeline();
                pp.QueryCapture().SetFilter(form.GetCheckedDevice());
            }

			/* Set Module */
			pp.EnableFaceLocation();
			pp.EnableFaceLandmark();
            pp.EnableGesture(form.GetCheckedModule());

            /* Initialization */
            form.UpdateStatus("Init Started");
            if (pp.Init())
            {
                form.UpdateStatus("Streaming");

                while (!form.stop)
                {
                    if (!pp.AcquireFrame(true)) break;
                    if (!DisplayDeviceConnection(pp.IsDisconnected()))
                    {
                        /* Display Results */
                        PXCMGesture gesture = pp.QueryGesture();
                        PXCMImage img = pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_COLOR);

						Bitmap bmp = ConvertImageToBitmap(img);

						#region EMGU Stuff
						Image<Bgr, Byte> frame = new Image<Bgr, Byte>(bmp); //http://stackoverflow.com/questions/503427/the-type-initializer-for-emgu-cv-cvinvoke-threw-an-exception

						Image<Gray, Byte> circleFrame = frame.Convert<Gray, Byte>().PyrDown().PyrUp();
						Image<Gray, Byte> smallGrayFrame = circleFrame.PyrDown();
						Image<Gray, Byte> smoothedGrayFrame = smallGrayFrame.PyrUp();
						Image<Gray, Byte> cannyFrame = smoothedGrayFrame.Canny(100, 60);

						//new Bgr(60, 90, 160), new Bgr(255, 255, 255) -- with light reflecting off the orange
						//new Bgr(40, 90, 190), new Bgr(120, 190, 255)
						
						//circleFrame = frame.InRange(new Bgr(100, 100, 100), new Bgr(255, 255, 255));               // somehow range for orange related colors
						
						//circleFrame = frame.InRange(new Bgr(100, 110, 220), new Bgr(130, 140, 255));               // somehow range for orange related colors
						
						//circleFrame = frame.InRange(new Bgr(100, 100, 100), new Bgr(255, 255, 255));               // somehow range for orange related colors
						
						//circleFrame = circleFrame.SmoothGaussian(9);

						double cannyThreshold = 180.0;
						double circleAccumulatorThreshold = 40;

						CircleF[] circles = circleFrame.HoughCircles(
							new Gray(cannyThreshold),
							new Gray(circleAccumulatorThreshold),
							2.0, //Resolution of the accumulator used to detect centers of the circles
							20.0, //min distance 
							18, //min radius
							30 //max radius
							)[0]; //Get the circles from the first channel\

						/*
						 * 
						 * 
						 * 
						 * 
						 * 
						 * 
						 * 
						 * Create a Timer
						 * Use the timer to space out the HoughCircles calls, so they aren't happening every frame
						 * 
						 * Check for the puck more frequently
						 * Check for the fruits less frequently, and then only remove the info from the List after a certain time the fruit has been missing (like 50ms)
						 * 
						 * Never do more than one HoughCircls call per frame.
						 * 
						 * 
						 * 
						 * 
						 * 
						 */

						Image<Bgr, Byte> circleImage = frame;
						/*
						foreach (CircleF circle in circles)
						{
							circleImage.Draw(circle, new Bgr(Color.Brown), 2);
							Console.WriteLine(circle.Radius);
						}
						*/
						if (circles.Length > 0)
						{
							foreach (CircleF circle in circles)
							{
								circleImage.Draw(circle, new Bgr(Color.Brown), 2);

								Point center = new Point((int)circle.Center.X, (int)circle.Center.Y);

								if (frame[center].Red > Object1RGBLower[0] && frame[center].Red <= Object1RGBUpper[0] &&
									frame[center].Green > Object1RGBLower[1] && frame[center].Green <= Object1RGBUpper[1] &&
									frame[center].Blue > Object1RGBLower[2] && frame[center].Blue <= Object1RGBUpper[2])
								{
									//orange center of circle
									Console.WriteLine("orange");

									if (RecognizedObjects.Contains("orange") == false)
										RecognizedObjects.Add("orange");

                                    Projected.TimeSinceLastSeenOrange = Projected.TimeElapsed;
								}
								else 
								{
									RecognizedObjects.Remove("orange");
								}
								if (frame[center].Red > Object2RGBLower[0] && frame[center].Red <= Object2RGBUpper[0] &&
									frame[center].Green > Object2RGBLower[1] && frame[center].Green <= Object2RGBUpper[1] &&
									frame[center].Blue > Object2RGBLower[2] && frame[center].Blue <= Object2RGBUpper[2])
								{
									//orange center of circle
									Console.WriteLine("onion");

									if (RecognizedObjects.Contains("onion") == false)
										RecognizedObjects.Add("onion");

                                    Projected.TimeSinceLastSeenOnion = Projected.TimeElapsed;
								}
								else
								{
									RecognizedObjects.Remove("onion");
								}
								if (frame[center].Red > PuckRGBLower[0] && frame[center].Red <= PuckRGBUpper[0] &&
									frame[center].Green > PuckRGBLower[1] && frame[center].Green <= PuckRGBUpper[1] &&
									frame[center].Blue > PuckRGBLower[2] && frame[center].Blue <= PuckRGBUpper[2])
								{
									//orange center of circle
									Console.WriteLine("puck");

									if (RecognizedObjects.Contains("puck") == false)
									{
										//RecognizedObjects.Add("puck"); //--don't add it to the object list, ruins the count.
									}


									if (Program.VerticallPuck)
									{
										PuckY = circle.Center.X;
									}
									else
									{
										PuckY = circle.Center.Y;
									}

                                    Projected.TimeSinceLastSeenPuck = Projected.TimeElapsed;
								}
								else
								{
									RecognizedObjects.Remove("puck");
								}
							}
						}
						else
						{
							PuckY = -1;
						}
						#endregion

						DisplayPicture(ConvertToPXCMImage(frame.Bitmap), gesture);
                        DisplayGeoNodes(gesture);
                        DisplayGesture(gesture);
                        form.UpdatePanel();
                    }
                    pp.ReleaseFrame();
                }
            }
            else
            {
                form.UpdateStatus("Init Failed");
                sts = false;
            }

            pp.Close();
            pp.Dispose();
            if (sts) form.UpdateStatus("Stopped");
        }

        public void AdvancedPipeline()
        {
            PXCMSession session;
            pxcmStatus sts = PXCMSession.CreateInstance(out session);
            if (sts<pxcmStatus.PXCM_STATUS_NO_ERROR) {
                form.UpdateStatus("Failed to create an SDK session");
                return;
            }

            /* Set Module */
            PXCMSession.ImplDesc desc=new PXCMSession.ImplDesc();
            desc.friendlyName.set(form.GetCheckedModule());

            PXCMGesture gesture;
            sts=session.CreateImpl<PXCMGesture>(ref desc,PXCMGesture.CUID,out gesture);
            if (sts<pxcmStatus.PXCM_STATUS_NO_ERROR) {
                form.UpdateStatus("Failed to create the gesture module");
                session.Dispose();
                return;
            }

            UtilMCapture capture=null;
            if (form.GetRecordState())
            {
                capture = new UtilMCaptureFile(session,form.GetRecordFile(),true);
                capture.SetFilter(form.GetCheckedDevice());
            }
            else if (form.GetPlaybackState())
            {
                capture = new UtilMCaptureFile(session, form.GetPlaybackFile(), false);
            }
            else
            {
                capture = new UtilMCapture(session);
                capture.SetFilter(form.GetCheckedDevice());
            }

            form.UpdateStatus("Pair moudle with I/O");
            for (uint i=0;;i++) {
                PXCMGesture.ProfileInfo pinfo;
                sts=gesture.QueryProfile(i,out pinfo);
                if (sts<pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                sts=capture.LocateStreams(ref pinfo.inputs);
                if (sts<pxcmStatus.PXCM_STATUS_NO_ERROR) continue;
                sts=gesture.SetProfile(ref pinfo);
                if (sts>=pxcmStatus.PXCM_STATUS_NO_ERROR) break;
            }
            if (sts<pxcmStatus.PXCM_STATUS_NO_ERROR) {
                form.UpdateStatus("Failed to pair the gesture module with I/O");
                capture.Dispose();
                gesture.Dispose();
                session.Dispose();
                return;
            }

            form.UpdateStatus("Streaming");
            PXCMImage[] images = new PXCMImage[PXCMCapture.VideoStream.STREAM_LIMIT];
            PXCMScheduler.SyncPoint[] sps = new PXCMScheduler.SyncPoint[2];
            while (!form.stop)
            {
                PXCMImage.Dispose(images); 
                PXCMScheduler.SyncPoint.Dispose(sps);
                sts = capture.ReadStreamAsync(images, out sps[0]);
                if (DisplayDeviceConnection(sts == pxcmStatus.PXCM_STATUS_DEVICE_LOST)) continue;
                if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                sts = gesture.ProcessImageAsync(images, out sps[1]);
                if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                PXCMScheduler.SyncPoint.SynchronizeEx(sps);
                sts=sps[0].Synchronize();
                if (DisplayDeviceConnection(sts==pxcmStatus.PXCM_STATUS_DEVICE_LOST)) continue;
                if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                /* Display Results */
                PXCMImage depth=capture.QueryImage(images,PXCMImage.ImageType.IMAGE_TYPE_DEPTH);
                DisplayPicture(depth,gesture);
                DisplayGeoNodes(gesture);
                DisplayGesture(gesture);
                form.UpdatePanel();
            }
            PXCMImage.Dispose(images);
            PXCMScheduler.SyncPoint.Dispose(sps);

            capture.Dispose();
            gesture.Dispose();
            session.Dispose();
            form.UpdateStatus("Stopped");
        }


		public PXCMImage ConvertToPXCMImage(Bitmap bitmap)
		{
			/* Get a system memory allocator */
			PXCMAccelerator accel;
			form.session.CreateAccelerator(out accel);

			PXCMImage.ImageInfo iinfo = new PXCMImage.ImageInfo();
			iinfo.width = (uint)bitmap.Width;
			iinfo.height = (uint)bitmap.Height;
			iinfo.format = PXCMImage.ColorFormat.COLOR_FORMAT_RGB32;


			/* Create the image */
			PXCMImage image;
			accel.CreateImage(ref iinfo, out image);
			PXCMImage.ImageData idata;
			image.AcquireAccess(PXCMImage.Access.ACCESS_WRITE, out idata);
			BitmapData bdata = new BitmapData();
			bdata.Scan0 = idata.buffer.planes[0];
			bdata.Stride = idata.buffer.pitches[0];
			bdata.PixelFormat = PixelFormat.Format32bppRgb;
			bdata.Width = bitmap.Width;
			bdata.Height = bitmap.Height;
			BitmapData bdata2 = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly | ImageLockMode.UserInputBuffer, PixelFormat.Format32bppRgb, bdata);
			image.ReleaseAccess(ref idata);
			bitmap.UnlockBits(bdata);
			return image;
		}

		public Bitmap ConvertImageToBitmap(PXCMImage image)
		{
			PXCMImage.ImageData data;
			image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_RGB32, out data);
			Bitmap bitmap = new Bitmap((int)image.imageInfo.width, (int)image.imageInfo.height, data.buffer.pitches[0], PixelFormat.Format32bppRgb, data.buffer.planes[0]);
			image.ReleaseAccess(ref data);
			return bitmap;
		}

    }
}
