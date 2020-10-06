using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UO_EC_Super_Viewer
{
    public class Mobile
    {
        // --------------------------------------------------------------
        #region ENUMS/STRUCTURES
        // --------------------------------------------------------------

        /// <summary>
        /// Types of mobiles (used in the XML)
        /// </summary>
        public enum MobileTypes
        {
            None = -1,
            Monster = 0,
            Sea = 1,
            Animal = 2,
            Human = 3,
            Equipment = 4,
            Mount = 5,
        }

        /// <summary>
        /// Layer of the mobile (used in the XML)
        /// </summary>
        public enum Layers
        {
            None = -1,
            Character = 0,
            Cloak = 1,
            Hair = 2,
            FacialHair = 3,
            Tops = 4,
            Bottoms = 5,
            Footwear = 6,
            Legs = 7,
            Torso = 8,
            Arms = 9,
            Hands = 10,
            Bottoms2 = 11,
            Tops2 = 12,
            Torso2 = 13,
            Waist = 14,
            Neck = 15,
            Head = 16,
            RightHand = 17,
            LeftHand = 18,
            Bracelet = 19,
            Earrings = 20,
            Rings = 21,
            Mount = 995,
            Mobile = 999,
        }

        #endregion

        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        private int m_BodyId;
        private Layers m_Layer = Layers.Mobile;
        private MobileTypes m_MobileType = MobileTypes.Monster;

        private bool m_gargoyleItem = false;

        private bool m_maleOnly = false;
        private bool m_femaleOnly = false;

        private string m_name;

        private UOAnimation[] m_Actions = new UOAnimation[100];

        /// <summary>
        /// VD File type
        /// </summary>
        private int m_FileType;

        /// <summary>
        /// VD animation type
        /// </summary>
        private short m_AnimType = -1;

        /// <summary>
        /// Number of animations in the VD file
        /// </summary>
        private int m_VDLength;

        private bool m_VDOriginal = false;

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Flag: Has Dispose already been called?
        /// </summary>
        public bool disposed = false;

        /// <summary>
        /// Mobile body ID
        /// </summary>
        public int BodyId
        {
            get { return m_BodyId; }
            set { m_BodyId = value; }
        }

        /// <summary>
        /// Layer of the mobile
        /// </summary>
        public Layers Layer
        {
            get { return m_Layer; }
            set { m_Layer = value; }
        }

        /// <summary>
        /// Mobile Type
        /// </summary>
        public MobileTypes MobileType
        {
            get { return m_MobileType; }
            set { m_MobileType = value; }
        }

        /// <summary>
        /// Mobile name
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        /// <summary>
        /// Is this item for gargoyles? (gargoyles can use ONLY this type of items, while humans and elves can't)
        /// </summary>
        public bool GargoyleItem
        {
            get { return m_gargoyleItem; }
            set { m_gargoyleItem = value; }
        }

        /// <summary>
        /// Indicates this item is only for male bodies
        /// </summary>
        public bool MaleOnly
        {
            get { return m_maleOnly; }
            set { m_maleOnly = value; }
        }

        /// <summary>
        /// Indicates this item is only for female bodies
        /// </summary>
        public bool FemaleOnly
        {
            get { return m_femaleOnly; }
            set { m_femaleOnly = value; }
        }

        /// <summary>
        /// Indicates this item can be used by male and female alike
        /// </summary>
        public bool Unisex
        {
            get { return ( !m_femaleOnly && !m_maleOnly); }
        }

        /// <summary>
        /// List of all the action animations for the mobile
        /// </summary>
        public UOAnimation[] Actions
        {
            get { return m_Actions; }
            set { m_Actions = value; }
        }

        /// <summary>
        /// CC animation type: 13 (low), 22 (high), 35 (people/equipment)
        /// </summary>
        public short CCAnimationType
        {
            get
            {
                // is the animation type unassigned?
                if ( m_AnimType == -1)
                {
                    // count the actions available
                    int actionsCount = Actions.Where( c => c != null ).ToArray().Count();

                    // more than 22 is people/equipment for sure
                    if ( actionsCount > 22 )
                        m_AnimType = 2;

                    // less than 13 is low anim
                    else if ( actionsCount <= 13 )
                        m_AnimType = 1;

                    else // any other case is high anim
                        m_AnimType = 0;
                }

                return m_AnimType;
            }
        }

        /// <summary>
        /// flag that indicates this mobile has been loaded from a VD file originally
        /// </summary>
        public bool VDOriginal
        {
            get { return m_VDOriginal; }
        }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create the mobile data
        /// </summary>
        public Mobile( int bodyId )
        {
            // set the body ID for the mobile
            BodyId = bodyId;
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Load an animation from a VD file
        /// </summary>
        /// <param name="vdFileName">VD file name</param>
        public void LoadFromVD( string vdFileName )
        {
            // make sure the file exist
            if ( !File.Exists( vdFileName ) )
                return;

            // flag that the mobile has been loaded from a VD file
            m_VDOriginal = true;

            // open the file
            using ( FileStream fs = new FileStream( vdFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
            {
                // start reading the file
                using ( BinaryReader reader = new BinaryReader( fs ) )
                {
                    // read the vd file header
                    ReadVDHeader( reader );

                    // read the vd animations
                    ReadVDAnimations( reader );
                }
            }
        }

        /// <summary>
        /// Save the mobile animations into a VD file
        /// </summary>
        /// <param name="vdFileName">VD file name</param>
        public void ExportToVD( string vdFileName )
        {
            // is the VD anim type unknown?
            if ( m_AnimType == -1 )
            {
                // count the number of actions available
                UOAnimation[] acts = m_Actions.Where( c => c != null ).ToArray();

                // CC animation type.
                m_AnimType = (short)( acts.Length > 22 ? 2 : 0 ); // acts.Length > 13 ? 0 : 1 - Low anim disabled because cc actions does not match EC action
            }

            // break the file name in: path, file name and extension
            string fDir = Path.GetDirectoryName( vdFileName );
            string fName = Path.GetFileNameWithoutExtension( vdFileName );
            string fExt = Path.GetExtension( vdFileName );

            // name of the file to use
            string fn = String.Concat( fName, m_AnimType == 0 ? "_H" : m_AnimType == 1 ? "_L" : "_P", fExt );

            // add H/L/P to the vd file name in order to make it easy to find which body ID to use it with
            vdFileName = Path.Combine( fDir, fn );

            // if the file exist, and the user DO NOT want to override it, we skip the animation
            if ( File.Exists( vdFileName ) && MessageBox.Show( "The file:\n\n" + fn + "\n\nALREADY EXIST!\n\nOK to overwrite\nCANCEL to abort", "File Exist", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning ) == DialogResult.Cancel )
                return;

            // open the output file
            using ( FileStream fs = new FileStream( vdFileName, FileMode.Create, FileAccess.Write, FileShare.Write ) )
            {
                // open the binary writer
                using ( BinaryWriter writer = new BinaryWriter( fs ) )
                {
                    // write the VD header
                    WriteVDHeader( writer );

                    // write the VD animations
                    WriteVDAnimations( writer );
                }
            }

            // force garbage collection
            GC.WaitForPendingFinalizers();
            GC.GetTotalMemory( true );
        }

        /// <summary>
        /// Save the mobile animations into a VD file
        /// </summary>
        /// <param name="vdFileName">VD file name</param>
        public void ExportToVD( string vdFileName, ExportVD xp )
        {
            // is the VD anim type unknown?
            if ( m_AnimType == -1 )
                m_AnimType = (short) ( xp.toExport.Count == 35 ? 2 : xp.toExport.Count == 22 ? 0 : 1 );

            // break the file name in: path, file name and extension
            string fDir = Path.GetDirectoryName( vdFileName );
            string fName = Path.GetFileNameWithoutExtension( vdFileName );
            string fExt = Path.GetExtension( vdFileName );

            // name of the file to use
            string fn = String.Concat( fName, xp.fileAppend, fExt );

            // add H/L/P to the vd file name in order to make it easy to find which body ID to use it with
            vdFileName = Path.Combine( fDir, fn );

            // if the file exist, and the user DO NOT want to override it, we skip the animation
            if ( File.Exists( vdFileName ) && MessageBox.Show( "The file:\n\n" + fn + "\n\nALREADY EXIST!\n\nOK to overwrite\nCANCEL to abort", "File Exist", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning ) == DialogResult.Cancel )
                return;

            // open the output file
            using ( FileStream fs = new FileStream( vdFileName, FileMode.Create, FileAccess.Write, FileShare.Write ) )
            {
                // open the binary writer
                using ( BinaryWriter writer = new BinaryWriter( fs ) )
                {
                    // write the VD header
                    WriteVDHeader( writer );

                    // write the VD animations
                    WriteVDAnimations( writer, xp.toExport );
                }
            }

            // force garbage collection
            GC.WaitForPendingFinalizers();
            GC.GetTotalMemory( true );
        }

        /// <summary>
        /// Delete the animations and free the memory
        /// </summary>
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose( true );

            // Suppress finalization.
            GC.SuppressFinalize( this );

            // force garbage collection
            GC.WaitForPendingFinalizers();
            GC.GetTotalMemory( true );
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Write the VD file header
        /// </summary>
        /// <param name="writer">binary writer attached to the file</param>
        private void WriteVDHeader( BinaryWriter writer )
        {
            // file type flag (constant value)
            writer.Write( (short)6 );

            // write the animation type
            writer.Write( m_AnimType );
        }

        /// <summary>
        /// Read VD header
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        /// <returns></returns>
        private bool ReadVDHeader( BinaryReader reader )
        {
            // get the file type
            m_FileType = reader.ReadInt16();

            // fileType must be = 6 or it's not a valid animation file
            if ( m_FileType != 6 )
            {
                return false;
            }

            // get the animation type
            m_AnimType = reader.ReadInt16();

            // high anim (animals 0-200 body ID)
            if ( m_AnimType == 0 )
            {
                m_VDLength = 22;
            }
            // low anim (monsters 200-400 body ID)
            else if ( m_AnimType == 1 )
            {
                m_VDLength = 13;
            }
            // character anim (people/equipment 400+ body ID)
            else if ( m_AnimType == 2 )
            {
                m_VDLength = 35;
            }
            else // unknown type
                return false;

            return true;
        }

        /// <summary>
        /// Read VD animations
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        /// <returns></returns>
        private bool ReadVDAnimations( BinaryReader reader )
        {
            // initialize the action idex
            int actionIndex = -1;

            // scan the animations count
            for ( int i = 0; i < m_VDLength; i++ )
            {
                // temporary list containing all the frame sets
                List<byte[]> framesets = new List<byte[]>();

                // each animation in a VD file is a single direction, so we need to load 5 in order to have a full animation
                for ( int direction = 0; direction < 5; direction++ )
                {
                    // starting position of the animation
                    int startPos = reader.ReadInt32();

                    // byte size of the animation
                    int size = reader.ReadInt32();

                    // unused flag (is always 0)
                    reader.ReadInt32();

                    // undefined animation data
                    if ( startPos <= -1 || startPos >= reader.BaseStream.Length || size == 0 )
                        continue;

                    // store the current position (we'll go back here after we loaded the animation of this direction)
                    long pos = (int)reader.BaseStream.Position;

                    // move to the animation position
                    reader.BaseStream.Seek( startPos, SeekOrigin.Begin );

                    // add the frame set to the list
                    framesets.Add( reader.ReadBytes( size ) );

                    // move the to the next animation header starting point
                    reader.BaseStream.Seek( pos, SeekOrigin.Begin );

                    // prevent the app from freezing
                    Application.DoEvents();
                }

                // increase the action index
                actionIndex++;

                // make sure we have 5 framesets or the animation is incomplete
                if ( framesets.Count < 5 )
                    continue;

                // load the animation data
                UOAnimation anim = new UOAnimation( framesets );

                // store the animation
                Actions[actionIndex] = anim;

                // prevent the app from freezing
                Application.DoEvents();
            }

            return true;
        }

        /// <summary>
        /// Write the animations into the VD file
        /// </summary>
        /// <param name="writer">binary writer attached to the file</param>
        private void WriteVDAnimations( BinaryWriter writer, List<int> toExport = null )
        {
            // initialize the animations array
            UOAnimation[] acts;

            // do we have the list of animations to export?
            if ( toExport != null )
            {
                // get the amount of animations we need
                m_VDLength = toExport.Count;

                // initialize the array length
                acts = new UOAnimation[m_VDLength];

                // copy the animations
                for ( int i = 0; i < m_VDLength; i++ )
                {
                    // copy the animation only if we have the index (otherwise it will remain null)
                    if ( toExport[i] != -1 )
                        acts[i] = Actions[toExport[i]];
                }
            }
            else // create a CC compatible list of actions of this mobile
                acts = ConvertToCCAnimationsList();

            // current position inside the file
            long headerPos = writer.BaseStream.Position;

            // calculate the first animation position
            long animPos = writer.BaseStream.Position + (12 * m_VDLength * 5);

            // scan the animations count
            for ( int i = 0; i < m_VDLength; i++ )
            {
                // get the animation
                UOAnimation anim = acts[i];

                // does the animation for this action exist?
                if ( anim == null )
                {
                    // make sure we're pointed to the headers position
                    writer.BaseStream.Seek( headerPos, SeekOrigin.Begin );

                    // we need to write 5 empty frameset to skip 1 full animation
                    for ( int direction = 0; direction < 5; direction++ )
                    {
                        // store an empty start position
                        writer.Write( (int)-1 );

                        // store an empty size
                        writer.Write( (int)-1 );

                        // store the useless extra flag
                        writer.Write( (int)0 );
                    }

                    // update the current position
                    headerPos = writer.BaseStream.Position;
                }
                else // animation exist
                {
                    // each animation in a VD file is a single direction, so we need to create 5 separate animations to make a complete set.
                    for ( int direction = 0; direction < 5; direction++ )
                    {
                        // store the VD data for the current animation direction
                        anim.ExportAnimationToVD( writer, direction, ref headerPos, ref animPos );

                        // allow the form to update before we move to the next
                        Application.DoEvents();
                    }

                    // dispose of the frame images
                    anim.DisposeFrames();
                }

                // allow the form to update before we move to the next
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Convert the EC animations list to a CC compatible one so it can be exported to a VD file
        /// </summary>
        /// <returns>CC compatible animations list</returns>
        private UOAnimation[] ConvertToCCAnimationsList()
        {
            // is the animation length unassigned?
            if ( m_VDLength == 0 )
            {
                // count the actions available
                int actionsCount = Actions.Where( c => c != null ).ToArray().Count();

                // more than 22 is people/equipment for sure
                if ( actionsCount > 22 )
                    m_VDLength = 35;

                // less than 13 is low anim
                else if ( actionsCount <= 13 && false ) // disabled because low animations on CC have different actions set
                    m_VDLength = 13;

                else // any other case is high anim
                    m_VDLength = 22;
            }

            // initialize the animations array
            UOAnimation[] output = new UOAnimation[m_VDLength];

            // people and equipment uses the same animations id (if it's originally from a VD file, we keep the animations list as it is)
            if ( m_VDLength == 35 || m_VDOriginal )
            {
                // copy the animations
                for ( int i = 0; i < m_VDLength; i++ )
                    output[i] = Actions[i];
            }
            else // creatures uses different IDs
            {
                output[0] = GetAction( 22 );
                output[1] = GetAction( 25 );
                output[2] = GetAction( 2 );
                output[3] = GetAction( 3 );
                output[4] = GetAction( 4 );
                output[5] = GetAction( 5 );
                output[6] = GetAction( 6 );
                output[7] = GetAction( 7 );
                output[8] = GetAction( 8 );
                output[9] = GetAction( 9 );
                output[10] = GetAction( 10 );
                output[11] = GetAction( 11 );
                output[12] = GetAction( 12 );

                // only for high animations
                if ( m_VDLength > 13 )
                {
                    output[13] = GetAction( 13 );
                    output[14] = GetAction( 14 );
                    output[15] = GetAction( 15 );
                    output[16] = GetAction( 16 );
                    output[17] = GetAction( 28 );
                    output[18] = GetAction( 26 );
                    output[19] = GetAction( 19 );
                    output[20] = GetAction( 20 );
                    output[21] = GetAction( 21 );
                }
            }

            return output;
        }

        private UOAnimation GetAction( int actionId )
        {
            // action is missing?
            if ( actionId >= Actions.Length )
                return null;

            return Actions[actionId];
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose( bool disposing )
        {
            // has the frame already been disposed?
            if ( disposed )
                return;

            // are we disposing of this frame?
            if ( disposing )
            {
                // scan all the animations
                for ( int i = 0; i < Actions.Length; i++ )
                {
                    // get rid of the animation
                    if ( Actions[i] != null && !Actions[i].disposed )
                    {
                        Actions[i].Dispose();
                        Actions[i] = null;
                    }
                }
            }

            // flag the frame as disposed
            disposed = true;
        }

        #endregion
    }
}
