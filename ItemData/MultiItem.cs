using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UO_EC_Super_Viewer.Properties;

namespace UO_EC_Super_Viewer
{
    class MultiItem
    {
        /// <summary>
        /// Type of items set
        /// </summary>
        public enum MultiType
        {
            Boat = 0,
            House = 1,
            Decoration = 2,
            Other = 3,
        }

        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Highest X we encountered in the list of parts
        /// </summary>
        private int MaxX = 0;

        /// <summary>
        /// Highest Y we encountered in the list of parts
        /// </summary>
        private int MaxY = 0;

        /// <summary>
        /// Lowest X we encountered in the list of parts
        /// </summary>
        private int MinX = 0;

        /// <summary>
        /// Lowest Y we encountered in the list of parts
        /// </summary>
        private int MinY = 0;

        /// <summary>
        /// Default tile size
        /// </summary>
        private Size defaultTileSize = new Size( 22, 32 );

        /// <summary>
        /// cache for the images used in this multi
        /// </summary>
        private Dictionary<int, Bitmap> itemsImageCache = new Dictionary<int, Bitmap>();

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// ID of the multi item
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// List of the parts of the multi
        /// </summary>
        public List<MultiItemPart> Parts { get; set; }

        /// <summary>
        /// Multi name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Milti type
        /// </summary>
        public MultiType Type { get; set; }

        /// <summary>
        /// Highest Z we encountered in the list of parts
        /// </summary>
        public int MaxZ { get; set; }

        /// <summary>
        /// Lowest Z we encountered in the list of parts
        /// </summary>
        public int MinZ { get; set; }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a new multi item
        /// </summary>
        /// <param name="reader">Current binary reader for the item</param>
        public MultiItem( BinaryReader reader, string name = "" )
        {
            // get the multi ID
            ID = reader.ReadInt32();

            // store the name
            Name = name;

            // get the amount of items composing the multi
            int count = reader.ReadInt32();

            // initialize the parts list
            Parts = new List<MultiItemPart>();


            // read all the components
            for ( int i = 0; i < count; i++ )
            {
                // count how many items there are still available in the file
                long items = ( reader.BaseStream.Length - reader.BaseStream.Position ) / sizeof(Int16);

                // no more items? then we can get out
                if ( items <= 0 )
                    break;

                // add the item part
                AddPart( new MultiItemPart( reader ) );
            }
        }

