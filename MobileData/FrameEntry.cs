using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace UO_EC_Super_Viewer
{
    public class FrameEntry : IDisposable
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        private ushort m_ID;
        private ushort m_Frame;
        private short m_CenterX;
        private short m_CenterY;
        private short m_InitCoordsX;
        private short m_InitCoordsY;
        private short m_EndCoordsX;
        private short m_EndCoordsY;
        private uint m_DataOffset;
        private int m_width;
        private int m_height;
        private DirectBitmap m_image;
        private DirectBitmap m_OriginalImage;

        /// <summary>
        /// flag that indicates if the frame is from a VD file or not originally
        /// </summary>
        private bool m_originalVD = false;

        /// <summary>
        /// data of the VD frame (pixels in the final image). We use strings so we get null where there is no color (easier to make the transparent bg).
        /// </summary>
        private string[,] m_VDImageData;

        private byte[] m_VDFrameHeader = new byte[0];

        private List<ColourEntry> m_VDFrameColors = new List<ColourEntry>();

        // Instantiate a SafeHandle instance.
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        // double xor operator for the VD frame headers
        private const int _doubleXor = (0x200 << 22) | (0x200 << 12);

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Flag: Has Dispose already been called?
        /// </summary>
        public bool disposed = false;

        /// <summary>
        /// Original image (used in the merge of multiple images)
        /// </summary>
        public DirectBitmap OriginalImage
        {
            get { return m_OriginalImage; }
            set
            {
                // get rid of the old image first
                if ( m_OriginalImage != null )
                    m_OriginalImage.Dispose();

                m_OriginalImage = value;
            }
        }

        /// <summary>
        /// Frame move ID
        /// </summary>
        public ushort ID
        {
            get
            {
                return this.m_ID;
            }
        }

        /// <summary>
        /// Frame number
        /// </summary>
        public ushort Frame
        {
            get
            {
                return this.m_Frame;
            }
        }

        /// <summary>
        /// coord X of the center of the tile
        /// </summary>
        public short CenterX
        {
            get
            {
                return this.m_CenterX;
            }
        }

        /// <summary>
        /// coord Y of the center of the tile
        /// </summary>
        public short CenterY
        {
            get
            {
                return this.m_CenterY;
            }
        }

        /// <summary>
        /// start coord X, relative to center of a tile
        /// </summary>
        public short InitCoordsX
        {
            get
            {
                return this.m_InitCoordsX;
            }
        }

        /// <summary>
        /// start coord Y, relative to center of a tile
        /// </summary>
        public short InitCoordsY
        {
            get
            {
                return this.m_InitCoordsY;
            }
        }

        /// <summary>
        /// end coord X, relative to center of a tile
        /// </summary>
        public short EndCoordsX
        {
            get
            {
                return this.m_EndCoordsX;
            }
        }

        /// <summary>
        /// end coord Y, relative to center of a tile
        /// </summary>
        public short EndCoordsY
        {
            get
            {
                return this.m_EndCoordsY;
            }
        }

        /// <summary>
        /// Frame pixel data offset, relative to start of the frame table
        /// </summary>
        public uint DataOffset
        {
            get
            {
                return this.m_DataOffset;
            }
        }

        /// <summary>
        /// Frame Width
        /// </summary>
        public int Width
        {
            get
            {
                return this.m_width;
            }
        }

        /// <summary>
        /// Frame height
        /// </summary>
        public int Height
        {
            get
            {
                return this.m_height;
            }
        }

        /// <summary>
        /// The frame image
        /// </summary>
        public DirectBitmap Image
        {
            get
            {
                return m_image;
            }
            set
            {
                // get rid of the old image first
                if ( m_image != null )
                    m_image.Dispose();

                m_image = value;
            }
        }

        /// <summary>
        /// frame colors used on VD files frames
        /// </summary>
        public List<ColourEntry> VDFrameColors
        {
            get { return m_VDFrameColors; }
        }

        /// <summary>
        /// byte array with the whole VD frame data
        /// </summary>
        public byte[] VDFrameData
        {
            get { return m_VDFrameHeader; }
        }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a new frame
        /// </summary>
        /// <param name="ID">Action ID</param>
        /// <param name="Frame">Frame number in the animation</param>
        /// <param name="initcoordsX">Start coord X, relative to center of a tile</param>
        /// <param name="InitCoordsY">Start coord Y, relative to center of a tile</param>
        /// <param name="EndCoordsX">End coord X, relative to center of a tile</param>
        /// <param name="EndcoordsY">End coord Y, relative to center of a tile</param>
        /// <param name="DataOffset">Frame pixel data offset, relative to start of the frame table</param>
        /// <param name="Colournumber"></param>
        public FrameEntry( ushort ID, ushort Frame, short initCoordsX, short InitCoordsY, short EndCoordsX, short EndcoordsY, uint DataOffset )
        {
            m_ID = ID;
            m_Frame = Frame;
            m_InitCoordsX = initCoordsX;
            m_InitCoordsY = InitCoordsY;
            m_EndCoordsX = EndCoordsX;
            m_EndCoordsY = EndcoordsY;
            m_DataOffset = DataOffset;
            m_width = Math.Abs( (int)EndCoordsX - (int)InitCoordsX );
            m_height = Math.Abs( (int)EndCoordsY - (int)InitCoordsY );

            m_CenterX = (short)( m_width - EndCoordsX );
            m_CenterY = (short)( -EndCoordsY );
        }

        /// <summary>
        /// Load the frame from a VD file
        /// </summary>
        /// <param name="initialCenterX">center X of the tile where ti place the frame</param>
        /// <param name="initialCenterY">center Y of the tile where ti place the frame</param>
        /// <param name="width">width of the frame</param>
        /// <param name="height">height of the frame</param>
        /// <param name="colors">colors table to use in the frame</param>
        /// <param name="reader">binary reader attached to the file</param>
        public FrameEntry( int initialCenterX, int initialCenterY, int width, int height, List<ColourEntry> colors, BinaryReader reader )
        {
            // flag the frame as from a VD file
            m_originalVD = true;

            // store the center position of the tile
            m_CenterX = (short)initialCenterX;
            m_CenterY = (short)initialCenterY;

            // frame size
            m_width = width;
            m_height = height;

            // initialize the matrix
            m_VDImageData = new string[width, height];

            // store the colors table
            m_VDFrameColors = colors.ToList();

            // initialize the pixel chunk header variable
            int header;

            // the frame image is saved as colored segments to draw between x,y and long N
            // scan the byte chunks until we find 0x7FFF7FFF as value
            while ( ( header = reader.ReadInt32() ) != 0x7FFF7FFF )
            {
                // fix the header
                header ^= _doubleXor;

                // get the chunk length
                int length = header & 0xFFF;

                // get the x offset
                int xOffset = ( header >> 22 ) & 0x3FF;

                // get the y offset
                int yOffset = ( header >> 12 ) & 0x3FF;

                // calculate the current pixel coordinates
                int x = xOffset + CenterX - 0x200;
                int y = yOffset + CenterY + height - 0x200;

                // read the whole chunk of bytes
                byte[] chunk = reader.ReadBytes( length );

                // process the segment data
                for ( int i = 0; i < length; i++ )
                {
                    // is the current pixel inside the area?
                    if ( x < width && y < height && x >= 0 && y >= 0 )
                    {
                        // store the color index (from the colors array)
                        m_VDImageData[x, y] = chunk[i].ToString();
                    }
                    else
                    {
                        Debug.WriteLine( "Skip: len: " + length + " w: " + width + ", h: " + height + " centerX: " + initialCenterX + " centerY: " + initialCenterY + " x: " + x + ", y: " + y + " xOff: " + xOffset + " yOff: " + yOffset );
                    }

                    // move along the line
                    x++;
                }
            }

            // generate the image
            LoadVDFrameImage();
        }

        /// <summary>
        /// Duplicate the frame
        /// </summary>
        /// <param name="original"></param>
        private FrameEntry( FrameEntry original )
        {
            m_ID = original.ID;
            m_Frame = original.Frame;
            m_CenterX = original.CenterX;
            m_CenterY = original.CenterY;
            m_InitCoordsX = original.InitCoordsX;
            m_InitCoordsY = original.InitCoordsY;
            m_EndCoordsX = original.EndCoordsX;
            m_EndCoordsY = original.EndCoordsY;
            m_DataOffset = original.DataOffset;
            m_width = original.Width;
            m_height = original.Height;

            // copy the image only if we have it
            if ( m_image != null )
                m_image = new DirectBitmap( original.Image.Bitmap );
        }

        #endregion


        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Load the frame image
        /// </summary>
        /// <param name="_ImageData">Byte array to read</param>
        /// <param name="_ImageDataOffset">Data offset</param>
        /// <param name="m_Colours">Colors table to use</param>
        public void LoadFrameImage( byte[] _ImageData, long _ImageDataOffset, List<ColourEntry> m_Colours )
        {
            // create the VD image data array (in case we want to save the image in VD later)
            m_VDImageData = new string[m_width, m_height];

            // create the basic image
            m_image = new DirectBitmap( m_width, m_height );

            // get the current byte index
            int currByte = (int) ((long) DataOffset - _ImageDataOffset);

            // starting x and y coordinates for drawing pixels
            int curx = 0;
            int cury = 0;

            // are we still within the image boundries?
            while ( cury < m_height )
            {
                // get the next byte
                byte curr = _ImageData[currByte++];

                // are we positioned before the header?
                if ( curr < (byte)128 )
                {
                    // move to the correct starting point
                    for ( ; curr > (byte)0; curr-- )
                        NextCoordinate( ref curx, ref cury, m_width, m_height );
                }
                else // correct position
                {
                    // get the next byte
                    byte next = _ImageData[currByte++];

                    // calculate the factors
                    int factor1 = (int) next / 16;
                    int factor2 = (int) next % 16;

                    // is the factor greater than 0?
                    if ( factor1 > 0 )
                    {
                        // combine the colors
                        Color color = CombineColors(m_Colours[(int) _ImageData[currByte++]].Pixel, m_image.GetPixel(curx, cury), factor1);

                        // color the pixel
                        m_image.SetPixel( curx, cury, color );

                        // flag that this pixel is used (for VD save)
                        m_VDImageData[curx, cury] = "1";

                        // get the next coordinate in the image
                        NextCoordinate( ref curx, ref cury, m_width, m_height );
                    }

                    // scan the other bytes
                    for ( byte i = (byte)( (uint)curr - 128U ); i > (byte)0; i-- )
                    {
                        // get the pixel color
                        Color pixel = m_Colours[(int) _ImageData[currByte++]].Pixel;

                        // set the color to the image
                        m_image.SetPixel( curx, cury, pixel );

                        // flag that this pixel is used (for VD save)
                        m_VDImageData[curx, cury] = "1";

                        // move to the next coordinate in the image
                        NextCoordinate( ref curx, ref cury, m_width, m_height );
                    }

                    // is the second factor greater than 0?
                    if ( factor2 > 0 )
                    {
                        // combine the colors
                        Color color = CombineColors(m_Colours[(int) _ImageData[currByte++]].Pixel, m_image.GetPixel(curx, cury), factor2);

                        // color the pixel
                        m_image.SetPixel( curx, cury, color );

                        // flag that this pixel is used (for VD save)
                        m_VDImageData[curx, cury] = "1";

                        // get the next coordinate in the image
                        NextCoordinate( ref curx, ref cury, m_width, m_height );
                    }
                }
            }
        }

        /// <summary>
        /// Load the VD frame image
        /// </summary>
        public void LoadVDFrameImage()
        {
            // make sure we have the image data
            if ( m_VDImageData == null )
                return;

            // create the basic image
            m_image = new DirectBitmap( m_width, m_height );

            // cycle through all lines
            for ( int y = 0; y < m_height; y++ )
            {
                // cycle through all rows
                for ( int x = 0; x < m_width; x++ )
                {
                    // no color for this pixel?
                    if ( m_VDImageData[x, y] == null )
                        continue;

                    // set the pixel color
                    m_image.SetPixel( x, y, VDFrameColors[ int.Parse( m_VDImageData[x, y] ) ].Pixel );
                }
            }
        }

        /// <summary>
        /// Create the VD frame data from the image
        /// </summary>
        /// <param name="writer">active binary writer</param>
        /// <param name="colors">animation colors list (only required for EC animations)</param>
        /// <param name="centerX">tile center X</param>
        /// <param name="centerY">tile center X</param>
        /// <param name="frameImage">Frame image (with the animation size)</param>
        /// <param name="topX">top X coordinate from the animation frame</param>
        /// <param name="topY">top Y coordinate from the animation frame</param>
        public void ExportVDImageData( BinaryWriter writer, DirectBitmap frameImage, short centerX, short centerY, int topX, int topY, List<ColourEntry> colors = null )
        {
            // write the image initial X coordinate
            writer.Write( centerX );

            // write the image initial Y coordinate
            writer.Write( centerY );

            // write the image width
            writer.Write( (ushort)( m_originalVD ? m_width : frameImage.Width ) );

            // write the image height
            writer.Write( (ushort)( m_originalVD ? m_height : frameImage.Height ) );

            // parse the image line by line
            for ( int y = 0; y < m_height; y++ )
            {
                // index used to search the first used (NON transparent) pixel in the line
                int i = 0;

                // current position in the line
                int x = 0;

                // we keep cycling until the whole line hans been loaded
                while ( i < m_width )
                {
                    // scan all the pixels in the line in search of the first one NON transparent
                    for ( i = x; i < m_width; i++ )
                    {
                        // did we find the first color?
                        if ( m_VDImageData[i, y] != null )
                        {
                            break;
                        }
                    }

                    // did we reach the end of the line?
                    if ( i >= m_width )
                        continue;

                    // index of the first unused (transparent) pixel in the line AFTER the first colored pixel
                    int j;

                    // now we search the last color of this segment to determine the length
                    for ( j = i + 1; j < m_width; j++ )
                    {
                        // did we find the last color?
                        if ( m_VDImageData[j, y] == null )
                        {
                            break;
                        }
                    }

                    // calculate the size of the segment
                    int length = j - i;

                    // calculate the offset x for the point to start drawing the segment
                    int xOffset = ( ( j - length - centerX ) + ( m_originalVD ? 0 : topX ) ) + 0x200;

                    // calculate the offset y for the point to start drawing the segment
                    int yOffset = ( ( y - centerY - ( m_originalVD ? m_height : frameImage.Height ) )  + ( m_originalVD ? 0 : topY ) ) + 0x200;

                    // create the data array for this segment
                    byte[] data = new byte[length];

                    // scan the segment to store the index in the colors list
                    for ( int r = 0; r < length; r++ )
                    {
                        // is this frame from a VD file?
                        if ( m_originalVD )
                        {
                            // get the color index from the array
                            string stringColor = m_VDImageData[r + i, y];

                            // get the index for the color in the colors list
                            data[r] = (byte)int.Parse( stringColor );
                        }
                        else // for EC animations we have to search for the color index
                        {
                            data[r] = (byte)GetPaletteIndex( frameImage.GetPixel( r + i + topX, y + topY ), colors ); ;
                        }
                    }

                    // create the header (in bytes)
                    int header = length | ( yOffset << 12 ) | ( xOffset << 22 ) ;

                    // fix the header
                    header ^= _doubleXor;

                    // write the header
                    writer.Write( header );

                    // write each data byte of the frame
                    foreach ( byte b in data )
                    {
                        writer.Write( b );
                    }

                    // move to the next segment
                    x = j + 1;
                    i = x;

                    // allow the form to update before we move to the next
                    Application.DoEvents();
                }
            }

            // write the end of frame flag (header)
            writer.Write( 0x7FFF7FFF );
        }

        /// <summary>
        /// Generate the colors palette for an EC frame
        /// </summary>
        public List<ColourEntry> GeneratePaletteFromImage()
        {
            // initialize the colors list
            List<ColourEntry> colors;

            // convert the image to 256 colors
            using ( Bitmap gifImage = Image.ToGif() )
            {
                // fill the colors list
                colors = gifImage.Palette.Entries.Select( col => new ColourEntry( col.R, col.G, col.B, col.A  ) ).ToList();

                // if there are colors missing, we add more at the end
                if ( colors.Count < 256 )
                    colors.AddRange( Enumerable.Repeat( new ColourEntry( 0, 0, 0, 0 ), 256 - colors.Count ) );
            }

            return colors;
        }

        /// <summary>
        /// Duplicate the frame
        /// </summary>
        public FrameEntry Clone()
        {
            return new FrameEntry( this );
        }

        /// <summary>
        /// Delete the image and keep the frame data
        /// </summary>
        public void DisposeImage()
        {
            // do nothing if the frame is already null
            if ( m_image == null || m_image.Disposed )
                return;

            // delete the image
            m_image.Dispose();
            m_image = null;
        }

        /// <summary>
        /// Delete the frame and free the memory
        /// </summary>
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose( true );

            // Suppress finalization.
            GC.SuppressFinalize( this );
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Calculate the next coordinate
        /// </summary>
        /// <param name="curx">Current X position</param>
        /// <param name="cury">Current Y position</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns></returns>
        private bool NextCoordinate( ref int curx, ref int cury, int width, int height )
        {
            // move to the next X coordinate
            curx++;

            // are we outside the image boundries?
            if ( curx >= width )
            {
                // reset X
                curx = 0;

                // move to the next Y coordinate
                cury++;
            }

            return cury < height && curx < width;
        }

        /// <summary>
        /// Combine 2 colors to obtain the real one
        /// </summary>
        /// <param name="sourcecolor">Source color</param>
        /// <param name="targetcolour">Target color</param>
        /// <param name="factor">Tolerance</param>
        /// <returns>Correct final color</returns>
        private Color CombineColors( Color sourcecolor, Color targetcolour, int factor )
        {
            // get the decimal value for the source color
            long argb1 = (long) sourcecolor.ToArgb();

            // get the decimal value for the target color
            long argb2 = (long) targetcolour.ToArgb();

            return Color.FromArgb( (int)( ( ( argb1 & 16711935L ) * (long)factor + ( argb2 & 16711935L ) * (long)( 16 - factor ) >> 4 ^ ( argb1 >> 4 & 4293922800L ) * (long)factor + ( argb2 >> 4 & 4293922800L ) * (long)( 16 - factor ) ) & 16711935L ^ ( argb1 >> 4 & 267390960L ) * (long)factor + ( argb2 >> 4 & 267390960L ) * (long)( 16 - factor ) ) );
        }

        /// <summary>
        /// Get the index in the VD colors array given an RGB555 color.
        /// </summary>
        /// <param name="col">RGB555 color</param>
        /// <returns>index of the color in the array</returns>
        private int GetPaletteIndex( Color col, List<ColourEntry> colors )
        {
            // get the colors table to use
            List<ColourEntry> cols = colors != null ? colors : m_VDFrameColors;

            // search for the exact color in the list
            int found = cols.FindIndex( c => c.ColorRGB555 == ColourEntry.ARGBtoRGB555( col ) );

            // if we found the color, we can return it
            if ( found != -1 )
                return found;

            // if we haven't found the color, we look for the most similar we can find in the list
            return FindNearestColor( cols, col );
        }

        /// <summary>
        /// Find the nearest color given an array of colors
        /// </summary>
        /// <param name="map">list of colors</param>
        /// <param name="current">color to search</param>
        /// <returns>Index of the color inside the list</returns>
        private int FindNearestColor( List<ColourEntry> map, Color current )
        {
            // find the closest color
            int colorDiffs = map.Select( n => DirectBitmap.GetDistance( Color.FromArgb( n.Alpha, n.R, n.G, n.B ), current ) ).Min( n => n );

            // find the index of the color
            return map.FindIndex( n => DirectBitmap.GetDistance( Color.FromArgb( n.Alpha, n.R, n.G, n.B ), current ) == colorDiffs );
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
                // dispose of the memory used by the frame
                handle.Dispose();

                // destroy the image
                if ( m_image != null && !m_image.Disposed )
                    m_image.Dispose();

                // nullify the image
                m_image = null;

                // destroy the original image backup
                if ( m_OriginalImage != null && !m_OriginalImage.Disposed )
                    m_OriginalImage.Dispose();

                // nullify the original image backup
                m_OriginalImage = null;
            }

            // flag the frame as disposed
            disposed = true;
        }

        #endregion
    }
}
