using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UO_EC_Super_Viewer
{
    public partial class ItemTooltip : Form
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------
        public ItemTooltip()
        {
            InitializeComponent();
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL EVENTS
        // --------------------------------------------------------------

        /// <summary>
        /// Form hidden
        /// </summary>
        private void ItemTooltip_VisibleChanged( object sender, EventArgs e )
        {
            // form hidden?
            if ( !Visible )
            {
                // remove the images
                imgCC.Image = null;
                imgKR.Image = null;

                // shrink the images
                imgCC.Size = new Size( 0, 0 );
                imgKR.Size = new Size( 0, 0 );

                // resize the form
                Size = new Size( 0, 0 );
            }
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Set the images on the picturebox
        /// </summary>
        /// <param name="item">Item to show</param>
        public void SetImages( string GamePath, ItemData item )
        {
            // get the CC image
            Bitmap imageCC = item.GetItemImage( GamePath, false );

            // get the old KR image
            Bitmap imageKR = item.GetItemImage( GamePath, true );

            // draw the images
            imgCC.Image = DirectBitmap.CropImage( imageCC, 10 );
            imgKR.Image = DirectBitmap.CropImage( imageKR, 10 );

            // re-position the KR image properly
            imgKR.Location = new Point( imgCC.Width, imgKR.Location.Y );

            // get the item flags
            string flags = item.GetAllFlags();

            // set the item data text
            lblItemData.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( item.Name.Replace( "_", " " ) ) + "\nID: " + item.ID.ToString() + ( item.Properties.ContainsKey( ItemData.TileArtProperties.Animation ) ? "\nAnimation: " + item.Properties[ItemData.TileArtProperties.Animation] + "\n(DOUBLE CLICK TO VIEW)\n" + ( item.Properties.ContainsKey( ItemData.TileArtProperties.Layer ) ? "\nLayer: " + (ItemData.Layers)item.Properties[ItemData.TileArtProperties.Layer] : "" ) : "" ) + ( flags != string.Empty ? "\nFlags: " + item.GetAllFlags() : "" );

            // resize the form
            Size = new Size( Math.Max( ( imgCC.Image != null ? imgCC.Image.Width : 0 ) + ( imgKR.Image != null ? imgKR.Image.Width : 0 ) + 1, pnlItemData.Width + 1 ) + 1, Math.Max( imgCC.Image != null ? imgCC.Image.Height : 0, imgKR.Image != null ? imgKR.Image.Height : 0 ) + pnlItemData.Height + 4 );

            // move the item data at the bottom of the tooltip
            pnlItemData.Location = new Point( 0, Size.Height - 2 - pnlItemData.Height + 1);

            // show the tooltip
            Show();
        }

        /// <summary>
        /// Attach the mouse-leave event to the tooltip
        /// </summary>
        /// <param name="currentControl">control to attach it to</param>
        public void AttachHandlers( Control currentControl )
        {
            // attach the mouse-over event
            currentControl.MouseLeave += GenericMouseLeave;
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// hide the tooltip
        /// </summary>
        private void GenericMouseLeave( object sender, EventArgs e )
        {
            // do we need to hide the tooltip?
            if ( Visible )
                Hide();
        }

        #endregion


    }
}
