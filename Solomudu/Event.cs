using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;

namespace Solomudu
{
    public class Event
    {
        public virtual Guid Id { get; protected set; }

        public virtual Entity Target { get; set; }

        public virtual String Text { get; set; } // todo: less string, more fields

        public static void AddEvent(ISession s, Entity target, String text)
        {
            s.Save(new Event
            {
                Target = target,
                Text = text
            });
        }
    }
}
