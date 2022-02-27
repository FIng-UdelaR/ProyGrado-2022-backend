using AssetAdministrationShellProject.Models;
using AssetAdministrationShellProject.Utils;
using BaSyx.AAS.Server.Http;
using BaSyx.API.AssetAdministrationShell.Extensions;
using BaSyx.API.Components;
using BaSyx.Models.Connectivity;
using BaSyx.Models.Core.AssetAdministrationShell.Enums;
using BaSyx.Models.Core.AssetAdministrationShell.Identification;
using BaSyx.Models.Core.AssetAdministrationShell.Implementations;
using BaSyx.Models.Core.AssetAdministrationShell.References;
using BaSyx.Models.Core.Common;
using BaSyx.Registry.Client.Http;
using BaSyx.Registry.ReferenceImpl.FileBased;
using BaSyx.Registry.Server.Http;
using BaSyx.Submodel.Server.Http;
using BaSyx.Utils.Settings.Sections;
using BaSyx.Utils.Settings.Types;
using CloudManufacturingSharedLibrary;
using ProjectAAS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static AssetAdministrationShellProject.Utils.LoggerConstants;
using static CloudManufacturingSharedLibrary.Constants;

namespace ProjectAAS
{
    public class Program
    {
        #region PROPERTIES
        static RegistryHttpClient registryClient;
        static AssetAdministrationShellRepositoryServiceProvider repositoryService;
        static MultiAssetAdministrationShellHttpServer multiServer;
        static bool FirstIteration;
        static int CurrentPortNumber = 5000; //All the Machine Port Numbers must be geq than this
        static CloudManufacturingDBAccess.DBAccess DBAccess;
        #endregion PROPERTIES

        #region AUXILIAR METHODS
        private static void LoadRegistry()
        {
            ServerSettings registrySettings = ServerSettings.CreateSettings();
            registrySettings.ServerConfig.Hosting = new HostingConfiguration()
            {
                Urls = new List<string>()
                {
                    "http://localhost:4999"
                }
            };

            RegistryHttpServer registryServer = new RegistryHttpServer(registrySettings);
            FileBasedRegistry fileBasedRegistry = new FileBasedRegistry();
            registryServer.SetRegistryProvider(fileBasedRegistry);
            registryServer.RunAsync();
        }

        private static void CreateAAS(
            MATERIAL supportedMaterial,
            List<SIZE> supportedSizes,
            List<QUALITY> supportedQualities,
            GeoCoordinate machineLocation,
            int? portNumber = null,
            string machineName = "")
        {
            int i = repositoryService.AssetAdministrationShells.Count();
            machineName = string.IsNullOrWhiteSpace(machineName) ? "Machine_" + i : machineName.Replace(" ", "_");
            AssetAdministrationShell aas = new AssetAdministrationShell()
            {
                IdShort = machineName,
                Identification = new Identifier($"http://basyx.de/shells/Machine/" + i, KeyType.IRI),
                Description = new LangStringSet()
                    {
                       new LangString("de-DE", i + ". VWS"),
                       new LangString("en-US", i + ". AAS")
                    },
                Administration = new AdministrativeInformation()
                {
                    Version = "1.0",
                    Revision = "120"
                },
                Asset = new Asset()
                {
                    IdShort = "Asset_" + i,
                    Identification = new Identifier($"http://basyx.de/assets/Asset/" + i, KeyType.IRI),
                    Kind = AssetKind.Instance,
                    Description = new LangStringSet()
                        {
                              new LangString("de-DE", i + ". Asset"),
                              new LangString("en-US", i + ". Asset")
                        },
                }
            };

            //Create the submodel with its properties and operations
            Submodel machineSubmodel = new PrinterSubmodel(
                         machineName,
                         supportedMaterial,
                         supportedSizes,
                         supportedQualities,
                         machineLocation
                      );

            aas.Submodels.Add(machineSubmodel);

            if (!portNumber.HasValue) portNumber = CurrentPortNumber;

            ServerSettings submodelServerSettings = ServerSettings.CreateSettings();
            submodelServerSettings.ServerConfig.Hosting.ContentPath = "Content";
            submodelServerSettings.ServerConfig.Hosting.Urls.Add($"http://localhost:{portNumber.Value}");

            SubmodelHttpServer submodelServer = new SubmodelHttpServer(submodelServerSettings);
            ISubmodelServiceProvider submodelServiceProvider = machineSubmodel.CreateServiceProvider();
            submodelServer.SetServiceProvider(submodelServiceProvider);
            submodelServiceProvider.UseAutoEndpointRegistration(submodelServerSettings.ServerConfig);

            submodelServer.RunAsync();

            var aasServiceProvider = aas.CreateServiceProvider(true);
            repositoryService.RegisterAssetAdministrationShellServiceProvider(aas.IdShort, aasServiceProvider);
            CurrentPortNumber = portNumber.Value + 1;
        }

