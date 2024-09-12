using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBT
        {
            public abstract class CBTManeuver
            {
                public abstract bool Init();
                public abstract bool Run();
                public abstract bool Closeout();
            }
        }
    }
}
