using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NHibernate;
using NHibernate.Criterion;

namespace Solomudu.Components
{
    public class Exit : Component
    {

        public enum Direction
        {
            None = 0,

            North,
            East,
            South,
            West,

            Up,
            Down

            // todo not lazy

        }

        [Flags]
        public enum ExFlags
        {
            Closed = 0x1,
            Locked = 0x2,
            Hidden = 0x4,
        }

        

        /*
         * In both instances, the entity referred to will either
         * posess a Physical component or a Location component.
         * Location components are less numerous and more likely to be present for this component.
         * they will be searched first, followed by Physical components. 
         * In the event of a Location component, that is the destination.
         * In the event of a Physical component, that component's Location is the destination.
         */
        //public virtual Entity Entity { get; set; } (inherited)
        public virtual Entity Destination { get; set; }

        public virtual Direction Dir { get; set; }
        public virtual ExFlags Flags { get; set; }

        public virtual String Name { get; set; }


        #region Static


        public static Location GetDestination(ISession s, Exit x)
        {
            if (x == null) return null;
            var loc = x.Destination.GetFirstComponent<Location>(s);
            if (loc == null)
            {
                var phy = x.Destination.GetFirstComponent<Physical>(s);
                if (phy == null) return null;
                return phy.Location;
                
            }
            return loc;
        }

