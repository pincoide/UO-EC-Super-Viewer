using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
using Mythic.Package;
using UO_EC_Super_Viewer.Properties;
using System.Configuration;
using System.Drawing.Text;
using System.Runtime.Remoting.Messaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Drawing.Imaging;
using System.Diagnostics.Contracts;
using NAudio;
using NAudio.Wave;
using System.Media;
using NAudio.Midi;
using System.Collections;
using NAudio.Gui;

namespace UO_EC_Super_Viewer
{
    public partial class UOECSuperViewer : Form
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Actions for the character
        /// </summary>
        private Dictionary<int, string> CharActions = new Dictionary<int, string>();

        /// <summary>
        /// Actions for the creatures
        /// </summary>
        private Dictionary<int, string> CreatureActions = new Dictionary<int, string>();

        /// <summary>
        /// Default draw order of the layers on the character
        /// </summary>
        private List<int> charEquipSort = new List<int>() { 21, 20, 19, 5, 4, 15, 9, 10, 6, 8, 7, 12, 11, 3, 2, 16, 13, 14, 17, 1, 18 };

        /// <summary>
        /// Custom draw order of the layers on the character
        /// </summary>
        private List<int> charEquipCustomSort;

        /// <summary>
        /// Path of EC Client
        /// </summary>
        public string GamePath;

        /// <summary>
        /// Path of the animations XML file
        /// </summary>
        private string AnimXMLFile = Path.Combine( Application.StartupPath, "AnimationsCollection.xml" );

        /// <summary>
        /// Path of the multi XML file
        /// </summary>
        private string MultiXMLFile = Path.Combine( Application.StartupPath, "MultiCollection.xml" );

        /// <summary>
        /// Path of the audio XML file
        /// </summary>
        private string AudioXMLFile = Path.Combine( Application.StartupPath, "AudioCollection.xml" );

        /// <summary>
        /// Main list of the loaded animations
        /// </summary>
        private List<Mobile> Mobiles = new List<Mobile>();

        /// <summary>
        /// storage for the current mobile
        /// </summary>
        private Mobile m_currentMobile;

        /// <summary>
        /// active mobile on display (including equipment and all)
        /// </summary>
        private Mobile CurrentMobile
        {
            get { return m_currentMobile; }
            set
            {
                // toggle the animation controls based on if there is a current mobile active
                ToggleAnimationControls( value != null && !chkPaperdoll.Checked );

                // store the current mobile value
                m_currentMobile = value;
            }
        }

        /// <summary>
        /// last frame played in the preview
        /// </summary>
        private int LastFramePlayed = 0;

        /// <summary>
        /// Currently selected action to preview
        /// </summary>
        private int m_SelectedAction = 0;

        /// <summary>
        /// Flag indicating that something is loading
        /// </summary>
        private bool m_Loading = false;

        /// <summary>
        /// Default size for the character image
        /// </summary>
        private Size char_Size = new Size( 400, 400 );

        /// <summary>
        /// Default hue picker dialog
        /// </summary>
        HuePicker hp;

        /// <summary>
        /// Default export to vd dialog
        /// </summary>
        ExportVD xp;

        /// <summary>
        /// flag to prevent multiple image loading during the combo reset
        /// </summary>
        private bool comboReset = false;

        /// <summary>
        /// string dictionary (containing the objects names)
        /// </summary>
        private List<string> stringsDictionary = new List<string>();

        /// <summary>
        /// List of all the items available
        /// </summary>
        private List<ItemData> itemsCache = new List<ItemData>();

        /// <summary>
        /// List of all the multi items available
        /// </summary>
        private List<MultiItem> multiCache = new List<MultiItem>();

        /// <summary>
        /// Cache for the cliloc data files
        /// </summary>
        private Dictionary<ClilocLanguages, List<KeyValuePair<long, string>>> clilocCache = new Dictionary<ClilocLanguages, List<KeyValuePair<long, string>>>();

        /// <summary>
        /// Default SendMessage function (used to fix the listview line spacing)
        /// </summary>
        [DllImport( "user32.dll" )]
        private static extern IntPtr SendMessage( IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam );

        /// <summary>
        /// flag indicating that the last search we did was an animation search
        /// </summary>
        private bool lastItemAnimSearch = false;

        /// <summary>
        /// flag indicating we are loading the animation from the list
        /// </summary>
        private bool loadAnimFromList = false;

        /// <summary>
        /// flag indicating that the user is changing the multi height
        /// </summary>
        private bool changingHeight = false;

        /// <summary>
        /// cache for the images currently shown in the list
        /// </summary>
        private Dictionary<int, Bitmap> itemsImageCache = new Dictionary<int, Bitmap>();

        // index of the first and last visible item in the listview
        private int firsttVisibleItem = 0;
        private int lastVisibleItem = 0;

        /// <summary>
        /// cache for the audio data to be shown in the list
        /// </summary>
        private Dictionary<int, AudioData> audioCache = new Dictionary<int, AudioData>();

        /// <summary>
        /// Current sound player
        /// </summary>
        private IWavePlayer soundPlayer;

        /// <summary>
        /// Current audio data
        /// </summary>
        private WaveStream audioData;

        /// <summary>
        /// Search nodes list
        /// </summary>
        private List<TreeNode> foundNodes;

        /// <summary>
        /// Last node selected from the found list
        /// </summary>
        private int lastNode = 0;

        /// <summary>
        /// Male paperdoll items cache
        /// </summary>
        private UOAnimation malePaperdollCache;

        /// <summary>
        /// Female paperdoll items cache
        /// </summary>
        private UOAnimation femalePaperdollCache;

        /// <summary>
        /// flag indicating we're dragging the picture around
        /// </summary>
        private bool moving;

        /// <summary>
        /// Variable used to store the cursor position BEFORE dragging the image
        /// </summary>
        private Point cursorOffset;

        /// <summary>
        /// Variable to store the combo text when focused
        /// </summary>
        private string currentComboSelectedTxt;

        /// <summary>
        /// Flag that indicates the combo text is being reset (so we don't have to load anything)
        /// </summary>
        private bool resetCombo;

        /// <summary>
        /// Current image scale
        /// </summary>
        private float ImageScale = 1.0f;

        /// <summary>
        /// Known client languages ID
        /// </summary>
        private enum ClilocLanguages
        {
            English = 1,
            Japanese = 2,
            Korean = 3,
            French = 4,
            German = 5,
            Spanish = 6,
            Unused = 7,
            Chinese_Traditional = 8,
        }

        // initialize the item tooltip form
        ItemTooltip it = new ItemTooltip();

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Indicate the app is busy and needs to show the loading screen
        /// </summary>
        public bool Loading
        {
            get { return m_Loading; }
            set
            {
                // do nothing if we try to set the same value
                if ( m_Loading == value )
                    return;

                // remove the text to restore on focus lost for the last combo
                currentComboSelectedTxt = "";

                // store the new value
                m_Loading = value;

                // toggle the loading status on the forum
                ToggleLoading( m_Loading );
            }
        }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        public UOECSuperViewer()
        {
            InitializeComponent();
        }

        #endregion


        // --------------------------------------------------------------
        #region LOCAL EVENTS
        // --------------------------------------------------------------

        /// <summary>
        /// Handle keys pressed
        /// </summary>
        protected override bool ProcessCmdKey( ref Message msg, Keys keyData )
        {
            // play/pause button pressed
            if ( keyData == Keys.MediaPlayPause || ( keyData == Keys.Space && ActiveControl.GetType() != typeof( TextBox ) && ActiveControl.GetType() != typeof( ComboBox ) ) )
                btnPlayPause_Click( btnPlayPause, new EventArgs() );

            // stop button pressed
            else if ( keyData == Keys.MediaStop )
                btnStop_Click( btnStop, new EventArgs() );

            // next track button pressed
            else if ( keyData == Keys.MediaNextTrack )
                btnFast_Click( btnFast, new EventArgs() );

            // prev track button pressed
            else if ( keyData == Keys.MediaPreviousTrack )
                btnSlow_Click( btnSlow, new EventArgs() );

            return base.ProcessCmdKey( ref msg, keyData );
        }

        /// <summary>
        /// Executed at the start of the application
        /// </summary>
        private void Form_Load( object sender, EventArgs e )
        {
            // make sure it starts invisible
            Visible = false;

            // show the version on the title bar
            Text = "UO EC Super Viewer ( " + ProductVersion + " )";

            // set the loading image at the correct position
            imgLoading.Location = pnlPreview.Location;

            // show the loading screen
            Loading = true;

            // load the game path from settings
            LoadGamePath();

            // load the window position
            LoadWindowPosition();

            // set the custom sort = default sort at beginning
            charEquipCustomSort = charEquipSort.ToList();

            // initialize the actions dictionaries
            InitializeDictionaries();

            // load the strings dictionary from the game
            LoadStringsDictionary();

            // load the items list
            LoadItems();

            // initialize the item style on the list
            InitializeItemStyle();

            // load the multi items data
            LoadMultis();

            // load the audiop data
            LoadAudio();

            // update the mobiles list in the comboboxes
            InitializeCombos();

            // initialize the hue picker
            hp = new HuePicker( GamePath );

            // fill the hues table
            hp.LoadHues();

            // initialize the export to vd dialog
            xp = new ExportVD();

            // initialize the colors button
            InitializeColorButtons();

            // load the paperdoll items cache
            LoadPaperdoll();

            // load the cliloc data
            LoadCliloc();

            // initialize the cliloc page
            InitializeCliloc();

            // set the mouse-over event for the items list
            it.AttachHandlers( lstItems );

            // update the buttons visibility
            tabViewer.SelectedTab = pagCharacter;

            // toggle the animation controls
            ToggleAnimationControls( false );

            // clear the canvas
            ResetCurrentImage();

            // initialize the drag info text
            lblDragInfo.Text = "DRAG THE IMAGE TO SCROLL\n   (MOUSE WHEEL TO ZOOM)";

            // make sure the directions control is visible at start
            SwitchExportFrame( false );

            // hide the loading screen
            Loading = false;
        }

        /// <summary>
        /// Form closing and application ending
        /// </summary>
        private void UOECSuperViewer_FormClosing( object sender, FormClosingEventArgs e )
        {
            // save the window position for the next session
            SaveWindowPosition();
        }

        /// <summary>
        /// When the form resize, we fix the position of the image
        /// </summary>
        private void UOECSuperViewer_Resize( object sender, EventArgs e )
        {
            CenterImage();
        }

        /// <summary>
        /// Update XML button
        /// </summary>
        private void btnUpdate_Click( object sender, EventArgs e )
        {
            ctxUpdateXML.Show( Cursor.Position );
        }

        /// <summary>
        /// update the animations XML
        /// </summary>
        private void updateAnimationsXMLToolStripMenuItem_Click( object sender, EventArgs e )
        {
            UpdateAnimationsList();
        }

        /// <summary>
        /// Update the multi XML
        /// </summary>
        private void updateMultiXMLToolStripMenuItem_Click( object sender, EventArgs e )
        {
            UpdateMultiList();
        }

        /// <summary>
        /// Switch between animation and paperdoll view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkPaperdoll_CheckedChanged( object sender, EventArgs e )
        {
            // change the text based on the current status
            chkPaperdoll.Text = chkPaperdoll.Checked ? "Show Animation" : "Show Paperdoll";

            // disable the animation if it's going
            animPlayer.Enabled = false;

            // change the button symbol
            btnPlayPause.Text = "▶";

            // toggle the animation controls
            ToggleAnimationControls( !chkPaperdoll.Checked && CurrentMobile != null );

            // redraw the image accordingly
            ResetCurrentImage();

            // update the export frame button to export paperdoll
            SwitchExportFrame( false );
        }

        /// <summary>
        /// Center the image
        /// </summary>
        private void btnCenterImage_Click( object sender, EventArgs e )
        {
            // reset the scale
            ImageScale = 1.0f;

            // scale the image
            if ( imgPreview.Image != null )
                imgPreview.Size = new Size( (int)( imgPreview.Image.Width * ImageScale ), (int)( imgPreview.Image.Height * ImageScale ) );

            // center the image
            CenterImage();
        }

        /// <summary>
        /// Clicked preview image
        /// </summary>
        private void imgPreview_MouseDown( object sender, MouseEventArgs e )
        {
            // is the left button down?
            if ( e.Button == MouseButtons.Left )
            {
                // flag that we need to begin moving the image
                moving = true;

                // store the current cursor position
                cursorOffset = e.Location;
            }
        }

        /// <summary>
        /// Mouse release on preview image
        /// </summary>
        private void imgPreview_MouseUp( object sender, MouseEventArgs e )
        {
            // flag that we must stop moving the image
            moving = false;
        }

        /// <summary>
        /// Allows to drag the image around
        /// </summary>
        private void imgPreview_MouseMove( object sender, MouseEventArgs e )
        {
            // are we moving the image?
            if ( moving )
            {
                // map the screen position of the mouse cursor
                Point clientPos = pnlPreview.PointToClient( Cursor.Position );

                // calculate the new location
                imgPreview.Location = new Point( ( clientPos.X - cursorOffset.X ), ( clientPos.Y - cursorOffset.Y ) );
            }
        }

        /// <summary>
        /// Zoom image
        /// </summary>
        private void imgPreview_MouseWheel( object sender, MouseEventArgs e )
        {
            // this will work only when the cursor is inside the preview area
            if ( pnlPreview.ClientRectangle.Contains( pnlPreview.PointToClient( Cursor.Position ) ) && tabViewer.SelectedTab != pagAudio && tabViewer.SelectedTab != pagCliloc )
            {
                // the amount by which we adjust scale per wheel
                const float scale_per_delta = 0.1f / 120;

                // calculate the new scale level
                ImageScale += e.Delta * scale_per_delta;

                // make sure the scale doesn't go below 0
                ImageScale = Math.Max( ImageScale, 0.1f );

                // scale the image
                if ( imgPreview.Image != null )
                    imgPreview.Size = new Size( (int)( imgPreview.Image.Width * ImageScale ), (int)( imgPreview.Image.Height * ImageScale ) );
            }
        }

        /// <summary>
        /// Show/hide the drag info label and some buttons
        /// </summary>
        private void tmrDragCheck_Tick( object sender, EventArgs e )
        {
            // toggle the drag info label (visible only when the cursor is on the preview area)
            lblDragInfo.Visible = pnlPreview.ClientRectangle.Contains( pnlPreview.PointToClient( Cursor.Position ) ) && !Loading && imgPreview.Image != null && tabViewer.SelectedTab != pagAudio && tabViewer.SelectedTab != pagCliloc;

            // toggle the export frame button
            btnExportFrame.Enabled = !animPlayer.Enabled && imgPreview.Image != null && tabViewer.SelectedTab != pagAudio && tabViewer.SelectedTab != pagCliloc;

            // toggle the export to vd button
            btnExportToVD.Enabled = CurrentMobile != null && tabViewer.SelectedTab == pagCreatures;

            // toggle the export sprite sheet button
            btnExportSpritesheet.Enabled = CurrentMobile != null && ( tabViewer.SelectedTab == pagCreatures || tabViewer.SelectedTab == pagCharacter );

            // toggle the export multi data button
            btnExportMultiData.Enabled = cmbMulti.SelectedIndex > 0;
        }

