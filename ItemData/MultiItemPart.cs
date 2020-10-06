using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UO_EC_Super_Viewer
{
    class MultiItemPart
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// ID of the item component
        /// </summary>
        public int ItemID { get; set; }

        /// <summary>
        /// X position where to draw the item
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y position where to draw the item
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Z position where to draw the item
        /// </summary>
        public int Z { get; set; }

        /// <summary>
        /// Used in case of adjustments
        /// </summary>
        public int OriginalZ { get; set; }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a new multi item
        /// </summary>
        /// <param name="reader">Current binary reader for the item</param>
        public MultiItemPart( BinaryReader reader )
        {
            // get the item ID
            ItemID = reader.ReadUInt16();

            // get the component coordinates
            X = reader.ReadInt16();
            Y = reader.ReadInt16();
            Z = reader.ReadInt16();

            // store the original Z
            OriginalZ = Z;

            // unknown flag
            reader.ReadByte();
            reader.ReadByte();

            // counter for the unknown stuff we're going to read now...
            int count = reader.ReadInt32();

            try
            {
                // we read some stuff that we know nothing about (but we have to or we'll just mess up the reading for the next item).
                for ( int i = 0; i < count; i++ )
                    reader.ReadInt32();
            }
            catch
            {}
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        #endregion
    }
}
