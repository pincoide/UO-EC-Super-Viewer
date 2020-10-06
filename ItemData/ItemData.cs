using Mythic.Package;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UO_EC_Super_Viewer.Properties;

namespace UO_EC_Super_Viewer
{
    public class ItemData
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        // unused stuff
        private ushort m_header;
        private bool m_unk;
        private byte m_unk7;
        private float m_unk2;
        private float m_unk3;
        private int m_fixedZero;
        private int m_oldID;
        private float m_unk6;
        private int m_unk_type;
        private byte m_unk8;
        private int m_unk9;
        private int m_unk10;
        private float m_unk11;
        private float m_unk12;
        private int m_unk13;
        private int m_unk16;
        private long m_int_flags;
        private long m_int_flags_full;
        //private ColourEntry m_radarCol;
        //private ItemAminationData m_appearance;
        //private ItemSittingAnimationData m_sittingAnimation;
        private List<KeyValuePair<int, int>> m_stackaliases = new List<KeyValuePair<int, int>>();

        // useful data
        private uint m_id;
        private uint m_nameIndex;
        private string m_name;
        private TileFlag m_flags;
        private TileFlag m_flags2;
        private Dictionary<TileArtProperties, int> m_props = new Dictionary<TileArtProperties, int>();
        private Dictionary<TileArtProperties, int> m_props2 = new Dictionary<TileArtProperties, int>();
        private int[] m_imgoff2D = new int[6];
        private int[] m_imgoffEC = new int[6];
        private MythicPackageFile m_image;
        private string m_fileName;

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Item Properties
        /// </summary>
        public enum TileArtProperties
        {
            Weight,
            Quality,
            Quantity,
            Height,
            Value,
            AcVc,
            Layer,
            off_C8,
            Animation,
            Race,
            Gender,
            Paperdoll,
        }

        /// <summary>
        /// Layer names used on the items
        /// </summary>
        public enum Layers
        {
            /// <summary>
            ///     Invalid layer.
            /// </summary>
            Invalid = 0x00,

            /// <summary>
            ///     One handed weapon.
            /// </summary>
            OneHanded = 0x01,

            /// <summary>
            ///     Two handed weapon or shield.
            /// </summary>
            TwoHanded = 0x02,

            /// <summary>
            ///     Shoes.
            /// </summary>
            Shoes = 0x03,

            /// <summary>
            ///     Pants.
            /// </summary>
            Pants = 0x04,

            /// <summary>
            ///     Shirts.
            /// </summary>
            Shirt = 0x05,

            /// <summary>
            ///     Helmets, hats, and masks.
            /// </summary>
            Helm = 0x06,

            /// <summary>
            ///     Gloves.
            /// </summary>
            Gloves = 0x07,

            /// <summary>
            ///     Rings.
            /// </summary>
            Ring = 0x08,

            /// <summary>
            ///     Talismans.
            /// </summary>
            Talisman = 0x09,

            /// <summary>
            ///     Gorgets and necklaces.
            /// </summary>
            Neck = 0x0A,

            /// <summary>
            ///     Hair.
            /// </summary>
            Hair = 0x0B,

            /// <summary>
            ///     Half aprons.
            /// </summary>
            Waist = 0x0C,

            /// <summary>
            ///     Torso, inner layer.
            /// </summary>
            InnerTorso = 0x0D,

            /// <summary>
            ///     Bracelets.
            /// </summary>
            Bracelet = 0x0E,

            /// <summary>
            ///     Face.
            /// </summary>
            Face = 0x0F,

            /// <summary>
            ///     Beards and mustaches.
            /// </summary>
            FacialHair = 0x10,

            /// <summary>
            ///     Torso, outer layer.
            /// </summary>
            MiddleTorso = 0x11,

            /// <summary>
            ///     Earings.
            /// </summary>
            Earrings = 0x12,

            /// <summary>
            ///     Arms and sleeves.
            /// </summary>
            Arms = 0x13,

            /// <summary>
            ///     Cloaks.
            /// </summary>
            Cloak = 0x14,

            /// <summary>
            ///     Backpacks.
            /// </summary>
            Backpack = 0x15,

            /// <summary>
            ///     Torso, outer layer.
            /// </summary>
            OuterTorso = 0x16,

            /// <summary>
            ///     Leggings, outer layer.
            /// </summary>
            OuterLegs = 0x17,

            /// <summary>
            ///     Leggings, inner layer.
            /// </summary>
            InnerLegs = 0x18,

            /// <summary>
            ///     Last valid non-internal layer. Equivalent to <c>Layer.InnerLegs</c>.
            /// </summary>
            LastUserValid = 0x18,

            /// <summary>
            ///     Mount item layer.
            /// </summary>
            Mount = 0x19,

