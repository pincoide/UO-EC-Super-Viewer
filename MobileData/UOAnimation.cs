using Microsoft.Win32.SafeHandles;
using Mythic.Package;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UO_EC_Super_Viewer
{
    public class UOAnimation : IDisposable
    {
        // --------------------------------------------------------------
        #region ENUM/STRUCTURES
        // --------------------------------------------------------------


        #endregion

        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        private byte[] m_Head;
        private uint m_Version;
        private uint m_Length;
        private uint m_ID;
        private int m_ActionID;
        private short m_InitCoordsX;
        private short m_InitCoordsY;
        private short m_EndCoordsX;
        private short m_EndCoordsY;
        private uint m_ColourCount;
        private uint m_ColourAddress;
        private uint m_FrameCount;
        private uint m_FrameAddress;
        private List<ColourEntry> m_Colours = new List<ColourEntry>();
        private List<FrameEntry> m_Frames = new List<FrameEntry>();
        private byte[] _ImageData;
        private long _ImageDataOffset;
        private int m_width;
        private int m_height;
        private DirectBitmap m_spriteSheet;

        private string m_uopFileName;
        private int m_blockID;
        private int m_fileID;

        /// <summary>
        ///  array with the starting position to read each frame (VD files only)
        /// </summary>
        long[] m_vdFramesStartOffset;

        /// <summary>
        /// temporary VD frameset colors lists
        /// </summary>
        private List<List<ColourEntry>> m_VDFramesetColours = new List<List<ColourEntry>>();

        // Instantiate a SafeHandle instance.
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Flag: Has Dispose already been called?
        /// </summary>
        public bool disposed = false;

        /// <summary>
        /// Current creature body ID
        /// </summary>
        public uint BodyID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// Number of frame per direction of the animation
        /// </summary>
        public int FramesPerDirection
        {
            get { return (int)m_FrameCount / 5; }
        }

        /// <summary>
        /// Sprite sheet cell width
        /// </summary>
        public int CellWidth
        {
            get { return m_width; }
            set { m_width = value; }
        }

        /// <summary>
        /// Sprite sheet cell height
        /// </summary>
        public int CellHeight
        {
            get { return m_height; }
            set { m_height = value; }
        }

        /// <summary>
        /// X coordinate on where to start drawing the frames
        /// </summary>
        public int StartX
        {
            get { return m_InitCoordsX; }
        }

        /// <summary>
        /// Y coordinate on where to start drawing the frames
        /// </summary>
        public int StartY
        {
            get { return m_InitCoordsY; }
        }

        /// <summary>
        /// X coordinate on where to end drawing the frames
        /// </summary>
        public int EndX
        {
            get { return m_EndCoordsX; }
        }

        /// <summary>
        /// Y coordinate on where to end drawing the frames
        /// </summary>
        public int EndY
        {
            get { return m_EndCoordsY; }
        }

        /// <summary>
        /// Animation sprite sheet
        /// </summary>
        public Bitmap SpriteSheet
        {
            get { return m_spriteSheet.Bitmap; }
        }

        /// <summary>
        /// Get the animation action ID
        /// </summary>
        public int ActionID
        {
            get { return m_ActionID; }
        }

        /// <summary>
        /// The animation frames
        /// </summary>
        public List<FrameEntry> Frames
        {
            get { return m_Frames; }
        }

        /// <summary>
        /// UOP file name containing this animation
        /// </summary>
        public string UopFileName
        {
            get { return m_uopFileName; }
            set { m_uopFileName = value; }
        }

        /// <summary>
        /// Block ID inside the UOP file of this animation
        /// </summary>
        public int BlockID
        {
            get { return m_blockID; }
            set { m_blockID = value; }
        }

        /// <summary>
        /// File ID inside the UOP file block of this animation
        /// </summary>
        public int FileID
        {
            get { return m_fileID; }
            set { m_fileID = value; }
        }

        /// <summary>
        /// Have the frames been loaded?
        /// </summary>
        public bool FramesLoaded
        {
            get { return Frames[0].Image != null; }
        }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a new animation
        /// </summary>
        /// <param name="memoryStream">Memory stream data of the animation</param>
        public UOAnimation( Stream memoryStream )
        {
            // read the animation file
            ReadFile( memoryStream, true );
        }

        /// <summary>
        /// Create a new animation
        /// </summary>
        /// <param name="memoryStream">Memory stream data of the animation</param>
        /// <param name="uopFile">File name of the UOP file containing this animation</param>
        /// <param name="blockId">Block ID inside the UOP file containing this animation</param>
        /// <param name="fileId">File ID inside the UOP file block of this animation</param>
        /// <param name="loadImages">Do we have to load the images too? (default: false)</param>
        public UOAnimation( Stream memoryStream, string uopFile, int blockId, int fileId, bool loadImages = false )
        {
            // store the file data
            m_uopFileName = uopFile;
            m_blockID = blockId;
            m_fileID = fileId;

            // read the animation file
            ReadFile( memoryStream, loadImages );
        }

        /// <summary>
        /// Create the animation from a VD animation data array
        /// </summary>
        /// <param name="animationData"></param>
        public UOAnimation( List<byte[]> framesets )
        {
            // scan all the framesets available (5: 1 per direction)
            for ( int i = 0; i < framesets.Count; i++ )
            {
                // parse the frameset data
                using ( MemoryStream animData = new MemoryStream( framesets[i] ) )
                {
                    // start reading the data
                    using ( BinaryReader reader = new BinaryReader( animData ) )
                    {
                        // read the VD animation header (for the current direction)
                        ReadVDHeader( reader );

                        // read the VD animation frames (for the current direction)
                        ReadVDFrames( reader, i );

                        // update the number of frames in the animation
                        m_FrameCount = (uint)Frames.Count;
                    }
                }
            }

            // clear the frames color list (we won't need anymore since every frame will have its own colors list)
            m_VDFramesetColours.Clear();
        }

        /// <summary>
        /// Create a cloned animation given the data of another animation
        /// </summary>
        public UOAnimation( int width, int height, short startX, short startY, short endX, short endY, int actionId, List<FrameEntry> frames, byte[] imageData, long imageDataOffset, List<ColourEntry> colours )
        {
            m_width = width;
            m_height = height;
            m_InitCoordsX = startX;
            m_InitCoordsY = startY;
            m_EndCoordsX = endX;
            m_EndCoordsY = endY;
            m_ActionID = actionId;

            // we duplicate all frames now
            foreach ( FrameEntry f in frames )
            {
                m_Frames.Add( f.Clone() );
            }

            // update the frames count
            m_FrameCount = (uint)m_Frames.Count;

            // copy the image data
            _ImageData = imageData.ToArray();
            _ImageDataOffset = imageDataOffset;

            // copy the colours list
            m_Colours = colours.ToList();
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Load the animation from the UOP file
        /// </summary>
        /// <param name="gamePath">EC Client path</param>
        public void LoadAnimationFromUOP( string gamePath )
        {
            // list of all the uop files
            MythicPackage UOP = new MythicPackage( Path.Combine( gamePath, m_uopFileName ) );

            // load the file memory stream data
            using ( MemoryStream stream = new MemoryStream( UOP.Blocks[m_blockID].Files[m_fileID].Unpack( UOP.FileInfo.FullName ) ) )
            {
                // load the animation frames images
                ReadFile( stream, true );
            }

            // clear the UOP data
            UOP = null;
        }

        /// <summary>
        /// Load the animation from the UOP file
        /// </summary>
        /// <param name="gamePath">EC Client path</param>
        /// <param name="uopFileName">UOP file name containing this animation</param>
        /// <param name="blockID">Block ID inside the UOP file of this animation</param>
        /// <param name="fileID">File ID inside the UOP file block of this animation</param>
        public void LoadAnimationFromUOP( string gamePath, string uopFileName, int blockID, int fileID )
        {
            // set the animation location inside the UOP file
            m_uopFileName = uopFileName;
            m_blockID = blockID;
            m_fileID = fileID;

            // load the animation
            LoadAnimationFromUOP( gamePath );
        }

        /// <summary>
        /// Save the animation in the VD file
        /// </summary>
        /// <param name="writer">binary writer attached to the file</param>
        /// <param name="headerPos">position in the file for the current animation headers</param>
        /// <param name="dir">direction to export</param>
        /// <param name="animPos">position in the file in which to write the animation data (color, frames, etc...)</param>
        /// <returns></returns>
        public bool ExportAnimationToVD( BinaryWriter writer, int dir, ref long headerPos, ref long animPos )
        {
            //// make sure the animation images are available
            //GenerateImages();

            // move to the animation header position
            writer.BaseStream.Seek( headerPos, SeekOrigin.Begin );

            // write the location of the animation data
            writer.Write( (int)animPos );

            // update the header position (for the next animation/direction)
            headerPos = writer.BaseStream.Position;

            // move to the animation data address
            writer.BaseStream.Seek( animPos, SeekOrigin.Begin );

            // initialize the list of colors (EC animations only)
            List<ColourEntry> cols = new List<ColourEntry>();

            // generate the colors for this direction (EC animations only)
            if ( Frames[dir * FramesPerDirection].VDFrameColors.Count <= 0 )
            {
                // create the image
                m_Frames[dir * FramesPerDirection].LoadFrameImage( _ImageData, _ImageDataOffset, m_Colours );

                // load the palette
                cols = Frames[dir * FramesPerDirection].GeneratePaletteFromImage();
            }

            // scan the colors table of the current frameset (there are always 256 colors)
            for ( int i = 0; i < 0x100; i++ )
            {
                // write the color (from VD file)
                if ( Frames[dir * FramesPerDirection].VDFrameColors.Count > 0 )
                    writer.Write( (ushort)( Frames[dir * FramesPerDirection].VDFrameColors[i].ColorRGB555 ^ 0x8000 ) );

                else // from EC animation
                    writer.Write( (ushort)( cols[i].ColorRGB555 ^ 0x8000 ) );
            }

            // store the current position
            long startPos = (int)writer.BaseStream.Position;

            // write the amount of frames in this direction
            writer.Write( FramesPerDirection );

            // store this position
            long seek = (int)writer.BaseStream.Position;

            // calculate the length of the frame data
            long curr = writer.BaseStream.Position + ( 4 * FramesPerDirection );

            // scan the frames of this direction of the animation
            for ( int f = dir * FramesPerDirection; f < ( dir * FramesPerDirection ) + FramesPerDirection; f++ )
            {
                // move the cursor to the first address available for this frame
                writer.BaseStream.Seek( seek, SeekOrigin.Begin );

                // write the frame data length
                writer.Write( (int)( curr - startPos ) );

                // update the position for the next frame
                seek = writer.BaseStream.Position;

                // move to write the frame data
                writer.BaseStream.Seek( curr, SeekOrigin.Begin );

                // create the frame with the animation (or forced) size
                using ( DirectBitmap realFrame = CreateRealFrameImage( f, null, 0, 0, true ) )
                {
                    // calculate the tile center
                    short centerX = (short)( m_width - EndX );
                    short centerY = (short)( -EndY );

                    // calculate the top-left corner coordinates
                    int topX = Math.Abs( (int)m_InitCoordsX - (int)m_Frames[f].InitCoordsX );
                    int topY = Math.Abs( (int)m_InitCoordsY - (int)m_Frames[f].InitCoordsY );

                    // write the frame data
                    Frames[f].ExportVDImageData( writer, realFrame, centerX, centerY, topX, topY, cols );
                }

                // update the position to write the next frame data
                curr = writer.BaseStream.Position;
            }

            // calculate the length of this frameset data
            long length = writer.BaseStream.Position - animPos;

            // update the position for the next frameset
            animPos = writer.BaseStream.Position;

            // move back to the headers location
            writer.BaseStream.Seek( headerPos, SeekOrigin.Begin );

            // write the frameset size
            writer.Write( (int)length );

            // write the "extra" flag (is always 0)
            writer.Write( (int)0 );

            // update the headers position for the next frameset
            headerPos = writer.BaseStream.Position;

            return true;
        }

        /// <summary>
        /// Gemerate all the images of the animation (so they can be used)
        /// </summary>
        public void GenerateImages()
        {
            // scan all the frames
            for ( int i = 0; i < m_Frames.Count; i++ )
            {
                // create the normal frame image
                if ( _ImageData != null )
                    m_Frames[i].LoadFrameImage( _ImageData, _ImageDataOffset, m_Colours );

                else // VD Image
                    m_Frames[i].LoadVDFrameImage();
            }
        }

        /// <summary>
        /// Create the sprite sheet for the current animation
        /// </summary>
        /// <param name="h">Hue color to use</param>
        /// <param name="width">Width to use for the frame cell</param>
        /// <param name="height">Height to use for the frame cell</param>
        public void CreateSpriteSheet( Hue h = null, int width = 0, int height = 0 )
        {
            // calculate the final spritesheet size
            int spriteSheetW = Math.Max( CellWidth, width ) * FramesPerDirection;
            int spriteSheetH = Math.Max( CellHeight, height ) * 5;

            // create the empty sprite sheet
            m_spriteSheet = new DirectBitmap( spriteSheetW, spriteSheetH );

            // current x/y coordinates in the spritesheet
            int currX = 0;
            int currY = 0;

            // current frame
            int currFrame = 0;

            // scan all the frames
            for ( int i = 0; i < m_Frames.Count; i++ )
            {
                // create the frame with the animation (or forced) size
                DirectBitmap realFrame = CreateRealFrameImage( i, h, width, height );

                // add the frame to the sheet
                using ( Graphics g = Graphics.FromImage( m_spriteSheet.Bitmap ) )
                    g.DrawImage( realFrame.Bitmap, currX, currY, Math.Max( CellWidth, width ), Math.Max( CellHeight, height ) );

                // increase the frame counter
                currFrame++;

                // move in position for the next frame
                if ( currFrame >= FramesPerDirection )
                {
                    currX = 0;
                    currY += Math.Max( CellHeight, height );

                    currFrame = 0;
                }
                else
                {
                    currX += Math.Max( CellWidth, width );
                }

                // delete the current frame images that we don't use
                m_Frames[i].DisposeImage();
                realFrame.Dispose();

                // prevent the app from freezing
                Application.DoEvents();
            }
        }

        /// <summary>
        /// Get the image for the specified frame.
        /// </summary>
        /// <param name="frameID">Frame to retrieve</param>
        /// <param name="original">Get the original image backup</param>
        /// <param name="isAnim">Indicate that we are seeking the frame of an animation (NOT paperdoll)</param>
        /// <returns>Bitmap of the frame</returns>
        public Bitmap GetFrameImage( int frameID, bool original = false, bool isAnim = true )
        {
            // search for the frame
            FrameEntry frm;

            // search for the paperdoll image
            if ( !isAnim )
                frm = Frames.Where( f => f.Frame == frameID ).FirstOrDefault();

            else // get the animation frame
                frm = Frames[frameID];

            // wrong frame ID?
            if ( frm == null || frameID < 0 )
                return null;

            // do we have to get the original image backup?
            if ( original && frm.OriginalImage != null )
                return frm.OriginalImage.Bitmap;

            // if the frames have been loaded, we just pick the image
            if ( frm.Image != null )
                return frm.Image.Bitmap;

            else // frames have not been loaded
            {
                // create the normal frame image
                if ( _ImageData != null )
                    frm.LoadFrameImage( _ImageData, _ImageDataOffset, m_Colours );

                else // VD Image
                    frm.LoadVDFrameImage();

                // verify if the frame s been loaded correctly before we pick the image
                if ( frm.Image != null )
                    return frm.Image.Bitmap;
            }

            return null;
        }

        /// <summary>
        /// Clone this animation
        /// </summary>
        public UOAnimation Clone()
        {
            return new UOAnimation( m_width, m_height, m_InitCoordsX, m_InitCoordsY, m_EndCoordsX, m_EndCoordsY, m_ActionID, m_Frames, _ImageData, _ImageDataOffset, m_Colours );
        }

        /// <summary>
        /// Free the memory from the frames images
        /// </summary>
        public void DisposeFrames()
        {
            // scan all the frames
            for ( int i = 0; i < m_Frames.Count; i++ )
            {
                // delete the image
                m_Frames[i].DisposeImage();
            }

            // remove the spritesheet
            if ( m_spriteSheet != null && !m_spriteSheet.Disposed )
            {
                m_spriteSheet.Dispose();
                m_spriteSheet = null;
            }
        }

        /// <summary>
        /// Delete the animation and free the memory
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
        /// Read the animation file
        /// </summary>
        /// <param name="memoryStream">Data stream of the file</param>
        /// <param name="loadImages">Do we have to load the images now? (default: false)</param>
        private void ReadFile( Stream memoryStream, bool loadImages = false )
        {
            // open the reader
            using ( BinaryReader reader = new BinaryReader( (Stream)memoryStream ) )
            {
                // do we have a correct file header?
                if ( ReadHeader( reader ) )
                {
                    // do we have to load all the images?
                    if ( loadImages == true )
                    {
                        // read the frame colors
                        ReadColours( reader );

                        // read the frames
                        ReadFrames( reader );

                        // read the pixels
                        ReadPixels( reader );

                        // store the action ID
                        m_ActionID = m_Frames[0].ID;
                    }
                    else // data only
                    {
                        // move to the frames table address
                        reader.BaseStream.Seek( (long)m_FrameAddress, SeekOrigin.Begin );

                        // we need to load the first frame to determine the action ID
                        FrameEntry firstFrame = new FrameEntry( reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), (uint)( (ulong)m_FrameAddress + (ulong)( 16 ) + (ulong)reader.ReadUInt32() ) );

                        // store the action ID
                        m_ActionID = firstFrame.ID;
                    }
                }

                // terminate the reader
                reader.Close();

                // destroy the reader
                reader.Dispose();
            }
        }

        /// <summary>
        /// Read the animation header
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        /// <returns>operation successful?</returns>
        private bool ReadHeader( BinaryReader reader )
        {
            // read the AMOU chars
            m_Head = reader.ReadBytes( 4 );

            // is the header correct?
            if ( m_Head[0] != (byte)65 || m_Head[1] != (byte)77 || m_Head[2] != (byte)79 )
                return false;

            // read the animation version
            m_Version = reader.ReadUInt32();

            // read the data length
            m_Length = reader.ReadUInt32();

            // read the body ID
            m_ID = reader.ReadUInt32();

            // read start coord X, relative to center of a tile
            m_InitCoordsX = reader.ReadInt16();

            // read start coord Y, relative to center of a tile
            m_InitCoordsY = reader.ReadInt16();

            // read end coord X, relative to center of a tile
            m_EndCoordsX = reader.ReadInt16();

            // read end coord Y, relative to center of a tile
            m_EndCoordsY = reader.ReadInt16();

            // read the number of colors
            m_ColourCount = reader.ReadUInt32();

            // read the colors table address
            m_ColourAddress = reader.ReadUInt32();

            // read the frames count
            m_FrameCount = reader.ReadUInt32();

            // read the frames table address
            m_FrameAddress = reader.ReadUInt32();

            // calculate the size of a sprite sheet cell size
            m_width = Math.Abs( (int)m_EndCoordsX - (int)m_InitCoordsX );
            m_height = Math.Abs( (int)m_EndCoordsY - (int)m_InitCoordsY );

            return true;
        }

        /// <summary>
        /// Read VD file animation header
        /// </summary>
        /// <param name="reader">binary reader attached to the file<</param>
        /// <returns></returns>
        private bool ReadVDHeader( BinaryReader reader )
        {
            // read the colors palette
            m_VDFramesetColours.Add( ReadVDColours( reader ) );

            // starting position to begin reading the frames (must be BEFORE the frame count)
            long start = (int)reader.BaseStream.Position;

            // number of frames to load
            m_FrameCount = (uint)reader.ReadInt32();

            // read the VD frames starting points
            ReadVDFramesOffset( reader, start );

            return true;
        }

        /// <summary>
        /// Read the VD frames byte starting points
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        /// <param name="start">starting position to sum to the starting point of each frame starting point</param>
        /// <returns></returns>
        private bool ReadVDFramesOffset( BinaryReader reader, long start )
        {
            // array with the starting position to read each frame
            m_vdFramesStartOffset = new long[m_FrameCount];

            // fill the lookup array with all the starting positions of all the frames
            for ( int i = 0; i < m_FrameCount; i++ )
            {
                // calculate the starting position for the frame
                m_vdFramesStartOffset[i] = start + reader.ReadInt32();
            }

            return true;
        }

        /// <summary>
        /// Read the animation colors
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        /// <returns>operation successful?</returns>
        private bool ReadColours( BinaryReader reader )
        {
            // move the to the colors table address
            reader.BaseStream.Seek( (long)m_ColourAddress, SeekOrigin.Begin );

            // scan all the colors in the table
            for ( int i = 0; (long)i < (long)m_ColourCount; i++ )
            {
                // add the color to the table
                m_Colours.Add( new ColourEntry( reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte() ) );
            }

            return true;
        }

        /// <summary>
        /// Read the colors palette of a VD frameset
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        /// <returns>list of colors for the current frameset</returns>
        private List<ColourEntry> ReadVDColours( BinaryReader reader )
        {
            // old files don't have a color count, we have to use a constant
            m_ColourCount = 256;

            // create the colors list for the current frameset
            List <ColourEntry> cols = new List<ColourEntry>();

            // scan all the colors of the animation
            for ( int i = 0; i < m_ColourCount; i++ )
            {
                // load the color (old files uses RGB555 instead of ARGB, so it will be converted later)
                cols.Add( new ColourEntry( (ushort)( reader.ReadUInt16() ^ 0x8000 ) ) );
            }

            return cols;
        }

        /// <summary>
        /// Read the animation frames
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        /// <returns>operation successful?</returns>
        private bool ReadFrames( BinaryReader reader )
        {
            // move to the frames table address
            reader.BaseStream.Seek( (long)m_FrameAddress, SeekOrigin.Begin );

            // scan all the frames
            for ( int i = 0; (long)i < (long)m_FrameCount; i++ )
            {
                // add the frame to the table
                m_Frames.Add( new FrameEntry( reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), (uint)( (ulong)m_FrameAddress + (ulong)( i * 16 ) + (ulong)reader.ReadUInt32() ) ) );
            }

            return true;
        }

        /// <summary>
        /// Read the animation frames of a VD file
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        ///  <param name="frameSet">current frame set index</param>
        /// <returns>operation successful?</returns>
        private bool ReadVDFrames( BinaryReader reader, int frameSet )
        {
            // scan all the frames
            for ( int i = 0; (long)i < (long)m_FrameCount; i++ )
            {
                // move to the frame table address
                reader.BaseStream.Seek( m_vdFramesStartOffset[i], SeekOrigin.Begin );

                // get the frame center X
                int centerX = reader.ReadInt16();

                // get the frame center Y
                int centerY = reader.ReadInt16();

                // get the frame width
                int width = reader.ReadUInt16();

                // get the frame height
                int height = reader.ReadUInt16();

                // the animation size should always be the size of the biggest frame
                m_width = Math.Max( m_width, width );
                m_height = Math.Max( m_height, height );

                // make sure there is something to load
                if ( m_width <= 0 || m_height <= 0 )
                    continue;

                // add the frame to the table
                m_Frames.Add( new FrameEntry( centerX, centerY, width, height, m_VDFramesetColours[frameSet], reader ) );
            }

            return true;
        }

        /// <summary>
        /// Read the animation frame pixels
        /// </summary>
        /// <param name="reader">binary reader attached to the file</param>
        /// <returns>operation successful?</returns>
        private bool ReadPixels( BinaryReader reader )
        {
            // calculate the image data offset
            _ImageDataOffset = (long)( m_FrameAddress + m_FrameCount * 16U );

            // create the image data array
            _ImageData = new byte[(int)( (long)m_Length - _ImageDataOffset )];

            // move to the pixels table address
            reader.BaseStream.Seek( _ImageDataOffset, SeekOrigin.Begin );

            // read all the pixels of the image
            _ImageData = reader.ReadBytes( (int)( (long)m_Length - _ImageDataOffset ) );

            return true;
        }

        /// <summary>
        /// Create a frame with the animation size
        /// </summary>
        /// <param name="i">frame index</param>
        /// <param name="h">hue to use</param>
        /// <param name="width">replace the animation width with this value</param>
        /// <param name="height">replace the animation width with this value</param>
        /// <returns></returns>
        private DirectBitmap CreateRealFrameImage( int i, Hue h = null, int width = 0, int height = 0, bool gif = false )
        {
            // if the frame image is not available, we load it first
            if ( m_Frames[i].Image == null )
                m_Frames[i].LoadFrameImage( _ImageData, _ImageDataOffset, m_Colours );

            // get the frame imge
            using ( DirectBitmap frameImage = h != null && h.ID != 0 ? new DirectBitmap( m_Frames[i].Image.ApplyHue( h.HueDiagram ) ) : m_Frames[i].Image )
            {
                // create the frame cell
                DirectBitmap realFrame = new DirectBitmap( Math.Max( CellWidth, width ), Math.Max( CellHeight, height ) );

                // draw the frame at the correct position
                using ( Graphics g = Graphics.FromImage( realFrame.Bitmap ) )
                {
                    // no gif? we just draw the image
                    if ( !gif )
                        g.DrawImage( frameImage.Bitmap, width != 0 ? 0 : Math.Abs( (int)m_InitCoordsX - (int)m_Frames[i].InitCoordsX ), height != 0 ? 0 : Math.Abs( (int)m_InitCoordsY - (int)m_Frames[i].InitCoordsY ) );

                    else // we need a gif
                    {
                        // create the gif and draw it
                        using ( Bitmap gifImage = new DirectBitmap( frameImage.Bitmap ).ToGif() )
                            g.DrawImage( gifImage, width != 0 ? 0 : Math.Abs( (int)m_InitCoordsX - (int)m_Frames[i].InitCoordsX ), height != 0 ? 0 : Math.Abs( (int)m_InitCoordsY - (int)m_Frames[i].InitCoordsY ) );
                    }
                }

                // clear the frame image
                m_Frames[i].DisposeImage();

                return realFrame;
            }
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose( bool disposing )
        {
            // has the animation already been disposed?
            if ( disposed )
                return;

            // are we disposing of the animation?
            if ( disposing )
            {
                // dispose of the memory used by the animation
                handle.Dispose();

                // scan all the frames
                for ( int i = 0; i < m_Frames.Count; i++ )
                {
                    // delete the image
                    m_Frames[i].Dispose();
                }

                // clear the lists
                m_Frames.Clear();
                m_Frames = null;

                m_Colours.Clear();
                m_Colours = null;

                // delete the sprite sheet
                if ( m_spriteSheet != null && !m_spriteSheet.Disposed )
                    m_spriteSheet.Dispose();

                // nullify all we can
                m_Head = null;
                m_Colours = null;
                _ImageData = null;
                m_spriteSheet = null;
            }

            // flag the animation as disposed
            disposed = true;
        }

        #endregion
    }
}
