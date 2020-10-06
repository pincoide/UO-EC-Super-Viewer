using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UO_EC_Super_Viewer
{
    public class Hue
    {
        private string m_Name;
        private int m_ID;
        private Bitmap m_HueDiagram;

        /// <summary>
        /// Hue name
        /// </summary>
        public string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// Hue ID
        /// </summary>
        public int ID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// Hue color diagram
        /// </summary>
        public Bitmap HueDiagram
        {
            get { return m_HueDiagram; }
        }

        /// <summary>
        /// Create a new hue
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="name"></param>
        /// <param name="diagram"></param>
        public Hue( int ID, string name, Bitmap diagram )
        {
            m_ID = ID;
            m_Name = name;
            m_HueDiagram = diagram;
        }
    }
}
