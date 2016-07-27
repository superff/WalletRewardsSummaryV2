using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RewardsSummary
{
    class Store
    {
        private string Name;

        private string StoreID;

        private string Address;

        private string PhoneNumber;

        private string Distance;

        public Store(String Name, String StoreID, String Address, String PhoneNumber, String Distance)
        {
            this.Name = Name;
            this.StoreID = StoreID;
            this.Address = Address;
            this.PhoneNumber = PhoneNumber;
            this.Distance = Distance;
        }

        public string getName()
        {
            return Name;
        }

        public string getStoreID()
        {
            return StoreID;
        }

        public string getAddress()
        {
            return Address;
        }

        public string getPhoneNumber()
        {
            return PhoneNumber;
        }

        public string getDistance()
        {
            return Distance;
        }
    }
}