            /// <summary>
            ///     Vendor 'buy pack' layer.
            /// </summary>
            ShopBuy = 0x1A,

            /// <summary>
            ///     Vendor 'resale pack' layer.
            /// </summary>
            ShopResale = 0x1B,

            /// <summary>
            ///     Vendor 'sell pack' layer.
            /// </summary>
            ShopSell = 0x1C,

            /// <summary>
            ///     Bank box layer.
            /// </summary>
            Bank = 0x1D,

            /// <summary>
            /// Unused, using this layer makes you invisible to other players. Strange.
            /// </summary>
            ///
            Reserved_1 = 0x1E,

            /// <summary>
            ///     Secure Trade Layer
            /// </summary>
            SecureTrade = 0x1F,
        }

        /// <summary>
        /// Flags used on the tile item
        /// </summary>
        [Flags]
        public enum TileFlag : ulong
        {
            None = 0,
            Background = 1,
            Weapon = 2,
            Transparent = 4,
            Translucent = 8,
            Wall = 16, // 0x0000000000000010
            Damaging = 32, // 0x0000000000000020
            Impassable = 64, // 0x0000000000000040
            Wet = 128, // 0x0000000000000080
            Ignored = 256, // 0x0000000000000100
            Surface = 512, // 0x0000000000000200
            Bridge = 1024, // 0x0000000000000400
            Generic = 2048, // 0x0000000000000800
            Window = 4096, // 0x0000000000001000
            NoShoot = 8192, // 0x0000000000002000
            ArticleA = 16384, // 0x0000000000004000
            ArticleAn = 32768, // 0x0000000000008000
            ArticleThe = ArticleAn | ArticleA, // 0x000000000000C000
            Mongen = 65536, // 0x0000000000010000
            Foliage = 131072, // 0x0000000000020000
            PartialHue = 262144, // 0x0000000000040000
            UseNewArt = 524288, // 0x0000000000080000
            Map = 1048576, // 0x0000000000100000
            Container = 2097152, // 0x0000000000200000
            Wearable = 4194304, // 0x0000000000400000
            LightSource = 8388608, // 0x0000000000800000
            Animation = 16777216, // 0x0000000001000000
            HoverOver = 33554432, // 0x0000000002000000
            ArtUsed = 67108864, // 0x0000000004000000
            Armor = 134217728, // 0x0000000008000000
            Roof = 268435456, // 0x0000000010000000
            Door = 536870912, // 0x0000000020000000
            StairBack = 1073741824, // 0x0000000040000000
            StairRight = 2147483648, // 0x0000000080000000
            NoHouse = 4294967296, // 0x0000000100000000
            NoDraw = 8589934592, // 0x0000000200000000
            Unused1 = 17179869184, // 0x0000000400000000
            AlphaBlend = 34359738368, // 0x0000000800000000
            NoShadow = 68719476736, // 0x0000001000000000
            PixelBleed = 137438953472, // 0x0000002000000000
            Unused2 = 274877906944, // 0x0000004000000000
            PlayAnimOnce = 549755813888, // 0x0000008000000000
            MultiMovable = 1099511627776, // 0x0000010000000000
        }

        /// <summary>
        /// Item ID
        /// </summary>
        public uint ID
        {
            get { return m_id; }
        }

        /// <summary>
        /// Item name
        /// </summary>
        public string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// Item image data to create the actual DDS image
        /// </summary>
        public MythicPackageFile Image
        {
            get { return m_image; }
            set { m_image = value; }
        }

        /// <summary>
        /// Item Flags
        /// </summary>
        public TileFlag Flags
        {
            get { return m_flags2; }
        }

        /// <summary>
        /// Item properties
        /// </summary>
        public Dictionary<TileArtProperties, int> Properties
        {
            get { return m_props; }
        }

        /// <summary>
        /// EC drawing offsets
        /// </summary>
        public int[] ECOffsets
        {
            get { return m_imgoffEC; }
        }

        /// <summary>
        /// CC drawing offsets
        /// </summary>
        public int[] CCOffsets
        {
            get { return m_imgoff2D; }
        }

        /// <summary>
        /// Image file name (from the dictionary)
        /// </summary>
        public string FileName
        {
            get { return m_fileName;  }
        }

