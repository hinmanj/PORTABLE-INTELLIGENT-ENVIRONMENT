/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2013 Intel Corporation. All Rights Reserved.

*******************************************************************************/
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace gesture_viewer.cs
{
    static class Program
    {

		public static bool VerticallPuck = false;
		public static bool ReversedScrolling = false;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            PXCMSession session = null;
            pxcmStatus sts = PXCMSession.CreateInstance(out session);
            if (sts >= pxcmStatus.PXCM_STATUS_NO_ERROR)
			{
				var thread = new Thread(ThreadStart);
				// allow UI with ApartmentState.STA though [STAThread] above should give that to you
				thread.TrySetApartmentState(ApartmentState.STA);
				thread.Start();

                Application.Run(new MainForm(session));
                session.Dispose();
            }
        }


		private static void ThreadStart()
		{
			Application.Run(Projected.Instance); //Projected
		}
    }
}
