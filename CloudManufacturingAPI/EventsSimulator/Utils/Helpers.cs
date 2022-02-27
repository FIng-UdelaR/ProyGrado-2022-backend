using CloudManufacturingSharedLibrary.Models;
using EventsSimulator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static CloudManufacturingSharedLibrary.Constants;

namespace EventsSimulator.Utils
{
    public static class Helpers
    {
        static readonly HttpClient httpClient = new HttpClient();
        static readonly Dictionary<int, LocalMachineStaus> machineList = new Dictionary<int, LocalMachineStaus>();

        #region UI/UX
        public static void SayHello()
        {
            Console.WriteLine();
            Console.WriteLine(" ===================================================");
            Console.WriteLine("  Welcome to the Cloud Manufacturing Simulator Tool");
            Console.WriteLine(" ===================================================");
            Console.WriteLine();
            AskForOption();
        }

        public static void AskForOption()
        {
            Console.WriteLine(" What do you want to do? (Use the arrows to choose the option)");
        }

        /// <summary>
        /// Prompts a question that the user can reply with Y/N.
        /// Checks that the user's answer is indeed Y or N.
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        public static ConsoleKey PromptUser(string question)
        {
            Console.Write($"{question} ");
            var option = Console.ReadKey();
            while (option.Key != ConsoleKey.Y && option.Key != ConsoleKey.N
                && option.Key != ConsoleKey.Enter && option.Key != ConsoleKey.Escape)
            {
                Console.WriteLine();
                Console.Write($"Invalid option \"{option.Key}\". {question} ");
                option = Console.ReadKey();
            }

            return option.Key == ConsoleKey.Y || option.Key == ConsoleKey.Enter ? ConsoleKey.Y : ConsoleKey.N;
        }

        /// <summary>
        /// Displays a final message before ending the application.
        /// </summary>
        public static void SayBye()
        {
            Console.WriteLine();
            Console.WriteLine("Bye! :)");
        }

        public static int MultipleChoice(bool clearConsole, bool canCancel, int startX, int startY, int optionsPerLine, params string[] options)
        {
            int spacingPerLine = options.Select(o => o.Length).Max() + 2;

            int currentSelection = 0;
            ConsoleKey key;
            Console.CursorVisible = false;

            do
            {
                if (clearConsole)
                    Console.Clear();

                for (int i = 0; i < options.Length; i++)
                {
                    Console.SetCursorPosition(startX + (i % optionsPerLine) * spacingPerLine, startY + i / optionsPerLine);

                    if (i == currentSelection) Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine(i == currentSelection ? "> " + options[i] : "  " + options[i]);
                    Console.ResetColor();
                }

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        if (currentSelection % optionsPerLine > 0) currentSelection--;
                        break;
                    case ConsoleKey.RightArrow:
                        if (currentSelection % optionsPerLine < optionsPerLine - 1) currentSelection++;
                        break;
                    case ConsoleKey.UpArrow:
                        if (currentSelection >= optionsPerLine) currentSelection -= optionsPerLine;
                        break;
                    case ConsoleKey.DownArrow:
                        if (currentSelection + optionsPerLine < options.Length) currentSelection += optionsPerLine;
                        break;
                    case ConsoleKey.Escape:
                        if (canCancel) return -1;
                        break;
                }
            } while (key != ConsoleKey.Enter);

            Console.CursorVisible = true;

            return currentSelection;
        }
        #endregion UI/UX

        private static readonly Random randomizer = new Random();
        private static List<CreateMachineEvent> machines = new List<CreateMachineEvent>();
        /// <summary>
        /// Return random value between two double
        /// </summary>
        /// <returns></returns>
        private static double GetRandomNumber(double minimum, double maximum)
        {
            Random random = new Random();
            return random.NextDouble() * (maximum - minimum) + minimum;
        }

        private static (double, double) GetRandomCoordinates()
        {
            double longmin = -58.5;
            double latgmin = -35.783;
            double longmax = -51.283;
            double latmax = -30.117;

            double lat = GetRandomNumber(longmin, longmax);
            double lng = GetRandomNumber(latgmin, latmax);

            return (lat, lng);
        }

