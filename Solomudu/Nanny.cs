using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Solomudu
{
    class Nanny : Brain
    {
        public Nanny(Connection c)
            : base(c, Guid.Empty)
        {
            c.Write("Enter your name: ");

        }

        public override void OnLine(string line)
        {
            var e = new Entity {
                HumanName = "Character:" + line
            };
            var ph = new Components.Physical {
                Entity = e,
                Location = Program.startingRoom,
                Name = line
            };
            var l = new Components.Listen
            {
                Entity = e,
                Location = Program.startingRoom
            };
            var i = new Components.Inventory
            {
                Entity = e
            };
            
            Console.WriteLine("Adding player {0}", line);

            using (var session = Program.SF.OpenSession())
            {
                    using (var tx = session.BeginTransaction())
                    {

                        session.Save(e);
                        session.Save(ph);
                        session.Save(l);
                        session.Save(i);
                        tx.Commit();
                    }
            }
            var p = new Player(conn, e.Id);
            conn.Brain = p;
            conn.Write("Welcome to Solomudu, {0}.", line);
        }

        public override string Prompt()
        {
            return "\r\n] ";
        }
    }
}
