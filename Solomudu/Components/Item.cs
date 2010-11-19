using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Solomudu.Components
{
    public class Item : Component
    {

        public virtual String Keywords { get; set; }
        public virtual String ShortName { get; set; }
        
        public virtual Inventory Container { get; set; }

        [Command("create")]
        public static void DoCreate(Brain brain, string[] args)
        {
            if (args.Length == 0) {
                brain.Write("Create what?\r\n");
                return;
            }

            using (var s = Program.SF.OpenSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var inv = Entity.GetFirstComponent<Inventory>(s, brain.EntityID);
                    var e = new Entity
                    {
                        HumanName = args[0]
                    };
                    var item = new Item
                    {
                        Container = inv,
                        Entity = e,
                        Keywords = args[0],
                        ShortName = "a " + args[0]
                    };
                    s.Save(e);
                    s.Save(item);
                    tx.Commit();
                    brain.Write("Created {0}", item.ShortName);
                }
            }
        }
    }
}