        #region CLOUD MANUFACTURING EVENTS
        /// <summary>
        /// Creates a new event with random data, whose type is BREAK_MACHINE
        /// </summary>
        /// <returns></returns>
        public static CMfgSystemEvent BreakMachine(bool enableLog)
        {
            if (enableLog) Console.WriteLine("BREAK_MACHINE");
            int randomTime = randomizer.Next(4, 11);
            int machineId = randomizer.Next(1, machines.Count + 2) + 4999;
            return new BreakMachineEvent(
                machineId,
                randomTime);
        }

        /// <summary>
        /// Creates a machine for each MATERIAL that support all the qualities and sizes
        /// </summary>
        /// <returns></returns>
        public static List<CreateMachineEvent> CreateFirstMachines(bool enableLog)
        {
            if (enableLog) Console.WriteLine("CREATE_FIRST_MACHINES");
            var values = Enum.GetValues(typeof(MATERIAL));
            List<CreateMachineEvent> machineList = new List<CreateMachineEvent>();
            foreach (MATERIAL m in values)
            {
                MATERIAL supportedMaterial = m;
                List<SIZE> supportedSizes = Enum.GetValues(typeof(SIZE)).Cast<SIZE>().ToList();
                List<QUALITY> supportedQualities = Enum.GetValues(typeof(QUALITY)).Cast<QUALITY>().ToList();
                (double lat, double lng) = GetRandomCoordinates();
                CreateMachineEvent machine = new CreateMachineEvent(
                    supportedMaterial,
                    supportedSizes,
                    supportedQualities,
                    lat,
                    lng);
                machines.Add(machine);
                machineList.Add(machine);
            }

            for (int i = 0; i < 10; i++)
            {
                machineList.Add(CreateMachine(enableLog) as CreateMachineEvent);
            }
            return machineList;
        }

        /// <summary>
        /// Creates a new event with random data, whose type is CREATE_MACHINE
        /// </summary>
        /// <returns></returns>
        public static CMfgSystemEvent CreateMachine(bool enableLog)
        {
            if (enableLog) Console.WriteLine("CREATE_MACHINE");
            int material = randomizer.Next(0, 6);
            MATERIAL supportedMaterial = (MATERIAL)material;
            List<SIZE> supportedSizes = new List<SIZE>();
            List<QUALITY> supportedQualities = new List<QUALITY>();
            int sizeQuantity = randomizer.Next(1, 5);
            int qualityQuantity = randomizer.Next(1, 3);
            while (supportedSizes.Count < sizeQuantity)
            {
                int size = randomizer.Next(0, 4);
                if (!supportedSizes.Contains((SIZE)size))
                {
                    supportedSizes.Add((SIZE)size);
                }
            }
            while (supportedQualities.Count < qualityQuantity)
            {
                int quality = randomizer.Next(0, 2);
                if (!supportedQualities.Contains((QUALITY)quality))
                {
                    supportedQualities.Add((QUALITY)quality);
                }
            }
            (double lat, double lng) = GetRandomCoordinates();

            CreateMachineEvent machine = new CreateMachineEvent(
                supportedMaterial,
                supportedSizes,
                supportedQualities,
                lat,
                lng);

            machines.Add(machine);
            return machine;
        }

        /// <summary>
        /// Creates a new event with random data, whose type is NOTHING
        /// </summary>
        /// <returns></returns>
        public static CMfgSystemEvent Nothing(bool enableLog)
        {
            if (enableLog) Console.WriteLine("NOTHING");
            return new NothingEvent();
        }

        /// <summary>
        /// Creates a new event with random data, whose type is NEW_ORDER
        /// </summary>
        /// <returns></returns>
        public static CMfgSystemEvent NewOrder(bool enableLog)
        {
            if (enableLog) Console.WriteLine("NEW_ORDER");
            int ordersQuantity = randomizer.Next(1, 11);
            List<OrderItem> orderItems = new List<OrderItem>();
            while (orderItems.Count < ordersQuantity)
            {
                int material = randomizer.Next(0, 6);
                int sizeAsInt = randomizer.Next(0, 4);
                int qualityAsInt = randomizer.Next(0, 2);
                (double lat, double lng) = GetRandomCoordinates();

                OrderItem o = new OrderItem()
                {
                    Latitude = lat,
                    Longitude = lng,
                    Material = (MATERIAL)material,
                    Quality = (QUALITY)qualityAsInt,
                    Size = (SIZE)sizeAsInt
                };
                orderItems.Add(o);
            }

            return new NewOrderEvent(orderItems);
        }
        #endregion CLOUD MANUFACTURING EVENTS

