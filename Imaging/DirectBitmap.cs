using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UO_EC_Super_Viewer
{
    public class DirectBitmap : IDisposable
    {

        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Pointer to the allocated memory of the image
        /// </summary>
        protected GCHandle BitsHandle { get; private set; }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// The image
        /// </summary>
        public Bitmap Bitmap { get; private set; }

        /// <summary>
        /// Pixel array of the image
        /// </summary>
        public Int32[] Bits { get; private set; }

        /// <summary>
        /// Has the image been disposed?
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Image height
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Image width
        /// </summary>
        public int Width { get; private set; }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a new bitmap image
        /// </summary>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        public DirectBitmap( int width, int height )
        {
            // set the image width
            Width = width;

            // set the image height
            Height = height;

            // create the pixels array for the image
            Bits = new Int32[width * height];

            // allocate the memory for the image
            BitsHandle = GCHandle.Alloc( Bits, GCHandleType.Pinned );

            // create a new bitmap image in the allocated area of the memory
            Bitmap = new Bitmap( width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject() );

            // set the image empty (fully transparent)
            Graphics.FromImage( Bitmap ).Clear( Color.Transparent );
        }

        /// <summary>
        /// DirectBitmap from a bitmap image
        /// </summary>
        /// <param name="bmp">Bitmap image</param>
        public DirectBitmap( Bitmap bmp )
        {
            // make sure we have a valid image
            if ( bmp == null )
                return;

            // set the image width
            Width = bmp.Width;

            // set the image height
            Height = bmp.Height;

            // create the pixels array for the image
            Bits = new Int32[Width * Height];

            // allocate the memory for the image
            BitsHandle = GCHandle.Alloc( Bits, GCHandleType.Pinned );

            // create a new bitmap image in the allocated area of the memory
            Bitmap = new Bitmap( Width, Height, Width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject() );

            // initialize the graphics object on the image
            using ( Graphics g = Graphics.FromImage( Bitmap ) )
            {
                // set the image empty (fully transparent)
                g.Clear( Color.Transparent );

                // draw the image
                g.DrawImage( bmp, 0, 0 );
            }
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Remove an object from memory
        /// </summary>
        /// <param name="hObject">Memory pointer to the object to delete</param>
        [System.Runtime.InteropServices.DllImport( "gdi32.dll" )]
        public static extern bool DeleteObject( IntPtr hObject );

        /// <summary>
        /// Change the color of a pixel in the image
        /// </summary>
        /// <param name="x">X coodinate of the pixel in the image</param>
        /// <param name="y">Y coodinate of the pixel in the image</param>
        /// <param name="colour">Color to use as replacement</param>
        public void SetPixel( int x, int y, Color colour )
        {
            // get the pixel index in the array
            int index = x + (y * Width);

            // convert the color to argb
            int col = colour.ToArgb();

            // change the color into the array
            Bits[index] = col;
        }

        /// <summary>
        /// Get the color of a pixel in the image
        /// </summary>
        /// <param name="x">X coodinate of the pixel in the image</param>
        /// <param name="y">Y coodinate of the pixel in the image</param>
        /// <returns>Color of the pixel</returns>
        public Color GetPixel( int x, int y )
        {
            // get the pixel index in the array
            int index = x + (y * Width);

            // get the pixel color
            int col = Bits[index];

            // convert the color from argb to Color
            Color result = Color.FromArgb(col);

            return result;
        }

        /// <summary>
        /// Convert the image to gif with 256 colors
        /// </summary>
        /// <returns>256 colors gif image</returns>
        public Bitmap ToGif()
        {
            // initialize the color quantizer
            OctreeQuantizer quantizer = new OctreeQuantizer();

            // initialize the 256 colors palette
            List<Color> limitedPalette = new List<Color>();

            // scan the image pixels
            for ( int y = 0; y < Height; y++ )
            {
                for ( int x = 0; x < Width; x++ )
                {
                    // get the current color
                    Color c = GetPixel( x, y );

                    // add the color to the quantizer
                    quantizer.AddColor( c );
                }
            }

            // limit the colors to 256
            limitedPalette = quantizer.GetPalette( 256 );

            // initialize the bitmap memory lock data
            int stride = 4 * ( ( Width * 8 + 31 ) / 32 );
            byte[,] b = new byte[Height, stride];
            GCHandle gch = GCHandle.Alloc( b, GCHandleType.Pinned );

            // create the gif image
            using ( Bitmap quantizedBmp = new Bitmap( Width, Height, stride, PixelFormat.Format8bppIndexed, gch.AddrOfPinnedObject() ) )
            {
                // get the current palette
                ColorPalette pal = quantizedBmp.Palette;

                // fill the palette with the correct colors
                for ( int i = 0; i < pal.Entries.Length; i++ )
                {
                    // reset the color
                    pal.Entries[i] = Color.Transparent;

                    // if we have more colors, we add it
                    if ( i < limitedPalette.Count )
                        pal.Entries[i] = limitedPalette[i];
                }

                // store the palette
                quantizedBmp.Palette = pal;

                // scan the image
                for ( int y = 0; y < Height; y++ )
                {
                    for ( int x = 0; x < Width; x++ )
                    {
                        // replace the pixel with the new limited palette color
                        b[y, x] = (byte)Array.FindIndex( quantizedBmp.Palette.Entries, cl => cl == limitedPalette[quantizer.GetPaletteIndex( GetPixel( x, y ) )] );

                        // prevent the app from freezing
                        Application.DoEvents();
                    }
                }

                // unlock the bitmap
                gch.Free();

                // store the gif for future uses
                return (Bitmap)quantizedBmp.Clone();
            }
        }

        /// <summary>
        /// Convert the image to gif and keep it as DirectBitmap
        /// </summary>
        /// <returns></returns>
        public DirectBitmap ToGifDBmp()
        {
            return new DirectBitmap( ToGif() );
        }

        /// <summary>
        /// Get a grayscale copy of the image
        /// </summary>
        /// <returns>The grayscale copy of the image</returns>
        public Bitmap ToGrayscale()
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap( Bitmap.Width, Bitmap.Height );

            //get a graphics object from the new image
            using ( Graphics g = Graphics.FromImage( newBitmap ) )
            {
                //create the grayscale ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                                          new float[][]
                                          {
                                             new float[] {.3f, .3f, .3f, 0, 0},
                                             new float[] {.59f, .59f, .59f, 0, 0},
                                             new float[] {.11f, .11f, .11f, 0, 0},
                                             new float[] {0, 0, 0, 1, 0},
                                             new float[] {0, 0, 0, 0, 1}
                                          });

                //create some image attributes
                using ( ImageAttributes attributes = new ImageAttributes() )
                {

                    //set the color matrix attribute
                    attributes.SetColorMatrix( colorMatrix );

                    //draw the original image on the new image
                    //using the grayscale color matrix
                    g.DrawImage( Bitmap, new Rectangle( 0, 0, Bitmap.Width, Bitmap.Height ), 0, 0, Bitmap.Width, Bitmap.Height, GraphicsUnit.Pixel, attributes );
                }
            }

            return newBitmap;
        }

        /// <summary>
        /// Apply the specified hue
        /// </summary>
        /// <param name="hue">Hue bitmap (colors diagram) from the hues.uop file</param>
        /// <param name="onlyGrey">Color only the gray pixels?</param>
        /// <returns>Image with the hue applied</returns>
        public Bitmap ApplyHue( Bitmap hue, bool onlyGrey = false )
        {
            // initialize the colors list from the color diagram (hue)
            List<Color> levels =  new List<Color>();

            // we scan the hues horizzontally to get them all
            for ( int i = 0; i < hue.Width; i++ )
            {
                // get the color and store it in the list
                levels.Add( hue.GetPixel( i, 0 ) );
            }

            // list of all the pixel already handled (used for onlygray mode)
            List<Point> handled = new List<Point>();

            // get the grayscale version of the image
            using ( DirectBitmap gs = new DirectBitmap( onlyGrey ? Bitmap : ToGrayscale() ) )
            {
                // apply the color diagram to the grayscale image
                for ( int i = 0; i < gs.Width; i++ )
                {
                    for ( int j = 0; j < gs.Height; j++ )
                    {
                        // get the pixel color
                        Color pix = gs.GetPixel( i, j );

                        // skip the transparent pixels
                        if ( ColorMatch( pix, Color.FromArgb( 0, 0, 0, 0 ) ) )
                            continue;

                        // do we have to color only gray pixels or all of them?
                        if ( !onlyGrey )
                        {
                            // get the color level to apply
                            int level = pix.B;

                            // apply the color
                            gs.SetPixel( i, j, levels[level] );
                        }
                        else // only gray
                        {
                            // flood fill from this pixel (if it's gray)
                            FloodFillGrayPixels( gs, new Point( i, j ), levels, ref handled );
                        }
                    }
                }

                return (Bitmap)gs.Bitmap.Clone();
            }
        }

        /// <summary>
        /// Add/remove contrast to the pixel
        /// </summary>
        /// <param name="x">X coodinate of the pixel in the image</param>
        /// <param name="y">Y coodinate of the pixel in the image</param>
        /// <param name="threshold">Contrast level to apply (-100-100)</param>
        public void ContrastPixel( int x, int y, int threshold )
        {
            // make sure the threshold is capped correctly
            if ( threshold > 100 )
                threshold = 100;

            if ( threshold < -100 )
                threshold = -100;

            // calculate the contrast level to apply
            double contrastLevel = Math.Pow( ( 100.0 + threshold ) / 100.0, 2 );

            // get the current pixel color
            Color c = GetPixel( x, y );

            // convert the pixel color in float % values
            float rTint = c.R / 255.0f;
            float gTint = c.G / 255.0f;
            float bTint = c.B / 255.0f;

            double r = ( ( ( rTint - 0.5 ) * contrastLevel ) + 0.5 ) * 255.0;
            double g = ( ( ( gTint - 0.5 ) * contrastLevel ) + 0.5 ) * 255.0;
            double b = ( ( ( bTint - 0.5 ) * contrastLevel ) + 0.5 ) * 255.0;

            // cap the values to 255 or 0
            if ( r > 255 )
                r = 255;
            else if ( r < 0 )
                r = 0;

            if ( g > 255 )
                g = 255;
            else if ( g < 0 )
                g = 0;

            if ( b > 255 )
                b = 255;
            else if ( b < 0 )
                b = 0;

            // apply the new color
            SetPixel( x, y, Color.FromArgb( c.A, (int)r, (int)g, (int)b ) );
        }

        /// <summary>
        /// Add a color overlay to the pixel
        /// </summary>
        /// <param name="x">X coodinate of the pixel in the image</param>
        /// <param name="y">Y coodinate of the pixel in the image</param>
        /// <param name="c">Color to overlay</param>
        public void TintPixel( int x, int y, Color c )
        {
            // convert the colors in float % values
            float rTint = c.R / 255.0f;
            float gTint = c.G / 255.0f;
            float bTint = c.B / 255.0f;
            float aTint = c.A / 255.0f;

            // get the current pixel color
            Color px = GetPixel( x, y );

            // calculate the tint color
            float r = px.R + ( 255 - px.R ) * rTint;
            float g = px.G + ( 255 - px.G ) * gTint;
            float b = px.B + ( 255 - px.B ) * bTint;
            float a = px.A + ( 255 - px.A ) * aTint;

            // cap the values to 255
            if ( r > 255 )
                r = 255;

            if ( g > 255 )
                g = 255;

            if ( b > 255 )
                b = 255;

            if ( a > 255 )
                a = 255;

            // apply the new color
            SetPixel( x, y, Color.FromArgb( (int)a, (int)r, (int)g, (int)b ) );
        }

        /// <summary>
        /// Add a color shading to the pixel
        /// </summary>
        /// <param name="x">X coodinate of the pixel in the image</param>
        /// <param name="y">Y coodinate of the pixel in the image</param>
        /// <param name="c">Color to use as shade</param>
        public void ShadePixel( int x, int y, Color c )
        {
            // convert the colors in float % values
            float rTint = c.R / 255.0f;
            float gTint = c.G / 255.0f;
            float bTint = c.B / 255.0f;
            float aTint = c.A / 255.0f;

            // get the current pixel color
            Color px = GetPixel( x, y );

            // calculate the tint color
            float r = px.R * rTint;
            float g = px.G * gTint;
            float b = px.B * bTint;
            float a = px.A * aTint;

            // cap the values to 255
            if ( r > 255 )
                r = 255;

            if ( g > 255 )
                g = 255;

            if ( b > 255 )
                b = 255;

            if ( a > 255 )
                a = 255;

            // apply the new color
            SetPixel( x, y, Color.FromArgb( (int)a, (int)r, (int)g, (int)b ) );
        }

        /// <summary>
        /// Set the pixel color for a 8 bit indexed image (gif)
        /// </summary>
        /// <param name="bmp">Bitmap to set the pixel to</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="c">Color to set</param>
        public static void Set8bppIndexedPixel( Bitmap bmp, int x, int y, Color c )
        {
            // initialize the color index
            int i = Array.FindIndex( bmp.Palette.Entries, cl => cl == c );

            // crete a pixel image
            BitmapData bmpData = bmp.LockBits(new Rectangle(x, y, 1, 1), ImageLockMode.ReadOnly, bmp.PixelFormat);

            // set the pixel color
            Marshal.WriteByte( bmpData.Scan0, (byte)i );

            // release the image memory lock
            bmp.UnlockBits( bmpData );
        }

        /// <summary>
        /// Get the euclidean distance (indicating the difference between 2 colors)
        /// </summary>
        /// <param name="current">current color</param>
        /// <param name="match">color to compare</param>
        /// <returns>Euclidean distance between the 2 colors</returns>
        public static int GetDistance( Color current, Color match )
        {
            // calculate the rgb difference
            int redDifference = current.R - match.R;
            int greenDifference = current.G - match.G;
            int blueDifference = current.B - match.B;

            return redDifference * redDifference + greenDifference * greenDifference +
                                   blueDifference * blueDifference;
        }

        /// <summary>
        /// Crop the image removing the empty space around it
        /// </summary>
        /// <param name="image">Image to crop</param>
        /// <param name="margin">Optional margin to use in the final image</param>
        /// <returns>Image without the empty space around</returns>
        public static Bitmap CropImage( Bitmap image, int margin = 0 )
        {
            // do we have the image?
            if ( image == null )
                return null;

            // we create a directbitmap of the image so we can have better tools to analyze it
            using ( DirectBitmap img = new DirectBitmap( image ) )
            {
                // initialize the min an max x/y points
                int minX = int.MaxValue;
                int maxX = 0;
                int minY = int.MaxValue;
                int maxY = 0;

                // scan the image
                for ( int x = 0; x < image.Width; x++ )
                    for ( int y = 0; y < image.Height; y++ )
                    {
                        Color c = img.GetPixel( x, y );
                        // if this pixel is NOT transparent, then we found a point with something in it
                        if ( img.GetPixel( x, y ) != Color.FromArgb( 0, 0, 0, 0 ) )
                        {
                            // determine if this point is the min/max x
                            minX = Math.Min( minX, x );
                            maxX = Math.Max( maxX, x );

                            // determine if this point is the min/max y
                            minY = Math.Min( minY, y );
                            maxY = Math.Max( maxY, y );
                        }
                    }

                // calculate the final image size
                int newWidth = maxX - minX;
                int newHeight = maxY - minY;

                // create the final image
                Bitmap final = new Bitmap( newWidth + ( margin * 2 ), newHeight + ( margin * 2 ) );

                // create the drawing tools
                using ( Graphics g = Graphics.FromImage( final ) )
                {
                    // draw the image
                    g.DrawImage( image, margin, margin, new Rectangle( new Point( minX, minY ), new Size( newWidth, newHeight ) ), GraphicsUnit.Pixel );
                }

                return final;
            }
        }

        /// <summary>
        /// Rotate an image of X angle
        /// </summary>
        /// <param name="bmp">Image to rate</param>
        /// <param name="angle">Angle to rotate</param>
        /// <returns>Rotated image</returns>
        public static Bitmap RotateImage( Bitmap bmp, float angle )
        {
            // get the current image size
            float height = bmp.Height;
            float width = bmp.Width;

            // calculate the new image size
            int hypotenuse = (int)Math.Floor( Math.Sqrt( height * height + width * width ) );

            // create the image
            Bitmap rotatedImage = new Bitmap( hypotenuse, hypotenuse );

            // create the graphics tools
            using ( Graphics g = Graphics.FromImage( rotatedImage ) )
            {
                //set the rotation point as the center into the matrix
                g.TranslateTransform( (float)rotatedImage.Width / 2, (float)rotatedImage.Height / 2 );

                //rotate
                g.RotateTransform( angle );

                //restore rotation point into the matrix
                g.TranslateTransform( -(float)rotatedImage.Width / 2, -(float)rotatedImage.Height / 2 );

                // draw the image correctly
                g.DrawImage( bmp, ( hypotenuse - width ) / 2, ( hypotenuse - height ) / 2, width, height );
            }

            return rotatedImage;
        }

        /// <summary>
        /// Parse 2 colors
        /// </summary>
        /// <param name="a">First color</param>
        /// <param name="b">Second color</param>
        /// <returns>Does the color matches?</returns>
        public static bool ColorMatch( Color a, Color b )
        {
            return ( a.ToArgb() & 0xffffff ) == ( b.ToArgb() & 0xffffff );
        }

        /// <summary>
        /// Dispose of the image
        /// </summary>
        public void Dispose()
        {
            // has the image already been disposed?
            if ( Disposed )
                return;

            // flag the image as disposed
            Disposed = true;

            // free the memory used by the image
            BitsHandle.Free();

            // remove the image data array
            Bits = null;

            // destroy the bitmap object
            DeleteObject( Bitmap.GetHbitmap() );

            // dispose the image for good
            Bitmap.Dispose();
            Bitmap = null;

            // Suppress finalization.
            GC.SuppressFinalize( this );
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Check if a pixel is grayscale
        /// </summary>
        /// <param name="pxColor"></param>
        /// <returns></returns>
        private static bool IsPixelGrayScale( Color pxColor )
        {
            // ignore transparent pixels
            if ( ColorMatch( pxColor, Color.FromArgb( 0, 0, 0, 0 ) ) )
                return false;

            // max difference that 1 color can have
            int threshold = 20;

            int r = (int)Math.Floor( (double)pxColor.R / 10 ) * 10;
            int g = (int)Math.Floor( (double)pxColor.G / 10 ) * 10;
            int b = (int)Math.Floor( (double)pxColor.B / 10 ) * 10;

            // 2 colors are always the same, the third must be within the threshold with the others
            return ( r == g && Math.Abs( r - b ) <= threshold ) ||
                    ( r == b && Math.Abs( g - b ) <= threshold ) ||
                    ( g == b && Math.Abs( r - g ) <= threshold );
        }

        /// <summary>
        /// Floodfill gray pixels in an image
        /// </summary>
        /// <param name="bmp">Image to parse</param>
        /// <param name="pt">Starting point (must be gray)</param>
        /// <param name="levels">Hue colors table</param>
        /// <param name="handled">List of all the pixels already done</param>
        private static void FloodFillGrayPixels( DirectBitmap bmp, Point pt, List<Color> levels, ref List<Point> handled )
        {
            // create a points queue
            Queue<Point> q = new Queue<Point>();

            // add the current point to the queue
            q.Enqueue( pt );

            // keep going until the queue is empty
            while ( q.Count > 0 )
            {
                // get the first point
                Point n = q.Dequeue();

                // get the color for the current pixel
                Color currColor = bmp.GetPixel( n.X, n.Y );

                // if the pixel is not gray or is already been handled, we can get out (we also skil black and transparent colors)
                if ( !( currColor.R == currColor.G && currColor.R == currColor.B ) || ColorMatch( currColor, Color.Black ) || ColorMatch( currColor, Color.FromArgb( 0, 0, 0, 0 ) ) || handled.Contains( pt ) )
                    return;

                // get the next point
                Point w = n, e = new Point(n.X + 1, n.Y);

                // keep going until the colors are within the threshold or we reach the beginning of the line
                while ( ( w.X >= 0 ) && IsPixelGrayScale( bmp.GetPixel( w.X, w.Y ) ) )
                {
                    // replace the pixel color
                    bmp.SetPixel( w.X, w.Y, levels[bmp.GetPixel( w.X, w.Y ).B] );

                    // add the pixel to the list of the handled ones
                    if ( !handled.Contains( w ) )
                        handled.Add( w );

                    // if the previous pixel is grayscale, we put that in the queue
                    if ( ( w.Y > 0 ) && IsPixelGrayScale( bmp.GetPixel( w.X, w.Y - 1 ) ) )
                        q.Enqueue( new Point( w.X, w.Y - 1 ) );

                    // if the next pixel is grayscale, we put that in the queue
                    if ( ( w.Y < bmp.Height - 1 ) && IsPixelGrayScale( bmp.GetPixel( w.X, w.Y + 1 ) ) )
                        q.Enqueue( new Point( w.X, w.Y + 1 ) );

                    // move backwards in the line
                    w.X--;
                }

                // keep going until the colors are within the threshold or we reach the end of the line
                while ( ( e.X <= bmp.Width - 1 ) && IsPixelGrayScale( bmp.GetPixel( e.X, e.Y ) ) )
                {
                    // replace the pixel color
                    bmp.SetPixel( e.X, e.Y, levels[bmp.GetPixel( e.X, e.Y ).B] );

                    // add the pixel to the list of the handled ones
                    if ( !handled.Contains( e ) )
                        handled.Add( e );

                    // check the pixel on the previous line, if it matches, we add it to the queue
                    if ( ( e.Y > 0 ) && IsPixelGrayScale( bmp.GetPixel( e.X, e.Y - 1 ) ) )
                        q.Enqueue( new Point( e.X, e.Y - 1 ) );

                    // check the pixel in the next line, if it matches, we add it to the queue
                    if ( ( e.Y < bmp.Height - 1 ) && IsPixelGrayScale( bmp.GetPixel( e.X, e.Y + 1 ) ) )
                        q.Enqueue( new Point( e.X, e.Y + 1 ) );

                    // move forward along the line
                    e.X++;
                }
            }
        }

        #endregion
    }
}
