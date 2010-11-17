using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Solomudu
{
    public class Account
    {
        public virtual Guid Id { get; protected set; }
        public virtual String Name { get; set; }
        public virtual String PasswordHash { get; set; }
    }
}
