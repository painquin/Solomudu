using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Automapping;
using NHibernate.Criterion;

namespace Solomudu
{
    class Program
    {
        static ISessionFactory CreateSessionFactory(bool create)
        {
            return Fluently.Configure()
                .Database(MySQLConfiguration.Standard
                    .ConnectionString("Server=192.168.1.110; Database=solomudu; uid=solomudu; pwd=esmud;")
                )
                .Mappings(m =>
                    m.AutoMappings.Add(AutoMap.AssemblyOf<Program>()
                        .Where(t =>
                            t.Namespace == "Solomudu.Components" ||
                            t == typeof(Entity) ||
                            t == typeof(Account) ||
                            t == typeof(Event)
                        ))
                )
                .ExposeConfiguration(c => new NHibernate.Tool.hbm2ddl.SchemaExport(c).Create(false, create))
                .BuildSessionFactory();
        }

        public static ISessionFactory SF;

        public static bool Running = true;
        public static Components.Location startingRoom;

        static void Main(string[] args)
        {
#if DEBUG2
            bool initialize = false;
#else
            bool initialize = true;
#endif
            int port = 52883;
            if (args.Length > 0) int.TryParse(args[0], out port);


            SF = CreateSessionFactory(initialize);
            
            using (var session = SF.OpenSession())
            {
                if (initialize)
                {
                    using (var tx = session.BeginTransaction())
                    {
                        startingRoom = Components.Location
                            .CreateLocation(session, "The Void", "You float in the void.");
                        
                        

                        tx.Commit();
                    }
                }
                else
                {
                    startingRoom = session.CreateCriteria<Components.Location>().List<Components.Location>().FirstOrDefault();
                }
            }

            Connection.BeginHosting(port);
            Command.InitializeCommands();

            Console.WriteLine("Ready on port {0}.", Connection.Port);

            while (Running)
            {

                Connection.UpdateNetwork(500);


                // todo: encapsulate event update
                using (var s = Program.SF.OpenSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        foreach (var p in Connection.GetEntityIds())
                        {
                            var e = s.Get<Entity>(p.Key);

                            var evs = s.CreateCriteria<Event>().Add(Expression.Eq("Target", e)).List<Event>();
                            foreach (var ev in evs)
                            {
                                p.Value.Write(ev.Text);
                                s.Delete(ev);
                            }
                        }

                        tx.Commit();
                    }
                }


            }

        }
    }
}