        [Command("exits")]
        public static void DoExits(Brain brain, string[] args)
        {
            using (var session = Program.SF.OpenSession())
            {
                List<Exit> exits = new List<Exit>();

                var phy = Entity.GetFirstComponent<Physical>(session, brain.EntityID);

                if (phy == null) return; //whut


                exits.AddRange(phy.Location.Entity.GetComponents<Exit>(session));

                foreach (var p in session.CreateCriteria<Physical>().Add(Expression.Eq("Location", phy.Location)).List<Physical>())
                {
                    exits.AddRange(p.Entity.GetComponents<Exit>(session));
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("Exits:\r\n");
                foreach (var x in exits)
                {
                    sb.AppendFormat("{0}: {1}\r\n",
                        x.Dir,
                        Exit.GetDestination(session, x)
                            .Name
                        );
                }

                sb.Append("\r\n");

                brain.Write(sb.ToString());

            }


        }


        static Components.Exit GetExit(ISession s, ICriteria c, Location loc)
        {
            c.Add(Expression.Or(
                Expression.Eq("Entity", loc.Entity),
                Expression.InG<Entity>("Entity", s
                    .CreateCriteria<Physical>()
                    .Add(Expression.Eq("Location", loc))
                    .List<Physical>()
                    .Select(p => p.Entity).ToList())
            ));
            return c.List<Exit>().FirstOrDefault();
        }

        static Components.Exit ExitForDirection(ISession s, Location loc, Exit.Direction dir)
        {
            return GetExit(s, s.CreateCriteria<Exit>().Add(Expression.Eq("Dir", dir)), loc);
        }

        static Components.Exit ExitForName(ISession s, Location loc, string name)
        {
            return GetExit(s, s.CreateCriteria<Exit>().Add(Expression.Eq("Name", name)), loc);
        }

        static void MoveToLocation(ISession s, Physical p, Location loc)
        {
            // other components to move with Physicals:
            // Listen
            foreach (var lis in p.Entity.GetComponents<Listen>(s.CreateCriteria<Listen>().Add(Expression.Eq("Location", p.Location))))
            {
                lis.Location = loc;
                s.Update(lis);
            }
            p.Location = loc;
            s.Update(p);

        }

        static void useExit(ISession s, Physical p, Exit x)
        {
            if (x == null) return;
            var origin = p.Location;
            var destination = Exit.GetDestination(s, x);

            // todo: event messages
            using (var tx = s.BeginTransaction())
            {
                foreach (var lis in origin.Contents<Listen>(s))
                {
                    if (lis.Entity != p.Entity)
                    {
                        Event.AddEvent(s, lis.Entity, string.Format("{0} leaves.", p.Name));
                    }
                }
                foreach (var lis in destination.Contents<Listen>(s))
                {
                    if (lis.Entity != p.Entity)
                    {
                        Event.AddEvent(s, lis.Entity, string.Format("{0} enters.", p.Name));
                    }
                }
                MoveToLocation(s, p, destination);
                tx.Commit();
            }
        }

        static void Do_direction_(Brain brain, Direction dir) {
            if (dir == Direction.None) return;

            using (var session = Program.SF.OpenSession())
            {
                var phy = Entity.GetFirstComponent<Physical>(session, brain.EntityID);
                var x = ExitForDirection(session,
                    phy.Location,
                    dir
                    );
                if (x == null)
                {
                    brain.Write("You can't go {0}.\r\n", dir);
                    return;
                }
                useExit(session, phy, x);
            }
            Player.DoLook(brain, new string[] { });
        }

        [Command("n")]
        [Command("north")]
        public static void DoNorth(Brain brain, string[] args)
        {
            Do_direction_(brain, Direction.North);
        }

        [Command("s")]
        [Command("south")]
        public static void DoSouth(Brain brain, string[] args)
        {
            Do_direction_(brain, Direction.South);
        }

        [Command("e")]
        [Command("east")]
        public static void DoEast(Brain brain, string[] args)
        {
            Do_direction_(brain, Direction.East);
        }

        [Command("w")]
        [Command("west")]
        public static void DoWest(Brain brain, string[] args)
        {
            Do_direction_(brain, Direction.West);
        }

        [Command("u")]
        [Command("up")]
        public static void DoUp(Brain brain, string[] args)
        {
            Do_direction_(brain, Direction.Up);
        }

        [Command("d")]
        [Command("down")]
        public static void DoDown(Brain brain, string[] args)
        {
            Do_direction_(brain, Direction.Down);
        }

        static Direction DirectionFromName(string name) {
            return (from p in Enum.GetNames(typeof(Direction))
                    where p.StartsWith(name, StringComparison.CurrentCultureIgnoreCase)
                    orderby p descending
                    
                    select (Direction)Enum.Parse(typeof(Direction), p, true)
                ).FirstOrDefault();
        }

        static Direction InvertDirection(Direction d) {
            switch(d) {
                case Direction.North:
                    return Direction.South;
                case Direction.South:
                    return Direction.North;
                case Direction.East:
                    return Direction.West;
                case Direction.West:
                    return Direction.East;
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                default:
                    return Direction.None;
            }
        }

        [Command("tunnel")]
        public static void DoTunnel(Brain brain, string[] args)
        {
            // todo: command access restrictions

            if (args.Length == 0 || args.Length > 2)
            {
                brain.Write("Invalid argument count; tunnel <direction> [<destination>]\r\n");
                return;
            }

            

            using (var s = Program.SF.OpenSession())
            {
                var phy = Entity.GetFirstComponent<Physical>(s, brain.EntityID);
                var loc = phy.Location;
                var dir = DirectionFromName(args[0]);
                var x = ExitForDirection(s, loc, dir);

                if (x != null)
                {
                    brain.Write("Exit already exists in that direction; manual creation is required.\r\n");
                    return;
                }
                using (var tx = s.BeginTransaction())
                {
                    Entity destination = null;

                    if (args.Length == 2)
                    {
                        Guid destId;
                        if (Guid.TryParse(args[1], out destId))
                        {
                            var dloc = s.Get<Location>(destId);
                            if (dloc != null)
                            {
                                destination = dloc.Entity;
                            }
                        }
                    }

                    if (destination == null)
                    {
                        var newloc = Location.CreateLocation(s, "An empty room", "Empty Room");
                        destination = newloc.Entity;
                    }

                    x = new Exit
                    {
                        Entity = loc.Entity,
                        Destination = destination,
                        Dir = dir,
                    };

                    var x2 = new Exit
                    {
                        Entity = destination,
                        Destination = loc.Entity,
                        Dir = InvertDirection(dir)
                    };

                    s.Save(x);
                    s.Save(x2);
                    tx.Commit();
                }
                useExit(s, phy, x);
                Player.DoLook(brain, new string[] { });
            }
        }
        #endregion
    }
}