        #region SIMULATIONS
        /// <summary>
        /// Given a random probability, populates a list with all the probable events according to some thresholds
        /// </summary>
        /// <param name="probability"></param>
        /// <returns></returns>
        public static List<EVENT_TYPE> PopulateList(int probability, int currentTimeSlot)
        {
            List<EVENT_TYPE> items = new List<EVENT_TYPE>();

            if ((currentTimeSlot <= TOTAL_SIMULATION_TIME / 4 && probability <= CREATE_MACHINE_THRESHOLD_HIGH) || (probability <= CREATE_MACHINE_THRESHOLD)) items.Add(EVENT_TYPE.CREATE_MACHINE);
            if ((currentTimeSlot >= (TOTAL_SIMULATION_TIME / 4) * 3 && probability <= BREAK_MACHINE_THRESHOLD_HIGH) || (probability <= BREAK_MACHINE_THRESHOLD)) items.Add(EVENT_TYPE.BREAK_MACHINE);
            if (probability <= NOTHING_THRESHOLD) items.Add(EVENT_TYPE.NOTHING);
            if (probability <= NEW_ORDER_THRESHOLD) items.Add(EVENT_TYPE.NEW_ORDER);

            return items;
        }

        /// <summary>
        /// Persists the current simulation to a file and clears the current data from memory afterwards.
        /// </summary>
        /// <param name="simulatedEvents"></param>
        public static void SaveSimulation(ref List<CMfgSystemEvent> simulatedEvents)
        {
            Console.WriteLine();
            Console.WriteLine("Saving this simulation");
            List<string> lines = new List<string>();
            for (int i = 0; i < simulatedEvents.Count; i++)
            {
                var item = simulatedEvents[i];
                string serializedItem = item.Type switch
                {
                    EVENT_TYPE.BREAK_MACHINE => JsonSerializer.Serialize(simulatedEvents[i] as BreakMachineEvent),
                    EVENT_TYPE.CREATE_MACHINE => JsonSerializer.Serialize(simulatedEvents[i] as CreateMachineEvent),
                    EVENT_TYPE.NEW_ORDER => JsonSerializer.Serialize(simulatedEvents[i] as NewOrderEvent),
                    _ => JsonSerializer.Serialize(simulatedEvents[i] as NothingEvent),
                };
                lines.Add(serializedItem);
            }
            string simulationPath = $"Simulation_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}";
            File.WriteAllLines(simulationPath, lines);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Simulation saved to {simulationPath}");
            Console.ResetColor();
            ClearSimulation(ref simulatedEvents);
        }