        private static void RegisterAAS()
        {
            //foreach (var aasDescriptor in repositoryService.RetrieveAssetAdministrationShells().Entity)
            //{
            //    registryClient.DeleteAssetAdministrationShell(aasDescriptor.Identification.Id);
            //}
            //return;
            List<HttpEndpoint> endpoints = multiServer.Settings.ServerConfig.Hosting.Urls
                                            .ConvertAll(c => new HttpEndpoint(c.Replace("+", "127.0.0.1")));
            repositoryService.UseDefaultEndpointRegistration(endpoints);

            multiServer.SetServiceProvider(repositoryService);

            //if (!FirstIteration)
            //{
            //    foreach (var aasDescriptor in repositoryService.ServiceDescriptor.AssetAdministrationShellDescriptors)
            //    {
            //        registryClient.DeleteAssetAdministrationShell(aasDescriptor.Identification.Id);
            //    }
            //}

            multiServer.ApplicationStopping = () =>
            {
                foreach (var aasDescriptor in repositoryService.ServiceDescriptor.AssetAdministrationShellDescriptors)
                {
                    registryClient.DeleteAssetAdministrationShell(aasDescriptor.Identification.Id);
                    Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Deleted AssetAdministrationShell for {aasDescriptor.IdShort}");
                }
            };

            if (FirstIteration) multiServer.RunAsync();

            foreach (var aasDescriptor in repositoryService.ServiceDescriptor.AssetAdministrationShellDescriptors) //TODO: check this list
            {
                registryClient.CreateAssetAdministrationShell(aasDescriptor);
                Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Created AssetAdministrationShell for {aasDescriptor.IdShort}");
            }
            FirstIteration = false;
        }
        #endregion AUXILIAR METHODS

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Initialize();
            //Menu();
            _ = LookForNewMachinesAsync(); //Check if there's a new machine to create
            _ = CheckApplicationShouldStop(); //Check if we need to stop current instance of BaSyx server (simulations)
            Console.ReadLine(); //Leave the console opened
        }

        private static void Initialize()
        {
            Console.WriteLine("Initializing system...");
            Logger.InitializeDebug();
            Logger.WriteDebug(LogHelpers.GenerateLogString(DateTime.UtcNow, "", LOGG_EVENT_TYPE.INFORMATION, "", "-Starting Application-"));

            repositoryService = new AssetAdministrationShellRepositoryServiceProvider();
            registryClient = new RegistryHttpClient();
            FirstIteration = true;

            ServerSettings aasRepositorySettings = ServerSettings.CreateSettings();
            aasRepositorySettings.ServerConfig.Hosting.ContentPath = "Content";
            aasRepositorySettings.ServerConfig.Hosting.Urls.Add("http://+:5999");

            multiServer = new MultiAssetAdministrationShellHttpServer(aasRepositorySettings);
            //var shells = registryClient.RetrieveAssetAdministrationShells();

            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " Initializing DBAccess...");
            DBAccess = CloudManufacturingDBAccess.DBAccess.GetInstance();
            DBAccess.Initialize("Data Source=localhost; Initial Catalog=CloudManufacturing; Integrated Security = True");

