using Mythic.Package;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UO_EC_Super_Viewer
{
    public partial class HuePicker : Form
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// List of the hues
        /// </summary>
        private Dictionary<int, string> HueNames = new Dictionary<int, string>();

        /// <summary>
        /// List of all the available hues
        /// </summary>
        private List<Hue> m_Hues = new List<Hue>();

        /// <summary>
        /// Path of EC Client
        /// </summary>
        public string GamePath;

        /// <summary>
        /// backup of the list selected item
        /// </summary>
        private int m_SelectedItem;

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Loaded hues list
        /// </summary>
        public List<Hue> Hues
        {
            get { return m_Hues; }
        }

        /// <summary>
        /// Get the last selected hue
        /// </summary>
        public Hue SelectedHue
        {
            get { return lstHues.SelectedItems.Count > 0 ? m_Hues[int.Parse( lstHues.SelectedItems[0].Text )] : m_Hues[0] ; }
        }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Initialize the dialog
        /// </summary>
        /// <param name="gamePath"></param>
        public HuePicker( string gamePath )
        {
            InitializeComponent();
            GamePath = gamePath;
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL EVENTS
        // --------------------------------------------------------------

        /// <summary>
        /// Draw the hue in the proper column
        /// </summary>
        private void DrawHue( object sender, DrawListViewSubItemEventArgs e )
        {
            // all column are handled normally except the hue one
            if (e.Header != colHue )
            {
                e.DrawDefault = true;
                return;
            }

            // get the hue ID
            int hueID = int.Parse( e.Item.Text );

            // make sure the id exist in the hues table
            if ( hueID >= m_Hues.Count || m_Hues[hueID] == null )
                return;

            // if we have the color 0, we just leave the slot empty (default color)
            if ( hueID == 0 )
            {
                e.DrawDefault = true;
                return;
            }

            // fix the column width
            colHue.Width = m_Hues[hueID].HueDiagram.Width;

            // create the new image to use as background for the cell
            Bitmap hueImage = new Bitmap( colHue.Width, e.Bounds.Height );

            // scan the colors in the colors diagram of the hue
            for ( int i = 0; i < m_Hues[hueID].HueDiagram.Width; i++ )
            {
                // we color all the pixels in this line
                for ( int h = 0; h < hueImage.Height; h++ )
                {
                    // color the pixel
                    hueImage.SetPixel( i, h, m_Hues[hueID].HueDiagram.GetPixel( i, 0 ) );
                }
            }

            // draw the image
            e.Graphics.DrawImage( hueImage, e.Bounds );
        }

        /// <summary>
        /// Draw column header in the list
        /// </summary>
        private void DrawColumnHeader( object sender, DrawListViewColumnHeaderEventArgs e )
        {
            e.DrawDefault = true;
        }

        /// <summary>
        /// Search text changed
        /// </summary>
        private void txtSearch_TextChanged( object sender, EventArgs e )
        {
            // search the first item containing the text
            ListViewItem foundItem = lstHues.FindItemWithText( ( (TextBox)sender ).Text, true, 0, true );

            // did we find something?
            if ( foundItem == null )
                return;

            // select the row
            foundItem.Selected = true;

            // scroll to the item
            lstHues.EnsureVisible( foundItem.Index );
        }

        /// <summary>
        /// Clear the search textbox
        /// </summary>
        private void btnClearSearch_Click( object sender, EventArgs e )
        {
            // clear the textbox
            txtSearch.Clear();
        }

        /// <summary>
        /// prevent the "null" item when clicking in a blank spot
        /// </summary>
        private void lstHues_MouseUp( object sender, MouseEventArgs e )
        {
            // store the selected item (if there is a new selection)
            if ( lstHues.SelectedItems.Count > 0 )
                m_SelectedItem = lstHues.SelectedItems[0].Index;

            else // restore the selected item if there is no selection
                lstHues.Items[m_SelectedItem].Selected = true;
        }

        /// <summary>
        /// Execute the OK function on double click on the list
        /// </summary>
        private void lstHues_MouseDoubleClick( object sender, MouseEventArgs e )
        {
            // trigger the OK button
            DialogResult = DialogResult.OK;

            // close the dialog
            Close();
        }

        /// <summary>
        /// Make sure the dialog is visible
        /// </summary>
        private void HuePicker_Shown( object sender, EventArgs e )
        {
            Activate();
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Load all the hues
        /// </summary>
        public void LoadHues()
        {
            // fill the hue table
            FillHuesTable();

            // fill the hues list
            FillList();
        }

        /// <summary>
        /// Reset the selected hue to default
        /// </summary>
        /// <param name="currSelection">Hue to select on start</param>
        public void ResetSelection( int currSelection = 0 )
        {
            // clear the searchbox
            txtSearch.Clear();

            // clear the current selection
            lstHues.SelectedItems.Clear();

            // do we have to select the default color?
            if ( currSelection == 0)
            {
                // select the default color
                lstHues.Items[0].Selected = true;

                // scroll to the first element
                lstHues.EnsureVisible( 0 );
            }
            else // select a specific color
            {
                // search the hue with the specifed ID
                ListViewItem foundItem = lstHues.FindItemWithText( currSelection.ToString(), false, 0, false );

                // select the row
                foundItem.Selected = true;

                // scroll to the item
                lstHues.EnsureVisible( foundItem.Index );
            }
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Fill the hues list
        /// </summary>
        private void FillList()
        {
            // scan all the hues
            foreach ( Hue h in m_Hues )
            {
                // ignore the hue with missing diagram
                if ( h.HueDiagram == null )
                    continue;

                // create a new list item
                ListViewItem l = new ListViewItem( h.ID.ToString() );

                // add the hue name
                l.SubItems.Add( h.Name );

                // draw the hue (we just trigger the draw event like this)
                l.SubItems.Add( "" );



                // add the hue to the list
                lstHues.Items.Add( l );
            }

            // resize the columns
            colID.AutoResize( ColumnHeaderAutoResizeStyle.ColumnContent );
            colName.AutoResize( ColumnHeaderAutoResizeStyle.ColumnContent );
        }

        /// <summary>
        /// Load all the hues
        /// </summary>
        private void FillHuesTable()
        {
            // load the hues.uop file
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "Hues.uop" ) );

            // load the hue names
            LoadHueNames( UOP.Blocks[UOP.Blocks.Count - 1].Files[UOP.Blocks[UOP.Blocks.Count - 1].Files.Count - 1], UOP.FileInfo.FullName );

            // hue index
            int fileID = 0;

            // scan all the blocks
            for ( int block = 0; block < UOP.Blocks.Count; block++ )
            {
                // current block
                MythicPackageBlock blk = UOP.Blocks[block];

                // scan all the files
                for ( int file = 0; file < blk.Files.Count; file++ )
                {
                    // the first 2 files of the first block are NOT hues, the last file of the last block is the csv with the hue names.
                    if ( ( block == 0 && file <= 1 ) || ( block == UOP.Blocks.Count - 1 && file == blk.Files.Count - 1 ) )
                        continue;

                    // current file
                    MythicPackageFile fil = blk.Files[file];

                    // load the file memory stream data
                    MemoryStream stream = new MemoryStream( fil.Unpack( UOP.FileInfo.FullName ));

                    // load the colors diagram for the hue
                    Bitmap cd = new Bitmap( stream );

                    // add the hue to the list (if we have the colors diagram)
                    if ( cd != null )
                        m_Hues.Add( new Hue( fileID, HueNames[fileID + 1], new Bitmap( stream ) ) );

                    // increase the hue ID
                    fileID++;
                }
            }
        }

        /// <summary>
        /// Load all the hue names
        /// </summary>
        /// <param name="huesCSV">file CSV containing the hues data</param>
        /// <param name="fullName">UOP file name</param>
        private void LoadHueNames( MythicPackageFile huesCSV, string fullName )
        {
            // load the file memory stream data (we read it like a text file)
            using ( StreamReader reader = new StreamReader( new MemoryStream( huesCSV.Unpack( fullName ) ), Encoding.ASCII ) )
            {
                // initialize the text lines
                string line;

                // keep reading until the end of the file
                while ( ( line = reader.ReadLine() ) != null )
                {
                    // line that starts with # are comments
                    if ( !line.StartsWith( "#" ) )
                    {
                        // get the ID, name from the line
                        string[] data = line.Split( ',' );

                        // get the hue ID
                        int hueID = int.Parse( data[0] );

                        // store the value into the dictionary
                        HueNames.Add( hueID, data.Length < 2 ? hueID == 1 ? "Default" : "" : hueID == 1 ? "Default" : data[1] );
                    }
                }
            }
        }

        #endregion


    }
}
