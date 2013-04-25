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
        private static Boolean incLegality = false;
        private static string DBPath = null;
        private static int multiverseidstart = 1;
        private static int multiverseidend = 0;
        
        static void Main(string[] args)
        {
            begin: string input = mkDatabase();

            if (input == null)
                goto begin;
            else
                DBPath = input;

            for (int i = multiverseidstart; i <= multiverseidend; i++)
            {
                getInfo(i);
            }
        }

        static string mkDatabase()
        {
            try
            {
                Console.WriteLine("Enter the path/name for the new database. \nExample: C:\\Users\\w9jds\\Desktop\\GathererDB.mdb");
                string input = Console.ReadLine();

                do
                {
                    try
                    {
                        Console.WriteLine("What multiverseid would you like to start with?");
                        multiverseidstart = Convert.ToInt32(Console.ReadLine());
                        Console.WriteLine("What multiverseid would you like to end with?");
                        multiverseidend = Convert.ToInt32(Console.ReadLine());
                    }
                    catch (Exception) { multiverseidend = 0; multiverseidstart = 1; }
                } while (multiverseidstart > multiverseidend);

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

                //ask: Console.WriteLine("Would you like to add card legalities to the database? (Will add A LOT of time to runtime.) y/n");
                //    string leginput = Console.ReadLine();
                //    if (string.Equals(leginput, "y", StringComparison.OrdinalIgnoreCase) == true || string.Equals(leginput, "yes", StringComparison.OrdinalIgnoreCase))
                //        incLegality = true;
                //    else if (string.Equals(leginput, "n", StringComparison.OrdinalIgnoreCase) == true || string.Equals(leginput, "no", StringComparison.OrdinalIgnoreCase))
                //        incLegality = false;
                //    else
                //        goto ask;

                //if (incLegality == true)
                //{
                //    ADOX.Table Legality = new ADOX.Table();
                //    Legality.Name = "CardsLegality";
                //    Legality.Columns.Append("MultiverseID");
                //    Legality.Columns.Append("Format");
                //    Legality.Columns.Append("Legality");
                //    CreateDB.Tables.Append(Legality);
                //}

                OleDbConnection DBcon = CreateDB.ActiveConnection as OleDbConnection;
                if (DBcon != null)
                    DBcon.Close();

                return input;
            }
            catch (OleDbException) { Console.WriteLine("Entered Invalid Path"); return null; }
            catch (Exception) { Console.WriteLine("\nAn error has occured while making the Database"); return null; }
        }
        
        private static async Task getInfo(int i)
        {
            bool lang, WasSkipped;
            string source = null;
            var request = (HttpWebRequest)WebRequest.Create("http://gatherer.wizards.com/Pages/Card/Details.aspx?multiverseid=" + i);
            var langrequest = (HttpWebRequest)WebRequest.Create("http://gatherer.wizards.com/Pages/Card/Languages.aspx?multiverseid=" + i);
            request.AllowAutoRedirect = false;
            var httpRes = (HttpWebResponse)request.GetResponse();
            if (httpRes.StatusDescription.Equals("Found") == false)
            {
                using (StreamReader langreader = new StreamReader(langrequest.GetResponse().GetResponseStream()))
                {
                    string langsource = langreader.ReadToEnd();
                    lang = langsource.Contains("English");
                }
                if (lang == false)
                {
                    using (StreamReader reader = new StreamReader(httpRes.GetResponseStream()))
                    {
                        source = reader.ReadToEnd();
                        if (source.Contains("id=\"ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cardImage\"") == true)
                            WasSkipped = false;
                        else
                        {
                            Console.WriteLine(i + " was skipped.");
                            WasSkipped = true;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(i + " was skipped.");
                    WasSkipped = true;
                }
            }
            else
            {
                Console.WriteLine(i + " was skipped.");
                WasSkipped = true;
            }
                
            httpRes.Close();

            if (WasSkipped == false)
            {
                Classes.CardInfo Card = new Classes.CardInfo();

                Card.multiverseid = i;
                //Gets card name from web page source and removes the linebreaks and tabs
                int titlestart = source.IndexOf("<title>") + 7;
                int titleend = source.IndexOf("</title>") - 1;

                Card.name = source.Substring(titlestart, (titleend - titlestart));
                Card.name = Card.name.Substring(0, Card.name.IndexOf(" - Gatherer - Magic: The Gathering"));
                Card.name = Card.name.Replace("\n", string.Empty);
                Card.name = Card.name.Replace("\r", string.Empty);
                Card.name = Card.name.Replace("\t", string.Empty);
                Card.name = Card.name.Trim();

                //If the title contains parenthesis it cuts it out and puts what was inside into expansion
                if (Card.name.Contains("(") == true)
                {
                    Card.expansion = Card.name.Substring(Card.name.IndexOf("(") + 1, (Card.name.IndexOf(")") - Card.name.IndexOf("(")) - 1);
                    Card.name = Card.name.Substring(0, Card.name.IndexOf("("));
                    Card.name = Card.name.Trim();
                }

                if (source.Contains("Converted Mana Cost:</div>") == true) // if the card has a converted mana cost
                {
                    //parse the info from the site
                    int manacoststart = source.IndexOf("Converted Mana Cost:</div>");
                    int manacostend = source.IndexOf("<br />", manacoststart);

                    Card.convmanacost = source.Substring(manacoststart, (manacostend - manacoststart));
                    Card.convmanacost = Card.convmanacost.Substring((Card.convmanacost.IndexOf("\">") + 2), (Card.convmanacost.Length - (Card.convmanacost.IndexOf("\">") + 2)));
                    Card.convmanacost = Card.convmanacost.Replace("\n", string.Empty);
                    Card.convmanacost = Card.convmanacost.Replace("\r", string.Empty);
                    Card.convmanacost = Card.convmanacost.Replace("\t", string.Empty);
                    Card.convmanacost = Card.convmanacost.Trim();
                }

                //Gets the card's type
                int typestart = source.IndexOf("Types:</div>");
                int typeend = source.IndexOf("</div>", (typestart + 15));

                Card.type = source.Substring(typestart, (typeend - typestart));
                Card.type = Card.type.Substring((Card.type.IndexOf(">", 15) + 1), (Card.type.Length - (Card.type.IndexOf(">", 15) + 1)));
                Card.type = Card.type.Replace("\n", string.Empty);
                Card.type = Card.type.Replace("\r", string.Empty);
                Card.type = Card.type.Replace("\t", string.Empty);
                Card.type = Card.type.Trim();

                Card.imgurl = "http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=" + i + "&type=card";

                if (source.Contains("P/T:</div>") == true) //if the card has power and toughness
                {
                    //parse it from the source
                    int PTstart = source.IndexOf("P/T:</div>");
                    int PTend = source.IndexOf("</div>", PTstart + 10);
                    string PT = source.Substring(PTstart, (PTend - PTstart));
                    PT = PT.Substring((PT.IndexOf("\">") + 2), (PT.Length - (PT.IndexOf("\">") + 2)));
                    PT = PT.Replace("\n", string.Empty);
                    PT = PT.Replace("\r", string.Empty);
                    PT = PT.Replace("\t", string.Empty);
                    PT = PT.Replace(" ", string.Empty);
                    string[] PTsplit = PT.Split('/');
                    Card.power = PTsplit[0];
                    Card.toughness = PTsplit[1];
                }

                //Gets the Cards Rarity
                int raritystart = source.IndexOf("Rarity:</div>");
                int rarityend = source.IndexOf("</span>", raritystart);
                Card.rarity = source.Substring(raritystart, (rarityend - raritystart));
                Card.rarity = Card.rarity.Substring(Card.rarity.IndexOf("'>") + 2, (Card.rarity.Length - Card.rarity.IndexOf("'>")) - 2);
                Card.rarity = Card.rarity.Trim();

                if (Card.rarity.Contains("/>") == true)
                    Card.rarity = "";

                saveCard(Card);

                //if (incLegality == true)
                //    getLegalitySource(Card.multiverseid);


            }
        }

        //static void getLegalitySource(int multiversid)
        //{
        //    WebRequest request = HttpWebRequest.Create("http://gatherer.wizards.com/Pages/Card/Printings.aspx?multiverseid=" + multiversid);
        //    request.Method = "GET";
        //    using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
        //    {
        //        string source = reader.ReadToEnd();
        //        getLegality(multiversid, source);
        //    }
        //}

        //static void getLegality(int multiversid, string source)
        //{
        //    string legacy = null;
        //    List<string> formats = new List<string>();
        //    List<string> legalities = new List<string>();

        //    int tablestart = source.IndexOf("<table class=\"cardList\" cellspacing=\"0\" cellpadding=\"2\">") + 1;
        //    legacy = source.Substring(tablestart, (source.Length - tablestart));
        //    tablestart = legacy.IndexOf("<table class=\"cardList\" cellspacing=\"0\" cellpadding=\"2\">");
        //    int tableend = legacy.IndexOf("</table>", tablestart);
        //    legacy = legacy.Substring(tablestart, (tableend - tablestart));
        //    string[] legacysplit = legacy.Split(new string[] { "<tr class=\"cardItem evenItem\">", "<tr class=\"cardItem oddItem\">" }, StringSplitOptions.RemoveEmptyEntries);
        //    for (int i = 1; i < legacysplit.Length; i++)
        //    {
        //        string[] split = legacysplit[i].Split(new string[] { "<td style=\"width:40%;\">", "<td style=\"text-align:center;\">", "<td>" }, StringSplitOptions.RemoveEmptyEntries);
        //        int end = split[1].IndexOf("</td>");
        //        string format = split[1].Substring(0, end);
        //        format = format.Replace("\n", string.Empty);
        //        format = format.Replace("\r", string.Empty);
        //        format = format.Replace("\t", string.Empty);
        //        format = format.Trim();
        //        formats.Add(format);
        //        end = split[2].IndexOf("</td>");
        //        string legality = split[2].Substring(0, end);
        //        legality = legality.Replace("\n", string.Empty);
        //        legality = legality.Replace("\r", string.Empty);
        //        legality = legality.Replace("\t", string.Empty);
        //        legality = legality.Trim();
        //        legalities.Add(legality);
        //    }
        //    saveLegality(multiversid, formats, legalities);
        //}

        private static async Task saveCard(Classes.CardInfo Card)
        {

            if (Card.cardtext == null)
                Card.cardtext = "";
            if (Card.convmanacost == null)
                Card.convmanacost = "";
            if (Card.power == null)
                Card.power = "";
            if (Card.rarity == null)
                Card.rarity = "";
            if (Card.toughness == null)
                Card.toughness = "";

            try
            {
                OleDbConnection DBcon = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + DBPath);
                await DBcon.OpenAsync(); //opens OLEBD connection to Database

                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = "INSERT INTO Cards([MultiverseID], [Name], [ConvManaCost], [Type], [CardText], [Power], [Toughness], [Expansion], [Rarity], [ImgURL]) VALUES (@MultiverseID, @Name, @ConvManaCost, @Type, @CardText, @Power, @Toughness, @Expansion, @Rarity, @ImgURL)";
                //Adds a new card and all the information for it to the CardTable
                cmd.Parameters.Add("@MultiverseID", OleDbType.Integer).Value = Card.multiverseid;
                cmd.Parameters.Add("@Name", OleDbType.VarChar).Value = Card.name;
                cmd.Parameters.Add("@ConvManaCost", OleDbType.VarChar).Value = Card.convmanacost;
                cmd.Parameters.Add("@Type", OleDbType.VarChar).Value = Card.type;
                cmd.Parameters.Add("@CardText", OleDbType.VarChar).Value = Card.cardtext;
                cmd.Parameters.Add("@Power", OleDbType.VarChar).Value = Card.power;
                cmd.Parameters.Add("@Toughness", OleDbType.VarChar).Value = Card.toughness;
                cmd.Parameters.Add("@Expansion", OleDbType.VarChar).Value = Card.expansion;
                cmd.Parameters.Add("@Rarity", OleDbType.VarChar).Value = Card.rarity;
                cmd.Parameters.Add("@ImgURL", OleDbType.VarChar).Value = Card.imgurl;
                cmd.Connection = DBcon;
                cmd.ExecuteNonQuery();
                DBcon.Close();

                Console.WriteLine(Card.multiverseid + " (" + Card.name + ") was added to the database.");
            }
            catch (Exception) { Console.WriteLine(Card.name + " was skipped"); };


        }

        //static void saveLegality(int multiverseid, List<string> formats, List<string> legalities)
        //{
        //    for (int i = 0; i < formats.Count(); i++)
        //    {
        //        OleDbConnection DBcon = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + DBPath);
        //        DBcon.Open(); //opens OLEBD connection to Database
        //        OleDbCommand cmd = new OleDbCommand();
        //        cmd.CommandText = "INSERT INTO CardsLegality([MultiverseID], [Format], [Legality]) VALUES (@MultiverseID, @Format, @Legality)";
        //        //Adds a new card and all the information for it to the CardTable
        //        cmd.Parameters.Add("@MultiverseID", OleDbType.VarChar).Value = multiverseid;
        //        cmd.Parameters.Add("@Format", OleDbType.VarChar).Value = formats[i];
        //        cmd.Parameters.Add("@Legality", OleDbType.VarChar).Value = legalities[i];
        //        cmd.Connection = DBcon;
        //        cmd.ExecuteNonQuery();
        //        DBcon.Close();
        //    }
        //}
    }
}