using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Solomudu.Components
{
    // a physical object
    public class Physical : Component
    {
        public virtual String Name { get; set; } // for referencing
        public virtual Location Location { get; set; } // for being places
    }
}
