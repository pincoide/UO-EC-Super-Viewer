using Mythic.Package;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UO_EC_Super_Viewer
{
    class AudioData
    {
        // --------------------------------------------------------------
        #region PRIVATE VARIABLES
        // --------------------------------------------------------------
        /// <summary>
        /// UOP block index of the audio
        /// </summary>
        private int UOPBlock;

        /// <summary>
        /// UOP file index of the audio inside the bloc
        /// </summary>
        private int UOPFile;

        /// <summary>
        /// UOP file name
        /// </summary>
        private string UOPFileName = "Audio.uop";

        /// <summary>
        /// File header
        /// </summary>
        private string header;

        /// <summary>
        /// Sound channels
        /// </summary>
        private int channels = 1;

        /// <summary>
        /// Sound sample rate
        /// </summary>
        private int sampleRate = 22050;

        /// <summary>
        /// Sound bits per sample
        /// </summary>
        private int bitsPerSample = 16;

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC VARIABLES
        // --------------------------------------------------------------

        /// <summary>
        /// ID of the audio
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Name of the audio
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Flag indicating if the file is an mp3 or wav
        /// </summary>
        public bool IsMP3 { get; set; }

        /// <summary>
        /// Flag indicating this audio is not an audio and needs to be trashed
        /// </summary>
        public bool NotAudio { get; set; }

        #endregion

        // --------------------------------------------------------------
        #region CONSTRUCTORS
        // --------------------------------------------------------------

        /// <summary>
        /// Create a new audio data
        /// </summary>
        /// <param name="id">ID of the audio</param>
        /// <param name="name">Name of the audio</param>
        /// <param name="block">UOP block index for the audio</param>
        /// <param name="file">UOP file index inside the block for the audio</param>
        public AudioData( string gamePath, int id, string name, int block, int file )
        {
            // assign the ID
            ID = id;

            // assign the name
            Name = name;

            // assign the UOP data
            UOPBlock = block;
            UOPFile = file;
            UOPFileName = Path.Combine( gamePath, UOPFileName );

            // by default we consider the file an MP3
            IsMP3 = true;

            // verify the audio data
            using ( MemoryStream parse = GetAudioData() )
            {
                // read the file data
                using ( BinaryReader read = new BinaryReader( parse ) )
                {
                    // get the file header
                    header = Encoding.Default.GetString( read.ReadBytes( 4 ) );

                    // if the header starts with "riff" it's a wav file
                    if ( header.ToLower() == "riff" )
                    {
                        // read a bunch of stuff we don't need
                        read.ReadUInt32();
                        read.ReadBytes( 4 );
                        read.ReadBytes( 4 );
                        read.ReadUInt32();
                        read.ReadUInt16();

                        // read the wav data
                        channels = Math.Max( read.ReadUInt16(), channels );
                        sampleRate = Math.Max( (int)read.ReadUInt32(), sampleRate );
                        read.ReadUInt32();
                        read.ReadUInt16();
                        bitsPerSample = Math.Max( read.ReadUInt16(), bitsPerSample );

                        // flag that is NOT an mp3
                        IsMP3 = false;
                    }
                    else if ( header.ToLower().StartsWith( "ÿ" ) || header.ToLower() == "\0\0\0\0" || header.StartsWith( "ID3" ) )
                    {
                        // flag that is NOT an mp3
                        IsMP3 = true;
                    }
                    else // not mp3 or wav
                    {
                        NotAudio = true;
                    }
                }
            }
        }

        #endregion

        // --------------------------------------------------------------
        #region PUBLIC FUNCTIONS
        // --------------------------------------------------------------

        /// <summary>
        /// Play the audio file
        /// </summary>
        public MemoryStream GetAudioData()
        {
            // UOP of the audio files
            MythicPackage UOP = new MythicPackage( UOPFileName );

            // initialize the memory stream
            MemoryStream stream = new MemoryStream( UOP.Blocks[UOPBlock].Files[UOPFile].Unpack() );

            // reset the memory stream position
            stream.Seek( 0, SeekOrigin.Begin );

            return stream;
        }

        /// <summary>
        /// Get the wavestream for this audio
        /// </summary>
        /// <returns>Wavestream of this audio</returns>
        public WaveStream GetWaveStream()
        {
            // get the audio data
            MemoryStream ms = GetAudioData();

            // initialize the waveStream
            WaveStream ws = null;

            // if this is not an audio file, we can get out
            if ( NotAudio )
                return ws;

            // is the file an mp3?
            if ( IsMP3 )
            {
                // create the mp3 file reader
                ws = new Mp3FileReader( ms );
            }
            else // wav file
            {
                // create the wav file reader
                ws = new WaveFileReader( ms );
            }

            return ws;
        }

        /// <summary>
        /// Get the waveform points
        /// </summary>
        /// <param name="points">List of the waveform points</param>
        public static List<float> GetWavePoints( WaveStream ws )
        {
            // create the list of points to return
            List<float> points = new List<float>();

            // get the wave data
            WaveChannel32 wave = new WaveChannel32( ws );

            // create the reading buffer
            byte[] buffer = new byte[16384];

            // initialize the reading position
            int read;

            // read the whole file
            while ( wave.Position < wave.Length )
            {
                // read the first chunk
                read = wave.Read( buffer, 0, 16384 );

                // scan the data chunk
                for ( int i = 0; i < read / 4; i++ )
                {
                    // get the point
                    points.Add( BitConverter.ToSingle( buffer, i * 4 ) );
                }

                // allow the form to update
                Application.DoEvents();
            }

            return points;
        }

        /// <summary>
        /// Debug tool to get the UOP data
        /// </summary>
        /// <returns></returns>
        public string GetUOPDataString()
        {
            return "Block: " + UOPBlock + " File: " + UOPFile;
        }

        #endregion

        // --------------------------------------------------------------
        #region LOCAL FUNCTIONS
        // --------------------------------------------------------------

        #endregion

    }
}
