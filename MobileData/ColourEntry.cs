using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace UO_EC_Super_Viewer
{
    public class ColourEntry
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------

        private byte m_R;
        private byte m_G;
        private byte m_B;
        private byte m_Alpha;
        private Color m_Color;
        private int m_colorRGB555;

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// Red
        /// </summary>
        public byte R
        {
            get
            {
                return m_R;
            }
        }

        /// <summary>
        /// Green
        /// </summary>
        public byte G
        {
            get
            {
                return m_G;
            }
        }

        /// <summary>
        /// Blue
        /// </summary>
        public byte B
        {
            get
            {
                return m_B;
            }
        }

        /// <summary>
        /// Alpha
        /// </summary>
        public byte Alpha
        {
            get
            {
                return m_Alpha;
            }
        }

        /// <summary>
        /// Color of the pixel
        /// </summary>
        public Color Pixel
        {
            get
            {
                return m_Color;
            }
        }

        /// <summary>
        /// Color in RGB555 (used in VD files)
        /// </summary>
        public int ColorRGB555
        {
            get
            {
                return m_colorRGB555;
            }

            set
            {
                m_colorRGB555 = value;
            }
        }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a new color entry for the pixel
        /// </summary>
        /// <param name="R">Red</param>
        /// <param name="G">Green</param>
        /// <param name="B">Blue</param>
        /// <param name="Alpha">Alpha</param>
        public ColourEntry( byte R, byte G, byte B, byte Alpha )
        {
            this.m_R = R;
            this.m_B = B;
            this.m_G = G;
            this.m_Alpha = Alpha;
            this.m_Color = Color.FromArgb( (int)R, (int)G, (int)B );

            // store the 16 bit color (in case we want to save into VD)
            m_colorRGB555 = ARGBtoRGB555( this.m_Color );
        }

        /// <summary>
        /// Create a color entry from an ARGB color
        /// </summary>
        /// <param name="col">ARGB color</param>
        public ColourEntry( Color col )
        {
            this.m_R = col.R;
            this.m_B = col.B;
            this.m_G = col.G;
            this.m_Alpha = col.A;
            this.m_Color = col;

            // store the 16 bit color (in case we want to save into VD)
            m_colorRGB555 = ARGBtoRGB555( this.m_Color );
        }

        /// <summary>
        /// Create a color entry from a 16 bit color
        /// </summary>
        /// <param name="color15">16 bit color value of the color(RGB 555)</param>
        public ColourEntry( ushort colorRGB555 )
        {
            // calculate the rgba values
            this.m_Alpha = 255;

            // calculate the red
            int r = (colorRGB555 & 0x7c00) >> 10;
            r = ( r * 0xff ) / 0x1f;

            // save the red value
            this.m_R = (byte)( r << 16 );

            // calculate the green
            int g = ( colorRGB555 & 0x03e0 ) >> 5;
            g = ( g * 0xff ) / 0x1f;

            // save the green value
            this.m_G = (byte)( g << 8 );

            // calculate the blue
            int b = ( colorRGB555 & 0x001f );
            b = ( b * 0xff ) / 0x1f;

            // save the blue value
            this.m_B = (byte)b;

            // store the 16 bit color
            m_colorRGB555 = colorRGB555;

            // save the color
            this.m_Color = Color.FromArgb( this.m_Alpha, r, g, b );
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Convert ARGB to a 16 bit color (RGB555)
        /// </summary>
        /// <param name="color">Color to convert</param>
        /// <returns></returns>
        public static ushort ARGBtoRGB555( Color color )
        {
            return (ushort)( ( color.A >= 128 ? 0x8000 : 0x0000 ) | ( ( color.R & 0xF8 ) << 7 ) | ( ( color.G & 0xF8 ) << 2 ) | ( color.B >> 3 ) );
        }

        #endregion
    }
}