        public static IEnumerable<string> RetrieveSimulationNames()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory());
            int index = files.First().LastIndexOfAny(new char[2] { '/', '\\' }) + 1;
            return files.Where(f => f.Contains("Simulation_")).Select(f => f.Substring(index, f.Length - index));
        }

        /// <summary>
        /// Clears the current simulation data from memory.
        /// </summary>
        /// <param name="simulatedEvents"></param>
        public static void ClearSimulation(ref List<CMfgSystemEvent> simulatedEvents)
        {
            Console.WriteLine();
            Console.WriteLine(" Clearing this simulation");
            simulatedEvents = new List<CMfgSystemEvent>();
        }

        public static void ExecuteSimulation(string path)
        {
            if (!File.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Unable to read file {path} in the directory");
                Console.ResetColor();
                return;
            }

            var lines = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line));
            if (!lines.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"File {path} is an empty simulation.");
                Console.ResetColor();
                return;
            }

            try
            {
                int index = 0;
                foreach (var line in lines)
                {
                    index++;
                    if (index % RUN_MONITORING_TIME_UNIT == 0) //Run monitoring every RUN_MONITORING_TIME_UNIT time units!
                    {
                        RunMonitoring($"({(index * 100) / lines.Count()}%)");
                    }
                    var auxiliarObject = JsonSerializer.Deserialize<CMfgSystemEvent>(line);
                    switch (auxiliarObject.Type)
                    {
                        case EVENT_TYPE.BREAK_MACHINE: ProcessEvent(JsonSerializer.Deserialize<BreakMachineEvent>(line), index); break;
                        case EVENT_TYPE.CREATE_MACHINE: ProcessEvent(JsonSerializer.Deserialize<CreateMachineEvent>(line)); break;
                        case EVENT_TYPE.NEW_ORDER: ProcessEvent(JsonSerializer.Deserialize<NewOrderEvent>(line)); break;
                        default: break;
                    }
                    var task = Task.Run(async () => { await Task.Delay(TimeSpan.FromMilliseconds(DELAY_BETWEEN_SIMULATION_TIME_UNITS_IN_MILLISECONDS)); });
                    task.Wait();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Error reading file {path}: {ex}");
                Console.ResetColor();
            }
        }

        public static void InitializeSimulationsControlFile()
        {
            File.WriteAllText(SHOULD_APPLICATION_STOP_FILE_PATH, string.Empty);
        }

        public static void FinishSimulation(bool isThereAnotherSimulation = false)
        {
            WaitForSimulationToFinish();

            Console.WriteLine($" Cleaning database");
            var DBAccess = CloudManufacturingDBAccess.DBAccess.GetInstance();
            DBAccess.Initialize("Data Source=localhost; Initial Catalog=CloudManufacturing; Integrated Security = True");
            int deletedMachines = DBAccess.DeleteMachines();
            Console.WriteLine($" {deletedMachines} machines deleted");

            //2. Clean CMfg API's memory
            Console.WriteLine($" Cleaning CMfg API's memory...");
            ClearAPIMemory();

            Console.WriteLine(isThereAnotherSimulation ? " Restarting BaSyx system..." : " Stopping BaSyx system");

            if (isThereAnotherSimulation) //Tell BaSyx to drop current simulation and prepare for the next one
                File.WriteAllText(SHOULD_APPLICATION_STOP_FILE_PATH, END_CURRENT_SIMULATIONS_TEXT);
            else //Tell BaSyx to stop the server
                File.WriteAllText(SHOULD_APPLICATION_STOP_FILE_PATH, END_ALL_SIMULATIONS_TEXT);

            System.Threading.Thread.Sleep(5000);
        }

        private static void WaitForSimulationToFinish()
        {
            //To give BaSyx machines enough time to finish pending work
            Console.WriteLine($" Waiting for all Digital Twins to finish their pending work...");

            bool anyMachineStillWorking = true;
            while (anyMachineStillWorking)
            {
                System.Threading.Thread.Sleep(DELAY_BETWEEN_SIMULATION_TIME_UNITS_IN_MILLISECONDS);
                anyMachineStillWorking = false;
                foreach (var machine in machineList)
                {
                    anyMachineStillWorking = anyMachineStillWorking || MachineStillProcessingWorkload(machine.Value.Url);
                }
            }
            machineList.Clear();
            Console.WriteLine($" All Digital Twins have finished their pending work...");
        }
        #endregion SIMULATIONS

        #region COMMUNICATION WITH API AND DIGITAL TWINS
        private static void ProcessEvent(BreakMachineEvent simulationEvent, int currentTimeUnit)
        {
            if (machineList.ContainsKey(simulationEvent.MachineId))
            {
                var machine = machineList[simulationEvent.MachineId];
                machine.BrokenUntilTimeUnit = currentTimeUnit + simulationEvent.DownTime;
                machineList[simulationEvent.MachineId] = machine;
                var url = machine.Url + "/submodel/operations/BreakMachine";

                var objectForBody = new
                {
                    requestId = Guid.NewGuid().ToString(),
                    inputArguments = new List<object>() {
                        new {
                            modelType = new {name = "OperationVariable" },
                            value = new {
                                idShort = "BreakTime",
                                modelType = new { name = "Property"},
                                valueType = new { dataObjectType = new { name = "int" } },
                                value = simulationEvent.DownTime
                            }
                        }
                    }
                };
                Post(url, objectForBody, out _);
                RunMonitoring();
            }
        }

        private static void ProcessEvent(CreateMachineEvent simulationEvent)
        {
            var url = CMFG_API_BASE_URL + "Machine/CreateMachine";
            var objectToSend = new
            {
                machineName = "default",
                supportedMaterial = (int)simulationEvent.SupportedMaterial,
                supportedSizes = simulationEvent.SupportedSizes.Select(size => (int)size).ToArray(),
                supportedQualities = simulationEvent.SupportedQualities.Select(quality => (int)quality).ToArray(),
                latitude = simulationEvent.Latitude,
                longitude = simulationEvent.Longitude
            };
            bool success = Post(url, objectToSend, out string responseAsString);
            if (success)
            {
                var id = GetJsonPropertyAsString(JsonDocument.Parse(responseAsString).RootElement, "id");
                var port = GetJsonPropertyAsString(JsonDocument.Parse(responseAsString).RootElement, "portNumber");
                var uri = GetJsonPropertyAsString(JsonDocument.Parse(responseAsString).RootElement, "uri");

                //Add the uri to a dictionary in the simulator's memory
                machineList.Add(int.Parse(port), new LocalMachineStaus() { BrokenUntilTimeUnit = 0, Url = uri.Replace("\"", string.Empty) });
            }
        }

        private static void ProcessEvent(NewOrderEvent simulationEvent)
        {
            var url = CMFG_API_BASE_URL + "Work/AddCompoundWork";
            var objectToSend =
                simulationEvent.Items.Select(item => new
                {
                    material = (int)item.Material,
                    size = (int)item.Size,
                    quality = (int)item.Quality,
                    latitude = item.Latitude,
                    longitude = item.Longitude
                }).ToArray();
            Post(url, objectToSend, out _);
        }

        private static bool MachineStillProcessingWorkload(string url)
        {
            url += "/submodel/operations/GetWorkLoad";
            var objectToSend = new
            {
                requestId = Guid.NewGuid().ToString(),
                inputArguments = new List<object>()
            };

            bool success = Post(url, objectToSend, out string responseAsString, logInConsole: false);
            var parsedJsonIntoJObject = Newtonsoft.Json.Linq.JObject.Parse(responseAsString);
            var stringWorkload = parsedJsonIntoJObject["outputArguments"][0]["value"]["value"].ToString();
            return success && stringWorkload != "[]";
        }

        private static bool Post(string url, object bodyObject, out string responseAsString, bool logInConsole = true)
        {
            var requestBodyStr = JsonSerializer.Serialize(bodyObject);
            byte[] messageBytes = Encoding.UTF8.GetBytes(requestBodyStr);
            var content = new ByteArrayContent(messageBytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = httpClient.PostAsync(url, content).Result;

            var aux = url.Split("/").TakeLast(2).ToArray();
            string path = aux[0] + "/" + aux[1];
            responseAsString = response.Content.ReadAsStringAsync().Result;
            if (logInConsole)
            {
                if (!response.IsSuccessStatusCode) Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(
                    response.IsSuccessStatusCode
                        ? $" Successfully executed {path}"
                        : $" Error in {path}: {responseAsString}");
                Console.ResetColor();
            }

            return response.IsSuccessStatusCode;
        }

        private static void RunMonitoring(string percentage = "")
        {
            var url = CMFG_API_BASE_URL + "Test/RunMonitoring";
            var response = httpClient.GetAsync(url).Result;
            if (!response.IsSuccessStatusCode) Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(
                response.IsSuccessStatusCode
                    ? $" Monitoring Service executed successfully {percentage}"
                    : $" Error running Monitoring code: {response.Content.ReadAsStringAsync().Result}");
            Console.ResetColor();
        }

        private static void ClearAPIMemory()
        {
            var url = CMFG_API_BASE_URL + "Test/ClearSystemMemory";
            var response = httpClient.PutAsync(url, null).Result;
            if (!response.IsSuccessStatusCode) Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(
                response.IsSuccessStatusCode
                    ? $" API's memory cleaned successfully"
                    : $" Error cleaning API's memory: {response.Content.ReadAsStringAsync().Result}");
            Console.ResetColor();
        }
        #endregion COMMUNICATION WITH API AND DIGITAL TWINS

        static string GetJsonPropertyAsString(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in element.EnumerateObject())
                {
                    if (item.Value.ValueKind == JsonValueKind.Object)
                        return GetJsonPropertyAsString(item.Value, propertyName);

                    if (item.Name == propertyName)
                        return item.Value.GetRawText();
                }
            }
            return "";
        }
    }
}
