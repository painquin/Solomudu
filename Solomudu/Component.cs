using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Solomudu
{
    public abstract class Component
    {
        public virtual Guid Id { get; protected set; }
        public virtual Entity Entity { get; set; }


        public virtual string Peek()
        {
            return String.Format("[{0}]: {1}", Id, GetType());
        }

    }
}
