using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using UO_EC_Super_Viewer.Properties;

namespace UO_EC_Super_Viewer
{
    public partial class ExportVD : Form
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// index conversion between cc and ec for high
        /// </summary>
        Dictionary<int, int> actionsConvertHigh = new Dictionary<int, int>();

        /// <summary>
        /// index conversion between cc and ec for low
        /// </summary>
        Dictionary<int, int> actionsConvertLow = new Dictionary<int, int>();

        /// <summary>
        /// index conversion between cc and ec for people
        /// </summary>
        Dictionary<int, int> actionsConvertPeople = new Dictionary<int, int>();

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Actions list to use
        /// </summary>
        public Dictionary<int, string> Actions = new Dictionary<int, string>();

        /// <summary>
        /// Mobile to parse
        /// </summary>
        public Mobile CurrentMobile { get; set; }

        /// <summary>
        /// List of the index of the animations to export
        /// </summary>
        public List<int> toExport = new List<int>();

        /// <summary>
        /// string to append at the end of the file name
        /// </summary>
        public string fileAppend;

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        public ExportVD()
        {
            InitializeComponent();
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL EVENTS
        // --------------------------------------------------------------

        /// <summary>
        /// initialize the form
        /// </summary>
        private void ExportVD_Load( object sender, EventArgs e )
        {

        }

        /// <summary>
        /// Change between low/high/people
        /// </summary>
        private void chk_CheckedChanged( object sender, EventArgs e )
        {
            // get the current checkbox
            RadioButton curr = (RadioButton)sender;

            //toggle the panels based on which animation type has been chosen
            if ( curr == chkLow )
            {
                pnlLow.Visible = true;
                pnlHigh.Visible = false;
                pnlPeople.Visible = false;

                chkHigh.Checked = false;
                chkPeople.Checked = false;
            }
            else if ( curr == chkHigh )
            {
                pnlLow.Visible = false;
                pnlHigh.Visible = true;
                pnlPeople.Visible = false;

                chkLow.Checked = false;
                chkPeople.Checked = false;
            }
            else if ( curr == chkPeople )
            {
                pnlLow.Visible = false;
                pnlHigh.Visible = false;
                pnlPeople.Visible = true;

                chkLow.Checked = false;
                chkHigh.Checked = false;
            }
        }

        /// <summary>
        /// Check clicked
        /// </summary>
        private void chk_Click( object sender, EventArgs e )
        {
            // get the current checkbox
            RadioButton curr = (RadioButton)sender;

            // make sure the button is checked
            if ( !curr.Checked )
                curr.Checked = true;
        }

        /// <summary>
        /// Confirm export
        /// </summary>
        private void btnOk_Click( object sender, EventArgs e )
        {
            // get the current export option
            Panel pnl = pnlLow.Visible ? pnlLow : pnlHigh.Visible ? pnlHigh : pnlPeople;

            // create the string to append to the file
            fileAppend = pnlLow.Visible ? "_L" : pnlHigh.Visible ? "_H" : "_P";

            // create a regex to sort the combos
            Regex re = new Regex(@"\d+$");

            // get the combobox for the active tab
            IEnumerable<ComboBox> cmbs = from cmb in pnl.Controls.OfType<ComboBox>()
                                         orderby int.Parse( re.Match( cmb.Name ).Value )
                                         select cmb;

            // fill the list of actions id to export
            foreach ( ComboBox cmb in cmbs )
                toExport.Add( int.Parse( cmb.SelectedValue.ToString() ) );

            // save the actions seleccted for the next time
            SaveConvertActions();

            // set the dialog result as OK
            DialogResult = DialogResult.OK;

            // close the form
            Close();
        }

        /// <summary>
        /// Send the cancel result
        /// </summary>
        private void btnCancel_Click( object sender, EventArgs e )
        {
            // set the dialog result as Cancel
            DialogResult = DialogResult.Cancel;

            // close the form
            Close();
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Fill all the combos with the correct data sources
        /// </summary>
        public void PopulateForm()
        {
            // no mobile specified? we can get out
            if ( CurrentMobile == null )
                return;

            // initialize the dictionary
            InitializeConvert();

            // load the stored data for the conversion dictioanaries
            LoadStoredIndex();

            // get the actions available
            var actions = from act in Actions
                          orderby act.Key
                          select new
                          {
                              act.Key,
                              Value = act.Key != -1 ? act.Value + " (" + act.Key + ")" : act.Value
                          };

            // is this a piece of equipment?
            if ( CurrentMobile.MobileType == Mobile.MobileTypes.Equipment || CurrentMobile.MobileType == Mobile.MobileTypes.Human )
            {
                // select the people "tab"
                chkLow.Checked = false;
                chkHigh.Checked = false;
                chkPeople.Checked = true;
            }
            else // creature
            {
                // select the high "tab"
                chkPeople.Checked = false;
                chkLow.Checked = false;
                chkHigh.Checked = true;
            }

            // get the combobox for the people tab
            IEnumerable<ComboBox> peopleCmbs = from cmb in pnlPeople.Controls.OfType<ComboBox>()
                                               select cmb;

            // set the datasource for all the combos in the people tab
            foreach ( ComboBox cmb in peopleCmbs )
            {
                // set the data source
                cmb.DataSource = actions.ToList();

                // set the data to use
                cmb.DisplayMember = "Value";
                cmb.ValueMember = "Key";

                // get the index for this action
                int idx = int.Parse( cmb.Name.Replace( "cmbPeople", "" ) );

                // set the default actions in this combo
                cmb.SelectedValue = Actions.ContainsKey( idx ) ? ( actionsConvertPeople.ContainsKey( idx ) && actionsConvertPeople[idx] != -1 ? actionsConvertPeople[idx] : ( CurrentMobile.MobileType == Mobile.MobileTypes.Equipment || CurrentMobile.MobileType == Mobile.MobileTypes.Human ? idx : -1 ) ) : -1;
            }

            // get the combobox for the low tab
            IEnumerable<ComboBox> lowCmbs = from cmb in pnlLow.Controls.OfType<ComboBox>()
                                            select cmb;

            // set the datasource for all the combos in the low type
            foreach ( ComboBox cmb in lowCmbs )
            {
                // set the data source
                cmb.DataSource = actions.ToList();

                // set the data to use
                cmb.DisplayMember = "Value";
                cmb.ValueMember = "Key";

                // get the index for this action
                int idx = int.Parse( cmb.Name.Replace( "cmbLow", "" ) );

                // set the default actions in this combo
                cmb.SelectedValue = Actions.ContainsKey( idx ) ? ( actionsConvertLow.ContainsKey( idx ) && actionsConvertLow[idx] != -1 ? actionsConvertLow[idx] : -1 ) : -1;
            }

            // get the combobox for the high tab
            IEnumerable<ComboBox> highCmbs = from cmb in pnlHigh.Controls.OfType<ComboBox>()
                                             select cmb;

            // set the datasource for all the combos in the high type
            foreach ( ComboBox cmb in highCmbs )
            {
                // set the data source
                cmb.DataSource = actions.ToList();

                // set the data to use
                cmb.DisplayMember = "Value";
                cmb.ValueMember = "Key";

                // get the index for this action
                int idx = int.Parse( cmb.Name.Replace( "cmbHigh", "" ) );

                // set the default actions in this combo
                cmb.SelectedValue = Actions.ContainsKey( idx ) ? ( actionsConvertHigh.ContainsKey( idx ) && actionsConvertHigh[idx] != -1 ? actionsConvertHigh[idx] : -1 ) : -1;
            }

            // reset the list of the animations to export
            toExport = new List<int>();
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Initialize the covnersion dictionary
        /// </summary>
        private void InitializeConvert()
        {
            // fill the conversion dictionary (high)
            actionsConvertHigh.Clear();

            // equip/body
            if ( CurrentMobile.MobileType == Mobile.MobileTypes.Equipment || CurrentMobile.MobileType == Mobile.MobileTypes.Human )
            {
                actionsConvertHigh.Add( 0, -1 );
                actionsConvertHigh.Add( 1, -1 );
                actionsConvertHigh.Add( 2, -1 );
                actionsConvertHigh.Add( 3, -1 );
                actionsConvertHigh.Add( 4, -1 );
                actionsConvertHigh.Add( 5, -1 );
                actionsConvertHigh.Add( 6, -1 );
                actionsConvertHigh.Add( 7, -1 );
                actionsConvertHigh.Add( 8, -1 );
                actionsConvertHigh.Add( 9, -1 );
                actionsConvertHigh.Add( 10, -1 );
                actionsConvertHigh.Add( 11, -1 );
                actionsConvertHigh.Add( 12, -1 );
                actionsConvertHigh.Add( 13, -1 );
                actionsConvertHigh.Add( 14, -1 );
                actionsConvertHigh.Add( 15, -1 );
                actionsConvertHigh.Add( 16, -1 );
                actionsConvertHigh.Add( 17, -1 );
                actionsConvertHigh.Add( 18, -1 );
                actionsConvertHigh.Add( 19, -1 );
                actionsConvertHigh.Add( 20, -1 );
                actionsConvertHigh.Add( 21, -1 );
            }
            else // creatures
            {
                actionsConvertHigh.Add( 0, 22 );
                actionsConvertHigh.Add( 1, 25 );
                actionsConvertHigh.Add( 2, 2 );
                actionsConvertHigh.Add( 3, 3 );
                actionsConvertHigh.Add( 4, 4 );
                actionsConvertHigh.Add( 5, 5 );
                actionsConvertHigh.Add( 6, 6 );
                actionsConvertHigh.Add( 7, 7 );
                actionsConvertHigh.Add( 8, 8 );
                actionsConvertHigh.Add( 9, 9 );
                actionsConvertHigh.Add( 10, 10 );
                actionsConvertHigh.Add( 11, 11 );
                actionsConvertHigh.Add( 12, 23 );
                actionsConvertHigh.Add( 13, 12 );
                actionsConvertHigh.Add( 14, 14 );
                actionsConvertHigh.Add( 15, 15 );
                actionsConvertHigh.Add( 16, 16 );
                actionsConvertHigh.Add( 17, 25 );
                actionsConvertHigh.Add( 18, 26 );
                actionsConvertHigh.Add( 19, 19 );
                actionsConvertHigh.Add( 20, 20 );
                actionsConvertHigh.Add( 21, 21 );
            }

            // fill the conversion dictionary (low)
            actionsConvertLow.Clear();
            actionsConvertLow.Add( 0, -1 );
            actionsConvertLow.Add( 1, -1 );
            actionsConvertLow.Add( 2, -1 );
            actionsConvertLow.Add( 3, -1 );
            actionsConvertLow.Add( 4, -1 );
            actionsConvertLow.Add( 5, -1 );
            actionsConvertLow.Add( 6, -1 );
            actionsConvertLow.Add( 7, -1 );
            actionsConvertLow.Add( 8, -1 );
            actionsConvertLow.Add( 9, -1 );
            actionsConvertLow.Add( 10, -1 );
            actionsConvertLow.Add( 11, -1 );
            actionsConvertLow.Add( 12, -1 );

            // fill the conversion dictionary (people)
            actionsConvertPeople.Clear();
            actionsConvertPeople.Add( 0, -1 );
            actionsConvertPeople.Add( 1, -1 );
            actionsConvertPeople.Add( 2, -1 );
            actionsConvertPeople.Add( 3, -1 );
            actionsConvertPeople.Add( 4, -1 );
            actionsConvertPeople.Add( 5, -1 );
            actionsConvertPeople.Add( 6, -1 );
            actionsConvertPeople.Add( 7, -1 );
            actionsConvertPeople.Add( 8, -1 );
            actionsConvertPeople.Add( 9, -1 );
            actionsConvertPeople.Add( 10, -1 );
            actionsConvertPeople.Add( 11, -1 );
            actionsConvertPeople.Add( 12, -1 );
            actionsConvertPeople.Add( 13, -1 );
            actionsConvertPeople.Add( 14, -1 );
            actionsConvertPeople.Add( 15, -1 );
            actionsConvertPeople.Add( 16, -1 );
            actionsConvertPeople.Add( 17, -1 );
            actionsConvertPeople.Add( 18, -1 );
            actionsConvertPeople.Add( 19, -1 );
            actionsConvertPeople.Add( 20, -1 );
            actionsConvertPeople.Add( 21, -1 );
            actionsConvertPeople.Add( 22, -1 );
            actionsConvertPeople.Add( 23, -1 );
            actionsConvertPeople.Add( 24, -1 );
            actionsConvertPeople.Add( 25, -1 );
            actionsConvertPeople.Add( 26, -1 );
            actionsConvertPeople.Add( 27, -1 );
            actionsConvertPeople.Add( 28, -1 );
            actionsConvertPeople.Add( 29, -1 );
            actionsConvertPeople.Add( 30, -1 );
            actionsConvertPeople.Add( 31, -1 );
            actionsConvertPeople.Add( 32, -1 );
            actionsConvertPeople.Add( 33, -1 );
            actionsConvertPeople.Add( 34, -1 );
        }

        /// <summary>
        /// Load the saved index for the conversion
        /// </summary>
        private void LoadStoredIndex()
        {
            // equipment/body
            if ( CurrentMobile.MobileType == Mobile.MobileTypes.Equipment || CurrentMobile.MobileType == Mobile.MobileTypes.Human )
            {
                // do we have a saved array for the low?
                if ( Settings.Default.VDLow_Equip != null && Settings.Default.VDLow_Equip.Count > 0 )
                {
                    // load the values
                    for ( int i = 0; i < Settings.Default.VDLow_Equip.Count; i++ )
                        actionsConvertLow[i] = int.Parse( Settings.Default.VDLow_Equip[i] );
                }

                // do we have a saved array for the high?
                if ( Settings.Default.VDHigh_Equip != null && Settings.Default.VDHigh_Equip.Count > 0 )
                {
                    // load the values
                    for ( int i = 0; i < Settings.Default.VDHigh_Equip.Count; i++ )
                        actionsConvertHigh[i] = int.Parse( Settings.Default.VDHigh_Equip[i] );
                }

                // do we have a saved array for the people?
                if ( Settings.Default.VDPeople_Equip != null && Settings.Default.VDPeople_Equip.Count > 0 )
                {
                    // load the values
                    for ( int i = 0; i < Settings.Default.VDPeople_Equip.Count; i++ )
                        actionsConvertPeople[i] = int.Parse( Settings.Default.VDPeople_Equip[i] );
                }
            }
            else // creatures
            {
                // do we have a saved array for the low?
                if ( Settings.Default.VDLow_Creatures != null && Settings.Default.VDLow_Creatures.Count > 0 )
                {
                    // load the values
                    for ( int i = 0; i < Settings.Default.VDLow_Creatures.Count; i++ )
                        actionsConvertLow[i] = int.Parse( Settings.Default.VDLow_Creatures[i] );
                }

                // do we have a saved array for the high?
                if ( Settings.Default.VDHigh_Creatures != null && Settings.Default.VDHigh_Creatures.Count > 0 )
                {
                    // load the values
                    for ( int i = 0; i < Settings.Default.VDHigh_Creatures.Count; i++ )
                        actionsConvertHigh[i] = int.Parse( Settings.Default.VDHigh_Creatures[i] );
                }

                // do we have a saved array for the people?
                if ( Settings.Default.VDPeople_Creatures != null && Settings.Default.VDPeople_Creatures.Count > 0 )
                {
                    // load the values
                    for ( int i = 0; i < Settings.Default.VDPeople_Creatures.Count; i++ )
                        actionsConvertPeople[i] = int.Parse( Settings.Default.VDPeople_Creatures[i] );
                }
            }
        }

        /// <summary>
        /// Save the convert actions order
        /// </summary>
        private void SaveConvertActions()
        {
            // equipment/body
            if ( CurrentMobile.MobileType == Mobile.MobileTypes.Equipment || CurrentMobile.MobileType == Mobile.MobileTypes.Human )
            {
                // low
                if ( chkLow.Checked )
                {
                    // empty the list
                    Settings.Default.VDLow_Equip = new System.Collections.Specialized.StringCollection();

                    // fill the array again with the new values
                    for ( int i = 0; i < toExport.Count; i++ )
                        Settings.Default.VDLow_Equip.Add( toExport[i].ToString() );
                }
                // high
                else if ( chkHigh.Checked )
                {
                    // empty the list
                    Settings.Default.VDHigh_Equip = new System.Collections.Specialized.StringCollection();

                    // fill the array again with the new values
                    for ( int i = 0; i < toExport.Count; i++ )
                        Settings.Default.VDHigh_Equip.Add( toExport[i].ToString() );
                }
                // people
                else if ( chkPeople.Checked )
                {
                    // empty the list
                    Settings.Default.VDPeople_Equip = new System.Collections.Specialized.StringCollection();

                    // fill the array again with the new values
                    for ( int i = 0; i < toExport.Count; i++ )
                        Settings.Default.VDPeople_Equip.Add( toExport[i].ToString() );
                }
            }
            else // creatures
            {
                // low
                if ( chkLow.Checked )
                {
                    // empty the list
                    Settings.Default.VDLow_Creatures = new System.Collections.Specialized.StringCollection();

                    // fill the array again with the new values
                    for ( int i = 0; i < toExport.Count; i++ )
                        Settings.Default.VDLow_Creatures.Add( toExport[i].ToString() );
                }
                // high
                else if ( chkHigh.Checked )
                {
                    // empty the list
                    Settings.Default.VDHigh_Creatures = new System.Collections.Specialized.StringCollection();

                    // fill the array again with the new values
                    for ( int i = 0; i < toExport.Count; i++ )
                        Settings.Default.VDHigh_Creatures.Add( toExport[i].ToString() );
                }
                // people
                else if ( chkPeople.Checked )
                {
                    // empty the list
                    Settings.Default.VDPeople_Creatures = new System.Collections.Specialized.StringCollection();

                    // fill the array again with the new values
                    for ( int i = 0; i < toExport.Count; i++ )
                        Settings.Default.VDPeople_Creatures.Add( toExport[i].ToString() );
                }
            }

            // save the settings
            Settings.Default.Save();
        }

        #endregion
    }
}
