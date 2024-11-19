using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    partial class Program
    {
        public partial class CBT
        {
            public class MovePiston : STUStateMachine
            {
                public override string Name => "Move Piston";
                public MovePiston()
                {

                }

                public override bool Init()
                {
                    return true;
                }

                public override bool Run()
                {
                    return true;
                }

                public override bool Closeout()
                {
                    return true;
                }
            }
        }
    }
}
