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
                    getName(source); 

                }
            }
        }
        static void getName(string source)
        {
            int titlestart = source.IndexOf("<title>") + 7;
            int titleend = source.IndexOf("</title>") - 1;

            string name = source.Substring(titlestart, (titleend - titlestart));
            name = name.Substring(0, name.IndexOf("-"));

            name = name.Replace("\n", string.Empty);
            name = name.Replace("\r", string.Empty);
            name = name.Replace("\t", string.Empty);

            if (name.Contains("(") == true)
            {

            }

            Console.WriteLine(name);
        }
    }
}
