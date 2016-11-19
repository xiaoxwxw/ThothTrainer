using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothTrainer.Models
{
    public class User
    {
        public const int NEWID = 0;

        public long ID { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }

        public User()
        {
            ID = NEWID;
            Email = "";
            Name = "";
        }

        public User(string _Email, string _Name, long _ID)
        {
            Email = _Email;
            Name = _Name;
            ID = _ID;
        }
    }
}
