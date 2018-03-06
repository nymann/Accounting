using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Windows.Forms;
using Client.Helper;

namespace Client
{
    public class Program
    {
        private readonly ReadFileLineByLine _rF = new ReadFileLineByLine();
        public readonly List<Person> EligiblePeople;
        private readonly CultureInfo _dK;
        private int _numberOfDinnerClubMeals;
        private bool _doBeerClubAccounting;
        private readonly double _moneySpentOnBeers = 0;
        private double _moneyOwedToKristian;

        public Program()
        {
            var myCi = new CultureInfo("da-DK", false);
            _dK = (CultureInfo) myCi.Clone();
            EligiblePeople = SetupEligiblePersons();
            DoShoppingListAccountIng(SetupShoppingList());
            DoDinnerClubAccounting(SetupDinnerClubList());
            BeerAccounting();
            WriteLatexSource();
        }

        [STAThread]
        private static void Main(string[] args)
        {
            new Program();
        }

        private List<Person> SetupEligiblePersons()
        {
            var eligiblePersons = new List<Person>();
            var file = _rF.ReadFile("C://KK24//residents.csv");
            foreach (var line in file)
            {
                if (line.Contains("Værelse"))
                {
                    continue;
                }

                var split = line.Split(',');

                if (split.Length < 3 || split.Length > 6)
                {
                    Console.WriteLine(
                        "Wrong syntax, in residents.csv.\nWas: '{0}'.\nShould've been: '{1}'.\nOr: '{1}{2}'.", split,
                        "roomNumber, nameOfResident, isPartOfDinnerclub", ",Balance");
                    Console.WriteLine("\nClosing Program after a key has been pressed.");
                    Console.ReadKey();
                    Environment.Exit(0);
                }

                var balance = Convert.ToDouble(split[3]);
                if (balance < 0)
                {
                    // Personen har et udestående fra sidste regnskab
                    _moneyOwedToKristian += Math.Abs(balance);
                }
                else
                {
                    // Personen har penge til gode fra sidste regnskab.
                    _moneyOwedToKristian += balance;
                }
                var movingInDate = Convert.ToDateTime(!split[4].Equals("") ? split[4] : "1/1/2000", _dK);
                var movingOutDate = split.Length > 5
                    ? Convert.ToDateTime(!split[5].Equals("") ? split[5] : "1/1/2423", _dK)
                    : Convert.ToDateTime("1/1/2423", _dK);

                var roomNumber = Convert.ToInt32(split[0]);
                var name = split[1];
                var isPartOfDinnerclub = split[2].Equals("ja");
                var person = new Person(roomNumber, name, isPartOfDinnerclub, balance, movingInDate, movingOutDate);
                eligiblePersons.Add(person);
            }

            eligiblePersons.First(person => person.NameOfPerson.Equals("Kristian")).BalanceFromPreviousAccounting +=
                _moneyOwedToKristian;

            return eligiblePersons;
        }

        private IEnumerable<Purchase> SetupShoppingList()
        {
            var input = _rF.ReadFile("C://KK24//ShoppingList.csv");
            var purchases = new List<Purchase>();
            foreach (var line in input)
            {
                if (line.Contains("Dato"))
                {
                    continue;
                }

                var purchase = line.Split(',');

                if (purchase[0].Equals(""))
                {
                    continue;
                }

                #region Date

                // purchase[0], is where the date information is stored.
                var date = Convert.ToDateTime(purchase[0], _dK);

                #endregion

                #region People who paid upfront for listing

                // purchase[1], is where the information is stored.
                var buyersRoomNumber = Convert.ToUInt16(purchase[1]);
                var peopleWhoBoughtItem =
                    EligiblePeople.Where(person => person.RoomNumber == buyersRoomNumber).ToList();

                #endregion

                #region Price

                // purchase[2]
                var price = Convert.ToDouble(purchase[2]);

                #endregion

                #region What was bought

                // purchase[3]
                var itemsBought = purchase[3].Split('-').ToList();

                #endregion

                purchases.Add(new Purchase(date, price, peopleWhoBoughtItem, itemsBought));
            }

            return purchases;
        }

