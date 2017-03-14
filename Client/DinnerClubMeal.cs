using System;
using System.Collections.Generic;

namespace Client
{
    public class DinnerClubMeal
    {
        public string DishName { get; }
        public double Price { get; }
        public List<Person> Cooks { get; }
        public List<Person> PeopleWhoPaid { get; } // It's a list, since multiple people could pay, fx. two people live in room 1.
        public DateTime Date { get; }
        public List<Person> PeopleWhoAttended { get; }
        public List<int> Guests { get; }

        public DinnerClubMeal(DateTime date, List<Person> cooks, List<Person> peopleWhoPaid, double price, List<int> guests , List<Person> peopleWhoAttended, string dishName)
        {
            DishName = dishName;
            PeopleWhoAttended = peopleWhoAttended;
            Price = price;
            Cooks = cooks;
            PeopleWhoPaid = peopleWhoPaid;
            Date = date;
            Guests = guests;
        }
    }
}