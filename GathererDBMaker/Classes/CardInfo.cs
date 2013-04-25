using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GathererDBMaker.Classes
{
    public class CardInfo
    {
        public int multiverseid { get; set; }
        public string name { get; set; }
        public string convmanacost { get; set; }
        public string type { get; set; }
        public string cardtext { get; set; }
        public string power { get; set; }
        public string toughness { get; set; }
        public string expansion { get; set; }
        public string rarity { get; set; }
        public string imgurl { get; set; }
    }
}
