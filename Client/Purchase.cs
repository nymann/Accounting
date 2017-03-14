using System;
using System.Collections.Generic;

namespace Client
{
    public class Purchase
    {
        public double Price { get; }
        public List<Person> BoughtByPeople { get; } // two people live in one of the rooms, so it should be possible to split the bill.
        public DateTime Date { get; }
        public List<string> Items { get; }

        public Purchase(DateTime date, double price, List<Person> boughtByPeople, List<string> items)
        {
            Price = price;
            BoughtByPeople = boughtByPeople;
            Items = items;
            Date = date;
        }
    }
}