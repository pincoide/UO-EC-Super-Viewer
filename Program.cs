using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UO_EC_Super_Viewer
{
    static class Program
    {
        public static Form SplashScreen;
        public static Form MainForm;

        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );

            // create the splash screen
            SplashScreen = new Splash();

            // show the splash screen async
            Thread splashThread = new Thread( new ThreadStart( () => Application.Run( SplashScreen ) ) );
            splashThread.SetApartmentState( ApartmentState.STA );
            splashThread.Start();

            // create the main form
            MainForm = new UOECSuperViewer();

            // add the main form loading complete event
            MainForm.Load += MainForm_LoadCompleted;

            // open the main form
            Application.Run( MainForm );
        }

        /// <summary>
        /// Main form load complete
        /// </summary>
        private static void MainForm_LoadCompleted( object sender, EventArgs e )
        {
            // if the splash is still around, we close it
            if ( SplashScreen != null && !SplashScreen.Disposing && !SplashScreen.IsDisposed )
                SplashScreen.Invoke( new Action( () => SplashScreen.Close() ) );

            // show the form
            MainForm.Visible = true;

            // activate the main form
            MainForm.TopMost = true;
            MainForm.Activate();
            MainForm.TopMost = false;
        }

        /// <summary>
        /// Hide the splash screen
        /// </summary>
        public static void HideSplash()
        {
            SplashScreen.Visible = false;
        }

        /// <summary>
        /// Show the splash screen
        /// </summary>
        public static void ShowSplash()
        {
            SplashScreen.Visible = true;
        }
    }
}
