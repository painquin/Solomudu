using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using NHibernate.Criterion;
using NHibernate;

namespace Solomudu
{
    public class Entity
    {
        public virtual Guid Id { get; protected set; }
        public virtual String HumanName { get; set; }
        public virtual Boolean Active { get; set; }

        public Entity()
        {
            Active = true;
        }

        public static IList<T> GetComponents<T>(ISession s, Guid id) where T : Component
        {
            var e = s.Get<Entity>(id);

            if (e == null) return null; // TODO log bad entity id

            return e.GetComponents<T>(s);
        }

        public static T GetFirstComponent<T>(ISession s, Guid id) where T : Component
        {
            return GetComponents<T>(s, id).FirstOrDefault();
        }

        public virtual IList<T> GetComponents<T>(ISession s) where T : Component
        {
            return s.CreateCriteria<T>().Add(Expression.Eq("Entity", this)).List<T>();
        }

        public virtual IList<T> GetComponents<T>(ICriteria c) where T : Component
        {
            return c.Add(Expression.Eq("Entity", this)).List<T>();
        }

        public virtual T GetFirstComponent<T>(ISession s) where T : Component
        {
            return GetComponents<T>(s).FirstOrDefault();
        }
        
        List<Type> componentTypes = null;

        public virtual IEnumerable<Component> GetAllComponents(ISession s)
        {
            if (componentTypes == null)
            {
                componentTypes = new List<Type>();

                foreach (var t in Assembly.GetAssembly(typeof(Component)).GetTypes())
                {
                    if (t.Namespace.EndsWith("Components"))
                    {
                        foreach (var r in s.CreateCriteria(t.FullName).Add(Expression.Eq("Entity", this)).List<Component>())
                        {
                            yield return r;
                        }
                    }
                }
            }
            
        }
    }
}
