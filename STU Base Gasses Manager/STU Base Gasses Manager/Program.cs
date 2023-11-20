

using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        IMyBlockGroup gasSubscribers;
        GasDisplayService gasDisplayService;

        List<IMyGasTank> gasTanks = new List<IMyGasTank>();
        Dictionary<string, double> gasDictionary;

        double OXYGEN_CAPACITY = 0;
        double HYDROGEN_CAPACITY = 0;

        public Program()
        {
            getTanks();

            // NOTE: Initiating subscribers in the constructor means that the script
            // will need to be recompiled every time the user wants to enroll a new
            // LCD in the display service

            gasDictionary = new Dictionary<string, double>()
            {
                { "Hydrogen", 0 },
                { "Oxygen", 0 }
            };

            gasSubscribers = GridTerminalSystem.GetBlockGroupWithName("GAS_LCDS");
            gasDisplayService = new GasDisplayService(gasDictionary, gasSubscribers, HYDROGEN_CAPACITY, OXYGEN_CAPACITY, Echo);

            // Script will run every 100 ticks

        }

        public void getTanks()
        {
            GridTerminalSystem.GetBlocksOfType(gasTanks, tank => tank.CubeGrid == Me.CubeGrid);

            // Calculate total base o2/h2 capacities
            foreach (var tank in gasTanks)
            {
                if (tank.BlockDefinition.SubtypeName.Contains("Hydrogen"))
                {
                    HYDROGEN_CAPACITY += tank.Capacity;
                }
                else if (tank.BlockDefinition.ToString().Contains("Oxygen"))
                {
                    OXYGEN_CAPACITY += tank.Capacity;
                }
            }
        }

        public void measureGas()
        {
            foreach (var tank in gasTanks)
            {
                double capacity = tank.Capacity;
                double filledRatio = tank.FilledRatio;
                double quantity = filledRatio * capacity;

                if (tank.BlockDefinition.SubtypeName.Contains("Hydrogen"))
                {
                    addGas("Hydrogen", quantity);
                }
                else if (tank.BlockDefinition.ToString().Contains("Oxygen"))
                {
                    addGas("Oxygen", quantity);
                }
            }
        }

        public void addGas(string gas, double quantity)
        {
            if (!gasDictionary.ContainsKey(gas))
            {
                gasDictionary[gas] = 0;
            }
            gasDictionary[gas] += quantity;
        }

        public void clearGasMeasurements()
        {
            gasDictionary.Clear();
        }

        public void Main()
        {
            clearGasMeasurements();
            measureGas();
            gasDisplayService.publish();
        }
    }
}