        private IEnumerable<DinnerClubMeal> SetupDinnerClubList()
        {
            /*  
             *  CSV Format af Madklub er:
             *      Dato, Madlavere, Betaler, Pris, Gæster, Frameldninger, Ret\n  
             *      
             *  fx:
             *  2017/01/30,4 9, 4, 120.95, 9 9, 7, Stegt Flæsk.
             *  
             *  Hvilket betyder, at: 9 havde to ekstra gæster med.   
             */

            var input = _rF.ReadFile("C://KK24//DinnerClub.csv");
            var dinnerClubMeals = new List<DinnerClubMeal>();

            foreach (var line in input)
            {
                if (line.Contains("Dato"))
                {
                    continue;
                }

                var dinnerClubMeal = line.Split(',');

                #region date

                // dinnerClubMeal[0]


                var date = Convert.ToDateTime(!dinnerClubMeal[0].Equals("") ? dinnerClubMeal[0] : "01/01/2000", _dK);

                #endregion

                #region Cooks

                var strings = dinnerClubMeal[1].Split(' ');
                var roomNumbersOfCooks =
                    strings.Select(roomNumberOfCook => Convert.ToInt16(roomNumberOfCook))
                        .Select(dummy => (int) dummy)
                        .ToList();
                var cooks = EligiblePeople.Where(person => roomNumbersOfCooks.Contains(person.RoomNumber)).ToList();

                #endregion

                #region People that paid for the dinner upfront

                if (dinnerClubMeal[2].Equals(""))
                {
                    MessageBox.Show("ERROR: No one paid for the dinnerclub meal the " + date.ToString(_dK));
                }


                strings = dinnerClubMeal[2].Split(' ');
                var roomNumberOfpeopleWhoPaid =
                    strings.Select(roomNumberOfPersonWhoPaid => Convert.ToInt16(roomNumberOfPersonWhoPaid))
                        .Select(dummy => (int) dummy)
                        .ToList();
                var peopleWhoPaid =
                    EligiblePeople.Where(person => roomNumberOfpeopleWhoPaid.Contains(person.RoomNumber)).ToList();

                #endregion

                #region Price

                var priceString = dinnerClubMeal[3];
                var price = Convert.ToDouble(priceString);

                #endregion

                #region Guests

                var guests = Guests(dinnerClubMeal[4]);

                #endregion

                #region People Who Attended

                var namesOfPeopleWhoCancelled = dinnerClubMeal[5].Split(' ').ToList();
                namesOfPeopleWhoCancelled.Remove("");
                namesOfPeopleWhoCancelled.Remove(" ");
                foreach (var name in namesOfPeopleWhoCancelled)
                {
                    if (!EligiblePeople.Any(person => person.NameOfPerson.Equals(name)))
                    {
                        throw new Exception(
                            $"Der er angivet forkert navn i 'framelding' personen ({name}) eksisterer ikke!");
                    }
                }
                /*var peopleWhoAttended =
                    EligiblePeople.Where(
                            person => person.IsPartOfDinnerClub && !namesOfPeopleWhoCancelled.Contains(person.NameOfPerson) && DateTime.Compare(person.MovingOutDate, date) )
                        .ToList();*/

                var peopleWhoAttended = new List<Person>();
                foreach (var eligiblePerson in EligiblePeople)
                {
                    if (namesOfPeopleWhoCancelled.Contains(eligiblePerson.NameOfPerson))
                    {
                        continue;
                    }

                    if (!eligiblePerson.IsPartOfDinnerClub)
                    {
                        continue;
                    }

                    // Has the person already left Bergsøe at that date?
                    if (!eligiblePerson.MovingOutDate.Equals(null) &&
                        DateTime.Compare(date, eligiblePerson.MovingOutDate) > 0
                    ) // https://msdn.microsoft.com/en-us/library/system.datetime.compare(v=vs.110).aspx
                    {
                        continue;
                    }

                    // Has the person moved in yet at that date?
                    if (!eligiblePerson.MovingInDate.Equals(null) &&
                        DateTime.Compare(eligiblePerson.MovingInDate, date) > 0
                    ) // https://msdn.microsoft.com/en-us/library/system.datetime.compare(v=vs.110).aspx
                    {
                        continue;
                    }

                    peopleWhoAttended.Add(eligiblePerson);
                }

                #endregion

                #region Dish

                var dishName = dinnerClubMeal[6];

                #endregion

                dinnerClubMeals.Add(new DinnerClubMeal(date, cooks, peopleWhoPaid, price, guests, peopleWhoAttended,
                    dishName));
            }

            return dinnerClubMeals;
        }

