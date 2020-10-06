using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UO_EC_Super_Viewer
{
    class ItemSittingAnimationData
    {
        public bool HasSittingAnimation;
        public int unk1;
        public int unk2;
        public int unk3;
        public int unk4;

        public ItemSittingAnimationData( BinaryReader reader )
        {
            // get the sitting animation flag
            HasSittingAnimation = Convert.ToBoolean( reader.ReadByte() );

            // do we have the sitting animation?
            if ( HasSittingAnimation )
            {
                // read the sitting animation data (unknown)
                unk1 = reader.ReadInt32();
                unk2 = reader.ReadInt32();
                unk3 = reader.ReadInt32();
                unk4 = reader.ReadInt32();
            }
        }
    }
}
