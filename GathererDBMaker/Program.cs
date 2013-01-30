using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GathererDBMaker
{
    class Program
    {
        static Boolean incLegality = false;
        static string DBPath = null;

        static string mkDatabase()
        {
            try
            {
 
                Console.WriteLine("Enter the path/name for the new database. \nExample: C:\\Users\\w9jds\\Desktop\\GathererDB.mdb");
                string input = Console.ReadLine();

                ADOX.Catalog CreateDB = new ADOX.Catalog();
                
                ADOX.Table CardTable = new ADOX.Table();
                CardTable.Name = "Cards";
                CardTable.Columns.Append("MultiverseID");
                CardTable.Columns.Append("Name");
                CardTable.Columns.Append("ConvManaCost");
                CardTable.Columns.Append("Type");
                CardTable.Columns.Append("CardText");
                CardTable.Columns.Append("Power");
                CardTable.Columns.Append("Toughness");
                CardTable.Columns.Append("Expansion");
                CardTable.Columns.Append("Rarity");
                CardTable.Columns.Append("ImgURL");

                CreateDB.Create("Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" + input + "; Jet OLEDB:Engine Type=5");
                CreateDB.Tables.Append(CardTable);

                ask: Console.WriteLine("Would you like to add card legalities to the database? (Will add A LOT of time to runtime.) y/n");
                string leginput = Console.ReadLine();
                if (string.Equals(leginput, "y", StringComparison.OrdinalIgnoreCase) == true || string.Equals(leginput, "yes", StringComparison.OrdinalIgnoreCase))
                    incLegality = true;
                else if (string.Equals(leginput, "n", StringComparison.OrdinalIgnoreCase) == true || string.Equals(leginput, "no", StringComparison.OrdinalIgnoreCase))
                    incLegality = false;
                else
                    goto ask;

                if (incLegality == true)
                {
                    ADOX.Table Legality = new ADOX.Table();
                    Legality.Name = "CardsLegality";
                    Legality.Columns.Append("MultiverseID");
                    Legality.Columns.Append("Format");
                    Legality.Columns.Append("Legality");
                    CreateDB.Tables.Append(Legality);
                }

                OleDbConnection DBcon = CreateDB.ActiveConnection as OleDbConnection;
                if (DBcon != null)
                    DBcon.Close();

                return input;
            }
            catch (OleDbException) { Console.WriteLine("Entered Invalid Path"); return null; }
            catch (Exception) { Console.WriteLine("\nAn error has occured while making the Database"); return null; }
        }
        
        static void Main(string[] args)
        {
            begin: string input = mkDatabase();

            if (input == null)
                goto begin;
            else
                DBPath = input;

            for (int i = 1; i <= 1500; i++)
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
                string convmanacost = "";
                string type = null;
                string cardtext = "";
                string power = "";
                string toughness = "";
                string expansion = null;
                string rarity = null;
                string imgurl = "";

                //Gets card name from web page source and removes the linebreaks and tabs
                int titlestart = source.IndexOf("<title>") + 7;
                int titleend = source.IndexOf("</title>") - 1;
                name = source.Substring(titlestart, (titleend - titlestart));
                name = name.Substring(0, name.IndexOf(" - Gatherer - Magic: The Gathering"));
                name = name.Replace("\n", string.Empty);
                name = name.Replace("\r", string.Empty);
                name = name.Replace("\t", string.Empty);
                name = name.Trim();

                if (source.Contains("Converted Mana Cost:</div>") == true)
                {
                    int manacoststart = source.IndexOf("Converted Mana Cost:</div>");
                    int manacostend = source.IndexOf("<br />", manacoststart);
                    convmanacost = source.Substring(manacoststart, (manacostend - manacoststart));
                    convmanacost = convmanacost.Substring((convmanacost.IndexOf("\">") + 2), (convmanacost.Length - (convmanacost.IndexOf("\">") + 2)));
                    convmanacost = convmanacost.Replace("\n", string.Empty);
                    convmanacost = convmanacost.Replace("\r", string.Empty);
                    convmanacost = convmanacost.Replace("\t", string.Empty);
                    convmanacost = convmanacost.Trim();
                }

                //Gets the card's type
                int typestart = source.IndexOf("Types:</div>");
                int typeend = source.IndexOf("</div>", (typestart +15));
                type = source.Substring(typestart, (typeend - typestart));
                type = type.Substring((type.IndexOf(">", 15) + 1), (type.Length - (type.IndexOf(">", 15) + 1)));
                type = type.Replace("\n", string.Empty);
                type = type.Replace("\r", string.Empty);
                type = type.Replace("\t", string.Empty);
                type = type.Trim();

                //Gets the card's image
                imgurl = "http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=" + i + "&type=card";
                /*int imgstart = source.IndexOf("<td class=\"leftCol\" align=\"center\">");
                int imgend = source.IndexOf("/>", imgstart);
                imgurl = source.Substring(imgstart, (imgend - imgstart));*/

                if (source.Contains("P/T:</div>") == true)
                {
                    int PTstart = source.IndexOf("P/T:</div>");
                    int PTend = source.IndexOf("</div>", PTstart + 10);
                    string PT = source.Substring(PTstart, (PTend - PTstart));
                    PT = PT.Substring((PT.IndexOf("\">") + 2), (PT.Length - (PT.IndexOf("\">")+2)));
                    PT = PT.Replace("\n", string.Empty);
                    PT = PT.Replace("\r", string.Empty);
                    PT = PT.Replace("\t", string.Empty);
                    PT = PT.Replace(" ", string.Empty);
                    string[] PTsplit = PT.Split('/');
                    power = PTsplit[0];
                    toughness = PTsplit[1];
                }

                //Gets the Cards Rarity
                int raritystart = source.IndexOf("Rarity:</div>");
                int rarityend = source.IndexOf("</span>", raritystart);
                rarity = source.Substring(raritystart, (rarityend - raritystart));
                rarity = rarity.Substring(rarity.IndexOf("'>") + 2, (rarity.Length - rarity.IndexOf("'>")) - 2);
                rarity = rarity.Trim();

                //If the title contains parenthesis it cuts it out and puts what was inside into expansion
                if (name.Contains("(") == true)
                {
                    expansion = name.Substring(name.IndexOf("(")+1, (name.IndexOf(")") - name.IndexOf("("))-1);
                    name = name.Substring(0, name.IndexOf("("));
                    name = name.Trim();
                }

                saveCard(multiverseid, name, convmanacost, type, cardtext, power, toughness, expansion, rarity, imgurl);
                if (incLegality == true)
                    getLegalitySource(multiverseid);

                Console.WriteLine(name + "was added to the database.");
            }
        }

        static void getLegalitySource(int multiversid)
        {
            WebRequest request = HttpWebRequest.Create("http://gatherer.wizards.com/Pages/Card/Printings.aspx?multiverseid=" + multiversid);
            request.Method = "GET";
            using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                string source = reader.ReadToEnd();
                getLegality(multiversid, source);
            }
        }

        static void getLegality(int multiversid, string source)
        {
            string legacy = null;
            List<string> formats = new List<string>();
            List<string> legalities = new List<string>();

            int tablestart = source.IndexOf("<table class=\"cardList\" cellspacing=\"0\" cellpadding=\"2\">") + 1;
            legacy = source.Substring(tablestart, (source.Length - tablestart));
            tablestart = legacy.IndexOf("<table class=\"cardList\" cellspacing=\"0\" cellpadding=\"2\">");
            int tableend = legacy.IndexOf("</table>", tablestart);
            legacy = legacy.Substring(tablestart, (tableend - tablestart));
            string[] legacysplit = legacy.Split(new string[] { "<tr class=\"cardItem evenItem\">", "<tr class=\"cardItem oddItem\">" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < legacysplit.Length; i++)
            {
                string[] split = legacysplit[i].Split(new string[] { "<td style=\"width:40%;\">", "<td style=\"text-align:center;\">", "<td>" }, StringSplitOptions.RemoveEmptyEntries);                int end = split[1].IndexOf("</td>");
                string format = split[1].Substring(0, end);
                format = format.Replace("\n", string.Empty);
                format = format.Replace("\r", string.Empty);
                format = format.Replace("\t", string.Empty);
                format = format.Trim();
                formats.Add(format);
                end = split[2].IndexOf("</td>");
                string legality = split[2].Substring(0, end);
                legality = legality.Replace("\n", string.Empty);
                legality = legality.Replace("\r", string.Empty);
                legality = legality.Replace("\t", string.Empty);
                legality = legality.Trim();
                legalities.Add(legality);
            }
            saveLegality(multiversid, formats, legalities);
        }

        static void saveCard(int multiverseid, string name, string convmanacost, string type, string cardtext, string power, string toughness, string expansion, string rarity, string imgurl)
        {
            OleDbConnection DBcon = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + DBPath);
            DBcon.Open(); //opens OLEBD connection to Database
            OleDbCommand cmd = new OleDbCommand();
            cmd.CommandText = "INSERT INTO Cards([MultiverseID], [Name], [ConvManaCost], [Type], [CardText], [Power], [Toughness], [Expansion], [Rarity], [ImgURL]) VALUES (@MultiverseID, @Name, @ConvManaCost, @Type, @CardText, @Power, @Toughness, @Expansion, @Rarity, @ImgURL)";
            //Adds a new card and all the information for it to the CardTable
            cmd.Parameters.Add("@MultiverseID", OleDbType.VarChar).Value = multiverseid;
            cmd.Parameters.Add("@Name", OleDbType.VarChar).Value = name;
            cmd.Parameters.Add("@ConvManaCost", OleDbType.VarChar).Value = convmanacost;
            cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = type;
            cmd.Parameters.Add("@CardText", OleDbType.VarChar).Value = cardtext;
            cmd.Parameters.Add("@Power", OleDbType.VarChar).Value = power;
            cmd.Parameters.Add("@Toughness", OleDbType.VarChar).Value = toughness;
            cmd.Parameters.Add("@Expansion", OleDbType.VarChar).Value = expansion;
            cmd.Parameters.Add("@Rarity", OleDbType.VarChar).Value = rarity;
            cmd.Parameters.Add("@ImgURL", OleDbType.VarChar).Value = imgurl;
            cmd.Connection = DBcon;
            cmd.ExecuteNonQuery();
            DBcon.Close();
        }

        static void saveLegality(int multiverseid, List<string> formats, List<string> legalities)
        {
            for (int i = 0; i < formats.Count(); i++)
            {
                OleDbConnection DBcon = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + DBPath);
                DBcon.Open(); //opens OLEBD connection to Database
                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = "INSERT INTO CardsLegality([MultiverseID], [Format], [Legality]) VALUES (@MultiverseID, @Format, @Legality)";
                //Adds a new card and all the information for it to the CardTable
                cmd.Parameters.Add("@MultiverseID", OleDbType.VarChar).Value = multiverseid;
                cmd.Parameters.Add("@Format", OleDbType.VarChar).Value = formats[i];
                cmd.Parameters.Add("@Legality", OleDbType.VarChar).Value = legalities[i];
                cmd.Connection = DBcon;
                cmd.ExecuteNonQuery();
                DBcon.Close();
            }
        }
    }

}