        /// <summary>
        /// Tab page changed
        /// </summary>
        private void tabViewer_PagChanged( object sender, EventArgs e )
        {
            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // show the drag info
            lblDragInfo.Visible = false;

            // get the current tabpage
            TabPage p = tabViewer.SelectedTab;

            // character page
            if ( p == pagCharacter )
            {
                // get rid of the currently active sound player
                ClearSound();

                // disable export to VD
                btnExportToVD.Enabled = false;

                // disable the frame export
                btnExportFrame.Enabled = false;

                // enable the sprite sheet export
                btnExportSpritesheet.Enabled = true;

                // enable save/load character
                btnSaveChar.Enabled = true;
                btnLoadChar.Enabled = true;

                // disable the sound export button
                btnExportSound.Enabled = false;

                // show the reset button
                btnReset.Visible = true;

                // enable the actions controls
                pnlDirections.Visible = true;

                // show the toggle paperdoll button
                chkPaperdoll.Visible = true;

                // show the center image button
                btnCenterImage.Visible = true;

                // show the speed buttons
                btnSlow.Visible = true;
                btnFast.Visible = true;

                // show the animation controls
                pnlAnimationControls.Visible = true;

                // reset the character combos
                UpdateCharacterCombos();

                // make sure all items have be de-selected
                lstItems.SelectedIndices.Clear();

                // get the list of the combo boxes in the creatures tab
                IEnumerable<ComboBox> objs = from cmb in pagCreatures.Controls.OfType<ComboBox>()
                                             select cmb;

                // reset the creatures combos
                foreach ( ComboBox cmb in objs )
                    cmb.SelectedIndex = 0;
            }
            else if ( p == pagCreatures )
            {
                // get rid of the currently active sound player
                ClearSound();

                // enable export to VD
                btnExportToVD.Enabled = true;

                // disable the frame export
                btnExportFrame.Enabled = false;

                // enable the sprite sheet export
                btnExportSpritesheet.Enabled = true;

                // disable save/load character
                btnSaveChar.Enabled = false;
                btnLoadChar.Enabled = false;

                // disable the sound export button
                btnExportSound.Enabled = false;

                // hide the reset button
                btnReset.Visible = false;

                // enable the actions controls
                pnlDirections.Visible = true;

                // show the toggle paperdoll button
                chkPaperdoll.Visible = true;

                // show the center image button
                btnCenterImage.Visible = true;

                // show the speed buttons
                btnSlow.Visible = true;
                btnFast.Visible = true;

                // show the animation controls
                pnlAnimationControls.Visible = true;

                // make sure all items have be de-selected
                lstItems.SelectedIndices.Clear();

                // get the list of the combo boxes in the character tab
                IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                             select cmb;

                // reset the character combos
                foreach ( ComboBox cmb in objs )
                    cmb.SelectedIndex = 0;
            }
            else if ( p == pagAudio )
            {
                // get rid of the currently active sound player
                ClearSound();

                // enable export to VD
                btnExportToVD.Enabled = false;

                // disable the frame export
                btnExportFrame.Enabled = false;

                // disable the sprite sheet export
                btnExportSpritesheet.Enabled = false;

                // disable save/load character
                btnSaveChar.Enabled = false;
                btnLoadChar.Enabled = false;

                // disable the sound export button
                btnExportSound.Enabled = false;

                // hide the reset button
                btnReset.Visible = false;

                // disable the actions controls
                pnlDirections.Visible = false;

                // hide the multi controls panel
                pnlMultiControls.Visible = false;

                // hide the toggle paperdoll button
                chkPaperdoll.Visible = false;

                // hide the center image button
                btnCenterImage.Visible = false;

                // hide the speed buttons
                btnSlow.Visible = false;
                btnFast.Visible = false;

                // show the animation controls
                pnlAnimationControls.Visible = true;

                // make sure all items have be de-selected
                lstItems.SelectedIndices.Clear();

                // get the list of the combo boxes in the character and creatures tab
                IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>().Union( pagCreatures.Controls.OfType<ComboBox>() )
                                             select cmb;

                // reset the character and creatures combos
                foreach ( ComboBox cmb in objs )
                    cmb.SelectedIndex = 0;
            }
            else if ( p == pagCliloc )
            {
                // get rid of the currently active sound player
                ClearSound();

                // enable export to VD
                btnExportToVD.Enabled = false;

                // disable the frame export
                btnExportFrame.Enabled = false;

                // disable the sprite sheet export
                btnExportSpritesheet.Enabled = false;

                // disable save/load character
                btnSaveChar.Enabled = false;
                btnLoadChar.Enabled = false;

                // disable the sound export button
                btnExportSound.Enabled = false;

                // hide the reset button
                btnReset.Visible = false;

                // disable the actions controls
                pnlDirections.Visible = false;

                // hide the multi controls panel
                pnlMultiControls.Visible = false;

                // hide the toggle paperdoll button
                chkPaperdoll.Visible = false;

                // hide the center image button
                btnCenterImage.Visible = false;

                // hide the speed buttons
                btnSlow.Visible = false;
                btnFast.Visible = false;

                // show the animation controls
                pnlAnimationControls.Visible = false;

                // make sure all items have be de-selected
                lstItems.SelectedIndices.Clear();

                // get the list of the combo boxes in the character and creatures tab
                IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>().Union( pagCreatures.Controls.OfType<ComboBox>() )
                                             select cmb;

                // reset the character and creatures combos
                foreach ( ComboBox cmb in objs )
                    cmb.SelectedIndex = 0;
            }

            // change the text based on the current status
            chkPaperdoll.Text = chkPaperdoll.Checked ? "Show Animation" : "Show Paperdoll";

            // reset the current mobile
            if ( CurrentMobile != null )
            {
                // clear the current mobile
                CurrentMobile.Dispose();
                CurrentMobile = null;
            }

            // clear the current image
            ResetCurrentImage();

            // clear all the unused data
            ClearUnusedFrames();

            // reset the multi/animation status
            SwitchExportFrame( false );

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Draw item separator
        /// </summary>
        private void pnlItems_Paint( object sender, PaintEventArgs e )
        {
            // get the current graphics handler
            using ( Graphics g = e.Graphics )
            {
                // create the pen to draw with
                Pen p = new Pen( new SolidBrush( Color.Black ), 1.0f );

                // start and end x location in the panel to draw the lines
                int startX = lstItems.Location.X;
                int endX = lstItems.Location.X + lstItems.Size.Width - 1;

                // distance from the panel top border and the beginning of the list
                int startY = 30;
                int endY = lstItems.Location.Y;

                // draw the separator line
                g.DrawLine( p, new Point( startX, startY ), new Point( endX, startY ) );

                // draw the lines toward the list on the side
                g.DrawLine( p, new Point( startX, startY ), new Point( startX, endY ) );
                g.DrawLine( p, new Point( endX, startY ), new Point( endX, endY ) );

                // move the item type checkbox in the correct position
                chkItemsType.Location = new Point( ( ( pnlItems.Width - 10 ) - chkItemsType.Width ) / 2, 5 );
            }
        }

        /// <summary>
        /// Switch item type between default and old KR.
        /// </summary>
        private void chkItemsType_CheckedChanged( object sender, EventArgs e )
        {
            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // udapte the settings
            Settings.Default.useOldKRItems = chkItemsType.Checked;

            // save the changes to the settings
            Settings.Default.Save();

            // update the checkbox text
            if ( chkItemsType.Checked )
                chkItemsType.Text = "Items (Old KR Style)";
            else
                chkItemsType.Text = "Items (Default Style)";

            // empty the loaded items image cache
            itemsImageCache.Clear();

            // refresh the list
            lstItems.Invalidate();

            // refresh the checkbox position
            pnlItems.Invalidate();

            // make sure the image cache for the multi is cleared
            foreach ( MultiItem mi in multiCache )
            {
                // clear the items cache for the mult (if present)
                mi.ClearImageCache();

                // allow the form to update
                Application.DoEvents();
            }

            // redraw the multi (if there is one showing)
            DrawSelectedMulti();

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Reset the character selections
        /// </summary>
        private void btnReset_Click( object sender, EventArgs e )
        {
            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;


            // reset all combos
            InitializeCombos();

            // reset the hue buttons
            InitializeColorButtons();

            // get rid of the current mobile data
            if ( CurrentMobile != null )
            {
                CurrentMobile.Dispose();
                CurrentMobile = null;
            }

            // reset the preview
            ResetCurrentImage();

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Direction checkbox clicked
        /// </summary>
        private void btnDir_CheckedChanged( object sender, EventArgs e )
        {
            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;


            // update the checkboxes
            UpdateChecks( (CheckBox)sender );

            // make sure the checkbox clicked is checked at the end
            ( (CheckBox)sender ).Checked = true;

            // reset the current image preview
            ResetCurrentImage();

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Open a VD File
        /// </summary>
        private void btnVDFile_Click( object sender, EventArgs e )
        {
            // reset the file name
            ofdFile.FileName = "";

            // set the files filter
            ofdFile.Filter = "VD files(*.vd)|*.vd"; // |Binary files(*.bin)|*.bin"

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // file selected correctly?
            if ( ofdFile.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // remove the selection from creature and equipment first
                cmbCreatures.SelectedIndex = 0;
                cmbEquip.SelectedIndex = 0;

                // load the file
                BeginLoadVD();

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Combo lost focus
        /// </summary>
        private void cmb_Leave( object sender, EventArgs e )
        {
            // do nothing during loading
            if ( Loading )
                return;

            // nothing to restore?
            if ( currentComboSelectedTxt == "" )
                return;

            // get the active combo box
            ComboBox cmb = (ComboBox)sender;

            // flag that we are about to reset the combo
            resetCombo = true;

            // reset the combo text
            cmb.Text = currentComboSelectedTxt;

            // flag that the combo reset is over
            resetCombo = false;

            // reset the stored combo text
            currentComboSelectedTxt = "";
        }

        /// <summary>
        /// Combo got focus
        /// </summary>
        private void cmb_Enter( object sender, EventArgs e )
        {
            // get the active combo box
            ComboBox cmb = (ComboBox)sender;

            // store the combo text
            currentComboSelectedTxt = cmb.Text;
        }

        /// <summary>
        /// Select something from the items/creature combo box
        /// </summary>
        private void cmb_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( resetCombo )
                return;

            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // get the combo box object
            ComboBox cmb = (ComboBox)sender;

            // if the selected value is null, we get out
            if ( cmb.SelectedValue == null )
            {
                // reset the selection
                cmb.SelectedIndex = 0;

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;

                // BEEP!
                SystemSounds.Beep.Play();

                // reset the load from list flag if it's the equipment combo
                if ( cmb == cmbEquip )
                    loadAnimFromList = false;

                // disable the export frame button
                btnExportFrame.Enabled = false;

                return;
            }

            // NONE selected
            if ( cmb.SelectedIndex == 0 )
            {
                // do nothing during the combo reset
                if ( comboReset )
                    return;

                // update the frames of the current action (if it's the character tab)
                if ( tabViewer.SelectedTab == pagCharacter )
                    MergeFrames();

                // clear the current mobile
                if ( CurrentMobile != null && ( cmb == cmbCreatures || cmb == cmbEquip ) && cmbCreatures.SelectedIndex == 0 && cmbEquip.SelectedIndex == 0 )
                {
                    CurrentMobile.Dispose();
                    CurrentMobile = null;
                }

                // disable the export frame button
                btnExportFrame.Enabled = false;

                // reset the image
                ResetCurrentImage();

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;

                return;
            }

            // change the export frame button text and animations controls status
            SwitchExportFrame( false );

            // enable the export frame button
            btnExportFrame.Enabled = true;

            // update the stored text
            currentComboSelectedTxt = cmb.Text;

            // Load the xml
            XDocument xdoc = XDocument.Load( AnimXMLFile );

            // creature selected (in this case the current mobile IS the creature mobile)
            if ( cmb == cmbCreatures || cmb == cmbEquip )
            {
                // clear all the unused data
                ClearUnusedFrames();

                // if this is the all equip combo, we reset the creature
                if ( cmb == cmbEquip )
                {
                    // reset the creature selection
                    cmbCreatures.SelectedIndex = 0;

                    // highlight the related item (only if we haven't double-clicked the item from the list)
                    if ( !loadAnimFromList )
                    {
                        SearchItemByAnimID( int.Parse( cmb.SelectedValue.ToString() ) );

                        // reset the flag
                        loadAnimFromList = false;
                    }
                }

                else // if this is the creatures combo we reset the all equip combo
                    cmbEquip.SelectedIndex = 0;

                // reset the multi combo
                cmbMulti.SelectedIndex = 0;

                // get the body ID for the creature
                int bodyId = int.Parse( cmb.SelectedValue.ToString() );

                // we selected the same mobile again
                if ( CurrentMobile != null && CurrentMobile.BodyId == bodyId )
                {
                    // hide the loading screen
                    if ( !loadBck )
                        Loading = false;

                    return;
                }

                // do we have a current mobile that is not the selected one?
                if ( CurrentMobile != null && CurrentMobile.BodyId != bodyId )
                {
                    // remove from the memory the current mobile data
                    Mobiles.Remove( Mobiles.Where( mob => mob.BodyId == CurrentMobile.BodyId ).FirstOrDefault() );
                }

                // Search the xml for the mobiles with the selected body ID
                IEnumerable<XElement> items = from itm in xdoc.Root.Descendants( "Item" )
                                              where itm.Attribute( "Id" ).Value == bodyId.ToString()
                                              select itm;

                // select the active animation
                IEnumerable<XElement> anims = items.Descendants( "Animation" );

                // clear the current mobile before loading a new one
                if ( CurrentMobile != null )
                    CurrentMobile.Dispose();

                // set the mobile from the main list as current mobile (or create a new one if it's not on the main list)
                CurrentMobile = Mobiles.Where( mm => mm.BodyId == bodyId ).FirstOrDefault() ?? new Mobile( bodyId );

                // fill the mobile data
                LoadMobileFromXElement( items.FirstOrDefault(), ref m_currentMobile );

                // scan all the animations
                foreach ( XElement anim in anims )
                {
                    // load the animation
                    LoadMobileAnimation( anim.Attribute( "UOP" ).Value, int.Parse( anim.Attribute( "Block" ).Value ), int.Parse( anim.Attribute( "File" ).Value ), ref m_currentMobile, int.Parse( anim.Attribute( "Id" ).Value ) );

                    // allow the form to update
                    Application.DoEvents();
                }
            }
            else // body or equipment selected (in this case we need to overlay the images in the current mobile)
            {
                // get the body ID for the layer
                int bodyId = int.Parse( cmb.SelectedValue.ToString() );

                // Search the xml for the mobiles with the selected body ID
                IEnumerable<XElement> items = from itm in xdoc.Root.Descendants( "Item" )
                                              where itm.Attribute( "Id" ).Value == bodyId.ToString()
                                              select itm;

                // select the active animation
                IEnumerable<XElement> anims = items.Descendants( "Animation" );

                // set the mobile from the main list as current mobile (or create a new one if it's not on the main list)
                Mobile m = Mobiles.Where( mm => mm.BodyId == bodyId ).FirstOrDefault();

                // is the mobile missing from the main list?
                if ( m == null )
                {
                    // add a new mobile to the list
                    Mobiles.Add( new Mobile( bodyId ) );

                    // set the current mobile as the last one added
                    m = Mobiles[Mobiles.Count - 1];

                    // fill the mobile data
                    LoadMobileFromXElement( items.FirstOrDefault(), ref m );
                }

                // scan all the animations
                foreach ( XElement anim in anims )
                {
                    // load the animation
                    LoadMobileAnimation( anim.Attribute( "UOP" ).Value, int.Parse( anim.Attribute( "Block" ).Value ), int.Parse( anim.Attribute( "File" ).Value ), ref m, int.Parse( anim.Attribute( "Id" ).Value ) );

                    // allow the form to update
                    Application.DoEvents();
                }

                // clear all the unused data (animations included if it's the body)
                ClearUnusedFrames( cmb != cmbBody );

                // is this the body combo?
                if ( cmb == cmbBody )
                {
                    // update the content of the combos based on the body selected
                    ComboUpdateGarg();

                    // update the combos availability
                    UpdateCharacterCombos( true );
                }

                // update the frames of the current action
                MergeFrames();

                // allow the form to update
                Application.DoEvents();
            }

            // update the current actions list (to show only the available ones)
            UpdateActionsList( ref cmb );

            // update the preview
            ResetCurrentImage();

            // allow the form to update before we move to the next
            Application.DoEvents();

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Change the preview action
        /// </summary>
        private void cmbActions_SelectedIndexChanged( object sender, EventArgs e )
        {
            // do nothing during the combo reset
            if ( comboReset )
                return;

            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // initialize the selection changed flag
            bool selChanged = false;

            try // we use try catch because at loading the parse won't work
            {
                // flag indicating the action selection has changed
                selChanged = m_SelectedAction != (int)cmbActions.SelectedValue;
            }
            catch
            {
                return;
            }

            // delete the frames of the previously selected action (so we won't clutter the memory)
            if ( CurrentMobile != null && CurrentMobile.Actions[m_SelectedAction] != null && selChanged )
                CurrentMobile.Actions[m_SelectedAction].DisposeFrames();

            // store the selected action
            m_SelectedAction = (int)cmbActions.SelectedValue;

            // update the frames of the current action (if it's the character tab)
            if ( tabViewer.SelectedTab == pagCharacter && selChanged )
                MergeFrames();

            // reset the current image preview
            if ( selChanged )
                ResetCurrentImage();

            // update the stored text
            currentComboSelectedTxt = cmbActions.Text;

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Draw the multi
        /// </summary>
        private void cmbMulti_SelectedIndexChanged( object sender, EventArgs e )
        {
            // do nothing during the combo reset
            if ( comboReset )
                return;

            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // make sure the image cache for the multi is cleared
            foreach ( MultiItem mi in multiCache )
            {
                // clear the items cache for the mult (if present)
                mi.ClearImageCache();

                // allow the form to update
                Application.DoEvents();
            }

            // do nothing if the index is negative
            if ( (int)cmbMulti.SelectedValue == -1 )
            {
                // hide the loading screen
                if ( !loadBck )
                    Loading = false;

                return;
            }

            // clear the selection for creatures and equip
            cmbCreatures.SelectedIndex = 0;
            cmbEquip.SelectedIndex = 0;

            // clear the current mobile
            if ( CurrentMobile != null )
            {
                CurrentMobile.Dispose();
                CurrentMobile = null;
            }

            // reset the height trackbars
            ResetHeightTrackbars();

            // draw the multi
            DrawSelectedMulti();

            // change the export frame button text and animations controls status
            SwitchExportFrame( true );

            // update the stored text
            currentComboSelectedTxt = cmbMulti.Text;

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Part selected from the multi parts list
        /// </summary>
        private void cmbMultiParts_SelectedIndexChanged( object sender, EventArgs e )
        {
            try
            {
                // select the item from the list
                SearchItemByID( int.Parse( cmbMultiParts.SelectedValue.ToString() ) );

                // update the stored text
                currentComboSelectedTxt = cmbMulti.Text;
            }
            catch
            { }
        }

        /// <summary>
        /// Play/pause toggle
        /// </summary>
        private void btnPlayPause_Click( object sender, EventArgs e )
        {
            // make sure the button is enabled
            if ( !btnPlayPause.Enabled || !btnPlayPause.Visible )
                return;

            // is the audio tab selected?
            if ( tabViewer.SelectedTab == pagAudio )
            {
                // make sure the sound player is active
                if ( soundPlayer == null )
                    return;

                // is the sound playing?
                if ( soundPlayer.PlaybackState == PlaybackState.Playing )
                {
                    // change the button symbol
                    ( (Button)sender ).Text = "▶";

                    // pause the sound
                    soundPlayer.Pause();

                    // pause the audio time
                    audioPlayer.Enabled = false;
                }
                else // paused
                {
                    // change the button symbol
                    ( (Button)sender ).Text = "❚❚";

                    // pause the sound
                    soundPlayer.Play();

                    // progress the audio time
                    audioPlayer.Enabled = true;
                }
            }
            else // any other page
            {
                // do nothing while in paperdoll mode
                if ( chkPaperdoll.Checked )
                    return;

                // animation in progress
                if ( animPlayer.Enabled )
                {
                    // change the button symbol
                    ( (Button)sender ).Text = "▶";

                    // pause the animation
                    animPlayer.Enabled = false;
                }
                else // animation in pause
                {
                    // change the button symbol
                    ( (Button)sender ).Text = "❚❚";

                    // start the animation
                    animPlayer.Enabled = true;
                }

                // the export frame is enabled only if the animation is not running
                btnExportFrame.Enabled = !animPlayer.Enabled;
            }
        }

        /// <summary>
        /// Stop the animation preview
        /// </summary>
        private void btnStop_Click( object sender, EventArgs e )
        {
            // make sure the button is enabled
            if ( !btnStop.Enabled || !btnStop.Visible )
                return;

            // is the audio tab selected?
            if ( tabViewer.SelectedTab == pagAudio )
            {
                // make sure the sound player is active
                if ( soundPlayer == null )
                    return;

                // change the button symbol
                btnPlayPause.Text = "▶";

                // stop the sound
                soundPlayer.Stop();

                // reset the sound
                audioData.Seek( 0, SeekOrigin.Begin );

                // hide the audio time
                audioPlayer.Enabled = false;

                // update the current playing time
                UpdateSoundDuration();
            }
            else // other tabs
            {
                // change the play/pause button symbol
                btnPlayPause.Text = "▶";

                // pause the animation
                animPlayer.Enabled = false;

                // the export frame is enabled only if the animation is not running
                btnExportFrame.Enabled = !animPlayer.Enabled;

                // reset the image preview
                ResetCurrentImage();
            }
        }

        /// <summary>
        /// Slow the preview
        /// </summary>
        private void btnSlow_Click( object sender, EventArgs e )
        {
            // make sure the button is enabled
            if ( !btnSlow.Enabled || ! btnSlow.Visible )
                return;

            // we cap the speed at 1 frame per second
            if ( animPlayer.Interval >= 1000 )
            {
                animPlayer.Interval = 1000;

                return;
            }

            // increase the timer interval
            animPlayer.Interval += 50;
        }

        /// <summary>
        /// Speed up the preview
        /// </summary>
        private void btnFast_Click( object sender, EventArgs e )
        {
            // make sure the button is enabled
            if ( !btnFast.Enabled || !btnFast.Visible )
                return;

            // we cap the speed at 20 frames per second
            if ( animPlayer.Interval <= 50 )
            {
                animPlayer.Interval = 50;

                return;
            }

            // decrease the timer interval
            animPlayer.Interval -= 50;
        }

        /// <summary>
        /// Play the next animation frame in the active animation
        /// </summary>
        private void animPlayer_Tick( object sender, EventArgs e )
        {
            // initialize the first frame id
            int firstFrameID = -1;

            // initialize the max amount of frames in the current frameset
            int maxFrames = -1;

            // reset the image to the first frame of the current frameset
            SetImage( GetCurrentFrameImage( ref firstFrameID, ref maxFrames ) );

            // hide the label if there are no frames to show
            lblCurrFramePlaying.Visible = maxFrames != -1;

            // update the current frame label
            lblCurrFramePlaying.Text = "Frame: " + ( LastFramePlayed + 1 ) + "/" + maxFrames;

            // move to the next frame to play
            LastFramePlayed++;
        }

        /// <summary>
        /// Update the audio time label
        /// </summary>
        private void audioPlayer_Tick( object sender, EventArgs e )
        {
            // do we have the audio data?
            if ( audioData == null )
            {
                // disable the timer
                audioPlayer.Enabled = false;

                // clear the time label
                lblCurrFramePlaying.Text = "";

                return;
            }

            // update the current playing time
            UpdateSoundDuration();
        }

        /// <summary>
        /// Save the spritesheet for this animation
        /// </summary>
        private void btnExportSpritesheet_Click( object sender, EventArgs e )
        {
            // if there is no current mobile, we can get out
            if ( CurrentMobile == null )
                return;

            // set the save file dialog title
            sfd.Title = "Export Spritesheet";

            // set the save file dialog filter
            sfd.Filter = "PNG file (*.png)|*.png";

            // reset the file name
            sfd.FileName = tabViewer.SelectedTab == pagCharacter ? "Character" : CurrentMobile.Name;

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // save the spritesheet
            if ( sfd.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // ask the user if we have to export all actions or just the current one
                if ( MessageBox.Show( this, "Do you want to export the sprites sheet for all the actions?\n\nYes to export all animations\nNO to export only the currently active action.", "Export all?", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.Yes )
                {
                    // if we are in the character page, first we need to create the frames for all animations available
                    if ( tabViewer.SelectedTab == pagCharacter )
                        MergeFrames( true );

                    // export all the actions character sheet
                    ExportCharacterSheetAll();

                    // clear all the unused data
                    ClearUnusedFrames();
                }
                else // export the active action
                    ExportCharacterSheetCurrent();

                // update the frames of the current action (if this is the character page)
                if ( tabViewer.SelectedTab == pagCharacter )
                    MergeFrames();

                // play a sound to warn the process is ended
                new SoundPlayer( Resources.flute ).Play();

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Export equip/creature/loaded file to VD
        /// </summary>
        private void btnExportToVD_Click( object sender, EventArgs e )
        {
            // if there is no current mobile, we can get out
            if ( CurrentMobile == null )
                return;

            // set the save file dialog title
            sfd.Title = "Export as .VD";

            // set the save file dialog filter
            sfd.Filter = "VD file (*.vd)|*.vd";

            // reset the file name
            sfd.FileName = CurrentMobile.Name;

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // we setup the export to vd dialog to customize the actions ONLY if the file has not been loaded from a VD file originally
            if ( !CurrentMobile.VDOriginal )
            {
                // set the mobile to use on the export window
                xp.CurrentMobile = CurrentMobile;

                // clear the previous actions list
                xp.Actions.Clear();

                // is a piece of equipment selected (or a body)?
                if ( cmbEquip.SelectedIndex > 0 || CurrentMobile.MobileType == Mobile.MobileTypes.Human )
                    xp.Actions = CharActions.Where( act => CurrentMobile.Actions[act.Key] != null ).ToDictionary( v => v.Key, v => v.Value );

                else // creature selected
                    xp.Actions = CreatureActions.Where( act => CurrentMobile.Actions[act.Key] != null ).ToDictionary( v => v.Key, v => v.Value );

                // add an empty value at the beginning
                xp.Actions.Add( -1, "----" );

                // update the export to vd dialog
                xp.PopulateForm();
            }

            // process cancelled
            if ( !CurrentMobile.VDOriginal && xp.ShowDialog( f ) == DialogResult.Cancel )
                return;

            // save the mobile as VD
            if ( sfd.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // export to VD
                if ( CurrentMobile.VDOriginal )
                    CurrentMobile.ExportToVD( sfd.FileName );

                else // export to VD with the specified data
                    CurrentMobile.ExportToVD( sfd.FileName, xp );

                // clear the memory from trash
                ClearUnusedFrames();

                // play a sound to warn the process is ended
                new SoundPlayer( Resources.flute ).Play();

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Export the current frame
        /// </summary>
        private void btnExportFrame_Click( object sender, EventArgs e )
        {
            // export multi
            if ( tabViewer.SelectedTab == pagCreatures && cmbMulti.SelectedIndex > 0 )
            {
                ExportMulti();
            }
            else // export frame
            {
                ExportFrame();
            }
        }

        /// <summary>
        /// Export sound
        /// </summary>
        private void btnExportSound_Click( object sender, EventArgs e )
        {
            // make sure we have a node selected
            if ( trvAudio.SelectedNode == null )
                return;

            // get the audio data for the selected sound
            AudioData aud = (AudioData)trvAudio.SelectedNode.Tag;

            // make sure we have the audio data
            if ( aud == null )
                return;

            // set the save file dialog title
            sfd.Title = "Save Sound";

            // set the save file dialog filter
            if ( aud.IsMP3 )
                sfd.Filter = "MP3 file (*.mp3)|*.mp3";
            else
                sfd.Filter = "WAV file (*.wav)|*.wav";

            // reset the file name
            sfd.FileName = Path.GetFileName( aud.Name );

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // save the sound
            if ( sfd.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // get the file data
                using ( MemoryStream ms = aud.GetAudioData() )
                {
                    // create the file
                    using ( FileStream file = new FileStream( sfd.FileName, FileMode.Create ) )
                    {
                        // write the file
                        ms.WriteTo( file );
                    }
                }

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Export selected items
        /// </summary>
        private void btnExportItems_Click( object sender, EventArgs e )
        {
            // set the browse description
            fbd.Description = "Which folder do you want to use to save the images?";

            // use the last directory picked or the application path
            fbd.SelectedPath = Settings.Default.LastSavedItemDirectory != string.Empty ? Settings.Default.LastSavedItemDirectory : Application.StartupPath;

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // show the folder browse dialog
            if ( fbd.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // store the folder for the next time
                Settings.Default.LastSavedItemDirectory = fbd.SelectedPath;

                // scan all the selected items
                foreach ( int idx in lstItems.SelectedIndices )
                {
                    // get the item data for this item
                    ItemData itm = itemsCache[idx];

                    // create the file name
                    string fn = ( itm.Name != string.Empty ? itm.Name + " (" + itm.ID + ")" : itm.ID.ToString() ) + ".png";

                    // create the file path
                    string filePath = Path.Combine( fbd.SelectedPath, fn );

                    // if the file exist, and the user DO NOT want to override it, we skip the item
                    if ( File.Exists( filePath ) && MessageBox.Show( this, "The file:\n\n" + fn + "\n\nALREADY EXIST!\n\nOK to overwrite\nCANCEL to skip the item", "File Exist", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning ) == DialogResult.Cancel )
                        continue;

                    // item image
                    Bitmap image = itm.GetItemImage( GamePath, Settings.Default.useOldKRItems );

                    // get the selected color for the item
                    Hue h = (Hue)btnColorItems.Tag;

                    // draw the image
                    if ( image != null )
                    {
                        // is there a hue selected?
                        if ( h.ID != 0 )
                        {
                            // create a temporary image to apply the hue
                            using ( DirectBitmap db = new DirectBitmap( image ) )
                            {
                                // apply the hue
                                image = db.ApplyHue( h.HueDiagram, itm.Flags.HasFlag( ItemData.TileFlag.PartialHue ) );
                            }
                        }
                    }

                    // save the image
                    image.Save( filePath, ImageFormat.Png );
                }

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Validate the combo box when the user writes something and press enter
        /// </summary>
        private void cmb_Validated( object sender, KeyEventArgs e )
        {
            // enter pressed
            if ( e.KeyCode == Keys.Enter )
            {
                // get the current combo
                ComboBox cmb = (ComboBox)sender;

                // check if the list contains the text entered
                if ( !cmb.Items.Cast<Object>().Any( x => cmb.GetItemText( x ) == cmb.Text ) )
                {
                    // select the first element of the list if what has been entered is wrong
                    cmb.SelectedIndex = 0;
                }

                // remove the text selection
                cmb.SelectionLength = 0;
            }
        }

        /// <summary>
        /// Draw order changed
        /// </summary>
        private void cmbDO_SelectedIndexChanged( object sender, EventArgs e )
        {
            // current loading status
            bool loadbck = Loading;

            // show the loading screen
            if ( !loadbck )
                Loading = true;

            // fix the custom drawing order in the combo boxes
            FixCustomDrawingOrder( (ComboBox)sender );

            // hide the loading screen
            if ( !loadbck )
                Loading = false;
        }

        /// <summary>
        /// Change mobile color
        /// </summary>
        private void btnColor_Click( object sender, EventArgs e )
        {
            // get the button object
            Button btn = (Button)sender;

            // set the currently selected hue in the dialog
            hp.ResetSelection( ( (Hue)btn.Tag ).ID );

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // did we select a color?
            if ( hp.ShowDialog( f ) == DialogResult.OK )
            {
                // get the selected hue
                Hue selHue = hp.SelectedHue;

                // store the color
                btn.Tag = selHue;

                // set the tooltip text
                ttp.SetToolTip( btn, "Hue Color: " + selHue.ID.ToString() + " - " + selHue.Name );

                // set the hue diagram as bg for the button
                btn.BackgroundImage = selHue.HueDiagram;

                // is this the items color?
                if ( btn == btnColorItems )
                {
                    // redraw the items
                    lstItems.Invalidate();
                }
                else
                {
                    // backup of the loading status
                    bool loadBck = Loading;

                    // show the loading screen
                    if ( !loadBck )
                        Loading = true;

                    // if we are in the character tab we update the images
                    if ( tabViewer.SelectedTab == pagCharacter )
                        MergeFrames();

                    // reset the active frames
                    ResetCurrentImage();

                    // did we had a multi selected?
                    if ( cmbMulti.SelectedIndex != 0 )
                        DrawSelectedMulti();

                    // hide the loading screen
                    if ( !loadBck )
                        Loading = false;
                }
            }
        }

        /// <summary>
        /// Save character equipment and settings
        /// </summary>
        private void btnSaveChar_Click( object sender, EventArgs e )
        {
            // set the save file dialog title
            sfd.Title = "Save Character";

            // set the save file dialog filter
            sfd.Filter = "Character file (*.char)|*.char";

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // save the character data
            if ( sfd.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // save the character
                SaveCharacter();

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Load character equipment and settings
        /// </summary>
        private void btnLoadChar_Click( object sender, EventArgs e )
        {
            // reset the file name
            ofdFile.FileName = "";

            // set the files filter
            ofdFile.Filter = "Character files( *.char ) | *.char";

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // file selected correctly?
            if ( ofdFile.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // load the file
                LoadCharacter();

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Sort the XML file
        /// </summary>
        private void btnSortXML_Click( object sender, EventArgs e )
        {
            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // Load the xml
            XDocument xdoc = XDocument.Load( AnimXMLFile );

            // sort the XML
            SortXML( ref xdoc );

            // save the changes to the XML
            xdoc.Save( AnimXMLFile );

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Items list update
        /// </summary>
        private void lstItems_RetrieveVirtualItem( object sender, RetrieveVirtualItemEventArgs e )
        {
            // get the item data for this item
            ItemData itm = itemsCache[e.ItemIndex];

            // set the listview item name
            e.Item = new ListViewItem( CultureInfo.CurrentCulture.TextInfo.ToTitleCase( itm.Name.Replace( "_", " " ) ) );

            //// get the item flags
            //string flags = itm.GetAllFlags();

            //// set the item tooltip to show the item name and the animation ID
            //e.Item.ToolTipText = e.Item.Text + "\nID: " + itm.ID.ToString() + ( itm.Properties.ContainsKey( ItemData.TileArtProperties.Animation ) ? "\nAnimation: " + itm.Properties[ItemData.TileArtProperties.Animation] + "\n(DOUBLE CLICK TO VIEW)" + ( itm.Properties.ContainsKey( ItemData.TileArtProperties.Layer ) ? "\nLayer: " + (ItemData.Layers)itm.Properties[ItemData.TileArtProperties.Layer] : "" ) : "" ) + ( flags != string.Empty ? "\nFlags: " + itm.GetAllFlags() : "" );

            // store the item data
            e.Item.Tag = itm;
        }

        /// <summary>
        /// Draw the listview item
        /// </summary>
        private void lstItems_DrawItem( object sender, DrawListViewItemEventArgs e )
        {
            // get the item data for this item
            ItemData itm = (ItemData)e.Item.Tag;

            // create a new rectangle (to remove the space between lines)
            Rectangle bounds = e.Bounds;
            bounds.Height = Math.Min( bounds.Height, 100 );

            // highlight the background if the element is selected
            if ( e.Item.Selected )
                e.Graphics.FillRectangle( new SolidBrush( Color.FromArgb( 90, 0, 0, 128 ) ), bounds );

            // pen to draw the borders
            Pen p = new Pen( new SolidBrush( Color.FromArgb( 128, 0, 0, 0 ) ), 0.1f )
            {
                DashStyle = DashStyle.Dash
            };

            // draw the grid
            e.Graphics.DrawRectangle( p, bounds );

            // get the cached item image
            Bitmap image = itemsImageCache.ContainsKey((int)itm.ID) ? itemsImageCache[(int)itm.ID] : null;

            // no image in the cache?
            if ( image == null )
            {
                // load the image in the cache
                if ( !itemsImageCache.ContainsKey( (int)itm.ID ) )
                    itemsImageCache.Add( (int)itm.ID, itm.GetItemImage( GamePath, Settings.Default.useOldKRItems ) );

                // get the image
                image = itemsImageCache[(int)itm.ID];
            }

            // get the selected color for then item
            Hue h = (Hue)btnColorItems.Tag;

            // draw the image
            if ( image != null )
            {
                // is there a hue selected?
                if ( h != null && h.ID != 0 )
                {
                    // create a temporary image to apply the hue
                    using ( DirectBitmap db = new DirectBitmap( image ) )
                    {
                        // apply the hue
                        image = db.ApplyHue( h.HueDiagram, itm.Flags.HasFlag( ItemData.TileFlag.PartialHue ) );
                    }
                }

                // resize the image if it's too big
                if ( image.Width > bounds.Width || image.Height > bounds.Height )
                    ResizeImage( new Size( bounds.Width - 10, bounds.Height - 30 ), ref image );

                // draw the image
                e.Graphics.DrawImage( image, new Point( bounds.Left + ( ( ( bounds.Width - 10 ) - image.Width ) / 2 ), bounds.Top + 10 ) );
            }

            // the base text font (bold if selected)
            Font txtFont = new Font( "SegoeUI", 8, e.Item.Selected ? FontStyle.Bold : FontStyle.Regular );

            // text format flags
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom | TextFormatFlags.WordBreak;

            // text size area
            Size txtSize = new Size( bounds.Width - 10, bounds.Height );

            // measure the text size
            Size txtMeasure = TextRenderer.MeasureText( e.Item.Text, txtFont, txtSize, flags );

            // does the text fit in the area?
            while ( txtMeasure.Width > bounds.Width - 10 )
            {
                // reduce the font size
                txtFont = new Font( "SegoeUI", txtFont.SizeInPoints - 0.1f, e.Item.Selected ? FontStyle.Bold : FontStyle.Regular );

                // re-mesure the string to check if it fits
                txtMeasure = TextRenderer.MeasureText( e.Item.Text, txtFont, txtSize, flags );
            }

            // rectangle where to draw the text
            Rectangle txtRect = new Rectangle( bounds.Left + 5, bounds.Top + txtMeasure.Height, txtSize.Width, txtSize.Height - txtMeasure.Height - 10 );

            // add a light background to the text to make it easier to read
            e.Graphics.FillRectangle( new SolidBrush( Color.FromArgb( 150, 255, 255, 255 ) ), new Rectangle( txtRect.X, bounds.Bottom - txtMeasure.Height - 10, txtSize.Width, txtMeasure.Height ) );

            // draw the text
            TextRenderer.DrawText( e.Graphics, e.Item.Text, txtFont, txtRect, Color.Black, Color.Empty, flags );
        }

        /// <summary>
        /// Listview visible items changed
        /// </summary>
        private void lstItems_VirtualItemsSelectionRangeChanged( object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e )
        {
            // assign the first item idx after the reset of the variable
            firsttVisibleItem = e.StartIndex;

            // store the last visible item
            lastVisibleItem = e.EndIndex;

            // clear out the unused items
            ClearUnusedItems();
        }

        /// <summary>
        /// Items list selection changed
        /// </summary>
        private void lstItems_SelectedIndexChanged( object sender, EventArgs e )
        {
            // enable the export items button ONLY if there are items to export
            btnExportItems.Enabled = lstItems.SelectedIndices.Count > 0;
        }

        /// <summary>
        /// Item double-click event
        /// </summary>
        private void lstItems_MouseDoubleClick( object sender, MouseEventArgs e )
        {
            // search for the item in the clicked location
            ListViewItem clickedItem = lstItems.HitTest( e.Location ).Item;

            // do we have the item?
            if ( clickedItem == null )
            {
                SystemSounds.Beep.Play();
                return;
            }

            // check if the item contains the animation
            if ( !itemsCache[clickedItem.Index].Properties.ContainsKey( ItemData.TileArtProperties.Animation ) )
            {
                SystemSounds.Beep.Play();
                return;
            }

            // set the flag that we have request the anim from the list
            loadAnimFromList = true;

            // show the animation
            cmbEquip.SelectedValue = itemsCache[clickedItem.Index].Properties[ItemData.TileArtProperties.Animation];
        }

        /// <summary>
        /// Show/hide the item Tooltip
        /// </summary>

        private void lstItems_MouseMove( object sender, MouseEventArgs e )
        {
            // get the item under the mouse cursor
            ListViewItem itm = lstItems.GetItemAt( e.X, e.Y );

            // hide the tooltip if there is no item
            if ( itm == null )
            {
                it.Hide();

                return;
            }

            // draw the images
            it.SetImages( GamePath, (ItemData)itm.Tag );

            // move the tooltip at the mouse location
            it.Location = new Point( Cursor.Position.X, Cursor.Position.Y - it.Height );
        }

        /// <summary>
        /// Settings button shows the context menu with the few settings
        /// </summary>
        private void btnSettings_Click( object sender, EventArgs e )
        {
            ctxSettings.Show( Cursor.Position );
        }

        /// <summary>
        /// Change game path setting
        /// </summary>
        private void tsmChangeGamePath_Click( object sender, EventArgs e )
        {
            // old path
            string oldPath = GamePath;

            // set the current path in the browse dialog
            fbd.SelectedPath = GamePath;

            // pick a new game path
            LoadGamePath( true, false );

            // restart the application to reload the files from the new path (only if the path has changed)
            if ( oldPath != GamePath )
                Application.Restart();
        }

        /// <summary>
        /// Item search key down
        /// </summary>
        private void Search_KeyDown( object sender, KeyEventArgs e )
        {
            // enter pressed?
            if ( e.KeyCode == Keys.Enter || e.KeyCode == Keys.F3 )
            {
                // nothing to search?
                if ( txtSearch.Text == string.Empty )
                {
                    SystemSounds.Beep.Play();
                    return;
                }

                try
                {
                    // convert the text to int (if possible)
                    int id = Int32.Parse( txtSearch.Text );

                    // F3 pressed and the last search was by anim ID? search by anim id again
                    if ( lastItemAnimSearch && e.KeyCode == Keys.F3 )
                        SearchItemByAnimID( id, !e.Shift && e.KeyCode == Keys.F3, e.Shift && e.KeyCode == Keys.F3 );

                    else // search by ite id (default)
                        SearchItemByID( id );
                }
                catch // failed to convert to int
                {
                    // search by name (F3 = search next, SHIFT + F3 = search previous)
                    SearchItemByName( txtSearch.Text, !e.Shift && e.KeyCode == Keys.F3, e.Shift && e.KeyCode == Keys.F3 );
                }

                // prevent the windows beep
                e.Handled = e.SuppressKeyPress = true;
            }
            // escape to clear the text
            else if ( e.KeyCode == Keys.Escape )
            {
                // clear the text
                txtSearch.Text = string.Empty;

                // clear the selection
                lstItems.SelectedIndices.Clear();

                // move to the top
                lstItems.EnsureVisible( 0 );

                // prevent the windows beep
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Search by ID
        /// </summary>
        private void btnSearchItemID_Click( object sender, EventArgs e )
        {
            try
            {
                // convert the text to int (if possible)
                int id = Int32.Parse( txtSearch.Text );

                // search by item id
                SearchItemByID( id );
            }
            catch // failed to convert to int
            {
                SystemSounds.Beep.Play();
            }
        }

        /// <summary>
        /// Search by name
        /// </summary>
        private void btnSearchItemName_Click( object sender, EventArgs e )
        {
            // search by name
            SearchItemByName( txtSearch.Text, true );
        }

        /// <summary>
        /// search by animation
        /// </summary>
        private void btnSearchItemAnim_Click( object sender, EventArgs e )
        {
            try
            {
                // convert the text to int (if possible)
                int id = Int32.Parse( txtSearch.Text );

                // search by animation id
                SearchItemByAnimID( id, true );
            }
            catch // failed to convert to int
            {
                SystemSounds.Beep.Play();
            }
        }

        /// <summary>
        /// Item search text changed
        /// </summary>
        private void txtSearch_TextChanged( object sender, EventArgs e )
        {
            // disable the search buttons if no text is present
            btnSearchItemID.Enabled = txtSearch.Text != string.Empty;
            btnSearchItemName.Enabled = txtSearch.Text != string.Empty;
            btnSearchItemAnim.Enabled = txtSearch.Text != string.Empty;
        }

        /// <summary>
        /// Clear item search text
        /// </summary>
        private void btnClearSearch_Click( object sender, EventArgs e )
        {
            // clear the search text
            txtSearch.Text = string.Empty;

            // clear the selection
            lstItems.SelectedIndices.Clear();

            // move to the top
            lstItems.EnsureVisible( 0 );
        }

        /// <summary>
        /// Multi height scrollbar changed
        /// </summary>
        private void HeightTrackbar_Scroll( object sender, EventArgs e )
        {
            // update the trackbar labels
            lblMinZ.Text = "MIN: " + trkMinZ.Value;
            lblMaxZ.Text = "MAX: " + trkMaxZ.Value;

            // current trackbar
            TrackBar trk = (TrackBar)sender;

            // is this the min height trackbar?
            if ( trk == trkMinZ )
            {
                // make sure the max height is always at least 1 higher than min
                if ( trkMaxZ.Maximum != 0 && trkMaxZ.Value <= trkMinZ.Value )
                {
                    // is the max height still less than max?
                    if ( trkMaxZ.Value < trkMaxZ.Maximum )
                        trkMaxZ.Value = trkMinZ.Value + 1;

                    else // move the min if we are already at max
                        trkMinZ.Value = trkMaxZ.Value - 1;
                }
            }

            // is this the max height trackbar?
            if ( trk == trkMaxZ )
            {
                // make sure the min height is always at least 1 lower than max
                if ( trkMaxZ.Maximum != 0 && trkMinZ.Value >= trkMaxZ.Value )
                {
                    // is the min height still higher than min??
                    if ( trkMinZ.Value > trkMinZ.Minimum )
                        trkMinZ.Value = trkMaxZ.Value - 1;

                    else // move the max if we are already at min
                        trkMaxZ.Value = trkMinZ.Value + 1;
                }
            }

            // update the multi
            if ( !changingHeight )
                DrawSelectedMulti();
        }

        /// <summary>
        /// the user is clicking the trackbar
        /// </summary>
        private void Trackbar_MouseDown( object sender, MouseEventArgs e )
        {
            changingHeight = true;
        }

        /// <summary>
        /// the user released the trackbar
        /// </summary>
        private void Trackbar_MouseUp( object sender, MouseEventArgs e )
        {
            changingHeight = false;

            // update the multi
            DrawSelectedMulti();
        }

        /// <summary>
        /// Search for audio name
        /// </summary>
        private void txtAudioSearch_TextChanged( object sender, EventArgs e )
        {
            // make sure there is something to search
            if ( txtAudioSearch.Text.Trim() == string.Empty )
            {
                // clear the search list
                foundNodes = null;

                // reset the last node
                lastNode = 0;

                return;
            }

            // search for compatible nodes
            foundNodes = trvAudio.FlattenTree().Where( n => n.Text.ToLower().Contains( txtAudioSearch.Text.ToLower() ) && ( n.Text.ToLower().EndsWith( ".mp3" ) || n.Text.ToLower().EndsWith( ".wav" ) ) ).ToList();

            // set the node to show
            lastNode = 0;

            // did we found anything?
            if ( foundNodes.Count > 0 )
            {
                // select the first node
                trvAudio.SelectedNode = foundNodes[lastNode];

                // show the first node
                foundNodes[lastNode].EnsureVisible();
            }
        }

        /// <summary>
        /// Clear the current search for audio files
        /// </summary>
        private void btnClearAudioSearch_Click( object sender, EventArgs e )
        {
            // clear the text
            txtAudioSearch.Clear();

            // reset the tree
            trvAudio.CollapseAll();

            // clear the found nodes list
            foundNodes = null;

            // reset the last node index
            lastNode = 0;
        }

        /// <summary>
        /// Search next handler
        /// </summary>
        private void txtAudioSearch_KeyDown( object sender, KeyEventArgs e )
        {
            // f3 pressed = search next
            if ( !e.Shift && e.KeyCode == Keys.F3 )
            {
                // make sure we have a next
                if ( foundNodes != null && foundNodes.Count > 0 )
                {
                    // move to the next element
                    lastNode++;

                    // return at the beginning when we are at the end
                    if ( lastNode >= foundNodes.Count )
                        lastNode = 0;

                    // select the node
                    trvAudio.SelectedNode = foundNodes[lastNode];

                    // show the node
                    foundNodes[lastNode].EnsureVisible();
                }

                // flag that the key press has been handled
                e.Handled = e.SuppressKeyPress = true;
            }

            // shift + f3 pressed = search previous
            else if ( e.Shift && e.KeyCode == Keys.F3 )
            {
                // make sure we have a next
                if ( foundNodes != null && foundNodes.Count > 0 )
                {
                    // move to the next element
                    lastNode--;

                    // return at the beginning when we are at the end
                    if ( lastNode <= 0 )
                        lastNode = foundNodes.Count - 1;

                    // select the node
                    trvAudio.SelectedNode = foundNodes[lastNode];

                    // show the node
                    foundNodes[lastNode].EnsureVisible();
                }

                // flag that the key press has been handled
                e.Handled = e.SuppressKeyPress = true;
            }
            // play the sound
            else if ( e.KeyCode == Keys.Enter )
            {
                // get the clicked node
                TreeNode n = trvAudio.SelectedNode;

                // make sure we selected an audio file
                if ( n.Text.ToLower().EndsWith( "mp3" ) || n.Text.ToLower().EndsWith( "wav" ) && n.Tag != null )
                {
                    // play the current sound
                    PlaySound();
                }

                // flag that the key press has been handled
                e.Handled = e.SuppressKeyPress = true;
            }
            // escape reset the search
            else if ( e.KeyCode == Keys.Escape )
            {
                // clear the search
                btnClearAudioSearch_Click( btnClearAudioSearch, new EventArgs() );

                // flag that the key press has been handled
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Tree node selected
        /// </summary>
        private void trvAudio_AfterSelect( object sender, TreeViewEventArgs e )
        {
            // get the clicked node
            TreeNode n = e.Node;

            // make sure we selected an audio file
            if ( n.Text.ToLower().EndsWith( "mp3" ) || n.Text.ToLower().EndsWith( "wav" ) && n.Tag != null )
            {
                // get the waveStream
                WaveStream ws = ( (AudioData)n.Tag ).GetWaveStream();

                // if there is no wavestream, we can get out
                if ( ws == null )
                    return;

                // set the active audio
                SetActiveSound( ws );
            }
        }

        /// <summary>
        /// Audio tree node double-click
        /// </summary>
        private void trvAudio_NodeMouseDoubleClick( object sender, TreeNodeMouseClickEventArgs e )
        {
            // get the clicked node
            TreeNode n = e.Node;

            // make sure we selected an audio file
            if ( n.Text.ToLower().EndsWith("mp3" ) || n.Text.ToLower().EndsWith( "wav" ) && n.Tag != null )
            {
                // play the current sound
                PlaySound();
            }
        }

        /// <summary>
        /// Audio has stopped
        /// </summary>
        private void SoundPlayerStopped( object sender, StoppedEventArgs e )
        {
            // change the button symbol
            btnPlayPause.Text = "▶";

            // reset the sound
            audioData.Seek( 0, SeekOrigin.Begin );

            // disable the timer
            audioPlayer.Enabled = false;
        }

        /// <summary>
        /// Cliloc language selected
        /// </summary>
        private void cmbLanguage_SelectedIndexChanged( object sender, EventArgs e )
        {
            // get the current cliloc
            List<KeyValuePair<long, string>> cliloc = GetSelectedCliloc();

            // set the list size
            lstCliloc.VirtualListSize = cliloc.Count;

            // clear the list
            lstCliloc.Items.Clear();
        }

        /// <summary>
        /// Draw a new item
        /// </summary>
        private void lstCliloc_RetrieveVirtualItem( object sender, RetrieveVirtualItemEventArgs e )
        {
            // get the current cliloc
            List<KeyValuePair<long, string>> cliloc = GetSelectedCliloc();

            // first column is the ID
            ListViewItem lvi = new ListViewItem( cliloc[e.ItemIndex].Key.ToString() );

            // set the text in the tooltip
            lvi.ToolTipText = cliloc[e.ItemIndex].Value;

            // second column is the text
            lvi.SubItems.Add( cliloc[e.ItemIndex].Value );

            // load the item
            e.Item = lvi;
        }

        /// <summary>
        /// Show the context menu for the clicked item
        /// </summary>
        private void lstCliloc_MouseClick( object sender, MouseEventArgs e )
        {
            // right mouse button?
            if ( e.Button == MouseButtons.Right )
            {
                // show the context menu (if there is something selected)
                if ( lstCliloc.SelectedIndices.Count > 0 )
                    ctxCliloc.Show( Cursor.Position );
            }
        }

        /// <summary>
        /// Copy the selected cliloc ID to the clipboad
        /// </summary>
        private void copyIDToolStripMenuItem_Click( object sender, EventArgs e )
        {
            // do we have a selected item?
            if ( lstCliloc.SelectedIndices.Count <= 0 )
                return;

            // get the selected item
            ListViewItem lvi = lstCliloc.Items[lstCliloc.SelectedIndices[0]];

            // copy the ID
            Clipboard.SetText( lvi.Text );
        }

        /// <summary>
        /// Copy the selected cliloc text to the clipboad
        /// </summary>
        private void copyTextToolStripMenuItem_Click( object sender, EventArgs e )
        {
            // do we have a selected item?
            if ( lstCliloc.SelectedIndices.Count <= 0 )
                return;

            // get the selected item
            ListViewItem lvi = lstCliloc.Items[lstCliloc.SelectedIndices[0]];

            // copy the text
            Clipboard.SetText( lvi.SubItems[1].Text );
        }

        /// <summary>
        /// Search by ID
        /// </summary>
        private void btnSearchClilocID_Click( object sender, EventArgs e )
        {
            try
            {
                // convert the text to int (if possible)
                long id = long.Parse( txtClilocSearch.Text );

                // search the cliloc string by ID
                SearchClilocByID( id );
            }
            catch // not a number
            {
                SystemSounds.Beep.Play();
            }
        }

        /// <summary>
        /// Search cliloc text
        /// </summary>
        private void btnSearchClilocText_Click( object sender, EventArgs e )
        {
            // search the text
            SearchClilocByText( txtClilocSearch.Text, true );
        }

        /// <summary>
        /// Reset the search text
        /// </summary>
        private void btnClearSearchCliloc_Click( object sender, EventArgs e )
        {
            // reset the text
            txtClilocSearch.Text = "";
        }

        /// <summary>
        /// Begin/continue search
        /// </summary>
        private void txtClilocSearch_KeyDown( object sender, KeyEventArgs e )
        {
            // begin search / search next
            if ( e.KeyCode == Keys.Enter || e.KeyCode == Keys.F3 )
            {
                try
                {
                    // convert the text to int (if possible)
                    long id = long.Parse( txtClilocSearch.Text );

                    // search the cliloc string by ID
                    SearchClilocByID( id );
                }
                catch
                {
                    // search by name (F3 = search next, SHIFT + F3 = search previous)
                    SearchClilocByText( txtClilocSearch.Text, !e.Shift && e.KeyCode == Keys.F3, e.Shift && e.KeyCode == Keys.F3 );
                }

                // flag that the key press has been handled
                e.Handled = e.SuppressKeyPress = true;
            }
            // escape reset the search
            else if ( e.KeyCode == Keys.Escape )
            {
                // reset the search
                btnClearSearchCliloc_Click( sender, new EventArgs() );

                // flag that the key press has been handled
                e.Handled = e.SuppressKeyPress = true;
            }
            // is this the cliloc list?
            else if ( sender.GetType() == typeof(ListView) )
            {
                // do we have a selected item?
                if ( lstCliloc.SelectedIndices.Count <= 0 )
                    return;

                // get the selected item
                ListViewItem lvi = lstCliloc.Items[lstCliloc.SelectedIndices[0]];

                // CTRL + C
                if ( e.Control && e.KeyCode == Keys.C )
                {
                    // CTRL + SHIFT + C
                    if ( e.Shift )
                    {
                        // copy the text
                        Clipboard.SetText( lvi.SubItems[1].Text );
                    }
                    else // just CTRL + C
                    {
                        // copy the ID
                        Clipboard.SetText( lvi.Text );
                    }
                }

                // flag that the key press has been handled
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Toggle the search buttons
        /// </summary>
        private void txtClilocSearch_TextChanged( object sender, EventArgs e )
        {
            // disable the search buttons if there is nothing to search
            btnSearchClilocID.Enabled = txtClilocSearch.Text != string.Empty;
            btnSearchClilocText.Enabled = txtClilocSearch.Text != string.Empty;
        }

        /// <summary>
        /// Export the multi data into a csv file
        /// </summary>
        private void btnExportMultiData_Click( object sender, EventArgs e )
        {
            // export the data
            ExportMultiData();
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Initialize the dictionaries for the actions
        /// </summary>
        private void InitializeDictionaries()
        {
            // Character actions
            CharActions.Add( 0, "Walk" );
            CharActions.Add( 1, "Walk (With Weapon)" );
            CharActions.Add( 2, "Run" );
            CharActions.Add( 3, "Run (With Weapon)" );
            CharActions.Add( 4, "Idle" );
            CharActions.Add( 5, "Idle (With Weapon)" );
            CharActions.Add( 6, "Fidget" );
            CharActions.Add( 7, "Idle - Combat (1H Weapon)" );
            CharActions.Add( 8, "Idle - Combat (2H Weapon)" );
            CharActions.Add( 9, "Slash Attack (1H Weapon)" );
            CharActions.Add( 10, "Pierce Attack (1H Weapon)" );
            CharActions.Add( 11, "Bash Attack (1H Weapon)" );
            CharActions.Add( 12, "Bash Attack (2H Weapon)" );
            CharActions.Add( 13, "Slash Attack (2H Weapon)" );
            CharActions.Add( 14, "Pierce Attack (2H Weapon)" );
            CharActions.Add( 15, "Combat Walk (2H Weapon)" );
            CharActions.Add( 16, "Spell 1" );
            CharActions.Add( 17, "Spell 2" );
            CharActions.Add( 18, "Bow Attack" );
            CharActions.Add( 19, "Crossbow Attack" );
            CharActions.Add( 20, "Get Hit" );
            CharActions.Add( 21, "Die Backward" );
            CharActions.Add( 22, "Die Forward" );
            CharActions.Add( 23, "Walk Mounted" );
            CharActions.Add( 24, "Run Mounted" );
            CharActions.Add( 25, "Idle Mounted" );
            CharActions.Add( 26, "Bash Attack Mounted" );
            CharActions.Add( 27, "Bow Attack Mounted" );
            CharActions.Add( 28, "Crossbow Attack Mounted" );
            CharActions.Add( 29, "Slash Attack Mounted" );
            CharActions.Add( 30, "Shield Block" );
            CharActions.Add( 31, "Punch" );
            CharActions.Add( 32, "Bowing" );
            CharActions.Add( 33, "Salute (Armed)" );
            CharActions.Add( 34, "Drinking" );
            CharActions.Add( 35, "Combat Walk (1H Weapon)" );
            CharActions.Add( 36, "Combat Walk (Unarmed)" );
            CharActions.Add( 37, "Idle (Shield)" );
            CharActions.Add( 38, "Sitting" );
            CharActions.Add( 39, "Get Hit (2H Weapon)" );
            CharActions.Add( 40, "Mining" );
            CharActions.Add( 41, "Idle - Combat (Shield)" );
            CharActions.Add( 42, "Drinking (Sat Down)" );
            CharActions.Add( 47, "Idle (2H Weapon) Mounted" );
            CharActions.Add( 48, "Get Hit Mounted" );
            CharActions.Add( 49, "Spell Cast Mounted" );
            CharActions.Add( 50, "Get Hit (Shield) Mounted" );
            CharActions.Add( 51, "Drinking Mounted" );

            // Gargoyle actions
            CharActions.Add( 60, "Take off" );
            CharActions.Add( 61, "Land" );
            CharActions.Add( 62, "Fly Forward (Slow)" );
            CharActions.Add( 63, "Fly Forward (Fast)" );
            CharActions.Add( 64, "Fly Idle" );
            CharActions.Add( 65, "Fly Idle Combat" );
            CharActions.Add( 66, "Fly Fidget" );
            CharActions.Add( 67, "Fly Fidget 2" );
            CharActions.Add( 68, "Fly Get Hit" );
            CharActions.Add( 69, "Fly Die Backward" );
            CharActions.Add( 70, "Fly Die Forward" );
            CharActions.Add( 71, "Fly Attack (1H Weapon)" );
            CharActions.Add( 72, "Fly Attack (2H Weapon)" );
            CharActions.Add( 73, "Fly Attack (Boomerang)" );
            CharActions.Add( 74, "Fly Get Hit (Shield)" );
            CharActions.Add( 75, "Fly Spell 1" );
            CharActions.Add( 76, "Fly Spell 2" );
            CharActions.Add( 77, "Fly Get Hit" );
            CharActions.Add( 78, "Fly Drinking" );

            // Creatures Actions
            CreatureActions.Add( 0, "Walk Combat" );
            CreatureActions.Add( 1, "Idle Combat" );
            CreatureActions.Add( 2, "Die Backward" );
            CreatureActions.Add( 3, "Die Forward" );
            CreatureActions.Add( 4, "Attack 1" );
            CreatureActions.Add( 5, "Attack 2" );
            CreatureActions.Add( 10, "Get Hit" );
            CreatureActions.Add( 11, "Rummage" );
            CreatureActions.Add( 12, "Spellcast" );
            CreatureActions.Add( 15, "Block" );
            CreatureActions.Add( 19, "Fly" );
            CreatureActions.Add( 22, "Walk" );
            CreatureActions.Add( 23, "Special" );
            CreatureActions.Add( 24, "Run" );
            CreatureActions.Add( 25, "Idle" );
            CreatureActions.Add( 26, "Fidget" );
            CreatureActions.Add( 27, "Roar" );
            CreatureActions.Add( 28, "Peace to Combat" );

            // mounts only animations
            CreatureActions.Add( 29, "Mounted - Walk" );
            CreatureActions.Add( 30, "Mounted - Run" );
            CreatureActions.Add( 31, "Mounted - Idle" );

            // get the actions for the character/equip
            var actions = from act in CharActions
                          orderby act.Key
                          select new
                          {
                              act.Key,
                              Value = act.Value + " (" + act.Key + ")"
                          };

            // change the list of actions to the creatures animations list
            cmbActions.DataSource = actions.ToList();

            // set the actions data to use
            cmbActions.DisplayMember = "Value";
            cmbActions.ValueMember = "Key";

            // allow the form to update
            Application.DoEvents();
        }

        /// <summary>
        /// Initialize the hue selection buttons
        /// </summary>
        private void InitializeColorButtons()
        {
            // get the list of the buttons for hue selection
            IEnumerable<Button> objs = from btn in pagCharacter.Controls.OfType<Button>().Union( pagCreatures.Controls.OfType<Button>() )
                                       where btn.Name.StartsWith("btnColor")
                                       select btn;

            // scan all the buttons we found to set the initial color
            foreach ( Button btn in objs )
            {
                // reset the button
                SetColorButton( btn, 0 );

                // allow the form to update
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Set a hue button to a specific color
        /// </summary>
        /// <param name="btn">Button to reset</param>
        /// <param name="hue">Hue to set</param>
        private void SetColorButton( Button btn, int hue )
        {
            // the selected hue
            Hue selHue = hp.Hues[hue];

            // store the default hue
            btn.Tag = selHue;

            // set the default tooltip text
            ttp.SetToolTip( btn, "Hue Color: " + selHue.ID.ToString() + " - " + selHue.Name );

            // make sure the hue diagram stretch to fill the button
            btn.BackgroundImageLayout = ImageLayout.Stretch;

            // set the hue diagram as bg for the button
            btn.BackgroundImage = hp.Hues[0].HueDiagram;
        }

        /// <summary>
        /// Update the combo with the correct elements
        /// </summary>
        private void InitializeCombos()
        {
            // flag that the combos are now resetting
            comboReset = true;

            // Load the xml
            XDocument xdoc = XDocument.Load( AnimXMLFile );

            // get the list of the combo boxes in the character tab
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                         select cmb;

            // scan all the combos for the character section
            foreach ( ComboBox cmb in objs )
            {
                // update the combo
                UpdateCombo( cmb, ref xdoc );

                // get the draw order combo
                ComboBox cmbDO = (ComboBox)Controls.Find( cmb.Name.Replace( "cmb", "cmbDO" ), true ).FirstOrDefault();

                // is the draw order combo available?
                if ( cmbDO != null )
                {
                    // clear the combo items
                    cmbDO.Items.Clear();

                    // add the sort order index in all combos
                    for ( int i = 1; i <= charEquipSort.Count; i++ )
                        cmbDO.Items.Add( i );

                    // select the default sort order from the list
                    cmbDO.SelectedIndex = charEquipSort.IndexOf( int.Parse( cmb.Tag.ToString() ) );
                }

                // allow the form to update
                Application.DoEvents();
            }

            // update the combos in the creatures section
            UpdateCombo( cmbCreatures, ref xdoc );
            UpdateCombo( cmbEquip, ref xdoc );

            // toggle the combos availability
            UpdateCharacterCombos();

            // toggle the combos availability
            UpdateMultiCombos();

            // flag that the combos reset is complete
            comboReset = false;

            // reset the body selection
            cmbBody.SelectedIndex = 0;
        }

        /// <summary>
        /// Update the combo data query
        /// </summary>
        /// <param name="cmb">Combo to update</param>
        /// <param name="xdoc">XML document link</param>
        private void UpdateCombo( ComboBox cmb, ref XDocument xdoc )
        {
            // initialize the body ID
            int bodyId = -1;

            // do we have a body selected?
            if  ( cmbBody.SelectedValue != null )
            {
                try // we use try catch because at loading the parse won't work
                {
                    // get the main body ID
                    bodyId = int.Parse( cmbBody.SelectedValue.ToString() );
                }
                catch
                { }
            }

            // generic query to initialize the variable
            var items = from itm in xdoc.Root.Descendants( "Item" )
                        select new
                        {
                            name = "",
                            id = 0,
                            displayName = ""
                        };

            // is this the (all) equipment combo?
            if ( cmb == cmbEquip )
            {
                // Search the xml for the mobiles with the current layer ID
                items = from itm in xdoc.Root.Descendants( "Item" )
                        where int.Parse( itm.Attribute( "Type" ).Value ) == 4 || int.Parse( itm.Attribute( "Layer" ).Value ) == -1
                        select new
                        {
                            name = itm.Attribute( "Name" ).Value,
                            id = int.Parse( itm.Attribute( "Id" ).Value ),
                            displayName = int.Parse( itm.Attribute( "Id" ).Value ) != -1 ?
                                itm.Attribute( "Name" ).Value + " (" + itm.Attribute( "Id" ).Value + ")" + ( itm.Attribute( "FemaleOnly" ) != null && bool.Parse( itm.Attribute( "FemaleOnly" ).Value ) ? " (Female)" : "" ) + ( itm.Attribute( "MaleOnly" ) != null && bool.Parse( itm.Attribute( "MaleOnly" ).Value ) ? " (Male)" : "" ) + ( itm.Attribute( "GargoyleOnly" ) != null && bool.Parse( itm.Attribute( "GargoyleOnly" ).Value ) ? " (Gargoyle)" : "" ) :
                                itm.Attribute( "Name" ).Value + ( itm.Attribute( "FemaleOnly" ) != null && bool.Parse( itm.Attribute( "FemaleOnly" ).Value ) ? " (Female)" : "" ) + ( itm.Attribute( "MaleOnly" ) != null && bool.Parse( itm.Attribute( "MaleOnly" ).Value ) ? " (Male)" : "" ) + ( itm.Attribute( "GargoyleOnly" ) != null && bool.Parse( itm.Attribute( "GargoyleOnly" ).Value ) ? " (Gargoyle)" : "" )
                        };
            }

            // does the combobox has a tag? (the tag contains the layer ID)
            else if ( cmb.Tag != null )
            {
                // current layer of the combo
                int layer = int.Parse( cmb.Tag.ToString() );

                // creatures combo
                if ( layer == 999 )
                {
                    // Search the xml for the mobiles with the current layer ID (and we also include mounts)
                    items = from itm in xdoc.Root.Descendants( "Item" )
                            where int.Parse( itm.Attribute( "Layer" ).Value ) == layer || int.Parse( itm.Attribute( "Layer" ).Value ) == 995 || int.Parse( itm.Attribute( "Layer" ).Value ) == 0 || int.Parse( itm.Attribute( "Layer" ).Value ) == -1
                            select new
                            {
                                name = itm.Attribute( "Name" ).Value,
                                id = int.Parse( itm.Attribute( "Id" ).Value ),
                                displayName = int.Parse( itm.Attribute( "Id" ).Value ) != -1 ? itm.Attribute( "Name" ).Value + " (" + itm.Attribute( "Id" ).Value + ")" : itm.Attribute( "Name" ).Value
                            };
                }
                // all equipment
                else if ( layer != 0 && layer != 995 )
                {
                    // get the mobile data from the main list
                    Mobile m = Mobiles.Where( mm => mm.BodyId == bodyId ).FirstOrDefault();

                    // initialize the basig search flags
                    bool garg = false;
                    bool male = false;
                    bool female = false;

                    // if we have a body, we get the correct flag values
                    if ( m != null )
                    {
                        garg = m.GargoyleItem;
                        male = m.MaleOnly;
                        female = m.FemaleOnly;
                    }

                    // all flags are false, so we can get all the items
                    if ( !garg && !male && !female )

                        // Search the xml for the mobiles with the current layer ID
                        items = from itm in xdoc.Root.Descendants( "Item" )
                                where int.Parse( itm.Attribute( "Layer" ).Value ) == layer || int.Parse( itm.Attribute( "Layer" ).Value ) == -1
                                select new
                                {
                                    name = itm.Attribute( "Name" ).Value,
                                    id = int.Parse( itm.Attribute( "Id" ).Value ),
                                    displayName = int.Parse( itm.Attribute( "Id" ).Value ) != -1 ? itm.Attribute( "Name" ).Value + " (" + itm.Attribute( "Id" ).Value + ")" : itm.Attribute( "Name" ).Value
                                };

                    else // Search the xml for the mobiles with the current layer ID (checking for gargoyle/male/female flags)
                        items = from itm in xdoc.Root.Descendants( "Item" )
                                where   int.Parse( itm.Attribute( "Layer" ).Value ) == layer &&
                                        garg == ( itm.Attribute( "GargoyleOnly" ) != null ) &&
                                        ( ( male == ( itm.Attribute( "MaleOnly" ) != null ) && female == ( itm.Attribute( "FemaleOnly" ) != null ) ) || ( itm.Attribute( "MaleOnly" ) == null && itm.Attribute( "FemaleOnly" ) == null ) ) ||
                                        int.Parse( itm.Attribute( "Layer" ).Value ) == -1
                                select new
                                {
                                    name = itm.Attribute( "Name" ).Value,
                                    id = int.Parse( itm.Attribute( "Id" ).Value ),
                                    displayName = int.Parse( itm.Attribute( "Id" ).Value ) != -1 ? itm.Attribute( "Name" ).Value + " (" + itm.Attribute( "Id" ).Value + ")" : itm.Attribute( "Name" ).Value
                                };
                }
                else // only body and mounts
                {
                    // Search the xml for the mobiles with the current layer ID
                    items = from itm in xdoc.Root.Descendants( "Item" )
                            where ( int.Parse( itm.Attribute( "Layer" ).Value ) == layer || int.Parse( itm.Attribute( "Layer" ).Value ) == -1 )
                            select new
                            {
                                name = itm.Attribute( "Name" ).Value,
                                id = int.Parse( itm.Attribute( "Id" ).Value ),
                                displayName = int.Parse( itm.Attribute( "Id" ).Value ) != -1 ? itm.Attribute( "Name" ).Value + " (" + itm.Attribute( "Id" ).Value + ")" : itm.Attribute( "Name" ).Value
                            };
                }

            }

            // did we find any mobile?
            if ( items.Count() > 0 )
            {
                // sort by name (but we keep NONE at the first place)
                items = items.OrderBy( itm => itm.id != -1 ).ThenBy( itm => itm.displayName );

                // set the query result as content for the combobox
                cmb.DataSource = items.ToList();

                // make sure the combo displays the name of the mobile
                cmb.DisplayMember = "displayName";

                // make sure the combo value is the ID of the mobile
                cmb.ValueMember = "Id";
            }
        }

        /// <summary>
        /// Toggle the combos based on if the body is selected, and filter the data of the combos.
        /// </summary>
        /// <param name="resetCombos">Do we have to reset all combos to default too?</param>
        private void UpdateCharacterCombos( bool resetCombos = true )
        {
            // backup of the combo reset status
            bool comboResetBck = comboReset;

            // flag that the combo update has begun
            if ( !comboResetBck )
                comboReset = true;

            // get the list of the combo boxes in the character tab (except body)
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                         where cmb.Name != "cmbBody"
                                         select cmb;

            // initialize the body ID
            int bodyId = -1;

            // do we have a body selected?
            if ( cmbBody.SelectedValue != null )
            {
                try // we use try catch because at loading the parse won't work
                {
                    // get the main body ID
                    bodyId = int.Parse( cmbBody.SelectedValue.ToString() );
                }
                catch
                {
                    return;
                }
            }

            // set the mobile from the main list as current mobile (or create a new one if it's not on the main list)
            Mobile m = Mobiles.Where( mm => mm.BodyId == bodyId ).FirstOrDefault();

            // toggle the mount label based on if there is the mounted walk action for the body
            lblMount.Enabled = m != null && ( !m.GargoyleItem || m.Actions[23] != null );

            // toggle the mount combo based on if there is the mounted walk action for the body
            cmbMount.Enabled = m != null && ( !m.GargoyleItem || m.Actions[23] != null );

            // toggle the mount hue button based on if there is the mounted walk action for the body
            btnColorMount.Enabled = m != null && ( !m.GargoyleItem || m.Actions[23] != null );

            // scan all the combos for the character section
            foreach ( ComboBox cmb in objs )
            {
                // do nothing with the mount
                if ( cmb == cmbMount )
                    continue;

                // reset the combo selection
                if ( resetCombos && cmb.SelectedIndex != -1 )
                    cmb.SelectedIndex = 0;

                // flag that indicates if the controls need to be enabled/disabled
                bool enable = ( bodyId != -1 && cmb.Items.Count > 1 );

                // toggle the combo
                cmb.Enabled = enable;

                // get the label of this combo
                Label lbl = (Label)Controls.Find( cmb.Name.Replace( "cmb", "lbl" ), true ).FirstOrDefault();

                // toggle the label
                lbl.Enabled = enable;

                // get the hue button of this combo
                Button btn = (Button)Controls.Find( cmb.Name.Replace( "cmb", "btnColor" ), true ).FirstOrDefault();

                // toggle the color button
                btn.Enabled = enable;

                // get the draw order combo
                ComboBox cmbDO = (ComboBox)Controls.Find( cmb.Name.Replace( "cmb", "cmbDO" ), true ).FirstOrDefault();

                // toggle the draw order combo
                if ( cmbDO != null )
                    cmbDO.Enabled = enable;
            }

            // flag that the combo update has ended
            if ( !comboResetBck )
                comboReset = false;
        }

        /// <summary>
        /// Update the combo equipment list based on the selected body
        /// </summary>
        private void ComboUpdateGarg()
        {
            // backup of the combo reset status
            bool comboResetBck = comboReset;

            // flag that the combo update has begun
            if ( !comboResetBck )
                comboReset = true;

            // Load the xml
            XDocument xdoc = XDocument.Load( AnimXMLFile );

            // get the list of the combo boxes in the character tab (except body)
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                         where cmb.Name != "cmbBody"
                                         select cmb;

            // scan all the combos for the character section
            foreach ( ComboBox cmb in objs )
            {
                // update the combo
                UpdateCombo( cmb, ref xdoc );
            }

            // flag that the combo update has ended
            if ( !comboResetBck )
                comboReset = false;
        }

        /// <summary>
        /// Update the multi list combo
        /// </summary>
        private void UpdateMultiCombos()
        {
            // Create the multi data source
            var items = from itm in multiCache
                        select new
                        {
                            itm.ID,
                            displayName = itm.ID == -1 ? "None" : itm.Name != "" ? itm.Name + " (" + itm.ID + ")" : String.Format( "{0:0000}", itm.ID )
                        };

            // set the multi combo data source
            cmbMulti.DataSource = items.ToList();

            // set the actions data to use
            cmbMulti.DisplayMember = "displayName";
            cmbMulti.ValueMember = "ID";
        }

        /// <summary>
        /// Remove the check from all the checkbox except the one clicked
        /// </summary>
        /// <param name="exclude">Checkbox to keep checked</param>
        private void UpdateChecks( CheckBox exclude )
        {
            // scan the checkboxes
            foreach ( Control obj in pnlDirections.Controls )
            {
                // is the component a checkbox?
                if ( obj.GetType().Equals( typeof( CheckBox ) ) )
                {
                    // convert the object to checkbox
                    CheckBox chk = (CheckBox)obj;

                    // is this the checkbox to exclude?
                    if ( chk != exclude )
                    {
                        // uncheck the box
                        chk.Checked = false;
                    }
                }
            }
        }

        /// <summary>
        /// Reset all the lists and combos
        /// </summary>
        private void InitializeLists()
        {
            // clear the current mobiles list
            Mobiles.Clear();

            // scan all the form objects
            foreach ( Control obj in this.Controls )
            {
                // is the component a combobox?
                if ( obj.GetType().Equals( typeof( ComboBox ) ) )
                {
                    // convert the object to combobox
                    ComboBox cmb = (ComboBox)obj;

                    // does the combobox has a tag? (the tag contains the layer ID)
                    if ( cmb.Tag != null )
                    {
                        // clear the combobox
                        cmb.Items.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Load a mobile in the list
        /// </summary>
        /// <param name="uopFileID">Index of the AnimationFrame file. Example: 1 = AnimationFrame1.uop</param>
        /// <param name="uopFileBlock">Index of the block inside the file</param>
        /// <param name="uopFileIndex">Index of the file INSIDE the uop</param>
        /// <param name="xdoc">opened XML reference</param>
        /// <param name="news">counter for new mobiles</param>
        private Mobile LoadMobileAnimation( int uopFileID, int uopFileBlock, int uopFileIndex, ref XDocument xdoc, ref int news )
        {
            // select the main element of the xml
            XElement mobs = xdoc.Descendants("Items").FirstOrDefault();

            // current UOP animation file
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "AnimationFrame" + ( uopFileID + 1 ) + ".uop" ) );

            // initialize the animation variable
            UOAnimation anim;

            // load the file memory stream data
            using ( MemoryStream stream = new MemoryStream( UOP.Blocks[uopFileBlock].Files[uopFileIndex].Unpack() ) )
            {
                // get the animation data
                anim = new UOAnimation( stream, "AnimationFrame" + ( uopFileID + 1 ) + ".uop", uopFileBlock, uopFileIndex );
            }

            // do we have a valid animation?
            if ( anim != null )
            {
                // Determine if there is already a mobile with this body ID
                Mobile m = Mobiles.Find(Mobile => Mobile.BodyId == anim.BodyID );

                // get the index in the list for the mobile
                int idx = Mobiles.IndexOf( m );

                // new mobile?
                if ( m == null )
                {
                    // create the new mobile
                    m = new Mobile( (int)anim.BodyID );
                }

                // add the animation for the action
                m.Actions[anim.ActionID] = anim;

                // Search the xml for the mobiles with the current body ID
                IEnumerable<XElement> items = from itm in xdoc.Root.Descendants( "Item" )
                                              where itm.Attribute( "Id" ).Value == m.BodyId.ToString()
                                              select itm;

                // did we find the mobile?
                if ( items.Count() == 0 )
                {
                    // create the node with the uop file data
                    XElement elm = new XElement( "Item",
                                                    new XAttribute( "Name", "" ),
                                                    new XAttribute( "Id", m.BodyId.ToString() ),
                                                    new XAttribute( "Type", "0" ),
                                                    new XAttribute( "Layer", "999" )
                                               );

                    // create the animation data
                    XElement animElm = new XElement( "Animation",
                                                    new XAttribute( "Id", anim.ActionID.ToString() ),
                                                    new XAttribute( "UOP", "AnimationFrame" + ( uopFileID + 1 ) + ".uop" ),
                                                    new XAttribute( "Block", uopFileBlock ),
                                                    new XAttribute( "File", uopFileIndex )
                                               );

                    // add the animation data to the mobile
                    elm.Add( animElm );

                    // add the new mobile to the xml
                    mobs.Add( elm );

                    // increase the new mobiles counter
                    news++;
                }
                else // update the xml
                {
                    // select the XML element
                    XElement elm = items.First();

                    // do we have a layer specified?
                    if ( elm.Attribute( "Layer" ) != null )
                    {
                        // fix the missing layer
                        if ( elm.Attribute( "Layer" ).Value == string.Empty )
                            elm.Attribute( "Layer" ).Value = "999";

                        // we load that value in the mobiles list
                        m.Layer = (Mobile.Layers)int.Parse( elm.Attribute( "Layer" ).Value );
                    }

                    // do we have the type?
                    if ( elm.Attribute( "Type" ) != null )
                    {
                        // fix the missing type
                        if ( elm.Attribute( "Type" ).Value == string.Empty )
                            elm.Attribute( "Type" ).Value = "0";

                        // we load that value in the mobiles list
                        m.MobileType = (Mobile.MobileTypes)int.Parse( elm.Attribute( "Type" ).Value );
                    }

                    // do we have the gargoyle ONLY flag?
                    if ( elm.Attribute( "GargoyleOnly" ) != null )
                    {
                        // flag this item as gargoyles only
                        m.GargoyleItem = bool.Parse( elm.Attribute( "GargoyleOnly" ).Value );
                    }

                    // do we have the male ONLY flag?
                    if ( elm.Attribute( "MaleOnly" ) != null )
                    {
                        // flag this item as gargoyles only
                        m.MaleOnly = bool.Parse( elm.Attribute( "MaleOnly" ).Value );
                    }

                    // do we have the female ONLY flag?
                    if ( elm.Attribute( "FemaleOnly" ) != null )
                    {
                        // flag this item as gargoyles only
                        m.FemaleOnly = bool.Parse( elm.Attribute( "FemaleOnly" ).Value );
                    }

                    // if we have a name specified, we load that value in the mobiles list
                    if ( elm.Attribute( "Name" ) != null )
                        m.Name = elm.Attribute( "Name" ).Value;

                    // check if the animation data has already been saved
                    XElement animElm = elm.Descendants("Animation").Where( itm => int.Parse( itm.Attribute("Id").Value ) == anim.ActionID ).FirstOrDefault();

                    // is the animation data missing?
                    if ( animElm == null )
                    {
                        // create the animation data
                        animElm = new XElement( "Animation",
                                                new XAttribute( "Id", anim.ActionID.ToString() ),
                                                new XAttribute( "UOP", "AnimationFrame" + ( uopFileID + 1 ) + ".uop" ),
                                                new XAttribute( "Block", uopFileBlock ),
                                                new XAttribute( "File", uopFileIndex )
                                            );

                        // add the animation data to the mobile
                        elm.Add( animElm );
                    }
                    else // update the data of the animation
                    {
                        animElm.Attribute( "Id" ).Value = anim.ActionID.ToString();
                        animElm.Attribute( "UOP" ).Value = "AnimationFrame" + ( uopFileID + 1 ) + ".uop";
                        animElm.Attribute( "Block" ).Value = uopFileBlock.ToString();
                        animElm.Attribute( "File" ).Value = uopFileIndex.ToString();
                    }

                    // if there are multiple nodes with the same ID, we remove them all and insert only 1 correctly
                    if ( items.Count() > 1 )
                    {
                        // delete all nodes
                        items.Remove();

                        // re-add the node
                        mobs.Add( elm );
                    }
                }

                // if this is a new mobile we just add it to the list
                if ( idx == -1 )
                {
                    Mobiles.Add( m );
                }
                else // existing mobile
                {
                    // update the mobile on the list
                    Mobiles[idx] = m;
                }

                return m;
            }
            else
            {
                Debug.WriteLine( "Animation loading failed! UOP:" + uopFileID + ", BLOCK: " + uopFileBlock + ", FILE: " + uopFileIndex );
            }

            return null;
        }


        /// <summary>
        /// Load a specific mobile animation
        /// </summary>
        /// <param name="uopFile">UOP file name containing this animation (DO NOT include the game path)</param>
        /// <param name="uopFileBlock">Block ID inside the UOP file of this animation</param>
        /// <param name="uopFileIndex">File ID inside the UOP file block of this animation</param>
        /// <param name="mob">Mobile in which we'll add the animation</param>
        /// <param name="animId">ID of the animation to load</param>
        /// <returns></returns>
        private void LoadMobileAnimation( string uopFile, int uopFileBlock, int uopFileIndex, ref Mobile mob, int animId )
        {
            // does the animation already exist?
            if ( mob.Actions[animId] != null )
                return;

            // current UOP animation file
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, uopFile ) );

            // load the file memory stream data
            using ( MemoryStream stream = new MemoryStream( UOP.Blocks[uopFileBlock].Files[uopFileIndex].Unpack() ) )
            {
                // get the animation data
                mob.Actions[animId] = new UOAnimation( stream, uopFile, uopFileBlock, uopFileIndex, true );
            }
        }

        /// <summary>
        /// Sort the xml by body ID
        /// </summary>
        /// <param name="xdoc">current XML document</param>
        private void SortXML( ref XDocument xdoc )
        {
            // sort the XML by ID
            IEnumerable<XElement> sorted = xdoc.Root.Descendants( "Item" ).OrderBy( itm => int.Parse( itm.Attribute( "Layer" ).Value ) ).ThenBy( itm => int.Parse( itm.Attribute( "Type" ).Value ) ).ThenBy( itm => int.Parse( itm.Attribute( "Id" ).Value ) ).ToArray();

            // clear the xml
            xdoc.Root.Descendants( "Item" ).Remove();

            // add the sorted mobiles list
            xdoc.Root.Element( "Items" ).Add( sorted );

            // get all the elements
            XElement[] main = xdoc.Root.Descendants( "Item" ).ToArray();

            // scan all the elements
            foreach ( XElement elm in main )
            {
                // sort the animations
                sorted = elm.Descendants( "Animation" ).OrderBy( itm => int.Parse( itm.Attribute( "Id" ).Value ) ).ToArray();

                // remove all animations
                elm.Descendants( "Animation" ).Remove();

                // add the sorted animations
                elm.Add( sorted );
            }
        }

        /// <summary>
        /// Clean the XML from non existent mobiles
        /// </summary>
        /// <param name="xdoc">current XML document</param>
        private void CleanXML( ref XDocument xdoc )
        {
            // find all the XML mobiles that do not exist in the UOP files
            IEnumerable<XElement> missing = xdoc.Root.Descendants( "Item" ).Where( itm => !Mobiles.Exists( m => m.BodyId == int.Parse( itm.Attribute( "Id" ).Value ) )).ToList();

            // delete all the selected nodes
            missing.Remove();
        }

        /// <summary>
        /// Categorize the items with the wrong type flag and add the GARGOYLES ONLY flag if needed
        /// </summary>
        /// <param name="xdoc">current XML document</param>
        private void CategorizeMobiles( ref XDocument xdoc )
        {
            // Search the xml for the uncategorized gargoyle items
            IEnumerable<XElement> items = from itm in xdoc.Root.Descendants( "Item" )
                                          where itm.Attribute( "Type" ).Value != "3"
                                          select itm;

            // scan all the NON-player mobiles/items
            foreach ( XElement elm in items )
            {
                // get the mobile from the list with this body ID
                Mobile mob = Mobiles.Where( m => m.BodyId == int.Parse( elm.Attribute( "Id" ).Value ) ).FirstOrDefault();

                // is this NOT an equipment item (and it should be)?
                if ( mob.Actions[42] != null )
                {
                    // change the attribute value
                    elm.Attribute( "Type" ).Value = "4";

                    // update the type
                    mob.MobileType = Mobile.MobileTypes.Equipment;

                    // is there the GARGOYLES ONLY flag and shouldn't be there?
                    if ( elm.Attribute( "GargoyleOnly" ) != null && mob.Actions[60] == null )
                    {
                        // remove the attribute
                        elm.Attribute( "GargoyleOnly" ).Remove();
                    }

                    // does this item has the "fly" animation?
                    if ( mob.Actions[60] != null )
                    {
                        // add the GARGOYLES ONLY flag to the XML
                        elm.SetAttributeValue( "GargoyleOnly", "true" );

                        // flag this item as GARGOYLES ONLY
                        mob.GargoyleItem = true;
                    }
                }

                // is this mobile a mount?
                else if ( ( mob.Actions[29] != null && mob.Actions[30] != null && mob.Actions[31] != null ) && int.Parse( elm.Attribute( "Type" ).Value ) != 4 )
                {
                    // change the attribute value
                    elm.Attribute( "Type" ).Value = "5";
                    elm.Attribute( "Layer" ).Value = "995";
                }
            }
        }

        /// <summary>
        /// Get the current selected direction (from the checkboxes)
        /// </summary>
        /// <param name="mirror">does the image has to be mirrored?</param>
        /// <returns>the selected direction</returns>
        private int GetCurrentDirection( ref bool mirror )
        {
            // determine the direction from the checkboxes

            if ( btnDirSouthEast.Checked )
                return 0;

            else if ( btnDirSouth.Checked )
                return 1;

            else if ( btnDirSouthWest.Checked )
                return 2;

            else if ( btnDirWest.Checked )
                return 3;

            else if ( btnDirNorthWest.Checked )
                return 4;

            else if ( btnDirNorth.Checked )
            {
                mirror = true;
                return 3;
            }

            else if ( btnDirNorthEast.Checked )
            {
                mirror = true;
                return 2;
            }

            else if ( btnDirEast.Checked )
            {
                mirror = true;
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Get the current frame image
        /// </summary>
        /// <param name="firstFrameIndex">reference to the first frame of this frameset</param>
        /// <param name="maxFrames">max amount of frames of the current frameset</param>
        /// <returns>the current frame image</returns>
        public Image GetCurrentFrameImage( ref int firstFrameIndex, ref int maxFrames )
        {
            // initialize the image to return
            Image img = null;

            // we do nothing if there is no current mobile
            if ( CurrentMobile == null )
            {
                // reset the ID of the frame to play
                LastFramePlayed = 0;

                // reset the ID of the first frame
                firstFrameIndex = 0;

                // reset the max frames number
                maxFrames = -1;

                return img;
            }

            // get the active animation
            UOAnimation anim = CurrentMobile.Actions[m_SelectedAction];

            // if the current animation is not available we do nothing
            if ( anim == null )
            {
                // reset the ID of the frame to play
                LastFramePlayed = 0;

                // reset the ID of the first frame
                firstFrameIndex = 0;

                // reset the max frames number
                maxFrames = -1;

                return img;
            }

            // update the max amount of frames
            maxFrames = anim.FramesPerDirection;

            // do we have to mirror the image?
            bool mirror = false;

            // calculate the first frame for the current direction
            firstFrameIndex = anim.FramesPerDirection * GetCurrentDirection( ref mirror );

            // make sure we don't try to play frames out of range
            if ( LastFramePlayed >= maxFrames )
                LastFramePlayed = 0;

            // get the image for the current frame
            Bitmap frameImage = anim.GetFrameImage( firstFrameIndex + LastFramePlayed );

            // make sure the image exist
            if ( frameImage == null )
                return img;

            // reset the preview image at the first frame
            img = (Bitmap)CreateFrame( firstFrameIndex + LastFramePlayed, ref anim ).Clone();

            // do we have to mirror the image?
            if ( mirror )
            {
                // mirror the image
                img.RotateFlip( RotateFlipType.RotateNoneFlipX );
            }

            return img;
        }

        /// <summary>
        /// Get the current frame image
        /// </summary>
        /// <param name="firstFrameIndex">reference to the first frame of this frameset</param>
        /// <returns>the current frame image</returns>
        public Image GetCurrentFrameImage( ref int firstFrameIndex )
        {
            // unused here
            int maxFrames = -1;

            return GetCurrentFrameImage( ref firstFrameIndex, ref maxFrames );
        }

        /// <summary>
        /// Create the current paperdoll image
        /// </summary>
        private Image GetCurrentPaperdollImage()
        {
            // initialize the image to return
            Image img = null;

            // character page
            if ( tabViewer.SelectedTab == pagCharacter )
            {
                // initialize the body ID
                int bodyId = -1;

                // do we have a body selected?
                if ( cmbBody.SelectedValue != null )
                {
                    try // we use try catch because at loading the parse won't work
                    {
                        // get the main body ID
                        bodyId = int.Parse( cmbBody.SelectedValue.ToString() );
                    }
                    catch
                    {
                        return img;
                    }
                }

                // no body? we can get out
                if ( bodyId == -1 )
                    return img;

                // get the body data
                Mobile m = Mobiles.Where( mm => mm.BodyId == bodyId ).FirstOrDefault();

                // select the correct items cache to use (based on if the body is male or female)
                UOAnimation animToPickFrom = m.FemaleOnly ? femalePaperdollCache : malePaperdollCache;

                // initialize the image
                img = new Bitmap( animToPickFrom.CellWidth, animToPickFrom.CellHeight + 100 );

                // get the list of the combo boxes in the character tab (only the one with something selected)
                IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                             where int.Parse( cmb.SelectedValue.ToString() ) != -1
                                             orderby charEquipCustomSort.IndexOf( int.Parse( cmb.Tag.ToString() ) )
                                             select cmb;

                // scan all equipment
                foreach ( ComboBox cmb in objs )
                {
                    // get the body ID for the current layer
                    int itemId = int.Parse( cmb.SelectedValue.ToString() );

                    // get the hue selection button for this layer
                    Button btn = (Button)Controls.Find( cmb.Name.Replace( "cmb", "btnColor" ), true ).FirstOrDefault();

                    // the selected hue for the body
                    Hue h = (Hue)btn.Tag;

                    // draw the item
                    DrawPaperdollItem( itemId, h, animToPickFrom, ref img );

                    // allow the form to update
                    Application.DoEvents();
                }
            }
            // creatures page
            else if ( tabViewer.SelectedTab == pagCreatures )
            {
                // make sure we have a creature or equipment selected
                if ( cmbCreatures.SelectedIndex > 0 || cmbEquip.SelectedIndex > 0 )
                {
                    // get the active combo
                    ComboBox cmb = cmbCreatures.SelectedIndex > 0 ? cmbCreatures : cmbEquip;

                    // get the item ID for the paperdoll
                    int itemId = int.Parse( cmb.SelectedValue.ToString() );

                    // select the correct items cache to use (based on if the item is male or female)
                    UOAnimation animToPickFrom = CurrentMobile.FemaleOnly ? femalePaperdollCache : malePaperdollCache;

                    // initialize the image
                    img = new Bitmap( animToPickFrom.CellWidth, animToPickFrom.CellHeight + 100 );

                    // get the hue selection button for this layer
                    Button btn = (Button)Controls.Find( cmb.Name.Replace( "cmb", "btnColor" ), true ).FirstOrDefault();

                    // the selected hue for the body
                    Hue h = (Hue)btn.Tag;

                    // draw the item
                    DrawPaperdollItem( itemId, h, animToPickFrom, ref img );
                }
            }

            return img;
        }

        /// <summary>
        /// Draw an image on the paperdoll
        /// </summary>
        /// <param name="itemId">item to draw</param>
        /// <param name="h">hue to use</param>
        /// <param name="animToPickFrom">list of images to pick from</param>
        /// <param name="img">paperdoll image to draw into</param>
        private void DrawPaperdollItem( int itemId, Hue h, UOAnimation animToPickFrom, ref Image img )
        {
            // current frame data
            FrameEntry f = animToPickFrom.Frames.Where( ff => ff.Frame == itemId ).FirstOrDefault();

            // do we have the frame?
            if ( f == null )
                return;

            // get the body image
            Bitmap itemImage = (Bitmap)animToPickFrom.GetFrameImage( itemId, false, false );

            // apply the body hue if it's NOt the default one
            if ( h.ID != 0 )
            {
                // create a temporary direct bitmap to apply the hue
                using ( DirectBitmap db = new DirectBitmap( itemImage ) )
                    itemImage = (Bitmap)db.ApplyHue( h.HueDiagram ).Clone();
            }

            // calculate the top-left corner for the item in the canvas area
            int xDist = ( img.Width / 2 ) + f.InitCoordsX;
            int yDist = ( img.Height / 2 ) + f.InitCoordsY;

            // draw the body
            using ( Graphics g = Graphics.FromImage( img ) )
                g.DrawImage( itemImage, xDist, yDist );
        }

        /// <summary>
        /// Reset the current image preview
        /// </summary>
        private void ResetCurrentImage()
        {
            // do nothing during the combo reset
            if ( comboReset )
                return;

            // reset the canvas
            SetImage( null );

            // do we have to show the paperdoll image?
            if ( chkPaperdoll.Checked )
            {
                // update the current frame label
                lblCurrFramePlaying.Text = "";

                // reset the last frame ID
                LastFramePlayed = 0;

                // get the paperdoll image
                SetImage( GetCurrentPaperdollImage() );
            }
            else // show animations
            {
                // reset the last frame ID
                LastFramePlayed = 0;

                //unused
                int f = 0;

                // current amount of frames for the animation
                int maxFrames = 0;

                // reset the image to the first frame of the current frameset
                SetImage( GetCurrentFrameImage( ref f, ref maxFrames ) );

                // hide the label if there are no frames to show
                lblCurrFramePlaying.Visible = maxFrames != -1;

                // update the current frame label
                lblCurrFramePlaying.Text = "Frame: " + ( LastFramePlayed + 1 ) + "/" + maxFrames;
            }

            // center the preview on the panel
            CenterImage();
        }

        /// <summary>
        /// Draw the frame inside the animation size area
        /// </summary>
        /// <param name="frameIndex">ID of the frame to draw</param>
        /// <param name="anim">Animation to which the frame belongs to</param>
        /// <returns>frame image</returns>
        private Bitmap CreateFrame( int frameIndex, ref UOAnimation anim )
        {
            // initialize the bg image
            Bitmap bg = null;

            // is the character section active?
            if ( tabViewer.SelectedTab == pagCharacter )
            {
                // current frame data
                FrameEntry f = anim.Frames[frameIndex];

                // create the bg image with a fixed size
                bg = new Bitmap( char_Size.Width, char_Size.Height );

                // start the editing process
                using ( Graphics g = Graphics.FromImage( bg ) )
                {
                    // calculate the top-left corner for the frame in the animation area
                    int xDist = ( char_Size.Width - f.Width ) / 2;
                    int yDist = ( char_Size.Height - f.Height ) / 2;

                    // draw the frame properly positioned
                    Point drawPoint = new Point( 0, 0 );

                    // get the frame image
                    Bitmap img = anim.GetFrameImage( frameIndex );

                    // if the image is smaller than the default size, we change the drawing point
                    if ( img.Width < char_Size.Width || img.Height < char_Size.Height )
                        drawPoint = new Point( xDist, yDist );

                    // draw the frame
                    g.DrawImage( anim.GetFrameImage( frameIndex ), drawPoint );
                }
            }
            else // creatures
            {
                // create the bg image with the animation size
                bg = new Bitmap( anim.CellWidth, anim.CellHeight );

                // start the editing process
                using ( Graphics g = Graphics.FromImage( bg ) )
                {
                    // draw the frame properly positioned
                    Point drawPoint = new Point( Math.Abs( (int)anim.StartX - (int)anim.Frames[frameIndex].InitCoordsX ), Math.Abs( (int)anim.StartY - (int)anim.Frames[frameIndex].InitCoordsY ) );

                    // get the frame image
                    Bitmap img = anim.GetFrameImage( frameIndex );

                    // draw the frame
                    g.DrawImage( img, drawPoint );
                }

                // initialize the selected hue
                Hue h = GetCreatureHue();

                // is this any color but default?
                if ( h.ID != 0 )
                {
                    // create a temporary image to apply the hue
                    using ( DirectBitmap db = new DirectBitmap( bg ) )
                    {
                        // apply the hue
                        bg = db.ApplyHue( h.HueDiagram );
                    }
                }
            }

            return bg;
        }

        /// <summary>
        /// Get the hue to use for the creatures tab
        /// </summary>
        private Hue GetCreatureHue()
        {
            // initialize the selected hue
            Hue h;

            // is this a creature?
            if ( CurrentMobile.Layer == Mobile.Layers.Mobile )
            {
                // has this been loaded from a VD file?
                if ( CurrentMobile.VDOriginal )
                {
                    // get the selected creature hue
                    h = (Hue)btnColorVD.Tag;
                }
                else // creature from UOP
                {
                    // get the selected VD creature hue
                    h = (Hue)btnColorCreatures.Tag;
                }
            }
            else // equip
            {
                // get the selected equip hue
                h = (Hue)btnColorEquip.Tag;
            }

            return h;
        }

        /// <summary>
        /// Merge all the frames of the character section to create a single image
        /// </summary>
        /// <param name="allAnim">Do we have to merge ALL the animations or just the current one?</param>
        private void MergeFrames( bool allAnim = false )
        {
            // do nothing during the combo reset
            if ( comboReset )
                return;

            // initialize the body ID
            int bodyId = -1;

            // do we have a body selected?
            if ( cmbBody.SelectedValue != null )
            {
                try // we use try catch because at loading the parse won't work
                {
                    // get the main body ID
                    bodyId = int.Parse( cmbBody.SelectedValue.ToString() );
                }
                catch
                {
                    return;
                }
            }

            // reset the current mobile (if it's not the base body)
            if ( CurrentMobile != null && CurrentMobile.BodyId != bodyId )
            {
                // clear the current mobile
                CurrentMobile.Dispose();
                CurrentMobile = null;
            }

            // do we have a main body?
            if ( bodyId == -1 )
                return;

            // create a clean new mobile for current (if is missing)
            if ( CurrentMobile == null )
                CurrentMobile = new Mobile( bodyId );

            // get the list of the combo boxes in the character tab (only the one with something selected)
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                         where int.Parse( cmb.SelectedValue.ToString() ) != -1
                                         orderby charEquipCustomSort.IndexOf( int.Parse( cmb.Tag.ToString() ) )
                                         select cmb;

            // initialize a list of the current animation of all the selected items
            List<Mobile> equip = new List<Mobile>();

            // initialize the mount variable
            Mobile mount = null;

            // scan all equipment
            foreach ( ComboBox cmb in objs )
            {
                // get the body ID for the current layer
                int itemId = int.Parse( cmb.SelectedValue.ToString() );

                // get the mobile data from the main list
                Mobile m = Mobiles.Where( mm => mm.BodyId == itemId ).FirstOrDefault();

                // if this is the mount, we store it
                if ( cmb == cmbMount )
                    mount = m;

                // add the animation for the current action of the item to the list (except for body which is the base)
                if ( cmb != cmbBody && cmb != cmbMount )
                    equip.Add( m );
            }

            // merge the current animation
            if ( !allAnim )
                MergeAnimation( bodyId, m_SelectedAction, ref equip, mount );

            else // merge all animations
            {
                // get the common actions for all the animations
                Dictionary<int, string> common = CommonEquipActions();

                // parse all the actions
                foreach ( KeyValuePair<int, string> entry in common )
                {
                    // merge the action frames
                    MergeAnimation( bodyId, entry.Key, ref equip, mount );

                    // allow the form to update
                    Application.DoEvents();
                }
            }
        }

        /// <summary>
        /// Merge the action frames
        /// </summary>
        /// <param name="bodyId">main body ID</param>
        /// <param name="actionId">action to merge</param>
        /// <param name="mount">mobile to use for the mount (if selected)</param>
        /// <param name="equip">list of the equipment to apply</param>
        private void MergeAnimation( int bodyId, int actionId, ref List<Mobile> equip, Mobile mount = null )
        {
            // is the action missing?
            if ( CurrentMobile.Actions[actionId] == null )
            {
                // get the body mobile data
                Mobile mob = Mobiles.Where( mm => mm.BodyId == bodyId ).FirstOrDefault();

                // if the mob is null, something went wrong and we better get out...
                if ( mob == null )
                    return;

                // get the body animation
                UOAnimation bodyAnim = mob.Actions[actionId];

                // if the animation is missing, something went wrong and is better to get out
                if ( bodyAnim == null )
                    return;

                // use the body as base for all the frames
                CurrentMobile.Actions[actionId] = bodyAnim.Clone();

                // generate the images for the frames
                CurrentMobile.Actions[actionId].GenerateImages();

                // backup the base frames for the merge
                foreach ( FrameEntry frame in CurrentMobile.Actions[actionId].Frames )
                {
                    // store a copy of the original image
                    if ( frame.OriginalImage == null )
                        frame.OriginalImage = new DirectBitmap( (Bitmap)frame.Image.Bitmap );
                }

                // remove the frames form the memory (for the main animation)
                bodyAnim.DisposeFrames();
            }

            // base body animation
            UOAnimation bas = CurrentMobile.Actions[actionId];

            // scan all frames
            for ( int i = 0; i < bas.Frames.Count; i++ )
            {
                // the selected hue for the body
                Hue h = (Hue)btnColorBody.Tag;

                // get the base image
                Bitmap main = new Bitmap( char_Size.Width, char_Size.Height );

                // current frame data (body)
                FrameEntry f = bas.Frames[i];

                // additional space we need in order to draw the image properly (based on the frame height)
                int shiftY = (int)( bas.CellHeight / 2.5f );

                // calculate the top-left corner for the frame in the animation area
                int xDist = ( char_Size.Width / 2 ) + f.InitCoordsX;
                int yDist = ( char_Size.Height / 2 ) + shiftY + f.InitCoordsY;

                // initialize the edit tool on the image
                using ( Graphics g = Graphics.FromImage( main ) )
                {
                    // clear the image background
                    g.Clear( Color.Transparent );

                    // draw the mount first
                    if ( mount != null )
                        AddLayer( i, mount.Actions[GetMountActionID( mount, actionId )], (Hue)btnColorMount.Tag, shiftY, ref main );

                    // get the base image (body)
                    // NOTE: getting the frame before we add the mount destroys the image when it's colored for some reason...
                    Bitmap core = bas.GetFrameImage( i, true );

                    // apply the body hue if it's NOt the default one
                    if ( h.ID != 0 )
                    {
                        // create a temporary direct bitmap to apply the hue
                        using ( DirectBitmap db = new DirectBitmap( core ) )
                            core = (Bitmap)db.ApplyHue( h.HueDiagram ).Clone();
                    }

                    // draw the body
                    g.DrawImage( core, xDist, yDist );
                }

                // debug rect
                //g.DrawRectangle( new Pen( Color.Red ), new Rectangle( xDist, yDist, core.Width, core.Height ) );

                // overlap every single equipment to create a single image for this frame
                foreach ( Mobile m in equip )
                {
                    // skip missing mobiles
                    if ( m == null )
                        continue;

                    // make sure the animation is available
                    if ( m.Actions[actionId] == null )
                        continue;

                    // get the combobox related to this object
                    IEnumerable<ComboBox> cmbs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                                 where cmb.Tag.ToString() == ( (int)m.Layer ).ToString()
                                                 select cmb;

                    // get the hue selection button for this layer
                    Button btn = (Button)Controls.Find( cmbs.FirstOrDefault().Name.Replace( "cmb", "btnColor" ), true ).FirstOrDefault();

                    // draw the equipment in this layer
                    AddLayer( i, m.Actions[actionId], (Hue)btn.Tag, shiftY, ref main );
                }

                // save the image for this frame
                bas.Frames[i].Image = new DirectBitmap( main );
            }
        }

        /// <summary>
        /// Add an image over the current one on the active animation
        /// </summary>
        /// <param name="frameIndex">ID of the frame to draw</param>
        /// <param name="anim">Animation to draw</param>
        /// <param name="h">Hue to use for the item</param>
        /// <param name="shiftY">Extra shifting we need in order to draw the animation properly</param>
        /// <param name="main">Current image we're creating</param>
        /// <param name="isMount">Is this the mount?</param>
        private void AddLayer( int frameIndex, UOAnimation anim, Hue h, int shiftY, ref Bitmap main )
        {
            // if the frame index is out of range, better to get out
            if ( frameIndex >= anim.Frames.Count )
                return;

            // current frame data (item)
            FrameEntry f = anim.Frames[frameIndex];

            // calculate the top-left corner for the frame in the animation area
            int xDist = ( char_Size.Width / 2 ) + f.InitCoordsX;
            int yDist = ( char_Size.Height / 2 ) + shiftY + f.InitCoordsY;

            // draw the frame properly positioned
            Point drawPoint = new Point( xDist, yDist );

            // get the frame image
            Bitmap frame = anim.GetFrameImage( frameIndex );

            // apply the item hue if it's NOt the default one
            if ( h.ID != 0 )
            {
                // create a temporary direct bitmap
                using ( DirectBitmap db = new DirectBitmap( frame ) )
                    frame = db.ApplyHue( h.HueDiagram );
            }

            // debug rect
            //g.DrawRectangle( new Pen( Color.Green ), new Rectangle( xDist, yDist, frame.Width, frame.Height ) );

            // draw the frame
            using ( Graphics g = Graphics.FromImage( main ) )
            {
                g.DrawImage( frame, drawPoint );
            }

            // make sure we don't keep the frames in the main list
            //anim.DisposeFrames();

            // allow the form to update
            Application.DoEvents();
        }

        /// <summary>
        /// Update the list of actions available
        /// </summary>
        private void UpdateActionsList( ref ComboBox curr )
        {
            // get the current tab page
            TabPage current = tabViewer.SelectedTab;

            // is the creatures equip list?
            if ( curr == cmbEquip || CurrentMobile.MobileType == Mobile.MobileTypes.Human )
            {
                // get the actions for the creature
                var actions = from act in CharActions
                              where CurrentMobile.Actions[act.Key] != null
                              orderby act.Key
                              select new
                              {
                                  act.Key,
                                  Value = act.Value + " (" + act.Key + ")"
                              };

                // change the list of actions to the character animations list
                cmbActions.DataSource = actions.ToList();
            }

            // is the creatures page selected?
            else if ( current == pagCreatures )
            {
                // get the actions for the creature
                var actions = from act in CreatureActions
                              where CurrentMobile.Actions[act.Key] != null
                              orderby act.Key
                              select new
                              {
                                  act.Key,
                                  Value = ( ( act.Key == 29 || act.Key == 30 || act.Key == 31 ) && CurrentMobile.MobileType != Mobile.MobileTypes.Mount ? "Boss Special " + ( act.Key - 28 ) : act.Value ) + " (" + act.Key + ")"
                              };

                // change the list of actions to the creatures animations list
                cmbActions.DataSource = actions.ToList();
            }
            // is the character page selected?
            else if ( current == pagCharacter )
            {
                // backup the current selected action
                object selItem = cmbActions.SelectedItem;

                // initialize the body ID
                int bodyId = -1;

                try // we use try catch because at loading the parse won't work
                {
                    // get the main body ID
                    bodyId = int.Parse( cmbBody.SelectedValue.ToString() );
                }
                catch
                {
                    return;
                }

                // get the actions for the creature
                var actions = from act in CommonEquipActions()
                              orderby act.Key
                              select new
                              {
                                  act.Key,
                                  Value = act.Value + " (" + act.Key + ")"
                              };

                // change the list of actions to the character animations list
                cmbActions.DataSource = actions.ToList();

                // try to find the same action again
                int newIdx = cmbActions.Items.IndexOf( selItem );

                // restore the selected index (if the action still exist)
                cmbActions.SelectedIndex = newIdx != -1 ? newIdx : 0;
            }
        }

        /// <summary>
        /// Get a dictionary of all the actions common for all the equipment selected
        /// </summary>
        private Dictionary<int, string> CommonEquipActions()
        {
            // flag that indicates a mount has been seleted
            bool mountActive = cmbMount.SelectedIndex != 0;

            // create the new dictionary (by duplicating the character actions)
            Dictionary<int, string> commonActions = CharActions.ToDictionary( entry => entry.Key, entry => entry.Value );

            // get the list of the combo boxes in the character tab (only the one with something selected) - we exclude the mount
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                         where int.Parse( cmb.SelectedValue.ToString() ) != -1 && cmb.Name != "cmbMount"
                                         orderby cmb.Tag
                                         select cmb;

            // scan all the form objects
            foreach ( ComboBox cmb in objs )
            {
                // get the body ID for the current layer
                int itemId = int.Parse( cmb.SelectedValue.ToString() );

                // get the mobile data from the main list
                Mobile mob = Mobiles.Where( mm => mm.BodyId == itemId ).FirstOrDefault();

                // remove all the actions from the dictionary that are NOT available for this item.
                // if a mount is selected, we pick only "mounted" actions
                commonActions = commonActions.OrderBy( act => act.Key ).Where( act => mob.Actions[act.Key] != null && ( !mountActive || ( mountActive && act.Value.Contains( " Mounted" ) ) ) ).ToDictionary( entry => entry.Key, entry => entry.Value );
            }

            return commonActions;
        }

        /// <summary>
        /// Center the preview image on the parent panel
        /// </summary>
        private void CenterImage()
        {
            // is the preview image width shorter than the panel width?
            if ( imgPreview.Width < pnlPreview.Width )
            {
                // center horizontally
                imgPreview.Left = ( pnlPreview.Width - imgPreview.Width ) / 2;
            }
            else // if the horizontal scroll is active we se the left margin to 0
                imgPreview.Left = 0;

            // is the preview image height shorter than the panel height?
            if ( imgPreview.Height < pnlPreview.Height )
            {
                // center vertically
                imgPreview.Top = ( pnlPreview.Height - imgPreview.Height ) / 2;
            }
            else // if the vertical scroll is active we se the top margin to 0
                imgPreview.Top = 0;
        }

        /// <summary>
        /// toggle the loading status
        /// </summary>
        /// <param name="state">status to apply</param>
        private void ToggleLoading( bool state )
        {
            // backup the playback status
            bool timerBck = animPlayer.Enabled;

            // pause the playback during the loading procedure
            animPlayer.Enabled = false;

            // toggle the loading visibility
            imgLoading.Visible = state;

            // toggle the form interaction ability
            foreach ( Control c in Controls.OfType<Control>() )
            {
                c.Enabled = !state;
            }

            // make sure the image is enabled when needed
            imgLoading.Enabled = state;

            // change the cursor
            UseWaitCursor = state;

            // restore the playback status
            animPlayer.Enabled = timerBck;

            // allow the form to update
            Application.DoEvents();
        }

        /// <summary>
        /// Clear the unused frames and mobiles
        /// </summary>
        /// <param name="mobilesOnly">Remove only the unused mobiles?</param>
        private void ClearUnusedFrames( bool mobilesOnly = false )
        {
            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // get the list of the combo boxes in the character tab (only the one with something selected)
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>().Union( pagCreatures.Controls.OfType<ComboBox>() )
                                         where cmb.SelectedValue != null && int.Parse( cmb.SelectedValue.ToString() ) != -1
                                         orderby cmb.Tag
                                         select cmb;

            // if we want to clear only the mobiles, we skil the animations
            if ( !mobilesOnly )
            {
                // scan all the form objects
                foreach ( ComboBox cmb in objs )
                {
                    // get the body ID for the current layer
                    int itemId = int.Parse( cmb.SelectedValue.ToString() );

                    // get the mobile data from the main list
                    Mobile m = Mobiles.Where( mm => mm.BodyId == itemId ).FirstOrDefault();

                    // make sure the mobile exist
                    if ( m == null )
                        continue;

                    // scan all the actions
                    foreach ( UOAnimation anim in m.Actions )
                    {
                        // we clear the frames of every action
                        if ( anim != null )
                            anim.DisposeFrames();
                    }

                    // allow the form to update
                    Application.DoEvents();
                }
            }

            // find all the mobiles not used in the current equipment
            IEnumerable<Mobile> tempMobiles = Mobiles.Where( m => !objs.Any( cmb => int.Parse( cmb.SelectedValue.ToString() ) == m.BodyId ) );

            // store the initial mobiles count
            int found = tempMobiles.Count();

            // scan all the mobiles we found
            for ( int i = 0; i < found; i++ )
            {
                // get the first mobile (since we keep removing them, we keep getting the first
                Mobile m = tempMobiles.First();

                // dispose of the mobile
                m.Dispose();

                // remove the mobile from the main list
                Mobiles.Remove( m );

                // nullify the mobile
                m = null;

                // allow the form to update
                Application.DoEvents();
            }

            // force garbage collection
            GC.WaitForPendingFinalizers();
            GC.GetTotalMemory( true );

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Clear unused items from the cache
        /// </summary>
        private void ClearUnusedItems()
        {
            // initialize the list of all visible items id in the list
            List<int> visibleIds = new List<int>( Enumerable.Range( firsttVisibleItem, lastVisibleItem ) );

            // scan the images in cache
            foreach ( KeyValuePair<int, Bitmap> k in itemsImageCache )
            {
                // if the id is not in the list, we clear out the bitmap
                if ( !visibleIds.Contains( k.Key ) && k.Value != null )
                    k.Value.Dispose();
            }
        }

        /// <summary>
        /// Execute the VD load
        /// </summary>
        private void BeginLoadVD()
        {
            // clear the current mobile before loading a new one
            if ( CurrentMobile != null )
                CurrentMobile.Dispose();

            // set the new current mobile
            CurrentMobile = new Mobile( 99999 );

            // is this a VD file?
            if ( ofdFile.FileName.ToLower().EndsWith( ".vd" ) )
            {
                // load the vd file
                CurrentMobile.LoadFromVD( ofdFile.FileName );

                // use the file name for the creature name
                CurrentMobile.Name = Path.GetFileNameWithoutExtension( ofdFile.FileName );

                // change the actions list to people/equipment
                if ( CurrentMobile.CCAnimationType == 2 )
                {
                    // get the actions for the character/equip
                    var actions = from act in CharActions
                                  where CurrentMobile.Actions[act.Key] != null
                                  orderby act.Key
                                  select new
                                  {
                                      act.Key,
                                      Value = act.Value + " (" + act.Key + ")"
                                  };

                    // change the list of actions to the creatures animations list
                    cmbActions.DataSource = actions.ToList();
                }

                else // change the actions list to creatures
                {
                    // get the actions for the creature
                    var actions = from act in CreatureActions
                                  where CurrentMobile.Actions[act.Key] != null
                                  orderby act.Key
                                  select new
                                  {
                                      act.Key,
                                      Value = act.Value + " (" + act.Key + ")"
                                  };

                    // change the list of actions to the character animations list
                    cmbActions.DataSource = actions.ToList();
                }

                // reset the image preview
                ResetCurrentImage();
            }
            //// is this a bin file?
            //else if ( ofdFile.FileName.ToLower().EndsWith( ".bin" ) )
            //{
            //    // open the file
            //    using ( FileStream fileStream = new FileStream( ofdFile.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
            //    {
            //        // load the animation
            //        UOAnimation anim = new UOAnimation( fileStream );

            //        // generate the images for the animation
            //        anim.GenerateImages();

            //        // load the image frame
            //        imgPreview.Image = CreateFrame( 0, ref anim );

            //        // store the image on the button
            //        btnOpenFile.Tag = anim;
            //    }
            //}
        }

        /// <summary>
        /// Export the character sheet of all the animations of the current mobile
        /// </summary>
        private void ExportCharacterSheetAll()
        {
            // get the dictionary for the actions based on the animation type (character or creature)
            Dictionary<int, string> d = CurrentMobile.CCAnimationType == 2 ? CharActions : CreatureActions;

            // break the file name in: path, file name and extension
            string fDir = Path.GetDirectoryName( sfd.FileName );
            string fName = Path.GetFileNameWithoutExtension( sfd.FileName );
            string fExt = Path.GetExtension( sfd.FileName );

            // scan all the animations
            for ( int i = 0; i < CurrentMobile.Actions.Length; i++ )
            {
                // does the animation exist?
                if ( CurrentMobile.Actions[i] != null )
                {
                    // name of the file to use
                    string fn = String.Concat( fName, "_" + d[i], fExt );

                    // file name to use for saving
                    string fileName = Path.Combine( fDir, fn );

                    // if the file exist, and the user DO NOT want to override it, we skip the animation
                    if ( File.Exists( fileName ) && MessageBox.Show( this, "The file:\n\n" + fn + "\n\nALREADY EXIST!\n\nOK to overwrite\nCANCEL to skip the animation", "File Exist", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning ) == DialogResult.Cancel )
                        continue;

                    // define the size to use for the frames (we use the default frame size except for the character section)
                    int w = tabViewer.SelectedTab == pagCharacter ? char_Size.Width : 0;
                    int h = tabViewer.SelectedTab == pagCharacter ? char_Size.Height : 0;

                    // create the sprite sheet for the animation
                    CurrentMobile.Actions[i].CreateSpriteSheet( tabViewer.SelectedTab == pagCreatures ? GetCreatureHue() : null, w, h );

                    // save the spritesheet (with _actionName before the extension)
                    CurrentMobile.Actions[i].SpriteSheet.Save( fileName );

                    // get rid of the frames and sprite sheet of this animation
                    CurrentMobile.Actions[i].DisposeFrames();
                }

                // allow the form to update
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Export the character sheet of the active animation
        /// </summary>
        private void ExportCharacterSheetCurrent()
        {
            // get the dictionary for the actions based on the animation type (character or creature)
            Dictionary<int, string> d = CurrentMobile.CCAnimationType == 2 ? CharActions : CreatureActions;

            // break the file name in: path, file name and extension
            string fDir = Path.GetDirectoryName( sfd.FileName );
            string fName = Path.GetFileNameWithoutExtension( sfd.FileName );
            string fExt = Path.GetExtension( sfd.FileName );

            // name of the file to use
            string fn = String.Concat( fName, "_" + d[m_SelectedAction], fExt );

            // file name to use for saving
            string fileName = Path.Combine( fDir, fn );

            // if the file exist, and the user DO NOT want to override it, we can get out
            if ( File.Exists( fileName ) && MessageBox.Show( this, "The file:\n\n" + fn + "\n\nALREADY EXIST!\n\nOK to overwrite\nCANCEL to abort", "File Exist", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning ) == DialogResult.Cancel )
                return;

            // does the animation exist?
            if ( CurrentMobile.Actions[m_SelectedAction] != null )
            {
                // define the size to use for the frames (we use the default frame size except for the character section)
                int w = tabViewer.SelectedTab == pagCharacter ? char_Size.Width : 0;
                int h = tabViewer.SelectedTab == pagCharacter ? char_Size.Height : 0;

                // create the sprite sheet for the animation
                CurrentMobile.Actions[m_SelectedAction].CreateSpriteSheet( tabViewer.SelectedTab == pagCreatures ? GetCreatureHue() : null, w, h );

                // save the spritesheet (with _actionName before the extension)
                CurrentMobile.Actions[m_SelectedAction].SpriteSheet.Save( fileName );

                // get rid of the sprite sheet
                CurrentMobile.Actions[m_SelectedAction].SpriteSheet.Dispose();
            }
        }

        /// <summary>
        /// Load the game path from the settings
        /// </summary>
        /// <param name="exitOnFail">Exit if the user cancel</param>
        private void LoadGamePath( bool ignoreCurrent = false, bool exitOnFail = true )
        {
            // if we have a saved game path we get that from the settings
            if ( Settings.Default.GamePath != null )
                GamePath = Settings.Default.GamePath;

            // check if the game path is valid
            if ( !CheckGamePath( GamePath ) || ignoreCurrent )
            {
                // until a correct path is selected (or the cancel button is pressed), we force the user to pick the folder
                bool pathPicked = PickGamePath( exitOnFail );

                // if the path is still wrong, we ask the user to try again...
                while ( !CheckGamePath( GamePath ) || !pathPicked )
                {
                    // ask the user if he wants to pick the folder again
                    if ( MessageBox.Show( this, "The selected path does NOT contain the game files.\nDo you want to try again?", "Wrong Path", MessageBoxButtons.RetryCancel ) == DialogResult.Retry )
                        pathPicked = PickGamePath( false );

                    else if ( exitOnFail )
                        Environment.Exit( 0 );

                    else
                        return;
                }
            }
        }

        /// <summary>
        /// Check if the game path provided is correct
        /// </summary>
        /// <param name="path">Game path to check</param>
        /// <returns>Game path correct or not</returns>
        private bool CheckGamePath( string path )
        {
            return Directory.Exists( path ) && ( File.Exists( Path.Combine( path, "AnimationFrame1.uop" ) ) && File.Exists( Path.Combine( path, "Hues.uop" ) ) );
        }

        /// <summary>
        /// Select the game path through the file browse dialog
        /// </summary>
        /// <param name="exitOnFail">Exit if the user cancel</param>
        private bool PickGamePath( bool exitOnFail = true )
        {
            // set the browse description
            fbd.Description = "Select the Ultima Online Enhanced Client folder.";

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // show the folder browse dialog
            if ( fbd.ShowDialog( f ) == DialogResult.OK )
            {
                // is the game path correct?
                if ( CheckGamePath( fbd.SelectedPath ) )
                {
                    // path for the main KR exe
                    string uokrExe = Path.Combine( fbd.SelectedPath, "UOKR.exe" );

                    // determine if we are using the old KR files
                    bool isKR = File.Exists( uokrExe );

                    // a KR path has been chosen
                    if ( isKR )
                    {
                        // get the UO KR version
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo( Path.Combine( fbd.SelectedPath, "UOKR.exe" ) );

                        // make sure the game is at least the version 2.58
                        if ( versionInfo.FileMajorPart < 2 || versionInfo.FileMinorPart < 58  )
                        {
                            // only the version 2.58 or higher are allowed
                            MessageBox.Show( this, "If you want to use the old Kingdom Reborn, you need at least the version 2.58.0.6!", "Wrong Game Vesion", MessageBoxButtons.OK, MessageBoxIcon.Warning );

                            return false;
                        }

                        // no longer KR (last KR version was 2.60)
                        if ( versionInfo.FileMajorPart > 2 )
                            isKR = false;
                    }

                    // animation lists backup paths
                    string ECAnimListBackup = Path.Combine( Application.StartupPath, "AnimationsCollection - EC.xml" );
                    string KRAnimListBackup = Path.Combine( Application.StartupPath, "AnimationsCollection - KR.xml" );

                    // are we using the old KR files?
                    if ( isKR && File.Exists( KRAnimListBackup ) )
                    {
                        // get the last write date for the current animations list and backup
                        DateTime fileModDate = File.GetLastWriteTime( AnimXMLFile );
                        DateTime backupModDate = File.GetLastWriteTime( ECAnimListBackup );

                        // the backup is older than the current list?
                        if ( fileModDate > backupModDate )
                        {
                            // delete the backup
                            File.Delete( ECAnimListBackup );

                            // create a backup for the original animations list
                            File.Move( AnimXMLFile, ECAnimListBackup );
                        }

                        // delete the current animations list
                        if ( File.Exists( AnimXMLFile ) )
                            File.Delete( AnimXMLFile );

                        // copy the KR animations list
                        File.Copy( KRAnimListBackup, AnimXMLFile );
                    }
                    // do we have a backup for the EC animations list?
                    else if ( File.Exists( ECAnimListBackup ) )
                    {
                        // delete the current animations list
                        if ( File.Exists( AnimXMLFile ) )
                            File.Delete( AnimXMLFile );

                        // copy the EC animations list backup
                        File.Copy( ECAnimListBackup, AnimXMLFile );
                    }

                    // store the game path
                    GamePath = fbd.SelectedPath;

                    // store the game path in the settings
                    Settings.Default.GamePath = GamePath;

                    // save settings
                    Settings.Default.Save();

                    return true;
                }
                //else if ( exitOnFail )
                //    Environment.Exit( 0 );
            }
            else // if no folder has been chosen, we can get out
            {
                if ( exitOnFail )
                    Environment.Exit( 0 );
            }

            return false;
        }

        /// <summary>
        /// Save the current window position for the next session
        /// </summary>
        private void SaveWindowPosition()
        {
            // store the window location
            Settings.Default.WindowLocation = Location;

            // store the window size
            Settings.Default.WindowSize = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;

            // store the window maximized status
            Settings.Default.WindowMaximized = WindowState == FormWindowState.Maximized;

            // save settings
            Settings.Default.Save();
        }

        /// <summary>
        /// Load the window position from the previous session
        /// </summary>
        private void LoadWindowPosition()
        {
            // if we have a saved window position we get that from the settings
            if ( Settings.Default.WindowLocation != null && Settings.Default.WindowLocation != new Point( 0, 0 ) )
                Location = Settings.Default.WindowLocation;

            // if we have a saved window size we get that from the settings
            if ( Settings.Default.WindowSize != null && Settings.Default.WindowSize != new Size( 0, 0 ) )
                Size = Settings.Default.WindowSize;

            // restore the window maximized status
            if ( Settings.Default.WindowMaximized )
                WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// Reset all combos
        /// </summary>
        private void ClearAllCombos()
        {
            // get the list of the combo boxes in the character tab (except body)
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>().Union( pagCreatures.Controls.OfType<ComboBox>() )
                                         select cmb;

            // reset all combo box to the default value
            foreach ( ComboBox cmb in objs )
            {
                cmb.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// After changing a combo value for the sort order, it will swap the other combo making sure there are no duplicates
        /// </summary>
        /// <param name="currCmb">the combo box that has been changed</param>
        private void FixCustomDrawingOrder( ComboBox currCmb )
        {
            // get the index to replace on the other combo
            int idx = currCmb.SelectedIndex;

            // combo box that need to be updated (to find)
            ComboBox toChange = null;

            // list of all the index in use, containing only the currently selected one at start.
            List<int> usedIndex = new List<int>() { idx };

            // get the list of the drawing order combo boxes in the character tab
            IEnumerable<ComboBox> objs = from cmb in pagCharacter.Controls.OfType<ComboBox>()
                                         where cmb.Name.StartsWith( "cmbDO" )
                                         select cmb;

            // scan all the combos we have found
            foreach ( ComboBox cmb in objs )
            {
                // skip the current combo
                if ( cmb == currCmb )
                    continue;

                // add the index used by this combo if it's missing
                if ( !usedIndex.Contains( cmb.SelectedIndex ) )
                    usedIndex.Add( cmb.SelectedIndex );

                else // if it's already listed, we found the combo to update
                    toChange = cmb;
            }

            // nothing changed (happens during the app start)
            if ( toChange == null || toChange.Items.Count <= 0 )
                return;

            // search for the missing index
            int missingIdx = Enumerable.Range( 0, charEquipSort.Count - 1 ).Except( usedIndex ).FirstOrDefault();

            // change the sort order
            toChange.SelectedIndex = missingIdx;

            // get the list of the combo boxes in the character tab (only the one with something selected)
            objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                   where int.Parse( cmb.SelectedValue.ToString() ) != -1
                   orderby charEquipSort.IndexOf( int.Parse( cmb.Tag.ToString() ) )
                   select cmb;

            // scan all equipment
            foreach ( ComboBox cmb in objs )
            {
                // get the draw order combo
                ComboBox cmbDO = (ComboBox)Controls.Find( cmb.Name.Replace( "cmb", "cmbDO" ), true ).FirstOrDefault();

                // does this layer has a custom drawing order?
                if ( cmbDO == null )
                    continue;

                // update the custom sort table with the correct position for this layer
                charEquipCustomSort[cmbDO.SelectedIndex] = int.Parse( cmb.Tag.ToString() );
            }

            // update the frames of the current action
            MergeFrames();

            // reset the image preview
            ResetCurrentImage();
        }

        /// <summary>
        /// Get the mount action ID based on the current selected action (so it will be drawn correctly with the character)
        /// </summary>
        /// <param name="m">Mount mobile</param>
        /// <param name="currCharacterAction">current character action to mirror for the mount</param>
        /// <returns>Correct mount action ID</returns>
        private int GetMountActionID( Mobile m, int currCharacterAction )
        {
            // default action ID
            int currAction = 0;

            // is this the mount?
            if ( m.Layer == Mobile.Layers.Mount )
            {
                // is this a "run" action?
                if ( CharActions[currCharacterAction].Contains( "Run" ) )
                {
                    // does the mount have the "mounted run" action?
                    if ( m.Actions[30] != null )
                        currAction = 30;

                    else // without the "mounted run" action we have to use simply "run"
                        currAction = 24;
                }
                // is this a "idle" action?
                else if ( CharActions[currCharacterAction].Contains( "Walk" ) )
                {
                    // does the mount have the "mounted walk" action?
                    if ( m.Actions[29] != null )
                        currAction = 29;

                    else // without the "mounted walk" action we have to use simply "walk"
                        currAction = 22;
                }
                else // any other action we use the "idle" animation for the mount
                {
                    // does the mount have the "mounted idle" action?
                    if ( m.Actions[31] != null )
                        currAction = 31;

                    else // without the "mounted idle" action we have to use simply "idle"
                        currAction = 25;
                }
            }

            return currAction;
        }

        /// <summary>
        /// Save the character equipment and data
        /// </summary>
        private void SaveCharacter()
        {
            // get the file name to use
            string fileName = sfd.FileName;

            // create a new xml document
            XDocument xdoc = new XDocument();

            // create the root element
            xdoc.Add( new XElement( "Character" ) );

            // get the list of the combo boxes in the character tab (only the one with something selected)
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                         where int.Parse( cmb.SelectedValue.ToString() ) != -1
                                         orderby int.Parse( cmb.Tag.ToString() )
                                         select cmb;

            // scan all equipment
            foreach ( ComboBox cmb in objs )
            {
                // get the body ID for the current layer
                int itemId = int.Parse( cmb.SelectedValue.ToString() );

                // get the hue selection button for this layer
                Button btn = (Button)Controls.Find( cmb.Name.Replace( "cmb", "btnColor" ), true ).FirstOrDefault();

                // get the draw order combo
                ComboBox cmbDO = (ComboBox)Controls.Find( cmb.Name.Replace( "cmb", "cmbDO" ), true ).FirstOrDefault();

                // create a new xml element
                XElement elm = new XElement( "Item",
                                             new XAttribute( "Layer", cmb.Tag.ToString() ),
                                             new XAttribute( "ID", itemId.ToString() ),
                                             new XAttribute( "Hue", ( (Hue)btn.Tag ).ID.ToString() ),
                                             new XAttribute( "DrawOrder", cmbDO != null ? cmbDO.SelectedIndex.ToString() : "" )
                                           );

                // add the element to the root
                xdoc.Root.Add( elm );

                // allow the form to update
                Application.DoEvents();
            }

            // save the file
            xdoc.Save( fileName );
        }

        /// <summary>
        /// Load the character settings from a saved .char file
        /// </summary>
        private void LoadCharacter()
        {
            // reset all combos
            InitializeCombos();

            // reset the hue buttons
            InitializeColorButtons();

            // flag that the combos are now resetting
            comboReset = true;

            // get the file name to use
            string fileName = ofdFile.FileName;

            // open the xml document
            XDocument xdoc = XDocument.Load( fileName );

            // get the list of the combo boxes in the character tab (only the one with something selected)
            IEnumerable<ComboBox> objs = from cmb in pnlEquipment.Controls.OfType<ComboBox>()
                                         orderby int.Parse( cmb.Tag.ToString() )
                                         select cmb;

            // scan all equipment
            foreach ( ComboBox cmb in objs )
            {
                // Search the xml for the item with the current layer to use (if there is one)
                IEnumerable<XElement> items = from itm in xdoc.Root.Descendants( "Item" )
                                              where itm.Attribute( "Layer" ).Value == cmb.Tag.ToString()
                                              select itm;

                // get the item for the current layer
                XElement item = items.FirstOrDefault();

                // get the hue selection button for this layer
                Button btn = (Button)Controls.Find( cmb.Name.Replace( "cmb", "btnColor" ), true ).FirstOrDefault();

                // get the draw order combo
                ComboBox cmbDO = (ComboBox)Controls.Find( cmb.Name.Replace( "cmb", "cmbDO" ), true ).FirstOrDefault();

                // no items saved here?
                if ( item != null )
                {
                    // select the item
                    cmb.SelectedValue = int.Parse( item.Attribute( "ID" ).Value );

                    // set the hue for this layer
                    SetColorButton( btn, int.Parse( item.Attribute( "Hue" ).Value ) );

                    // select the drawing order
                    if ( cmbDO != null )
                        cmbDO.SelectedIndex = cmbDO.Items.IndexOf( int.Parse( item.Attribute( "DrawOrder" ).Value ) );
                }

                // allow the form to update
                Application.DoEvents();
            }

            // flag that the combos reset is complete
            comboReset = false;

            // update the controls availability
            UpdateCharacterCombos( false );

            // update the current action
            cmbActions_SelectedIndexChanged( cmbActions, new EventArgs() );
        }

        /// <summary>
        /// Load the string dictionaries (containing the names of the items)
        /// </summary>
        private void LoadStringsDictionary()
        {
            // open the uop file containing the tileart data
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "string_dictionary.uop" ) );

            // load the file memory stream data
            using ( MemoryStream stream = new MemoryStream( UOP.Blocks[0].Files[0].Unpack() ) )
            {
                // open the reader
                using ( BinaryReader reader = new BinaryReader( (Stream)stream ) )
                {
                    // unknown data
                    reader.ReadInt64();

                    // get the amount of strings to load
                    uint strCount = reader.ReadUInt32();

                    // unknown data
                    reader.ReadInt16();

                    // read all the characters
                    for ( int i = 0; i < strCount; i++ )
                    {
                        // get the string length
                        ushort lgt = reader.ReadUInt16();

                        // load the string
                        stringsDictionary.Add( Encoding.ASCII.GetString( reader.ReadBytes(lgt) ) );

                        // allow the form to update
                        Application.DoEvents();
                    }
                }
            }
        }

        /// <summary>
        /// Load the items list
        /// </summary>
        private void LoadItems()
        {
            // open the uop file containing the tileart data
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "tileart.uop" ) );

            // scan the tileart data
            foreach ( MythicPackageBlock block in UOP.Blocks )
            {
                foreach ( MythicPackageFile file in block.Files )
                {
                    // load the file memory stream data
                    using ( MemoryStream stream = new MemoryStream( file.Unpack() ) )
                    {
                        // open the reader
                        using ( BinaryReader reader = new BinaryReader( (Stream)stream ) )
                        {
                            // load the item data
                            ItemData it = new ItemData( reader, ref stringsDictionary );

                            // add the item to the main list
                            itemsCache.Add( it );
                        }
                    }

                    // allow the form to update
                    Application.DoEvents();
                }
            }

            // update the items list cache
            lstItems.VirtualListSize = itemsCache.Count;

            // set the item image size
            lstItems.LargeImageList = new ImageList();
            lstItems.LargeImageList.ImageSize = new Size( ( lstItems.Size.Width / 8 ) + 25, 100 );

            // remove the line spacing
            ListView_SetSpacing( lstItems, (short)lstItems.LargeImageList.ImageSize.Width, (short)lstItems.LargeImageList.ImageSize.Height );
        }

        /// <summary>
        /// Scale the image to fit a given size
        /// </summary>
        /// <param name="imgSize">Size to fit the image in</param>
        /// <param name="original">Image to resize</param>
        private void ResizeImage( Size imgSize, ref Bitmap original )
        {
            // make sure we actually need to resize
            if ( original.Height < imgSize.Height && original.Width < imgSize.Width )
                return;

            // calculate the scale required to fit
            float scaleHeight = (float)imgSize.Height / (float)original.Height;
            float scaleWidth = (float)imgSize.Width / (float)original.Width;

            // get the scale to use
            float scale = Math.Min(scaleHeight, scaleWidth);

            // redraw the scaled image
            original = new Bitmap( original, (int)( original.Width * scale ), (int)( original.Height * scale ) );
        }

        /// <summary>
        /// Fix the listview line spacing
        /// </summary>
        /// <param name="listview">Listview to fix</param>
        /// <param name="cx">Icon width</param>
        /// <param name="cy">Icon height</param>
        private void ListView_SetSpacing( ListView listview, short cx, short cy )
        {
            // listview linespacing messages
            const int LVM_FIRST = 0x1000;
            const int LVM_SETICONSPACING = LVM_FIRST + 53;

            // Fix the line spacing
            SendMessage( listview.Handle, LVM_SETICONSPACING, IntPtr.Zero, (IntPtr)MakeLong( cx, cy ) );
        }

        /// <summary>
        /// merge 2 values in a single long number
        /// </summary>
        private int MakeLong( short lowPart, short highPart )
        {
            return (int)( ( (ushort)lowPart ) | (uint)( highPart << 16 ) );
        }

        /// <summary>
        /// Search an item by ID
        /// </summary>
        /// <param name="id">ID of the item to find</param>
        private void SearchItemByID( int id )
        {
            // flag that we DIDN'T do an animation search
            lastItemAnimSearch = false;

            // clear the current selection
            lstItems.SelectedIndices.Clear();

            // search the first item with the specified ID
            int idx = itemsCache.FindIndex( it => it.ID == id );

            // did we find the item?
            if ( idx >= 0 )
            {
                // add the selected item (only if we find it)
                lstItems.SelectedIndices.Add( idx );

                // move the visual to the item
                lstItems.EnsureVisible( idx );
            }
            else // found nothing
                SystemSounds.Beep.Play();
        }

        /// <summary>
        /// Search an item by NAME
        /// </summary>
        /// <param name="name">NAME of the item to find</param>
        /// <param name="next">Search for another item from the current selection point?</param>
        private void SearchItemByName( string name, bool next = false, bool previous = false )
        {
            // get the current selected item (for the search next)
            int prevIndex = lstItems.SelectedIndices.Count > 0 ? lstItems.SelectedIndices[0] : -1;

            // flag that we DIDN'T do an animation search
            lastItemAnimSearch = false;

            // clear the current selection
            lstItems.SelectedIndices.Clear();

            // get all the items with the specified name (or that contains it - if it's not an exact match search)
            IEnumerable<ItemData> itms = itemsCache.Where( it => it.Name.ToLower() == name.ToLower() || ( !chkExactMatch.Checked && it.Name.ToLower().Contains( name.ToLower() ) ) );

            // initialize the next item
            ItemData nextItem = null;

            // previous item?
            if ( previous )
                nextItem = itms.Where( it => it.ID < prevIndex ).LastOrDefault();

            // next item?
            else if ( next )
                nextItem = itms.Where( it => it.ID > prevIndex ).FirstOrDefault();

            // get the first index (or the next in the list)
            int idx = !next && !previous ? itemsCache.IndexOf( itms.FirstOrDefault() ) : nextItem != null ? itemsCache.IndexOf( nextItem ) : itemsCache.IndexOf( itms.FirstOrDefault() );

            // did we find the item?
            if ( idx >= 0 )
            {
                // add the selected item (only if we find it)
                lstItems.SelectedIndices.Add( idx );

                // move the visual to the item
                lstItems.EnsureVisible( idx );
            }
            else // found nothing
                SystemSounds.Beep.Play();
        }

        /// <summary>
        /// Search an item by ANIMATION ID
        /// </summary>
        /// <param name="id">ID of the ANIMATION to find</param>
        private void SearchItemByAnimID( int id, bool next = false, bool previous = false )
        {
            // get the current selected item (for the search next)
            int prevIndex = lstItems.SelectedIndices.Count > 0 ? lstItems.SelectedIndices[0] : -1;

            // flag that we did an animation search
            lastItemAnimSearch = true;

            // clear the current selection
            lstItems.SelectedIndices.Clear();

            // get all the items with the specified animation
            IEnumerable<ItemData> itms = itemsCache.Where( it => it.Properties.ContainsKey( ItemData.TileArtProperties.Animation ) && it.Properties[ItemData.TileArtProperties.Animation] == id );

            // initialize the next item
            ItemData nextItem = null;

            // previous item?
            if ( previous )
                nextItem = itms.Where( it => it.ID < prevIndex ).LastOrDefault();

            // next item?
            else if ( next )
                nextItem = itms.Where( it => it.ID > prevIndex ).FirstOrDefault();

            // get the first index (or the next in the list)
            int idx = !next && !previous ? itemsCache.IndexOf( itms.FirstOrDefault() ) : nextItem != null ? itemsCache.IndexOf( nextItem ) : itemsCache.IndexOf( itms.FirstOrDefault() );

            // did we find the item?
            if ( idx >= 0 )
            {
                // add the selected item (only if we find it)
                lstItems.SelectedIndices.Add( idx );

                // move the visual to the item
                lstItems.EnsureVisible( idx );
            }
            else // found nothing
                SystemSounds.Beep.Play();
        }

        /// <summary>
        /// Search cliloc string by ID
        /// </summary>
        /// <param name="id">ID of the string to find</param>
        private void SearchClilocByID( long id )
        {
            // get the current cliloc
            List<KeyValuePair<long, string>> cliloc = GetSelectedCliloc();

            // clear the current selection
            lstCliloc.SelectedIndices.Clear();

            // search the first item with the specified ID
            int idx = cliloc.FindIndex( it => it.Key == id );

            // did we find the item?
            if ( idx >= 0 )
            {
                // add the selected item (only if we find it)
                lstCliloc.SelectedIndices.Add( idx );

                // move the visual to the item
                lstCliloc.EnsureVisible( idx );
            }
            else // found nothing
                SystemSounds.Beep.Play();
        }

        /// <summary>
        /// Search a text inside the cliloc
        /// </summary>
        /// <param name="name">Text to find</param>
        /// <param name="next">Search for another text from the current selection point?</param>
        private void SearchClilocByText( string text, bool next = false, bool previous = false )
        {
            // get the current selected item (for the search next)
            int prevIndex = lstCliloc.SelectedIndices.Count > 0 ? lstCliloc.SelectedIndices[0] : -1;

            // clear the current selection
            lstCliloc.SelectedIndices.Clear();

            // get the current cliloc
            List<KeyValuePair<long, string>> cliloc = GetSelectedCliloc();

            // get all the strings with the specified text (or that contains it)
            List<KeyValuePair<long, string>> itms = cliloc.Where( it => it.Value.ToLower() == text.ToLower() || ( it.Value.ToLower().Contains( text.ToLower() ) ) ).ToList();

            // initialize the next string
            KeyValuePair<long, string> nextItem = new KeyValuePair<long, string>();

            // item to use if we don't find anything else
            KeyValuePair<long, string> def = itms.FirstOrDefault();

            // previous string?
            if ( previous )
                nextItem = itms.Where( i => cliloc.IndexOf( i ) < prevIndex ).LastOrDefault();

            // next item?
            else if ( next )
                nextItem = itms.Where( i => cliloc.FindIndex( it => it.Key == i.Key && it.Value == i.Value ) > prevIndex ).FirstOrDefault();

            // get the first index (or the next in the list)
            int idx = !next && !previous ? cliloc.FindIndex( i => def.Key == i.Key && def.Value == i.Value ) : nextItem.Key != 0 ? cliloc.FindIndex( i => nextItem.Key == i.Key && nextItem.Value == i.Value ) : cliloc.FindIndex( i => def.Key == i.Key && def.Value == i.Value );

            // did we find the item?
            if ( idx >= 0 )
            {
                // add the selected text (only if we find it)
                lstCliloc.SelectedIndices.Add( idx );

                // move the visual to the item
                lstCliloc.EnsureVisible( idx );
            }
            else // found nothing
                SystemSounds.Beep.Play();
        }

        /// <summary>
        /// Initialize the items style between default and old KR
        /// </summary>
        private void InitializeItemStyle()
        {
            // update the items type checkbox
            chkItemsType.Checked = Settings.Default.useOldKRItems;

            // update the checkbox text
            if ( chkItemsType.Checked )
                chkItemsType.Text = "Items (Old KR Style)";
            else
                chkItemsType.Text = "Items (Default Style)";

            // set the checkbox tooltip
            ttp.SetToolTip( chkItemsType, "Switch item style between default and old KR.\n\nNOTE: the old KR style will have tons of missing items!!!" );
        }

        /// <summary>
        /// Load the multi data
        /// </summary>
        private void LoadMultis()
        {
            // Load the xml
            XDocument xdoc = XDocument.Load( MultiXMLFile );

            // add the "none" value
            multiCache.Add( new MultiItem() );

            // open the uop file containing the tileart data
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "MultiCollection.uop" ) );

            // scan the tileart data
            foreach ( MythicPackageBlock block in UOP.Blocks )
            {
                foreach ( MythicPackageFile file in block.Files )
                {
                    // skip the very last file (it's something else)
                    if ( block.Index == UOP.Blocks.Count - 1 && file.Index == block.Files.Count - 1 )
                        continue;

                    // load the file memory stream data
                    using ( MemoryStream stream = new MemoryStream( file.Unpack() ) )
                    {
                        // open the reader
                        using ( BinaryReader reader = new BinaryReader( (Stream)stream ) )
                        {
                            // load the multi data
                            MultiItem mi = new MultiItem( reader );

                            // Search the xml for the mobiles with the selected body ID
                            IEnumerable<XElement> items = from it in xdoc.Root.Descendants( "Multi" )
                                                          where it.Attribute( "id" ).Value == mi.ID.ToString()
                                                          select it;

                            // selected item
                            XElement itm = items.FirstOrDefault();

                            // get the multi name
                            if ( itm != null)
                            {
                                // store the name
                                mi.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( itm.Attribute( "name" ).Value );

                                // store the type
                                mi.Type = (MultiItem.MultiType) int.Parse( itm.Attribute( "type" ).Value );
                            }

                            // add the multi to the main list
                            multiCache.Add( mi );
                        }
                    }

                    // allow the form to update
                    Application.DoEvents();
                }
            }
        }

        /// <summary>
        /// Load all the audio files data
        /// </summary>
        private void LoadAudio()
        {
            // load the audio CSV data
            Dictionary<int, string> audioNames = LoadAudioCSV();

            // list of the parsed files
            List<KeyValuePair< int, int>> mf = new List<KeyValuePair< int, int>>();

            // UOP of the audio files
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "Audio.uop" ) );

            // search for the audio info file inside the UOP
            SearchResult sr = UOP.SearchExactFileName( "data/audio/audio_sounds.csv" );

            // add the CSV file to the list of the parsed ones
            mf.Add( new KeyValuePair<int, int>( sr.Block, sr.File ) );

            // search for the music info file inside the UOP
            sr = UOP.SearchExactFileName( "data/audio/audio_music.csv" );

            // add the CSV file to the list of the parsed ones
            mf.Add( new KeyValuePair<int, int>( sr.Block, sr.File ) );

            // scan the dictionary
            foreach ( KeyValuePair<int, string> data in audioNames )
            {
                // search for the image file inside the UOP
                sr = UOP.SearchExactFileName( data.Value );

                // did we find the csv?
                if ( sr.Found )
                {
                    // get the sound name
                    string audioName = data.Value.ToLower();

                    if ( audioName.Contains( "defense_mastery.mp3" ) )
                        audioName = "data/audio/sounds/samuraimoves/defense_mastery.mp3";

                    // create the audio name
                    audioName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( audioName.Replace( "data/audio/", "" ) );

                    // add the file to the audio cache
                    audioCache.Add( data.Key, new AudioData( GamePath, data.Key, audioName, sr.Block, sr.File ) );

                    // add the file to the list of the parsed ones
                    mf.Add( new KeyValuePair<int, int>( sr.Block, sr.File ) );
                }
                //else
                //    Debug.WriteLine( data.Value );
            }

            // no audio cache
            if ( audioCache.Count == 0 )
                return;

            // legacy audio files waveforms
            Dictionary<string, List<float>> leg = new Dictionary<string, List<float>>();

            // load legacy audio files waveforms
            LoadLegacyAudio( ref leg );

            // create a new xml document
            XDocument xdoc = XDocument.Load( AudioXMLFile );

            // unknown file counter
            int missing = 0;

            // counter for new audio found
            int newAudioFound = 0;

            // now we search the files that we didn't manage to parse
            foreach ( MythicPackageBlock block in UOP.Blocks )
            {
                // scan the files list
                foreach ( MythicPackageFile file in block.Files )
                {
                    // skip the already parsed files
                    if ( mf.Where( f => f.Key == block.Index && f.Value == file.Index ).Count() > 0  )
                        continue;

                    // get the next audio index
                    int idx = audioCache.Keys.Max() + 1;

                    // create the new audio data
                    AudioData newAudio = new AudioData( GamePath, idx, "", block.Index, file.Index );

                    // create the audio name
                    string audioName = "misc/unknown" + missing + ( newAudio.IsMP3 ? ".mp3" : ".wav" );

                    // is this an audio file? (there are some wild text file around...)
                    if ( !newAudio.NotAudio )
                    {
                        // find out if the file is already listed
                        XElement xlm = xdoc.Descendants("Sound").Where( itm => itm.Attribute("hash").Value == file.FileHash.ToString( "X16" ) ).FirstOrDefault();

                        // element missing, we need to add it to the file
                        if ( xlm == null )
                        {
                            // create a new xml element
                            XElement elm =  new XElement( "Sound",
                                            new XAttribute( "hash", file.FileHash.ToString( "X16" ) ),
                                            new XAttribute( "name", audioName.ToLower() )
                                            );

                            // add the element to the root
                            xdoc.Root.Add( elm );

                            // set the audio name
                            newAudio.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( audioName );

                            // increase the new audio counter
                            newAudioFound++;
                        }
                        else // xml element exist
                        {
                            // get the audio name from the xml
                            audioName = xlm.Attribute( "name" ).Value;

                            // does the name contains "unknown"?
                            if ( audioName.Contains( "unknown" ) && leg.Count > 0 )
                            {
                                // temp: intialize the waveform
                                List<float> points = AudioData.GetWavePoints( newAudio.GetWaveStream() );

                                // find a better file name from the legacy audio files
                                CompareWithLegacyAudio( points, leg, ref audioName );

                                // did we find a better name?
                                if ( !audioName.Contains( "unknown" ) )
                                {
                                    // update the name for the tree
                                    audioName = "legacy/" + audioName + ".wav";

                                    // store the new audio name in the xml
                                    xlm.Attribute( "name" ).Value = audioName;

                                    // increase the new sound found counter (so the xml will be saved)
                                    newAudioFound++;
                                }
                            }

                            // set the name
                            newAudio.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( audioName );
                        }

                        // add the file to the audio cache
                        audioCache.Add( idx, newAudio );

                        // increase the unknown file counter
                        missing++;
                    }
                    //else // get the unknown file data
                    //    Debug.WriteLine( "Unknown file type! - " + newAudio.GetUOPDataString() );
                }
            }

            // save the changes (if new sounds have been found)
            if ( newAudioFound > 0 )
                xdoc.Save( AudioXMLFile );

            // clear the legacy audio waveforms dictionary
            leg.Clear();

            // fill the treeview
            LoadAudioTree();
        }

        /// <summary>
        /// Load all the audio files data
        /// </summary>
        private Dictionary<int, string> LoadAudioCSV()
        {
            // initialize the dictionary to return
            Dictionary<int, string> audioNames = new Dictionary<int, string>();

            // UOP of the audio files
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "Audio.uop" ) );

            // search for the audio file info inside the UOP
            SearchResult sr = UOP.SearchExactFileName( "data/audio/audio_sounds.csv" );

            // did we find the csv?
            if ( sr.Found )
            {
                // open the CSV file
                using ( StreamReader reader = new StreamReader( new MemoryStream( UOP.Blocks[sr.Block].Files[sr.File].Unpack() ) ) )
                {
                    // read the file
                    ReadAudioCSV( reader, audioNames );
                }
            }

            // search for the music file info inside the UOP
            sr = UOP.SearchExactFileName( "data/audio/audio_music.csv" );

            // did we find the csv?
            if ( sr.Found )
            {
                // open the CSV file
                using ( StreamReader reader = new StreamReader( new MemoryStream( UOP.Blocks[sr.Block].Files[sr.File].Unpack() ) ) )
                {
                    // read the file
                    ReadMusicCSV( reader, audioNames );
                }
            }

            return audioNames;
        }

        /// <summary>
        /// Read the audio info CSV
        /// </summary>
        /// <param name="reader">open file memory stream</param>
        /// <param name="audioNames">dictionary to fill</param>
        private void ReadAudioCSV( StreamReader reader, Dictionary<int, string> audioNames )
        {
            // initialize the line string
            string line;

            // loop until the end of the file
            while ( ( line = reader.ReadLine() ) != null )
            {
                // old KR csv, we avoid it.
                if ( line.StartsWith( ";" ) || line.StartsWith( "w,s,id," ) )
                    return;

                // skip comments
                if ( line.StartsWith( "#" ) )
                continue;

                // split the string
                string[] parts = line.Split( ',' );

                // reformat the path
                parts[1] = "data/audio/" + parts[1].ToLower().Replace( "\\", "/" );

                // add the data to the dictionary
                audioNames.Add( int.Parse( parts[0] ), parts[1] );
            }
        }

        /// <summary>
        /// Read the music info CSV
        /// </summary>
        /// <param name="reader">open file memory stream</param>
        /// <param name="audioNames">dictionary to fill</param>
        private void ReadMusicCSV( StreamReader reader, Dictionary<int, string> audioNames )
        {
            // initialize the line string
            string line;

            // loop until the end of the file
            while ( ( line = reader.ReadLine() ) != null )
            {
                // old KR csv, we avoid it.
                if ( line.StartsWith( ";" ) )
                    return;

                // skip comments
                if ( line.StartsWith( "#" ) )
                    continue;

                // split the string
                string[] parts = line.Split( ',' );

                // reformat the path
                parts[1] = "data/audio/" + parts[1].ToLower().Replace( "\\", "/" );

                // music ID will start from 100k
                int musId = 100000 + int.Parse( parts[0] );

                // add the data to the dictionary
                audioNames.Add( musId, parts[1] );
            }
        }

        /// <summary>
        /// Create the audio tree
        /// </summary>
        private void LoadAudioTree()
        {
            // last node created
            TreeNode lastNode;

            // string used to draw the path
            string subPathAgg;

            // parse all the audio in the audio cache
            foreach ( KeyValuePair<int, AudioData> aud in audioCache )
            {
                // remove part of the path we don't need
                string path = aud.Value.Name;

                // reset the sub-path
                subPathAgg = string.Empty;

                // reset the last node
                lastNode = null;

                // create the path for the file
                foreach ( string subPath in path.Split( trvAudio.PathSeparator.ToCharArray() ) )
                {
                    // create the subpath line
                    subPathAgg += subPath + trvAudio.PathSeparator;

                    // determine if the path already exist
                    TreeNode[] nodes = trvAudio.Nodes.Find( subPathAgg, true );

                    // is the path missing?
                    if ( nodes.Length == 0 )

                        // no last node? create the node
                        if ( lastNode == null )
                            lastNode = trvAudio.Nodes.Add( subPathAgg, subPath );

                        else // create a nested node
                            lastNode = lastNode.Nodes.Add( subPathAgg, subPath );

                    else // path already exist, we just set it as last node
                        lastNode = nodes[0];

                    // set the folder icon
                    lastNode.ImageIndex = 1;
                    lastNode.SelectedImageIndex = 1;
                }

                // store the audio data in the tree node
                lastNode.Tag = aud.Value;

                // set the mp3 icon
                lastNode.ImageIndex = 0;
                lastNode.SelectedImageIndex = 0;

                // the audio files are NOT mp3
                if ( !( (AudioData)lastNode.Tag ).IsMP3 )
                {
                    // change the extension to wav
                    lastNode.Text = lastNode.Text.ToLower().Replace( ".mp3", ".Wav" );

                    // set the wav icon
                    lastNode.ImageIndex = 2;
                    lastNode.SelectedImageIndex = 2;
                }
            }

            // sort the tree
            trvAudio.TreeViewNodeSorter = new AlphanumComparatorFast();
            trvAudio.Sort();
        }

        /// <summary>
        /// Load the paperdoll data
        /// </summary>
        private void LoadPaperdoll()
        {
            // paperdoll items are saved as animations (1 male and 1 female) with a frame per item, and the frame ID is = animation ID of the object.

            // current UOP paperdoll file
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "Paperdoll.uop" ) );

            // load the file memory stream data for the female paperdoll
            using ( MemoryStream stream = new MemoryStream( UOP.Blocks[0].Files[0].Unpack() ) )
            {
                // get the items data
                femalePaperdollCache = new UOAnimation( stream, "Paperdoll.uop", 0, 0, true );
            }

            // load the file memory stream data for the male paperdoll
            using ( MemoryStream stream = new MemoryStream( UOP.Blocks[0].Files[1].Unpack() ) )
            {
                // get the items data
                malePaperdollCache = new UOAnimation( stream, "Paperdoll.uop", 0, 1, true );
            }
        }

        /// <summary>
        /// Load the cliloc data from the UOP file
        /// </summary>
        private void LoadCliloc()
        {
            // current UOP cliloc file
            MythicPackage UOP = new MythicPackage( Path.Combine( GamePath, "LocalizedStrings.uop" ) );

            // scan the uop data
            foreach ( MythicPackageBlock block in UOP.Blocks )
            {
                foreach ( MythicPackageFile file in block.Files )
                {
                    // initialize the cliloc for this language
                    List<KeyValuePair<long, string>> cliloc = new List<KeyValuePair<long, string>>();

                    // load the file memory stream data
                    using ( MemoryStream stream = new MemoryStream( file.Unpack() ) )
                    {
                        // open the reader
                        using ( BinaryReader reader = new BinaryReader( (Stream)stream ) )
                        {
                            // read the version number
                            reader.ReadUInt32();

                            // read unknown value
                            reader.ReadUInt16();

                            // keep reading until the end of the file
                            while ( reader.BaseStream.Length != reader.BaseStream.Position )
                            {
                                // read the ID
                                int id = reader.ReadInt32();

                                // read unknown flag
                                byte flag = reader.ReadByte();

                                // read string length
                                int lgt = reader.ReadUInt16();

                                // read string
                                string text = Encoding.UTF8.GetString( reader.ReadBytes( lgt ) );

                                // add the string to the cliloc
                                cliloc.Add( new KeyValuePair<long, string>( id, text ) );
                            }
                        }

                        // add the cliloc to the cache
                        clilocCache.Add( (ClilocLanguages)( file.Index + 1 ), cliloc );
                    }

                    // allow the form to update
                    Application.DoEvents();
                }
            }
        }

        /// <summary>
        /// Initialize the cliloc page
        /// </summary>
        private void InitializeCliloc()
        {
            // use the enum for the combo values
            cmbLanguage.DataSource = Enum.GetValues( typeof( ClilocLanguages ) );
        }

        /// <summary>
        /// Draw the selected multi
        /// </summary>
        private void DrawSelectedMulti()
        {
            // do nothing if the index is negative
            if ( cmbMulti.SelectedValue == null || (int)cmbMulti.SelectedValue == -1 )
                return;

            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // get the selected multi
            MultiItem sel = multiCache.Where( m => m.ID == (int)cmbMulti.SelectedValue ).FirstOrDefault();

            // update the parts combo
            GetMultiPartsList( sel );

            // update the type name
            lblMultiTypeValue.Text = sel.Type.ToString();

            // get the selected color for the multi
            Hue h = (Hue)btnColorMulti.Tag;

            // draw the image
            SetImage( sel.GetImage( GamePath, itemsCache, h, trkMinZ.Value, trkMaxZ.Value ) );

            // center the image
            CenterImage();

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Fill the multi parts combo
        /// </summary>
        /// <param name="mi">Multi item to get the parts from</param>
        private void GetMultiPartsList( MultiItem mi )
        {
            // get the unique parts list
            var parts = from mp in mi.Parts.Distinct()
                        orderby mp.ItemID
                        select new
                        {
                            mp.ItemID,
                            name = itemsCache.Where( it =>  it.ID == mp.ItemID ).FirstOrDefault().Name + " (" + mp.ItemID + ")"
                        };

            // set the data source to the combo
            cmbMultiParts.DataSource = parts.ToList();

            // make sure the combo displays the name of the item
            cmbMultiParts.DisplayMember = "name";

            // make sure the combo value is the ID of the item
            cmbMultiParts.ValueMember = "ItemID";

            // select the item from the list
            SearchItemByID( int.Parse( cmbMultiParts.SelectedValue.ToString() ) );
        }

        /// <summary>
        /// Reset the multi height trackbars
        /// </summary>
        private void ResetHeightTrackbars()
        {
            // get the selected multi
            MultiItem sel = multiCache.Where( m => m.ID == (int)cmbMulti.SelectedValue ).FirstOrDefault();

            // set the heigh slider max values
            trkMaxZ.Maximum = sel.MaxZ;
            trkMinZ.Maximum = sel.MaxZ;

            // set the heigh slider min values
            trkMaxZ.Minimum = sel.MinZ;
            trkMinZ.Minimum = sel.MinZ;

            // set the heigh slider for the max Z at max
            trkMaxZ.Value = sel.MaxZ;

            // set the height slider for the min z at min
            trkMinZ.Value = sel.MinZ;

            // update the trackbar labels
            lblMinZ.Text = "MIN: " + trkMinZ.Value;
            lblMaxZ.Text = "MAX: " + trkMaxZ.Value;

            // disable the track bars if there is no possible height selection
            trkMaxZ.Enabled = trkMaxZ.Maximum != trkMaxZ.Minimum;
            trkMinZ.Enabled = trkMinZ.Maximum != trkMinZ.Minimum;
        }

        /// <summary>
        /// Switch the export frame button text and animation controls status
        /// </summary>
        /// <param name="multi"></param>
        private void SwitchExportFrame( bool multi )
        {
            // do nothing on the audio tab
            if ( tabViewer.SelectedTab == pagAudio || tabViewer.SelectedTab == pagCliloc )
                return;

            // multi selected?
            if ( multi )
            {
                // change the export frame button text
                btnExportFrame.Text = "Export Multi Image";

                // hide the animation controls
                pnlAnimationControls.Visible = false;

                // hide the directions panel
                pnlDirections.Visible = false;

                // show the multi controls panel
                pnlMultiControls.Visible = true;
            }
            else // not a multi
            {
                // change the export frame button text
                btnExportFrame.Text = "Export Current Frame";

                // show the animation controls
                pnlAnimationControls.Visible = true;

                // show the directions panel
                pnlDirections.Visible = true;

                // hide the multi controls panel
                pnlMultiControls.Visible = false;
            }

            // change the export frame button text
            if ( chkPaperdoll.Checked )
                btnExportFrame.Text = "Export Current Paperdoll";
        }

        /// <summary>
        /// Update the animations XML
        /// </summary>
        private void UpdateAnimationsList()
        {
            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // list of all the uop files
            List<MythicPackage> UOPs = new List<MythicPackage>();

            // get the list of all the animation files
            string[] totalFiles = Directory.GetFiles(GamePath, "AnimationFrame*.uop" );

            // clear the mobiles list
            Mobiles.Clear();

            // create the "None" empty box for the combos
            Mobile empty = new Mobile( -1 );
            empty.MobileType = Mobile.MobileTypes.None;
            empty.Layer = Mobile.Layers.None;

            // add the "None" value to the list
            Mobiles.Add( empty );

            // scan all the annimation files we've found
            foreach ( string f in totalFiles )
            {
                // add the uop file to the list
                UOPs.Add( new MythicPackage( f ) );
            }

            // counter for new mobiles discovered
            int news = 0;

            // Load the xml
            XDocument xdoc = XDocument.Load( AnimXMLFile );

            // parse all the loaded uop files
            for ( int up = 0; up < UOPs.Count; up++ )
            {
                // scan all the blocks in the uop file
                for ( int block = 0; block < UOPs[up].Blocks.Count; block++ )
                {
                    // scan all files inside the block
                    for ( int fil = 0; fil < UOPs[up].Blocks[block].Files.Count; fil++ )
                    {
                        // load the mobile animation into the list
                        LoadMobileAnimation( up, block, fil, ref xdoc, ref news );

                        // update the loading status label
                        lblUpdate.Text = "UOP: " + ( up + 1 ) + " block: " + block + " file: " + fil;

                        // allow the form to update
                        Application.DoEvents();
                    }
                }
            }

            // clean the XML
            CleanXML( ref xdoc );

            // categorize the equipment
            CategorizeMobiles( ref xdoc );

            // sort the XML
            SortXML( ref xdoc );

            // save the changes to the XML
            xdoc.Save( AnimXMLFile );

            // update the recap label
            lblUpdate.Text = "Total of: " + Mobiles.Count + " mobiles.\nNew mobiles found: " + news;

            // hide the loading screen
            if ( !loadBck )
                Loading = false;

            // restart the app to make sure all the changes are loaded properly
            Application.Restart();
        }

        /// <summary>
        /// Update the animations XML
        /// </summary>
        private void UpdateMultiList()
        {
            // backup of the loading status
            bool loadBck = Loading;

            // show the loading screen
            if ( !loadBck )
                Loading = true;

            // Load the xml
            XDocument xdoc = XDocument.Load( MultiXMLFile );

            // select the main element of the xml
            XElement mults = xdoc.Descendants( "Multis" ).FirstOrDefault();

            // counter for new multis
            int news = 0;

            // current index of the multi
            int i = 1;

            // scan all the multi we have
            foreach ( MultiItem mi in multiCache )
            {
                // Search the xml for the multi with the current ID
                IEnumerable<XElement> items = from itm in xdoc.Root.Descendants( "Multi" )
                                              where itm.Attribute( "id" ).Value == mi.ID.ToString()
                                              select itm;

                // update the recap label
                lblUpdate.Text = "Scanning: " + i + "/" + multiCache.Count;

                // increase the current items count
                i++;

                // is the multi NOT listed?
                if ( items.Count() == 0 )
                {
                    // create the node with the uop file data
                    XElement elm = new XElement( "Multi",
                                                    new XAttribute( "name", "" ),
                                                    new XAttribute( "id", mi.ID.ToString() ),
                                                    new XAttribute( "type", "3" )
                                                );

                    // add the node to the XML
                    mults.Add( elm );

                    // increase the new items count
                    news++;

                    // allow the form to update
                    Application.DoEvents();
                }
            }

            // sort the XML by ID
            IEnumerable<XElement> sorted = xdoc.Root.Descendants( "Multi" ).OrderBy( itm => int.Parse( itm.Attribute( "id" ).Value ) ).ToArray();

            // clear the xml
            xdoc.Root.Descendants( "Multi" ).Remove();

            // add the sorted multi list
            xdoc.Root.Add( sorted );

            // save the xml
            xdoc.Save( MultiXMLFile );

            // update the recap label
            lblUpdate.Text = "Found: " + news + " new multis!";

            // hide the loading screen
            if ( !loadBck )
                Loading = false;
        }

        /// <summary>
        /// Export the current frame
        /// </summary>
        private void ExportFrame()
        {
            // initialize the first frame id
            int firstFrameID = -1;

            // initialize the max amount of frames in the current frameset
            int maxFrames = -1;

            // get the current frame/paperdoll image
            Image img = chkPaperdoll.Checked ? GetCurrentPaperdollImage() : GetCurrentFrameImage( ref firstFrameID, ref maxFrames );

            // do we have an image to save?
            if ( img == null )
            {
                SystemSounds.Beep.Play();
                return;
            }

            // set the save file dialog title
            sfd.Title = "Save Frame Image";

            // set the save file dialog filter
            sfd.Filter = "PNG file (*.png)|*.png";

            // reset the file name
            sfd.FileName = tabViewer.SelectedTab == pagCharacter ? ( chkPaperdoll.Checked ? "Character_Paperdoll" : "Character" ) : CurrentMobile.Name + ( chkPaperdoll.Checked ? "_Paperdoll" : "" );

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // save the frame image
            if ( sfd.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // save the image as PNG
                img.Save( sfd.FileName, ImageFormat.Png );

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Export multi item
        /// </summary>
        private void ExportMulti()
        {
            // set the save file dialog title
            sfd.Title = "Save Multi Image";

            // set the save file dialog filter
            sfd.Filter = "PNG file (*.png)|*.png";

            // reset the file name
            sfd.FileName = cmbMulti.Text;

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // save the multi image
            if ( sfd.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // save the image as PNG
                imgPreview.Image.Save( sfd.FileName, ImageFormat.Png );

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        /// <summary>
        /// Initialize the sound player with a sound file
        /// </summary>
        /// <param name="ws">Sound file data stream</param>
        private void SetActiveSound( WaveStream ws )
        {
            // clear the active sound (if there is one)
            ClearSound();

            // store the stream
            audioData = ws;

            // reset the sound
            audioData.Seek( 0, SeekOrigin.Begin );

            // re-create the sound player
            soundPlayer = new WaveOut();

            // set the sound to play
            soundPlayer.Init( audioData );

            // associate the stopped event
            soundPlayer.PlaybackStopped += new EventHandler<StoppedEventArgs>( SoundPlayerStopped );

            // make sure the label is visible
            lblCurrFramePlaying.Visible = true;

            // update the current playing time
            UpdateSoundDuration();

            // toggle the animation controls
            ToggleAnimationControls( true );

            // enable the export button
            btnExportSound.Enabled = true;
        }

        /// <summary>
        /// Play the current audio
        /// </summary>
        private void PlaySound()
        {
            // do we have the current sound?
            if ( soundPlayer != null )
            {
                // change the button symbol
                btnPlayPause.Text = "❚❚";

                // play the sound
                soundPlayer.Play();

                // show the audio time
                audioPlayer.Enabled = true;
            }
        }

        /// <summary>
        /// Clear the current sound data
        /// </summary>
        private void ClearSound()
        {
            // change the button symbol
            btnPlayPause.Text = "▶";

            // hide the audio time
            audioPlayer.Enabled = false;

            // remove the current playing time
            lblCurrFramePlaying.Text = "";

            // is the sound player active?
            if ( soundPlayer != null )
            {
                // stop the sound
                soundPlayer.Stop();

                // get rid of the sound player
                soundPlayer.Dispose();
                soundPlayer = null;
            }

            // dispose of the audio data
            if ( audioData != null )
            {
                audioData.Dispose();
                audioData = null;
            }

            // toggle the animation controls
            ToggleAnimationControls( false );

            // disable the export button
            btnExportSound.Enabled = false;
        }

        /// <summary>
        /// Get the audio duration
        /// </summary>
        private void UpdateSoundDuration()
        {
            // make sure we have the audio data
            if ( audioData == null )
                return;

            // make sure the label is visible
            if ( !lblCurrFramePlaying.Visible )
                lblCurrFramePlaying.Visible = true;

            // update the current playing time
            lblCurrFramePlaying.Text = audioData.CurrentTime.ToString( "mm':'ss'.'ff" ) + " \t\t - \t\t " + audioData.TotalTime.ToString( "mm':'ss'.'ff" );
        }

        /// <summary>
        /// Toggle the animation control buttons availability
        /// </summary>
        /// <param name="status">on/off</param>
        private void ToggleAnimationControls( bool status )
        {
            // toggle the animation controls buttons
            btnPlayPause.Enabled = status;
            btnStop.Enabled = status;
            btnSlow.Enabled = status;
            btnFast.Enabled = status;

            // toggle the directions controls
            foreach ( Control c in pnlDirections.Controls )
                c.Enabled = status;
        }

        /// <summary>
        /// Load the mobile data from an XElement
        /// </summary>
        /// <param name="item">XElement to load from</param>
        /// <param name="toFill">Mobile to compile</param>
        private void LoadMobileFromXElement( XElement item, ref Mobile toFill )
        {
            // make sure we have the XElement
            if ( item == null )
                return;

            // store the layer
            toFill.Layer = (Mobile.Layers)int.Parse( item.Attribute( "Layer" ).Value );

            // store the mobile type
            toFill.MobileType = (Mobile.MobileTypes)int.Parse( item.Attribute( "Type" ).Value );

            // store the mobile name
            toFill.Name = item.Attribute( "Name" ).Value;

            // store the gargoyle only flag
            if ( item.Attribute( "GargoyleOnly" ) != null )
                toFill.GargoyleItem = bool.Parse( item.Attribute( "GargoyleOnly" ).Value );

            // determine if this item is only for male
            if ( item.Attribute( "MaleOnly" ) != null )
                toFill.MaleOnly = bool.Parse( item.Attribute( "MaleOnly" ).Value );

            // determine if this item is only for female
            if ( item.Attribute( "FemaleOnly" ) != null )
                toFill.FemaleOnly = bool.Parse( item.Attribute( "FemaleOnly" ).Value );
        }

        /// <summary>
        /// Update the image in the preview
        /// </summary>
        /// <param name="img">Image to show</param>
        private void SetImage( Image img )
        {
            // set the image to show
            imgPreview.Image = img;

            // resize the image
            if ( img == null )
                imgPreview.Size = new Size( 0, 0 );
            else
                imgPreview.Size = new Size( (int)( imgPreview.Image.Width * ImageScale ), (int)( imgPreview.Image.Height * ImageScale ) );
        }

        /// <summary>
        /// Load all the legacy audio files waveforms
        /// </summary>
        /// <param name="leg">dictionary to fill with the data</param>
        private void LoadLegacyAudio( ref Dictionary<string, List<float>> leg )
        {
            // do nothing if we don't have the legacy sounds folder
            if ( !Directory.Exists( Path.Combine( Application.StartupPath, "Legacy Sound" ) ) )
                return;

            //get the legacy audio files
            string[] files = Directory.GetFiles( Path.Combine( Application.StartupPath, "Legacy Sound" ) );

            // load the legacy files data first
            if ( leg.Count == 0 )
            {
                // parse the files
                foreach ( string fileName in files )
                {
                    // open the file
                    using ( FileStream file = new FileStream( fileName, FileMode.Open ) )
                    {
                        // load the waveform points
                        List<float> points2 = AudioData.GetWavePoints( new WaveFileReader( file ) );

                        // add the points to the dictionary
                        leg.Add( Path.GetFileName( fileName ), points2 );
                    }

                    // allow the form to update
                    Application.DoEvents();
                }
            }
        }

        /// <summary>
        /// Compare the audio files
        /// </summary>
        /// <param name="points">Current audio file waveform points</param>
        /// <param name="leg">Dictionary with all the legacy audio files waveforms</param>
        /// <param name="currFileName">current file name we have (probably unknown)</param>
        private void CompareWithLegacyAudio( List<float> points, Dictionary<string, List<float>> leg, ref string currFileName )
        {
            //get the legacy audio files
            string[] files = Directory.GetFiles( Path.Combine( Application.StartupPath, "Legacy Sound" ) );

            // get only the files with the correct amount of points
            files = files.Where( f => leg[Path.GetFileName( f )].Count == points.Count ).ToArray();

            // closest possible match
            KeyValuePair<string, int> closest = new KeyValuePair<string, int>( "", 0 );

            // scan the files we found
            foreach ( string fileName in files )
            {
                // get the points from the dictionary
                List<float> points2 = leg[Path.GetFileName( fileName )];

                // if the list size is different, we can skip it
                if ( points2.Count != points.Count )
                    continue;

                // check if there are different points
                List<float> temp = points.Except( points2 ).ToList();

                // if there are no different points, we have found the correct file!
                if ( temp.Count == 0 )
                {
                    currFileName = Path.GetFileNameWithoutExtension( fileName );
                    return;
                }
                else if ( closest.Value == 0 || closest.Value > temp.Count )
                {
                    // store this value
                    closest = new KeyValuePair<string, int>( Path.GetFileNameWithoutExtension( fileName ), temp.Count );
                }

                // allow the form to update
                Application.DoEvents();
            }

            // if we got here we use the closest value
            if ( closest.Key != string.Empty )
                currFileName = closest.Key + " - guessed";
        }

        /// <summary>
        /// Get the selected cliloc from the combo
        /// </summary>
        /// <returns>current cliloc data</returns>
        private List<KeyValuePair<long, string>> GetSelectedCliloc()
        {
            // initialize the selection variable
            ClilocLanguages sel;

            // get the selected language
            Enum.TryParse<ClilocLanguages>( cmbLanguage.SelectedValue.ToString(), out sel );

            return clilocCache[sel];
        }

        /// <summary>
        /// Export the multi data into a CSV file
        /// </summary>
        private void ExportMultiData()
        {
            // get the selected multi
            MultiItem sel = multiCache.Where( m => m.ID == (int)cmbMulti.SelectedValue ).FirstOrDefault();

            // make sure we have a correct multi
            if ( sel == null )
                return;

            // set the browse description
            fbd.Description = "Which folder do you want to use to save the CSV and the item images?";

            // use the last directory picked or the application path
            fbd.SelectedPath = Settings.Default.LastSavedItemDirectory != string.Empty ? Settings.Default.LastSavedItemDirectory : Application.StartupPath;

            // create a temporary object to keep the dialogs on topmost
            Form f = new Form();
            f.TopMost = true;

            // show the folder browse dialog
            if ( fbd.ShowDialog( f ) == DialogResult.OK )
            {
                // backup of the loading status
                bool loadBck = Loading;

                // show the loading screen
                if ( !loadBck )
                    Loading = true;

                // store the folder for the next time
                Settings.Default.LastSavedItemDirectory = fbd.SelectedPath;

                // create the file path
                string filePath = Path.Combine( fbd.SelectedPath, "MultiParts.csv" );

                // create the text file
                using ( StreamWriter file = new StreamWriter( filePath ) )
                {
                    // write the header
                    file.WriteLine( "ItemID,x,y,z" );

                    // write all parts
                    foreach ( MultiItemPart part in sel.Parts )
                        file.WriteLine( part.ItemID + "," + part.X + "," + part.Y + "," + part.OriginalZ );
                }

                // get the unique parts list
                IEnumerable<MultiItemPart> parts = sel.Parts.Distinct();

                // parse all the unique parts
                foreach ( MultiItemPart part in parts )
                {
                    // get the item data
                    ItemData item = itemsCache.Where( it =>  it.ID == part.ItemID ).FirstOrDefault();

                    // get the item image
                    Bitmap img = item.GetItemImage( GamePath, Settings.Default.useOldKRItems );

                    // save the item image as PNG
                    if ( img != null )
                        img.Save( Path.Combine( fbd.SelectedPath, item.ID.ToString() + " - " + item.Name + ".png" ), ImageFormat.Png );
                }

                // hide the loading screen
                if ( !loadBck )
                    Loading = false;
            }
        }

        #endregion
    }

    // --------------------------------------------------------------
    #region TREEVIEW COMPARER/SEARCH
    // --------------------------------------------------------------

    /// <summary>
    /// Linq treeview search class
    /// </summary>
    public static class SOExtension
    {
        /// <summary>
        /// Create a single list of nodes from a treeview
        /// </summary>
        /// <param name="tv">treeview to convert</param>
        /// <returns>flattened tree view</returns>
        public static IEnumerable<TreeNode> FlattenTree( this TreeView tv )
        {
            return FlattenTree( tv.Nodes );
        }

        /// <summary>
        /// Merge all nodes of the tree view
        /// </summary>
        /// <param name="coll">current tree nodes collection</param>
        /// <returns>merged list of nodes</returns>
        public static IEnumerable<TreeNode> FlattenTree( this TreeNodeCollection coll )
        {
            return  coll.Cast<TreeNode>()
                        .Concat( coll.Cast<TreeNode>()
                        .SelectMany( x => FlattenTree( x.Nodes ) ) );
        }
    }

    /// <summary>
    /// Treeview comparator class
    /// </summary>
    public class AlphanumComparatorFast : IComparer
    {
        /// <summary>
        /// Compare 2 treenode objects
        /// </summary>
        /// <param name="x">first tree node</param>
        /// <param name="y">second tree node</param>
        /// <returns></returns>
        public int Compare( object x, object y )
        {
            // can't compare null nodes
            if ( x == null || y == null )
                return 0;

            // get the first node text
            string s1 = ( (TreeNode)x ).Text.ToLower();

            // empty strings goes at the top
            if ( string.IsNullOrEmpty( s1 ) )
                return 0;

            // get the second node text
            string s2 = ( (TreeNode)y ).Text.ToLower();

            // empty strings goes at the top
            if ( string.IsNullOrEmpty( s2 ) )
                return 0;

            // measure the strings
            int len1 = s1.Length;
            int len2 = s2.Length;

            // initialize the markers
            int marker1 = 0;
            int marker2 = 0;

            // walk through two the strings 1 character at time
            while ( marker1 < len1 && marker2 < len2 )
            {
                // get character from both strings
                char ch1 = s1[marker1];
                char ch2 = s2[marker2];

                // initialize string buffers to parse the strings
                char[] space1 = new char[len1];
                char[] space2 = new char[len2];

                // initialize the current position in the buffers
                int loc1 = 0;
                int loc2 = 0;

                do // check the first strings characters for digits
                {
                    // get a chraracter
                    space1[loc1++] = ch1;

                    // move to the next character in the string
                    marker1++;

                    // if there are still characters, we put it back in the buffer
                    if ( marker1 < len1 )
                        ch1 = s1[marker1];

                    else // string is over
                        break;

                } // we keep going as long as we still find digits
                while ( char.IsDigit( ch1 ) == char.IsDigit( space1[0] ) );

                do // check the second string characters for digits
                {
                    // get a chraracter
                    space2[loc2++] = ch2;

                    // move to the next character in the string
                    marker2++;

                    // if there are still characters, we put it back in the buffer
                    if ( marker2 < len2 )
                        ch2 = s2[marker2];

                    else // string is over
                        break;

                } // we keep going as long as we still find digits
                while ( char.IsDigit( ch2 ) == char.IsDigit( space2[0] ) );

                // build the strings with the digits we've found
                string str1 = new string(space1);
                string str2 = new string(space2);

                // initialize the result value
                int result;

                // is the first character in both strings a number?
                if ( char.IsDigit( space1[0] ) && char.IsDigit( space2[0] ) )
                {
                    // convert the strings to int
                    int thisNumericChunk = int.Parse(str1);
                    int thatNumericChunk = int.Parse(str2);

                    // compare the 2 numbers
                    result = thisNumericChunk.CompareTo( thatNumericChunk );
                }
                else // no numbers, we compare the strings normally
                    result = str1.CompareTo( str2 );

                // if we have a result we return it
                if ( result != 0 )
                    return result;
            }

            // at this point if we have no result, we compare the string length
            return len1 - len2;
        }
    }

    #endregion
}
