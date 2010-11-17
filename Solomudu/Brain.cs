using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Solomudu
{
    abstract public class Brain
    {
        public Guid EntityID { get; set; }


        protected Connection conn;
        public Brain(Connection c, Guid entity)
        {
            conn = c;
            EntityID = entity;
        }

        public virtual void OnLine(string line)
        {
            conn.Write("Not implemented.\r\n");
        }

        public virtual string Prompt()
        {
            return "";
        }

        public virtual void Write(string fmt, params object[] args)
        {
            conn.Write(fmt, args);
        }

    }
}
