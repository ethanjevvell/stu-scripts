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
            public class ExtendGangwayManeuver : STUStateMachine
            {
                public override string Name => "Extend Gangway";
                
                public ExtendGangwayManeuver()
                {
                    
                }

                public override bool Init()
                {
                    return true;
                }

                public override bool Run()
                {
                    // tell gangway state machine to extend
                    return true;
                }

                public override bool Closeout()
                {
                    // return condition should be to ask the gangway state machine if it is extended
                    return true;
                }
            }
        }
    }
}
