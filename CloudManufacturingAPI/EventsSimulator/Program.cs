using EventsSimulator.Models;
using EventsSimulator.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using static CloudManufacturingSharedLibrary.Constants;

namespace EventsSimulator
{
    class Program
    {
        static List<CMfgSystemEvent> SimulatedEvents = new List<CMfgSystemEvent>();
        private static readonly Random randomizer = new Random();

        static void Main(string[] args)
        {
            Helpers.SayHello();
            string[] executeAs = new string[4] { "Create 1 simulation", $"Create and save {SIMULATION_BATCH_SIZE} simulations", "Run simulation", "Exit" };
            int selectedChoice = Helpers.MultipleChoice(false, false, 1, 7, 1, executeAs);

            while (selectedChoice != 3) //Exit
            {
                if (selectedChoice == 2)
                    RunSimulation();
                else if (selectedChoice == 0) CreateOneSimulation();
                else CreateSeveralSimulations();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"The process has finished.");
                Console.ResetColor();
                Console.WriteLine($"Press any key to continue");
                Console.ReadKey();
                Console.Clear();
                Console.WriteLine();
                Helpers.AskForOption();
                selectedChoice = Helpers.MultipleChoice(false, false, 1, 2, 1, executeAs);
            }

            Helpers.SayBye();
        }

        private static void CreateSeveralSimulations()
        {
            for (int i = 0; i < SIMULATION_BATCH_SIZE; i++)
            {
                CreateSimulationImplementation(enableLog:false);
                Helpers.SaveSimulation(ref SimulatedEvents);
                System.Threading.Thread.Sleep(1000);
            }
        }

        private static void CreateOneSimulation()
        {
            Console.WriteLine("Starting simulation...");
            CreateSimulationImplementation();
            Console.WriteLine("Ending current simulation...");

            Console.WriteLine();
            var option = Helpers.PromptUser("Do you want to save this simulation? (Y/N)");
            if (option == ConsoleKey.Y) Helpers.SaveSimulation(ref SimulatedEvents);
            else Helpers.ClearSimulation(ref SimulatedEvents);
        }

        private static void CreateSimulationImplementation(bool enableLog = true)
        {
            int probability;
            var firstEvents = Helpers.CreateFirstMachines(enableLog);
            SimulatedEvents.AddRange(firstEvents);
            for (int i = 0; i < TOTAL_SIMULATION_TIME; i++)
            {
                //Which events can happen this time?
                probability = randomizer.Next(0, 85);
                List<EVENT_TYPE> items = Helpers.PopulateList(probability, i);
                
                if(enableLog) Console.WriteLine($"Probability: {probability}, we can choose between {items.Count} items");

                //Pick one event type from all the possible events that can happen
                EVENT_TYPE chosenEvent = items[randomizer.Next(items.Count)];
                if (enableLog) Console.WriteLine($"Chosen event: {chosenEvent}");
                SimulatedEvents.Add(chosenEvent switch
                {
                    EVENT_TYPE.BREAK_MACHINE => Helpers.BreakMachine(enableLog),
                    EVENT_TYPE.CREATE_MACHINE => Helpers.CreateMachine(enableLog),
                    EVENT_TYPE.NEW_ORDER => Helpers.NewOrder(enableLog),
                    _ => Helpers.Nothing(enableLog),
                });
            }
        }

        static void RunSimulation()
        {
            Console.Clear();
            Console.WriteLine();
            var fileNames = Helpers.RetrieveSimulationNames();
            if (!fileNames.Any())
            {
                Console.WriteLine(" There are no simulations to run!");
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Please, make sure the BaSyx server and CMfg API are running!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(" Select the simulation scenario");
            List<string> options = new List<string>() { "ALL SIMULATIONS" };
            options.AddRange(fileNames);
            var selectedChoice = Helpers.MultipleChoice(false, true, 1, 5, 1, options.ToArray());

            Helpers.InitializeSimulationsControlFile();
            if (selectedChoice == 0) //Run all simulations
            {
                for (int i = 0; i < fileNames.Count(); i++)
                {
                    Console.WriteLine();
                    Console.WriteLine($" Running simulation \"{fileNames.ElementAt(i)}\"");
                    Helpers.ExecuteSimulation(fileNames.ElementAt(i));
                    Helpers.FinishSimulation(isThereAnotherSimulation: i < fileNames.Count() - 1);
                }
                Console.WriteLine();
                Console.WriteLine($" All simulations have been executed!");
            }
            else if (selectedChoice > 0) //Run specific simulation
            {
                Console.WriteLine();
                Console.WriteLine($" Running simulation \"{fileNames.ElementAt(selectedChoice - 1)}\"");
                Helpers.ExecuteSimulation(fileNames.ElementAt(selectedChoice - 1));
                Helpers.FinishSimulation(isThereAnotherSimulation: false);
            }
        }
    }
}