        private void DoShoppingListAccountIng(IEnumerable<Purchase> purchases)
        {
            foreach (var purchase in purchases)
            foreach (var person in EligiblePeople)
            {
                if (purchase.BoughtByPeople.Contains(person))
                {
                    person.SpentShoppingList += purchase.Price / purchase.BoughtByPeople.Count;
                    person.NumberOfTimesPaidUpfront++;
                }

                if (DateTime.Compare(purchase.Date, person.MovingInDate) >= 0 &&
                    DateTime.Compare(purchase.Date, person.MovingOutDate) < 0)
                {
                    person.OweShoppingList += purchase.Price / NumberOfEligiblePeople(purchase.Date);
                }
            }
        }

        private int NumberOfEligiblePeople(DateTime date)
        {
            return EligiblePeople.Count(person => DateTime.Compare(date, person.MovingInDate) >= 0
                                                  && DateTime.Compare(date, person.MovingOutDate) < 0);
        }

        private void DoDinnerClubAccounting(IEnumerable<DinnerClubMeal> dinnerClubMeals)
        {
            foreach (var meal in dinnerClubMeals)
            {
                var doubleRoomPaidForGuest = false;
                var doubleRoomPaidForDinner = false;

                foreach (var person in meal.PeopleWhoAttended)
                {
                    if (meal.Cooks.Contains(person))
                    {
                        person.NumberOfTimesCooked++;
                    }

                    if (meal.PeopleWhoPaid.Contains(person))
                    {
                        var peopleWhoPaid = meal.PeopleWhoPaid.Count;
                        if (person.RoomNumber == 1 && !doubleRoomPaidForDinner)
                        {
                            doubleRoomPaidForDinner = true;
                            var roomOneHabitants = EligiblePeople.Where(p => p.RoomNumber == 1).ToList();
                            if (roomOneHabitants.Count != 2)
                            {
                                throw new Exception(
                                    "The number of people living in room number one is not equal to two.");
                            }
                            foreach (var roomOneHabitant in roomOneHabitants)
                            {
                                roomOneHabitant.SpentDinnerClub += meal.Price / peopleWhoPaid;
                                roomOneHabitant.NumberOfTimesPaidDinnerClubUpfront++;
                            }
                        } else if(person.RoomNumber != 1)
                        {
                            person.SpentDinnerClub += meal.Price / peopleWhoPaid;
                            person.NumberOfTimesPaidDinnerClubUpfront++;
                        }
                    }


                    person.OweDinnerClub += meal.Price / (meal.PeopleWhoAttended.Count + meal.Guests.Count);
                    person.NumberOfTimesParticipatedInTheDinnerClub++;

                    if (meal.Guests.Contains(person.RoomNumber))
                    {
                        if (person.RoomNumber == 1)
                        {
                            // Room 1 is a double room, and if they have a guest it shouldn't be added twice.
                            // However, we can't just divide by 2 since if only one person from room 1 attends with a guest.
                            if (!doubleRoomPaidForGuest)
                            {
                                person.OweDinnerClub += meal.Price / (meal.PeopleWhoAttended.Count + meal.Guests.Count);
                                doubleRoomPaidForGuest = true;
                            }
                        }
                        else
                        {
                            person.OweDinnerClub += meal.Price / (meal.PeopleWhoAttended.Count + meal.Guests.Count);
                        }
                    }
                }

                var roomNumbersOfPeopleWhoAttended =
                    meal.PeopleWhoAttended.Select(person => person.RoomNumber).ToList();
                foreach (var roomNumberOfGuest in meal.Guests)
                {
                    if (!roomNumbersOfPeopleWhoAttended.Contains(roomNumberOfGuest))
                    {
                        var guest = EligiblePeople.First(person => person.RoomNumber == roomNumberOfGuest);
                        guest.OweDinnerClub += meal.Price / (meal.PeopleWhoAttended.Count + meal.Guests.Count);
                    }
                }

                _numberOfDinnerClubMeals++;

                // Check balance
                var dinnerClubBalance = (int) EligiblePeople.Sum(person => person.DinnerClubBalance);
                if (dinnerClubBalance != 0)
                {
                    throw new Exception($"Dinnerclub total balance isn't 0! It's {dinnerClubBalance}.");
                }
            }
        }