        #endregion


        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a new item data
        /// </summary>
        /// <param name="reader">Current binary reader for the item</param>
        public ItemData( BinaryReader reader, ref List<string> stringDictionary )
        {
            // version
            m_header = reader.ReadUInt16();

            // StringDictionary Offset
            m_nameIndex = reader.ReadUInt32();

            // get the item name
            m_name = stringDictionary[(int)m_nameIndex];

            // item ID
            m_id = reader.ReadUInt32();

            // unknown flag
            m_unk = Convert.ToBoolean( reader.ReadByte() );

            // unknown value
            m_unk7 = reader.ReadByte();

            // unknown float value
            m_unk2 = reader.ReadSingle();

            // unknown float value
            m_unk3 = reader.ReadSingle();

            // 0?
            m_fixedZero = reader.ReadInt32();

            // another unknown value
            m_oldID = reader.ReadInt32();

            // unknown float value
            m_unk6 = reader.ReadInt32();

            // yet another unknown value
            m_unk_type = reader.ReadInt32();

            // unknown byte value
            m_unk8 = reader.ReadByte();

            // unknown value
            m_unk9 = reader.ReadInt32();

            // unknown value
            m_unk10 = reader.ReadInt32();

            // float light value
            m_unk11 = reader.ReadSingle();

            // float light value
            m_unk12 = reader.ReadSingle();

            // unknown value
            m_unk13 = reader.ReadInt32();

            // flags data
            m_int_flags = reader.ReadInt64();

            // all flags data
            m_int_flags_full = reader.ReadInt64();

            // parse the flags data
            m_flags = (TileFlag)Enum.Parse( typeof( TileFlag ), m_int_flags.ToString() );

            // parse all flags data
            m_flags2 = (TileFlag)Enum.Parse( typeof( TileFlag ), m_int_flags_full.ToString() );

            // unknown value
            m_unk16 = reader.ReadInt32();

            // EC image offset
            for ( int i = 0; i < 6; i++ )
                m_imgoffEC[i] = reader.ReadInt32();

            // CC image offset
            for ( int i = 0; i < 6; i++ )
                m_imgoff2D[i] = reader.ReadInt32();

            // get the first properties count
            int propsCount = reader.ReadByte();

            // load all the first properties
            for ( int i = 0; i < propsCount; i++ )
                m_props.Add( (TileArtProperties)reader.ReadByte(), reader.ReadInt32() );

            // get the secondary properties count
            propsCount = reader.ReadByte();

            // load all the secondary properties
            for ( int i = 0; i < propsCount; i++ )
            {
                // get the property
                TileArtProperties prop = (TileArtProperties)reader.ReadByte();

                // make sure the property isn't already present
                if ( !m_props2.ContainsKey( prop ) )
                    m_props2.Add( prop, reader.ReadInt32() );
            }

            try
            {
                // get the stacks count
                int stackCount = reader.ReadByte();

                // load all the stack aliases (used for gold/silver)
                for ( int i = 0; i < stackCount; i++ )
                    m_stackaliases.Add( new KeyValuePair<int, int>( reader.ReadInt32(), reader.ReadInt32() ) );
            }
            catch
            { }

            //// read the animations data
            //m_appearance = new ItemAminationData( reader );

            //// read the sitting animations data
            //m_sittingAnimation = new ItemSittingAnimationData( reader );

            //// read the radar color
            //m_radarCol = new ColourEntry( reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte() );

            // create the texture file name string
            m_fileName = String.Format( "{0:00000000}", m_id ) + ".dds";
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a string with all the active flags
        /// </summary>
        public string GetAllFlags()
        {
            // initialize the flags list
            string flags = "";

            // create the string with all the flags names
            foreach ( Enum value in Enum.GetValues( m_flags2.GetType() ) )
                if ( m_flags2.HasFlag( value ) && (TileFlag)value != TileFlag.None )
                    flags += ( flags == string.Empty ? value.ToString() : ", " + value.ToString() );

            return flags;
        }

        /// <summary>
        /// Get the item image base on the provided filename
        /// </summary>
        /// <param name="fileName">Filename (contained inside the UOP)</param>
        /// <returns></returns>
        public Bitmap GetItemImage( string gamePath, bool useOldKRItems = false )
        {
            // open the uop file containing the item images
            MythicPackage UOPimgs = new MythicPackage( Path.Combine( gamePath, useOldKRItems ? "Texture.uop" : "LegacyTexture.uop" ) );

            // build the file name based on the type of items we want to see
            string fn = ( useOldKRItems ? "build/worldart/" : "build/tileartlegacy/" ) + FileName;

            // search for the image file inside the UOP
            SearchResult sr = UOPimgs.SearchExactFileName( fn );

            // did we find the image?
            if ( sr.Found )
            {
                // create the image
                using ( DDSImage dds = new DDSImage( UOPimgs.Blocks[sr.Block].Files[sr.File].Unpack( UOPimgs.FileInfo.FullName ) ) )
                {
                    // store the image data
                    return (Bitmap)dds.BitmapImage.Clone();
                }
            }

            return null;
        }

        #endregion
    }
}
