using System;

namespace Client
{
    public class Person
    {
        #region Generic Person variables

        public DateTime MovingOutDate { get; }
        public DateTime MovingInDate { get; }
        public int RoomNumber { get; }
        public string NameOfPerson { get; }

        public double Balance
            =>
                (SpentShoppingList - OweShoppingList) + 
                (SpentDinnerClub - OweDinnerClub) + 
                (BalanceFromPreviousAccounting);

        public double SpentShoppingList { get; set; }
        public double OweShoppingList { get; set; }
        public double BalanceFromPreviousAccounting { get; }

        public double SpentDinnerClub { get; set; }
        public double OweDinnerClub { get; set; }
        public int NumberOfTimesParticipatedInTheDinnerClub { get; set; } = 0;
        #endregion

        #region Shopping list variables
        public int NumberOfTimesPaidUpfront { get; set; } = 0; // For statistics, non-essential.

        #endregion

        #region Dinner club variables

        public bool IsPartOfDinnerClub { get; }
        public uint NumberOfTimesCooked { get; set; } // For statistics, non-essential.
        public uint NumberOfTimesPaidDinnerClubUpfront { get; set; } // Hvor mange gange har personen lagt ud.

        #endregion

        public Person(int roomNumber, string nameOfPerson, bool isPartOfDinnerClub, double balance, DateTime movingInDate, DateTime movingOutDate)
        {
            RoomNumber = roomNumber;
            IsPartOfDinnerClub = isPartOfDinnerClub;
            MovingInDate = movingInDate;
            MovingOutDate = movingOutDate;
            BalanceFromPreviousAccounting = balance;

            NameOfPerson = nameOfPerson;
        }
    }
}