        private int NumberOfBeersConsumed()
        {
            return EligiblePeople.Sum(person => person.ConsumedBeers);
        }

        private void BeerAccounting()
        {
            var doAccounting = MessageBox.Show("Do you wanna do beer-club accounting?",
                "Beer-Club",
                MessageBoxButtons.YesNo);

            if (doAccounting == DialogResult.Yes)
            {
                _doBeerClubAccounting = true;
                HowManyBeersDidEachPersonConsume();
                /*Console.Write("\nBeverages bought: ");
                var beersBought = Convert.ToInt32(Console.ReadLine());
                Console.Write("Price paid for the bought beers including deposit (pant): ");
                _moneySpentOnBeers = Convert.ToDouble(Console.ReadLine());
                var pricePrBeer = _moneySpentOnBeers / beersBought;*/
                Console.Write("Price pr. beer");
                var pricePrBeer = Convert.ToDouble(Console.ReadLine());
                Console.WriteLine("value entered: {0}", _moneySpentOnBeers.ToString("C", _dK));
                Console.WriteLine("Price pr. beer with deposit, is set to {0}.", pricePrBeer.ToString("C", _dK));

                foreach (var person in EligiblePeople.Where(person => person.ConsumedBeers > 0))
                {
                    person.BeerBalance -= person.ConsumedBeers * pricePrBeer;
                    // Deduct that from the guy who bought the beers?
                    if (person.NameOfPerson.Equals("Kristian"))
                    {
                        person.BeerBalance += NumberOfBeersConsumed() * pricePrBeer;
                    }
                }
            }
        }

        private void HowManyBeersDidEachPersonConsume()
        {
            Console.WriteLine("Type in how many beers the following people have consumed.");
            foreach (var person in EligiblePeople)
            {
                check:
                Console.Write("{0}: ", person.NameOfPerson);
                person.ConsumedBeers = Convert.ToInt32(Console.ReadLine());
                if (person.ConsumedBeers < 0)
                {
                    Console.WriteLine("Did {0} really drink {1} beers?! Try again.");
                    goto check;
                }
            }
        }


        private static List<int> Guests(string guests)
        {
            var list = new List<int>();

            if (guests.Length == 1)
            {
                list.Add(Convert.ToInt32(guests));
            }
            else if (guests.Length > 1)
            {
                var split = guests.Split(' ');
                list.AddRange(split.Select(s => Convert.ToInt32(s)));
            }

            return list;
        }

