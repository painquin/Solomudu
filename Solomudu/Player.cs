using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Criterion;
using NHibernate;


namespace Solomudu
{
    public class Player : Brain
    {


        public Player(Connection c, Guid entity)
            : base(c, entity)
        {

        }

        public override void OnLine(string line)
        {
            if (line == "")
            {
                Write("\r\n");
                return;
            }

            string[] split = line.Split(' ');
            // todo quote matching

            var cd = Command.BestMatch(split[0]);
            if (cd != null)
            {
                cd(this, split.Skip(1).ToArray());
            }
            else
            {
                conn.Write("huh?\r\n");
            }
        }

        public override string Prompt()
        {
            return "\r\n> ";
        }

        [Command("look")]
        public static void DoLook(Brain brain, string[] args)
        {
            using (var session = Program.SF.OpenSession())
            {
                var physical = Entity.GetComponents<Components.Physical>(session, brain.EntityID).FirstOrDefault();
                if (physical == null) return;

                var objects = session.CreateCriteria<Components.Physical>()
                    .Add(Expression.Eq("Location", physical.Location))
                    .List<Components.Physical>();

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("[{0}]\r\n{1}\r\n{2}\r\n",
                    physical.Location.Id,
                    physical.Location.Name,
                    physical.Location.Description
                    );

                foreach (var o in objects)
                {
                    if (o != physical)
                        sb.AppendFormat("{0}\r\n", o.Name);
                }

                foreach (var i in physical
                    .Location
                    .Entity
                    .GetFirstComponent<Components.Inventory>(session)
                    .GetContents(session))
                {
                    sb.AppendLine(i.ShortName);
                }


                sb.Append("\r\n");
                brain.Write(sb.ToString());
            }
        }

        [Command("say")]
        public static void DoSay(Brain brain, string[] args)
        {
            using (var session = Program.SF.OpenSession())
            {
                var physical = Entity.GetComponents<Components.Physical>(session, brain.EntityID).FirstOrDefault();
                if (physical == null) return;

                var objects = session.CreateCriteria<Components.Listen>()
                    .Add(Expression.Eq("Location", physical.Location))
                    .List<Components.Listen>();

                string msg = String.Format("{0} says '{1}'\r\n", physical.Name, String.Join(" ", args));
                using (var tx = session.BeginTransaction())
                {
                    foreach (var o in objects)
                    {
                        Event.AddEvent(session, o.Entity, msg);
                    }
                    tx.Commit();
                }
            }
        }

        [Command("peek")]
        public static void DoPeek(Brain brain, string[] args)
        {
            if (args.Length != 1) {
                brain.Write("peek <target>\r\n");
                return;
            }

            using (var s = Program.SF.OpenSession())
            {
                var phy = Entity.GetFirstComponent<Components.Physical>(s, brain.EntityID);

                if (args[0] == "self")
                {

                }
                else
                {
                    var res = s.CreateCriteria<Components.Physical>()
                        .Add(Expression.Eq("Location", phy.Location))
                        .Add(Expression.InsensitiveLike("Name", "%" + args[0] + "%"))
                        .List<Components.Physical>();

                    if (res.Count == 0)
                    {
                        brain.Write("No {0} found.\r\n", args[0]);
                        return;
                    }
                    StringBuilder sb = new StringBuilder();
                    foreach (var target in res)
                    {
                        sb.AppendFormat("[{0}] {1}\r\n", target.Entity.Id, target.Entity.HumanName);
                        foreach (var com in target.Entity.GetAllComponents(s))
                        {
                            sb.AppendFormat("{0}\r\n", com.Peek());
                        }
                        
                    }
                    sb.AppendLine();

                    brain.Write(sb.ToString());
                }
            }

        }

        
    }
}
