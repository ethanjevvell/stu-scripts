using System;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public partial class STUFlightController
        {
            public class StopOnADime : ManeuverTemplate
            {
                public override string Name => "Stop On A Dime";

                private double oneTickAcceleration;
                private STUFlightController FC;

                public StopOnADime(STUFlightController thisFlightController)
                {
                    oneTickAcceleration = 0;
                    FC = thisFlightController;
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