        private void WriteLatexSource()
        {
            var colorPositive = "67FD9A";
            var colorNegative = "E58080";
            string lastMonth;
            if (DateTime.Now.Month > 1)
            {
                lastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month - 1, DateTime.Now.Day).ToString("MMMM",
                    _dK);
            }
            else
            {
                lastMonth = "December";
            }

            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lastMonth.ToLower());
            var content = new List<string>
            {
                @"\documentclass{article}",
                @"\usepackage[utf8]{inputenc}",
                @"\usepackage[table,xcdraw]{xcolor}",
                @"",
                @"\title{Regnskab}",
                @"\author{KK24}",
                @"\date{" + DateTime.Now.Date.ToString("dd-MMMM-yyyy", _dK) + @"}",
                @"",
                @"\begin{document}",
                @"",
                @"\maketitle",
                @"",
                @"\section{Balance}",
                @"\begin{table}[h]",
                @"\centering",
                @"\caption{Regnskab}",
                @"\label{my-label}"
            };

            //content.Add(@"\title{Regnskab for " + lastMonth + ", " + DateTime.Now.Year + @"}");

            if (!_doBeerClubAccounting)
            {
                content.Add(@"\begin{tabular}{|c|c|c|c|c|}");
                content.Add(@"\hline");
                content.Add(
                    @"\textbf{Navn} & \textbf{Vær.} & \textbf{Total Balance} & \textbf{Indkøbsliste} & \textbf{Madklub} \\ \hline");

                foreach (var person in EligiblePeople)
                {
                    Console.WriteLine("SHOPPINGLIST: {0}, shopped for: {1}, owe: {2}, balance shoppinglist: {3}\n\n",
                        person.NameOfPerson, person.SpentShoppingList.ToString("C", _dK),
                        person.OweShoppingList.ToString("C", _dK),
                        (person.SpentShoppingList - person.OweShoppingList).ToString("C", _dK));
                    var color = person.Balance >= 0 ? colorPositive : colorNegative;
                    var shoppinglistBalance = (person.SpentShoppingList - person.OweShoppingList).ToString("C", _dK);
                    var dinnerClubBalance = (person.SpentDinnerClub - person.OweDinnerClub).ToString("C", _dK);

                    content.Add(person.NameOfPerson + " & " + person.RoomNumber + @" & \cellcolor[HTML]{" + color +
                                "} " +
                                person.Balance.ToString("C", _dK) + " & " + shoppinglistBalance + " & " +
                                dinnerClubBalance +
                                @" \\ \hline");
                }
            }
            else
            {
                content.Add(@"\begin{tabular}{|c|c|c|c|c|c|}");
                content.Add(@"\hline");
                content.Add(
                    @"\textbf{Navn} & \textbf{Vær.} & \textbf{Total Balance} & \textbf{Indkøbsliste} & \textbf{Madklub} & \textbf{Ølklub} \\ \hline");

                foreach (var person in EligiblePeople)
                {
                    var color = person.Balance >= 0 ? colorPositive : colorNegative;
                    var shoppinglistBalance = (person.SpentShoppingList - person.OweShoppingList).ToString("C", _dK);
                    var dinnerClubBalance = (person.SpentDinnerClub - person.OweDinnerClub).ToString("C", _dK);

                    content.Add(person.NameOfPerson + " & " + person.RoomNumber + @" & \cellcolor[HTML]{" + color +
                                "} " +
                                person.Balance.ToString("C", _dK) + " & " + shoppinglistBalance + " & " +
                                dinnerClubBalance + " & " + person.BeerBalance.ToString("C", _dK) +
                                @" \\ \hline");
                }
                var totalBalanceSum = (int) EligiblePeople.Sum(person => person.Balance);
                var shoppingListBalanceSum = (int) EligiblePeople.Sum(person => person.ShoppingListBalance);
                var dinnerClubBalanceSum = (int) EligiblePeople.Sum(person => person.DinnerClubBalance);
                var beerClubBalanceSum = (int) EligiblePeople.Sum(person => person.BeerBalance);
                content.Add(@"\textbf{Sum} & & " +
                            $"{totalBalanceSum} kr. & {shoppingListBalanceSum} kr. & {dinnerClubBalanceSum} kr. & {beerClubBalanceSum} kr." +
                            @"\\ \hline");
            }


