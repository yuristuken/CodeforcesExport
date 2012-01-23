using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Google.GData.Calendar;
using Google.GData.Client;
using Google.GData.Extensions;

namespace CodeforcesExport
{
    public enum Division
    {
        None = 0,
        First = 1, 
        Second = 2, 
        Both = 3
    };

    class Contest
    {
        public string Title {
            get; 
            set;
        }

        public Division Division {
            get;
            set;
        }

        public DateTime Start {
            get;
            set;
        }

        public DateTime End {
            get;
            set;
        }
    }

    class Program
    {
        static List<Contest> getContests()
        {
            HtmlWeb htmlWeb = new HtmlWeb();

            HtmlDocument doc = htmlWeb.Load("http://codeforces.ru/contests");

            Console.WriteLine("Page loaded");

            List<Contest> contests = new List<Contest>();

            HtmlNodeCollection contestsNodes = doc.DocumentNode.SelectNodes("//div[@class='contestlist']/div[@class='datatable']/div/table/tr[position() > 1]");

            if (contestsNodes.Count == 1 && contestsNodes[0].SelectSingleNode("./td[1]").FirstChild.InnerText.Trim() == "Соревнования пока отсутствуют")
            {
                Console.WriteLine("No contests found");
                return contests;
            }
            Console.WriteLine(contestsNodes.Count + " contests found");

            

            foreach (HtmlNode node in contestsNodes)
            {
                Contest contest = new Contest();
                string firstCell = node.SelectSingleNode("./td[1]").FirstChild.InnerText.Trim();
                Regex r = new Regex(@"^(.*?)(?: \(Div\. ([12])(?: Only)?\))?$");
                Match m = r.Match(firstCell);
                if (m.Groups[2].Success)
                {
                    if (m.Groups[2].Value == "1")
                        contest.Division = Division.First;
                    else
                        contest.Division = Division.Second;

                }
                else
                {
                    contest.Division = Division.Both;
                }
                contest.Title = m.Groups[1].Value;

                string secondCell = node.SelectSingleNode("./td[2]").ChildNodes[1].InnerText;
                contest.Start = DateTime.Parse(secondCell);

                string thirdCell = node.SelectSingleNode("./td[3]").ChildNodes[0].InnerText.Trim();
                TimeSpan ts = TimeSpan.Parse(thirdCell);

                contest.End = contest.Start.Add(ts);
                contests.Add(contest);
            }
            return contests;
        }


        static void Main(string[] args)
        {


            string username = Properties.Settings.Default.username;
            string password = Properties.Settings.Default.password;
            Uri calendarUri = Properties.Settings.Default.calendarUri;
            Division divisionExclusion = Properties.Settings.Default.divisionExclusion;


            // Filter by division
            List<Contest> contests = getContests().Where((contest) => (contest.Division ^ divisionExclusion) > 0).ToList();

            CalendarService service = new CalendarService("codeforces-export");
            service.setUserCredentials(username, password);

            foreach (Contest contest in contests)
            {
                // Check if this contest already exists, update then
                EventQuery q = new EventQuery();
                q.Uri = calendarUri;
                q.StartTime = contest.Start;
                q.EndTime = contest.End;

                EventFeed feed = service.Query(q);

                EventEntry eventEntry;
                bool contestExists;
                if (feed.Entries.Count == 1)
                {
                    eventEntry = (EventEntry)feed.Entries[0];
                    contestExists = true;
                }
                else
                {
                    // No such contest, create new
                    eventEntry = new EventEntry();
                    contestExists = false;
                }

                eventEntry.Title.Text = contest.Title;
                eventEntry.Content.Content = contest.Title + " (";
                if (contest.Division == Division.Both)
                    eventEntry.Content.Content += "Both divs.";
                else if (contest.Division == Division.First)
                    eventEntry.Content.Content += "Div. 1";
                else
                    eventEntry.Content.Content += "Div. 2";
                eventEntry.Content.Content += ")";

                eventEntry.Times.Add(new When(contest.Start, contest.End));

                if (contestExists)
                {
                    eventEntry.Update();
                    Console.WriteLine("Updated " + eventEntry.Content.Content + " event");
                }
                else
                {
                    service.Insert(calendarUri, eventEntry);
                    Console.WriteLine("Added " + eventEntry.Content.Content + " event");
                }
            }
        }
    }
}
