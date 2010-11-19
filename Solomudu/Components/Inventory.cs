using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Criterion;

namespace Solomudu.Components
{
    public class Inventory : Component
    {

        public virtual IEnumerable<Item> GetContents(ICriteria c)
        {
            return c.Add(Expression.Eq("Container", this)).List<Item>();
        }

        public virtual IEnumerable<Item> GetContents(ISession s)
        {
            return s.CreateCriteria<Item>().Add(Expression.Eq("Container", this)).List<Item>();
        }

        [Command("inventory")]
        public static void DoInventory(Brain brain, string[] args)
        {
            using (var s = Program.SF.OpenSession())
            {
                var inv = Entity.GetFirstComponent<Inventory>(s, brain.EntityID);

                StringBuilder sb = new StringBuilder();
                sb.Append("Inventory:\r\n");
                foreach (var item in inv.GetContents(s))
                {
                    sb.AppendFormat("  {0}\r\n", item.ShortName);
                }
                sb.AppendLine();
                brain.Write(sb.ToString());
            }
        }

        [Command("get")]
        public static void DoGet(Brain brain, string[] args)
        {

            string item_name = args[0];

            using (var s = Program.SF.OpenSession())
            {
                var e = s.Get<Entity>(brain.EntityID);

                var phy = e.GetFirstComponent<Physical>(s);
                var loc = phy.Location;

                var orig_inv = loc 
                    .Entity
                    .GetFirstComponent<Inventory>(s);

                var dest_inv = e
                    .GetFirstComponent<Inventory>(s);

                var item = orig_inv
                    .GetContents(s
                        .CreateCriteria<Item>()
                        .Add(Expression
                            .InsensitiveLike("Keywords", "%" + item_name + "%")
                        )
                    )
                    .FirstOrDefault();

                if (item == null)
                {
                    brain.Write("Get what?\r\n");
                    return;
                }

                using (var tx = s.BeginTransaction())
                {
                    item.Container = dest_inv;
                    s.Save(item);
                    foreach (var l in loc.Contents<Listen>(s))
                    {
                        if (l.Entity == phy.Entity) continue;
                        Event.AddEvent(s, l.Entity, String.Format("{0} gets {1}.\r\n", phy.Name, item.ShortName));
                    }
                    tx.Commit();
                }

                brain.Write("Got {0}.\r\n", item.ShortName);
            }
        }

        [Command("drop")]
        public static void DoDrop(Brain brain, string[] args)
        {
            string item_name = args[0];

            using (var s = Program.SF.OpenSession())
            {
                var e = s.Get<Entity>(brain.EntityID);

                var phy = e.GetFirstComponent<Physical>(s);
                var loc = phy.Location;

                var dest_inv = loc
                    .Entity
                    .GetFirstComponent<Inventory>(s);

                var orig_inv = e
                    .GetFirstComponent<Inventory>(s);

                var item = orig_inv
                    .GetContents(s
                        .CreateCriteria<Item>()
                        .Add(Expression
                            .InsensitiveLike("Keywords", "%" + item_name + "%")
                        )
                    )
                    .FirstOrDefault();

                if (item == null)
                {
                    brain.Write("Drop what?\r\n");
                    return;
                }

                using (var tx = s.BeginTransaction())
                {
                    item.Container = dest_inv;
                    s.Save(item);
                    foreach (var l in loc.Contents<Listen>(s))
                    {
                        if (l.Entity == phy.Entity) continue;
                        Event.AddEvent(s, l.Entity, String.Format("{0} drops {1}.\r\n", phy.Name, item.ShortName));
                    }
                    tx.Commit();
                }

                brain.Write("Dropped {0}.\r\n", item.ShortName);
            }
        }

        [Command("put")]
        public static void DoPut(Brain brain, string[] args)
        {
            brain.Write("dur.\r\n");
        }


    }
}