            content.Add(@"\end{tabular}");
            content.Add(@"\end{table}");
            content.Add(
                @"Hvis total balance beløbet er negativt (rødt), skylder du penge. Til dem der har en MobilePay konto og jeg kender vedrørendes nummer er der sendt en anmodning. Hvis det ikke er tilfældet kan du betale via:");
            content.Add(@"\begin{itemize}");
            content.Add("\t" + @"\item MobilePay: 22 80 53 26");
            content.Add("\t" + @"\item Regnr. 5370, Kontonr. 302136, Arbejdernes landsbank");
            content.Add("\t" + @"\item PayPal: nymannjakobsen@gmail.com");
            content.Add(@"\end{itemize}");
            content.Add(@"");
            content.Add(
                @"Hvis beløbet er positivt (grønt), har du penge til gode. Dem får du ved at sende en besked til mig, med dine informationer til en af de 3 ovenstående muligheder.");
            content.Add(@"\newpage");
            content.Add(@"\section{Indkøbsliste statistik}");
            content.Add(@"\subsection{Hvem har købt ting ind oftest?}");
            content.Add(@"\begin{itemize}");

            var mostOccurences = EligiblePeople.OrderByDescending(x => x.NumberOfTimesPaidUpfront).ToList();
            var paidMost = EligiblePeople.OrderByDescending(x => x.SpentShoppingList).ToList();
            var doubleRoomAdded = false;
            foreach (var person in mostOccurences)
            {
                var name = person.NameOfPerson;

                if (person.RoomNumber == 1)
                {
                    if (!doubleRoomAdded)
                    {
                        doubleRoomAdded = true;
                        name = @"Emma \& Jacob";
                    }
                    else
                    {
                        continue;
                    }
                }
                content.Add("\t" + @"\item " + name + ": " + person.NumberOfTimesPaidUpfront);
            }
            content.Add(@"\end{itemize}");
            content.Add(@"\subsection{Hvem har lagt ud flest antal gange?}");
            content.Add(@"\begin{itemize}");
            doubleRoomAdded = false;
            foreach (var person in paidMost)
            {
                var name = person.NameOfPerson;

                if (person.RoomNumber == 1)
                {
                    if (!doubleRoomAdded)
                    {
                        doubleRoomAdded = true;
                        name = @"Emma \& Jacob";
                    }
                    else
                    {
                        continue;
                    }
                }

                content.Add("\t" + @"\item " + name + ": " + person.SpentShoppingList.ToString("C", _dK));
            }

            content.Add(@"\end{itemize}");
            content.Add(@"");
            content.Add(@"\newpage");
            content.Add(@"\section{Madklubs statistik}");
            content.Add(@"\subsection{Hvem har haft højest fremmøde?}");
            content.Add(@"\begin{itemize}");
            var attendancePercent =
                EligiblePeople.OrderByDescending(x => x.NumberOfTimesParticipatedInTheDinnerClub).ToList();
            var cookedOftestToLeast = EligiblePeople.OrderByDescending(x => x.NumberOfTimesCooked).ToList();
            var paidOftesToRarest =
                EligiblePeople.OrderByDescending(x => x.NumberOfTimesPaidDinnerClubUpfront).ToList();
            var paidMostToLeast = EligiblePeople.OrderByDescending(x => x.SpentDinnerClub);
            foreach (var person in attendancePercent.Where(x => x.OweDinnerClub != 0))
            {
                var name = person.NameOfPerson;

                // Todo check date aswell here.
                var percent = (double) person.NumberOfTimesParticipatedInTheDinnerClub /
                              (double) _numberOfDinnerClubMeals;

                var percentString = percent.ToString("P", _dK);
                percentString = percentString.Remove(percentString.IndexOf('%'));
                content.Add("\t" + @"\item " + name + ": " + percentString + @"\%");
            }