        /// <summary>
        /// Use to create an empty multi item
        /// </summary>
        public MultiItem()
        {
            ID = -1;
            Name = "None";
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Create the image for the multi item
        /// </summary>
        /// <param name="gamePath">Game path (used to load the images)</param>
        /// <param name="itemsCache">Reference to the list with all the items</param>
        /// <param name="minZ">Minimum Z to show</param>
        /// <param name="maxZ">Maximum Z to show</param>
        /// <returns>Multi item image</returns>
        public Bitmap GetImage( string gamePath, List<ItemData> itemsCache, Hue h, int minZ = -1, int maxZ = -1 )
        {
            // check if a minZ has been specified
            if ( minZ == -1 || minZ < MinZ )
                minZ = MinZ;

            // check if a maxZ has been specified
            if ( maxZ == -1 || maxZ > MaxZ )
                maxZ = MaxZ;

            // default tile size width
            int tileSize = defaultTileSize.Width;

            // tile size to use to calculate the width
            int widthTileSize = Settings.Default.useOldKRItems ? defaultTileSize.Height : tileSize;

            // calculate the image width
            int width = Math.Max( MaxX * widthTileSize + MaxY * widthTileSize - MinX * widthTileSize - MinY * widthTileSize + 200, 600 );

            // calculate the image height
            int height = width * 2;

            // calculate the middle of the canvas
            int halfWidth = width / 2;
            int halfHeight = height / 2;

            // original maxZ
            int orgMaxZ = MaxZ;

            // too big?
            if ( width > 10000 || height > 10000 )
                return null;

            // initialize the final image
            Bitmap final = new Bitmap( width, height );

            // initialize the drawing tool
            using ( Graphics g = Graphics.FromImage( final ) )
            {
                // set the background transparent
                g.Clear( Color.Transparent );

                // get the parts list
                List<MultiItemPart> PartsList = new List<MultiItemPart>( Parts );

                // scan all the parts in the list
                foreach ( MultiItemPart p in PartsList )
                {
                    // get the item data
                    ItemData itm = itemsCache.Where( it => it.ID == p.ItemID ).FirstOrDefault();

                    // item missing?
                    if ( itm == null )
                        continue;

                    // the floors with the old KR art tiles are not isometric, so we need to recalculate the positon
                    if ( Settings.Default.useOldKRItems && Type == MultiType.House && itm.Flags.HasFlag( ItemData.TileFlag.Surface ) && !itm.Flags.HasFlag( ItemData.TileFlag.Bridge ) && !itm.Flags.HasFlag( ItemData.TileFlag.StairRight ) )
                    {
                        p.X -= 1;
                        p.Y -= 1;
                    }

                    // ship mast are drawn incorrectly with the old KR art
                    if ( Settings.Default.useOldKRItems && Type == MultiType.Boat && itm.Flags.HasFlag( ItemData.TileFlag.Foliage ) )
                    {
                        // set the position (based on the mast ID)
                        switch ( itm.ID )
                        {
                            case 16093:
                            case 15962:
                                {
                                    p.X += 1;
                                    p.Y += 1;

                                    break;
                                }
                            case 15980:
                            case 16098:
                                {
                                    p.X += 2;
                                    p.Y += 2;

                                    break;
                                }
                        }
                    }

                    // signs need to be drawn higher
                    if ( Type == MultiType.House && itm.Flags.HasFlag( ItemData.TileFlag.Transparent ) && !itm.Flags.HasFlag( ItemData.TileFlag.Foliage ) )
                    {
                        p.Z += 5;
                    }

                    // fix the SA ship mast location
                    if ( Type == MultiType.Boat && IsSAMast( itm.Flags, itm.Name ) )
                    {
                        // move the mast higher
                        p.Z = orgMaxZ + ( orgMaxZ - p.Z );

                        // make sure the max Z is still correct
                        MaxZ = Math.Max( MaxZ, p.Z );
                    }
                }

                // we draw the image from bottom to top
                for ( int z = MinZ; z <= MaxZ; z++ )
                {
                    // and from back to front
                    for ( int y = MinY; y <= MaxY; y++ )
                    {
                        for ( int x = MinX; x <= MaxX; x++ )
                        {
                            // get the part at the current location
                            List<MultiItemPart> currParts = Parts.Where( pt => pt.X == x && pt.Y == y && pt.Z == z ).Where( pt => pt.OriginalZ >= minZ && pt.OriginalZ <= maxZ ).ToList();

                            // nothing in this position?
                            if ( currParts == null || currParts.Count == 0 )
                                continue;

                            // put the floors first then the rest
                            currParts = currParts.OrderBy( pt => itemsCache.Where( it => it.ID == pt.ItemID ).FirstOrDefault() != null && ( !( itemsCache.Where( it => it.ID == pt.ItemID ).FirstOrDefault() ).Flags.HasFlag( ItemData.TileFlag.Surface ) || ( itemsCache.Where( it => it.ID == pt.ItemID ).FirstOrDefault() ).Flags.HasFlag( ItemData.TileFlag.Bridge ) ) ).ToList();

                            // draw all the items in this tile
                            foreach ( MultiItemPart p in currParts )
                            {
                                // get the item data
                                ItemData itm = itemsCache.Where( it => it.ID == p.ItemID ).FirstOrDefault();

                                // missing item?
                                if ( itm == null )
                                    continue;

                                // calculate where to draw the item
                                Point drawPoint = GetDrawingPosition( p, itm, width, height );

                                // get the cached tile image
                                Bitmap img = itemsImageCache.ContainsKey((int)itm.ID) ? itemsImageCache[(int)itm.ID] : null;

                                // no image in the cache?
                                if ( img == null && !itemsImageCache.ContainsKey( (int)itm.ID ) )
                                {
                                    // load the image in the cache
                                    itemsImageCache.Add( (int)itm.ID, itm.GetItemImage( gamePath, Settings.Default.useOldKRItems ) );

                                    // get the image
                                    img = itemsImageCache[(int)itm.ID];
                                }

                                // draw the item image
                                if ( img != null )
                                {
                                    // is there a hue selected?
                                    if ( h.ID != 0 )
                                    {
                                        // for boats we apply the color only to the dyeable parts
                                        if ( ( Type == MultiType.Boat && itm.Flags.HasFlag( ItemData.TileFlag.PartialHue ) && !IsSAMast( itm.Flags, itm.Name ) ) || Type != MultiType.Boat )
                                        {
                                            // create a temporary image to apply the hue
                                            using ( DirectBitmap db = new DirectBitmap( img ) )
                                            {
                                                // apply the hue
                                                img = db.ApplyHue( h.HueDiagram, itm.Flags.HasFlag( ItemData.TileFlag.PartialHue ) );
                                            }
                                        }
                                    }

                                    // are we using the old KR items and this is a floor item?
                                    if ( Settings.Default.useOldKRItems && itm.Flags.HasFlag( ItemData.TileFlag.Surface ) && !itm.Flags.HasFlag( ItemData.TileFlag.Bridge ) && !itm.Flags.HasFlag( ItemData.TileFlag.StairRight ) && !itm.Flags.HasFlag( ItemData.TileFlag.Container ) )
                                    {
                                        // kr floors need to be rotated 45°
                                        g.DrawImage( DirectBitmap.RotateImage( img, 45 ), drawPoint );
                                    }

                                    else // draw the item
                                        g.DrawImage( img, drawPoint );
                                }

                                // update the form
                                Application.DoEvents();
                            }
                        }
                    }
                }
            }

            return DirectBitmap.CropImage( final, 50 );
        }

        /// <summary>
        /// Clear the items image cache
        /// </summary>
        public void ClearImageCache()
        {
            // clear all the images inside the cache
            foreach ( KeyValuePair<int, Bitmap> k in itemsImageCache )
            {
                // get rid of the image
                if ( k.Value != null )
                    k.Value.Dispose();
            }

            // clear the cache
            itemsImageCache.Clear();
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Add the part to the list and check the location
        /// </summary>
        /// <param name="part">Part to add</param>
        private void AddPart( MultiItemPart part )
        {
            // add the part to the list
            Parts.Add( part );

            // update the max coordinates
            MaxX = Math.Max( part.X, MaxX );
            MaxY = Math.Max( part.Y, MaxY );
            MaxZ = Math.Max( part.Z, MaxZ );

            // update the min coordinates
            MinX = Math.Min( part.X, MinX );
            MinY = Math.Min( part.Y, MinY );
            MinZ = Math.Min( part.Z, MinZ );
        }

        /// <summary>
        /// Calculate the final position in the canvas for the part to draw
        /// </summary>
        /// <param name="p">Part to draw</param>
        /// <param name="itm">Item data for the item to draw</param>
        /// <param name="width">Canvas width</param>
        /// <param name="height">Canvas height</param>
        /// <returns>Drawing position for the part</returns>
        private Point GetDrawingPosition( MultiItemPart p, ItemData itm, int width, int height )
        {
            // calculate the middle of the canvas (origin point 0,0)
            int halfWidth = width / 2;
            int halfHeight = height / 2;

            // are we using the old KR items?
            if ( Settings.Default.useOldKRItems )
            {
                // tile size to use
                int tileSize = defaultTileSize.Height;

                // calculate the item size
                int itmWidth = itm.ECOffsets[1] - itm.ECOffsets[3];
                int itmHeight = - itm.ECOffsets[0];

                // initialize the extra shifting
                int shifX = 0;
                int shifY = 0;

                // floors are not isometric, so we need to fix the way they are drawn...
                if ( itm.Flags.HasFlag( ItemData.TileFlag.Surface ) && !itm.Flags.HasFlag( ItemData.TileFlag.Bridge ) && !itm.Flags.HasFlag( ItemData.TileFlag.StairRight ) && !itm.Flags.HasFlag( ItemData.TileFlag.Container ) )
                {
                    // set the shifting value
                    shifX = -( tileSize / 2 );
                    shifY = -( tileSize / 2 );

                    // change the tile size
                    tileSize -= 2;
                }

                // ship mast are drawn incorrectly
                if ( itm.Flags.HasFlag( ItemData.TileFlag.Foliage ) )
                {
                    // set the shifting value (based on the mast ID)
                    switch ( itm.ID )
                    {
                        case 15962:
                            {
                                shifY = -2;
                                break;
                            }
                        case 16093:
                            {
                                shifY = -10;
                                break;
                            }
                        case 15980:
                            {
                                shifY = -25;
                                break;
                            }
                    }
                }

                // calculate the position of the item in the canvas
                int itmCanvasX = halfWidth - ( p.Y * tileSize ) + ( p.X * tileSize ) + itmHeight + shifX;
                int itmCanvasY = halfHeight + ( p.Y * tileSize ) + ( p.X * tileSize ) + itmWidth + 64 + itmHeight - ( p.OriginalZ * 6 ) + shifY;

                // calculate the final position
                int x = itmCanvasX + itm.ECOffsets[4];
                int y = itmCanvasY + itm.ECOffsets[5];

                return new Point( x, y );
            }
            else
            {
                // tile size to use
                int tileSize = defaultTileSize.Width;

                // calculate the item size
                int itmWidth = itm.CCOffsets[1] - itm.CCOffsets[3];
                int itmHeight = itm.CCOffsets[0];

                // calculate the position of the item in the canvas
                int itmCanvasX = halfWidth - ( p.Y * tileSize ) + ( p.X * tileSize ) + itmHeight;
                int itmCanvasY = halfHeight + ( p.Y * tileSize ) + ( p.X * tileSize ) + itmWidth + 64 + itmHeight - ( p.OriginalZ * 4 );

                // calculate the final position
                int x = itmCanvasX + itm.CCOffsets[4];
                int y = itmCanvasY + itm.CCOffsets[5];

                return new Point( x, y );
            }
        }

        /// <summary>
        /// Determine if the item is a piece of the SA ships mast
        /// </summary>
        /// <param name="flags">item flag</param>
        /// <param name="name">item name</param>
        /// <returns>is this a piece of the ship mast?</returns>
        private bool IsSAMast( ItemData.TileFlag flags, string name )
        {
            return ( name.ToLower().Contains( "mast " ) || name.ToLower().Contains( "sails " ) ) &&
                   ( flags.HasFlag( ItemData.TileFlag.PartialHue ) && flags.HasFlag( ItemData.TileFlag.PixelBleed ) );
        }

        #endregion
    }
}
