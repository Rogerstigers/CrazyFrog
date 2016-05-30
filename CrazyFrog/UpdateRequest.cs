using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrazyFrog
{
    internal class UpdateRequest
    {
        public string UrlToAudio { get; set; }
        public bool Enabled { get; set; }
        public int Volume { get; set; }
        public bool EnablePhotoSwitch { get; set; }
    }
}