            #region LOAD FROM DB
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " Loading memory from database...");
            CreateAASFromDBList(DBAccess.GetMachines());
            //RegisterAAS();
            LoadRegistry();
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " Initialization finished successfully...");
            #endregion LOAD FROM DB
        }

        private static void CreateAASFromDBList(IEnumerable<CloudManufacturingDBAccess.Models.MachineDBO> machineList)
        {
            if (machineList == null || !machineList.Any()) return;

            foreach (var machine in machineList.OrderBy(cm => cm.PortNumber))
            {
                try
                {
                    CreateAAS(
                        (MATERIAL)machine.SupportedMaterial,
                        machine.SupportedSizes.Select(x => (SIZE)x).ToList(),
                        machine.SupportedQualities.Select(x => (QUALITY)x).ToList(),
                        new GeoCoordinate(machine.Location),
                        portNumber: machine.PortNumber,
                        machineName: machine.Name
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Exception trying to create a new AAS for port {machine.PortNumber}: {ex}");
                }
            }
        }

        private static async Task LookForNewMachinesAsync()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        var newMachines = DBAccess.GetMachines().Where(m => m.PortNumber >= CurrentPortNumber).ToList();
                        if (newMachines.Any())
                        {
                            CreateAASFromDBList(newMachines);
                            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" {newMachines.Count()} new machines were detected");
                        }
                    }
                    catch (Exception ex)
                    {
                        //The database might have been dropped because we're about to start a new simulation
                        Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Exception in LookForNewMachines loop: {ex}");
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(BASYX_LOOP_CHECK_NEW_MACHINES_IN_MILLISECONDS));
                }
            }
            catch (Exception) { }
        }

        private static async Task CheckApplicationShouldStop()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        if (File.Exists(SHOULD_APPLICATION_STOP_FILE_PATH))
                        {
                            var text = File.ReadAllText(SHOULD_APPLICATION_STOP_FILE_PATH);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                if (text == END_CURRENT_SIMULATIONS_TEXT)
                                {
                                    File.WriteAllText(SHOULD_APPLICATION_STOP_FILE_PATH, string.Empty);
                                    Logger.WriteDebug(LogHelpers.GenerateLogString(DateTime.UtcNow, "", LOGG_EVENT_TYPE.INFORMATION, "", "-Stopping Application-"));
                                    var fileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                                    $"dotnet {fileName}".Shell();
                                    Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Stopping this application...");
                                    Environment.Exit(0);
                                }
                                else if (text == END_ALL_SIMULATIONS_TEXT)
                                {
                                    File.WriteAllText(SHOULD_APPLICATION_STOP_FILE_PATH, string.Empty);
                                    Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Stopping this application...");
                                    Environment.Exit(0);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Exception in CheckApplicationShouldStop loop: {ex}");
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(BASYX_LOOP_SHOULD_STOP_APPLICATION_IN_MILLISECONDS));
                }
            }
            catch (Exception) { }
        }

        #region MENU
        private static void ShowMenu()
        {
            Console.WriteLine(" ");
            Console.WriteLine("==========================");
            Console.WriteLine("=          MENU          =");
            Console.WriteLine("==========================");
            Console.WriteLine(" ");
            Console.WriteLine("1. Create New Machine");
            Console.WriteLine("2. Create Machine From JSON");
            Console.WriteLine("3. Create Test Machine");
            Console.WriteLine("9. Show Menu");
            Console.WriteLine("0. Stop Server And Exit Program");
            Console.WriteLine(" ");
        }

        private static void Menu()
        {
            ShowMenu();
            while (true)
            {
                Console.WriteLine(" ");
                Console.Write("> ");
                var command = Console.ReadLine();
                switch (command)
                {
                    case "0":
                        return; //Stop Server and Exit Program
                    case "1":
                        CreateMachineMenu();
                        break;
                    case "2":
                        CreateMachineFromJsonMenu();
                        break;
                    case "3":
                        CreateTestMachineMenu();
                        break;
                    case "9":
                        ShowMenu();
                        break;
                    default:
                        Console.WriteLine($"Invalid input: {command}");
                        break;
                }
            }
        }

        private static void CreateTestMachineMenu()
        {
            CreateAAS(
                MATERIAL.ALUMINA_POWDER,
                new List<SIZE>() { SIZE.MEDIUM, SIZE.LARGE },
                new List<QUALITY>() { QUALITY.HIGH },
                new GeoCoordinate() { Latitude = -34.90325, Longitude = -56.18816 },
                portNumber: CurrentPortNumber,
                machineName: CheckMachineName(""));
        }

        private static void CreateMachineFromJsonMenu()
        {
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " Enter the PrinterDBO serialized object");
            string serializedMachine = Console.ReadLine();
            try
            {
                var deserializedMachine = Newtonsoft.Json.JsonConvert.DeserializeObject<CloudManufacturingDBAccess.Models.MachineDBO>(serializedMachine);
                deserializedMachine.Name = CheckMachineName(deserializedMachine.Name);

                deserializedMachine.PortNumber = CurrentPortNumber;
                deserializedMachine.Uri = $"http://localhost:{deserializedMachine.PortNumber}";

                int id = DBAccess.InsertMachine(deserializedMachine);
                if (id <= 0)
                {
                    Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Unable to create the Machine: {JsonSerializer.Serialize(deserializedMachine)}");
                    return;
                }

                CreateAAS(
                    (MATERIAL)deserializedMachine.SupportedMaterial,
                    deserializedMachine.SupportedSizes.Select(x => (SIZE)x).ToList(),
                    deserializedMachine.SupportedQualities.Select(x => (QUALITY)x).ToList(),
                    new GeoCoordinate(deserializedMachine.Location),
                    portNumber: CurrentPortNumber,
                    machineName: deserializedMachine.Name
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" The serializedMachine {serializedMachine} raised the following exception: {ex}");
            }
        }

        private static void CreateMachineMenu()
        {
            Console.WriteLine("You will be prompted to enter the information required to create the Digital Twin");
            MATERIAL supportedMaterial = ReadSupportedMaterial();
            Console.WriteLine();
            List<SIZE> supportedSizes = ReadSupportedSizes();
            Console.WriteLine();
            List<QUALITY> supportedQualities = ReadSupportedQualities();
            Console.WriteLine();
            GeoCoordinate machineLocation = ReadMachineLocation();
            Console.WriteLine();
            string machineName = ReadMachineName();
            Console.WriteLine();

            var machineDBObject = new CloudManufacturingDBAccess.Models.MachineDBO()
            {
                Name = machineName,
                PortNumber = CurrentPortNumber,
                Location = new CloudManufacturingDBAccess.Models.Location()
                {
                    Latitude = machineLocation.Latitude,
                    Longitude = machineLocation.Longitude
                },
                SupportedMaterial = (int)supportedMaterial,
                SupportedQualities = supportedQualities.Select(x => (int)x).ToList(),
                SupportedSizes = supportedSizes.Select(x => (int)x).ToList(),
                Uri = $"http://localhost:{CurrentPortNumber}"
            };

            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Processing machine {Newtonsoft.Json.JsonConvert.SerializeObject(machineDBObject)}");
            int id = DBAccess.InsertMachine(machineDBObject);
            if (id <= 0)
            {
                Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Unable to create the Machine: {JsonSerializer.Serialize(machineDBObject)}");
                return;
            }

            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Creating Machine...");
            Console.WriteLine();
            CreateAAS(supportedMaterial, supportedSizes, supportedQualities, machineLocation, portNumber: CurrentPortNumber, machineName: machineName);
            Console.WriteLine();
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Machine successfully created");
        }

        private static MATERIAL ReadSupportedMaterial()
        {
            int materialInt = -1;
            bool done = false;
            while (!done)
            {
                Console.WriteLine($"Enter the supported material id (number between 0 and {Enum.GetValues(typeof(MATERIAL)).Cast<int>().Last()})");
                Console.Write("> ");
                string supportedMaterialStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(supportedMaterialStr) || !int.TryParse(supportedMaterialStr.Trim(), out materialInt) || !Enum.IsDefined(typeof(MATERIAL), materialInt))
                {
                    Console.WriteLine($"ERROR: '{supportedMaterialStr}' is not a valid material");
                    continue;
                }
                done = true;
            }
            Console.WriteLine($"Supported Material: {(MATERIAL)materialInt}");
            return (MATERIAL)materialInt;
        }

        private static List<SIZE> ReadSupportedSizes()
        {
            List<SIZE> result = null;
            bool done = false;
            while (!done)
            {
                Console.WriteLine($"Enter the supported sizes as comma separated values in range 0 to {Enum.GetValues(typeof(SIZE)).Cast<int>().Last()}. E.g.: 0,2");
                Console.Write("> ");
                string supportedSizesStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(supportedSizesStr))
                {
                    Console.WriteLine($"ERROR: '{supportedSizesStr}' is not a valid list of sizes");
                    continue;
                }
                List<string> stringList = supportedSizesStr.Split(',').Select(x => x.Trim()).ToList();
                if (stringList.Any(x => !int.TryParse(x, out _)) || stringList.Any(x => !Enum.IsDefined(typeof(SIZE), int.Parse(x))))
                {
                    Console.WriteLine($"ERROR: '{supportedSizesStr}' is not a valid list of sizes");
                    continue;
                }
                result = stringList.Select(x => (SIZE)int.Parse(x)).Distinct().ToList();
                done = true;
            }
            Console.WriteLine($"Supported Sizes: {string.Join(", ", result.Select(x => x))}");
            return result;
        }

        private static List<QUALITY> ReadSupportedQualities()
        {
            List<QUALITY> result = null;
            bool done = false;
            while (!done)
            {
                Console.WriteLine($"Enter the supported qualities as comma separated values in range 0 to {Enum.GetValues(typeof(QUALITY)).Cast<int>().Last()}. E.g.: 0,1");
                Console.Write("> ");
                string supportedQualityStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(supportedQualityStr))
                {
                    Console.WriteLine($"ERROR: '{supportedQualityStr}' is not a valid list of sizes");
                    continue;
                }
                List<string> stringList = supportedQualityStr.Split(',').Select(x => x.Trim()).ToList();
                if (stringList.Any(x => !int.TryParse(x, out _)) || stringList.Any(x => !Enum.IsDefined(typeof(QUALITY), int.Parse(x))))
                {
                    Console.WriteLine($"ERROR: '{supportedQualityStr}' is not a valid list of sizes");
                    continue;
                }
                result = stringList.Select(x => (QUALITY)int.Parse(x)).Distinct().ToList();
                done = true;
            }
            Console.WriteLine($"Supported Qualities: {string.Join(", ", result.Select(x => x))}");
            return result;
        }

        private static GeoCoordinate ReadMachineLocation()
        {
            GeoCoordinate result = null;
            bool done = false;
            while (!done)
            {
                Console.WriteLine($"Enter the machine location as \"latitude; longitude\" (without the quotation marks). E.g.: -34.90328; -56.18816");
                Console.Write("> ");
                string locationStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(locationStr) || locationStr.Split(';').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Count() != 2)
                {
                    Console.WriteLine($"ERROR: '{locationStr}' is not a valid coordinates set");
                    continue;
                }
                List<string> coordinatesList = locationStr.Split(';').Select(x => x.Trim().Replace(',', '.')).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                if (coordinatesList.Any(x => !double.TryParse(x, out _)))
                {
                    Console.WriteLine($"ERROR: '{locationStr}' is not a valid coordinates set");
                    continue;
                }
                result = new GeoCoordinate()
                {
                    Latitude = double.Parse(coordinatesList[0], CultureInfo.InvariantCulture),
                    Longitude = double.Parse(coordinatesList[1], CultureInfo.InvariantCulture)
                };
                done = true;
            }
            Console.WriteLine($"Machine Location: {Newtonsoft.Json.JsonConvert.SerializeObject(result)}");
            return result;
        }

        private static string ReadMachineName()
        {
            Console.WriteLine($"Enter the machine name (empty or \"Default\" for default name \"3D Printer Machine For Port {CurrentPortNumber}\")");
            Console.Write("> ");
            string result = Console.ReadLine();
            result = CheckMachineName(result);
            Console.WriteLine($"Machine Name: \"{result}\"");
            return result;
        }

        private static string CheckMachineName(string name)
        {
            string result = "";

            if (name == null) name = "";
            var nameAsList = name.ToList();
            foreach (var character in nameAsList)
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                    result += character;
                else if (character == ' ')
                    result += "_";
            }
            if (string.IsNullOrWhiteSpace(result) || result.ToLower() == "default")
                result = $"Printer Machine For Port {CurrentPortNumber}";

            if (char.IsDigit(result[0])) result = "a" + result;

            return result;
        }
        #endregion MENU
    }
}
