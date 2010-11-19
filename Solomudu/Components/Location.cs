using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Criterion;

namespace Solomudu.Components
{
    public class Location : Component
    {
        
        public virtual String Name { get; set; }
        public virtual String Description { get; set; }

        public static Location CreateLocation(ISession s, string name, string desc)
        {
            var ent = new Entity
            {
                HumanName = "Location:" + name
            };

            var loc = new Location
            {
                Name = name,
                Description = desc,
                Entity = ent
            };

            var inv = new Inventory
            {
                Entity = ent
            };

            s.Save(ent);
            s.Save(loc);
            s.Save(inv);

            return loc;

        }

        public virtual IEnumerable<T> Contents<T>(ISession s) where T : Component
        {
            return s.CreateCriteria<T>().Add(Expression.Eq("Location", this)).List<T>();
        }

        [Command("loc-edit")]
        public static void DoLocEdit(Brain brain, string[] args)
        {

            using (var s = Program.SF.OpenSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var phy = Entity.GetFirstComponent<Physical>(s, brain.EntityID);
                    var loc = phy.Location;



                    if (args.Length < 2)
                    {
                        brain.Write("Id: {2}\r\nName: {0}\r\nDescription:\r\n{1}\r\n",
                            loc.Name, loc.Description, loc.Id);
                        return;
                    }

                    switch (args[0].ToLower())
                    {
                        case "name":
                            loc.Name = args[1];
                            break;
                        case "desc": // todo: block editor
                            loc.Description = args[1];
                            break;
                    }
                    s.Update(loc);
                    tx.Commit();
                }
            }
        }
    }
}
