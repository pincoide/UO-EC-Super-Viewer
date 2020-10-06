using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UO_EC_Super_Viewer
{
    /// <summary>
    /// Container for the animation nested data
    /// </summary>
    class ItemAminationSubDataContainer
    {
        public byte animFlag;
        public List<KeyValuePair<int, int>> data = new List<KeyValuePair<int, int>>();

        public ItemAminationSubDataContainer( byte val )
        {
            animFlag = val;
        }
    }

    class ItemAminationData
    {
        /// <summary>
        /// animations count
        /// </summary>
        public int count;

        /// <summary>
        /// Nested animation data
        /// </summary>
        public ItemAminationSubDataContainer[] data;

        /// <summary>
        /// Create the animation data
        /// </summary>
        /// <param name="reader"></param>
        public ItemAminationData( BinaryReader reader )
        {
            // read the animations count
            count = reader.ReadInt32();

            // initialize the array of the nested data
            data = new ItemAminationSubDataContainer[count];

            // read the data
            for ( int i = 0; i < count; i++ )
            {
                // initialize the nested data item
                data[i] = new ItemAminationSubDataContainer( reader.ReadByte() );

                // do we have the animation flag?
                if ( data[i].animFlag != 0 )
                {
                    // is the flag 1?
                    if ( data[i].animFlag == 1 )
                    {
                        // read the data
                        data[i].data.Add( new KeyValuePair<int, int>( reader.ReadByte(), reader.ReadInt32() ) );
                    }
                }
                else // animation flag = 0
                {
                    // read the amount of nested data records we need
                    uint records = reader.ReadUInt32();

                    // load all the data
                    for ( int j = 0; j < records; j++ )
                    {
                        // read the data
                        data[i].data.Add( new KeyValuePair<int, int>( reader.ReadInt32(), reader.ReadInt32() ) );
                    }
                }
            }
        }
    }
}