            content.Add(@"\end{itemize}");
            content.Add(@"\subsection{Hvem har kokkereret mest?}");
            content.Add(@"\begin{itemize}");

            foreach (var person in cookedOftestToLeast.Where(x => x.OweDinnerClub != 0))
            {
                content.Add("\t" + @"\item " + person.NameOfPerson + ": " + person.NumberOfTimesCooked);
            }

            content.Add(@"\end{itemize}");

            content.Add(@"\subsection{Hvem har lagt ud flest antal gange?}");
            content.Add(@"\begin{itemize}");

            foreach (var person in paidOftesToRarest.Where(x => x.OweDinnerClub != 0))
            {
                content.Add("\t" + @"\item " + person.NameOfPerson + ": " + person.NumberOfTimesPaidDinnerClubUpfront);
            }
            content.Add(@"\end{itemize}");


            content.Add(@"");
            content.Add(@"\subsection{Hvem har brugt flest penge på mad?}");
            content.Add(@"\begin{itemize}");

            foreach (var person in paidMostToLeast.Where(x => x.OweDinnerClub != 0))
            {
                content.Add("\t" + @"\item " + person.NameOfPerson + ": " + person.SpentDinnerClub.ToString("C", _dK));
            }
            content.Add(@"\end{itemize}");
            content.Add(@"");
            if (_doBeerClubAccounting)
            {
                var beersConsumedMostToLeast = EligiblePeople.OrderByDescending(x => x.ConsumedBeers);
                content.Add(@"\newpage");
                content.Add(@"\section{Ølklubs statistik}");
                content.Add(@"\subsection{Hvem har drukket flest øl?}");
                content.Add(@"\begin{itemize}");
                foreach (var person in beersConsumedMostToLeast)
                {
                    content.Add("\t" + @"\item " + person.NameOfPerson + ": " + person.ConsumedBeers);
                }
                content.Add(@"\end{itemize}");
                content.Add(@"");
            }
            content.Add(@"\end{document}");

            WriteToFile(content.ToArray());
        }

        private void WriteToFile(string[] content)
        {
            var day = DateTime.Now.Day.ToString(_dK);
            var month = DateTime.Now.ToString("MMMM", _dK);
            var year = DateTime.Now.Year;

            var sfd = new SaveFileDialog()
            {
                Filter = "LaTeX File|*.tex",
                Title = "Save Latex Source to TEX file.",
                FileName = "Latex - Source - KK24 Regnskab - " + day + "-" + month + "-" + year
            };

            if (sfd.ShowDialog() == DialogResult.OK && sfd.FileName != "")
            {
                File.Create(sfd.FileName).Close();
                File.WriteAllLines(sfd.FileName, content);
            }

            var lastMonth = "";
            if (DateTime.Now.Month > 1)
            {
                lastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month - 1, DateTime.Now.Day).ToString("MMMM",
                    _dK);
            }
            else
            {
                lastMonth = "December";
            }
            lastMonth = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lastMonth.ToLower());

            var command = @"pdflatex """ + sfd.FileName + @""" -job-name=""KK24 Regnskab for " + lastMonth +
                          ", udskrevet d. " + day + " " + month + " " + year + @"""";

            var fileInfo = new FileInfo(sfd.FileName);
            var cmd = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = fileInfo.DirectoryName
                }
            };
            cmd.Start();

            cmd.StandardInput.WriteLine(command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();

            var pathToPdf = fileInfo.DirectoryName + "\\" + "KK24 Regnskab for " + lastMonth + ", udskrevet d. " +
                            day + " " + month + " " + year + ".pdf";

            Process.Start(pathToPdf);
        }
    }
}