using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using System.Threading.Tasks;

namespace GathererDBMaker
{
    class Program
    {
        static void Main(string[] args)
        {

            for (int i = 1; i <= 100; i++)
            {
                WebRequest request = HttpWebRequest.Create("http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid=" + i);
                request.Method = "GET";
                using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    string source = reader.ReadToEnd();
                    getInfo(source, i); 

                }
            }
        }
        static void getInfo(string source, int i)
        {
            if (source.Contains("id=\"ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cardImage\""))
            {
                int multiverseid = i;
                string name = null;
                string convmanacost = null;
                string type = null;
                string cardtext = null;
                string expansion = null;
                string rarity = null;
                string imgurl = null;

                //Gets card name from web page source and removes the linebreaks and tabs
                int titlestart = source.IndexOf("<title>") + 7;
                int titleend = source.IndexOf("</title>") - 1;

                name = source.Substring(titlestart, (titleend - titlestart));
                name = name.Substring(0, name.IndexOf(" - Gatherer - Magic: The Gathering"));
                name = name.Replace("\n", string.Empty);
                name = name.Replace("\r", string.Empty);
                name = name.Replace("\t", string.Empty);
                name = name.Trim();

                //Gets the card's type
                int typestart = source.IndexOf("Types:</div>");
                int typeend = source.IndexOf("</div>", (typestart +15));

                type = source.Substring(typestart, (typeend - typestart));
                type = type.Substring(type.IndexOf(">", 15), (type.Length - type.IndexOf(">", 15)));
                type = type.Replace("\n", string.Empty);
                type = type.Replace("\r", string.Empty);
                type = type.Replace("\t", string.Empty);
                type = type.Replace(">", string.Empty);
                type = type.Trim();

                

                //If the title contains parenthesis it cuts it out and puts what was inside into expansion
                if (name.Contains("(") == true)
                {
                    expansion = name.Substring(name.IndexOf("(")+1, (name.IndexOf(")") - name.IndexOf("("))-1);
                    name = name.Substring(0, name.IndexOf("("));
                }

                Console.WriteLine(name + "\n\tType: " + type + "\n\tExpansion: " + expansion);

            }
        }
    }
}