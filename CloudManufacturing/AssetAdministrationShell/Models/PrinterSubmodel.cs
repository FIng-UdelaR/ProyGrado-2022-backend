using AssetAdministrationShellProject.Models;
using BaSyx.Models.Core.AssetAdministrationShell.Generics;
using BaSyx.Models.Core.AssetAdministrationShell.Generics.SubmodelElementTypes;
using BaSyx.Models.Core.AssetAdministrationShell.Identification;
using BaSyx.Models.Core.AssetAdministrationShell.Implementations;
using BaSyx.Models.Core.AssetAdministrationShell.Implementations.SubmodelElementTypes;
using BaSyx.Models.Core.AssetAdministrationShell.References;
using BaSyx.Models.Core.Common;
using BaSyx.Models.Extensions;
using BaSyx.Utils.ResultHandling;
using CloudManufacturingSharedLibrary;
using CloudManufacturingSharedLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AssetAdministrationShellProject.Utils.LoggerConstants;
using static CloudManufacturingSharedLibrary.Constants;


namespace ProjectAAS.Models
{
    /// <summary>
    /// Extension of BaSyx's Submodel class with 3D Printer specific attributes and operations
    /// </summary>
    public class PrinterSubmodel : Submodel
    {
        private MACHINE_STATUS Status { get; set; }
        private MATERIAL SupportedMaterial { get; set; }
        private List<SIZE> SupportedSizes { get; set; }
        private List<QUALITY> SupportedQualities { get; set; }
        private List<Workload> Workload { get; set; }
        private GeoCoordinate MachineLocation { get; set; }
        private DateTime MaintenanceEnd { get; set; }
        private Workload CurrentWork { get; set; }
        private Task Work;

        /// <summary>
        /// Creates a new instance of a 3D Printer Submodel
        /// </summary>
        /// <param name="subModelIdShort"></param>
        /// <param name="supportedMaterial"></param>
        /// <param name="supportedSizes"></param>
        /// <param name="supportedQualities"></param>
        /// <param name="initialStatus"></param>
        public PrinterSubmodel(string subModelIdShort,
            MATERIAL supportedMaterial,
            List<SIZE> supportedSizes,
            List<QUALITY> supportedQualities,
            GeoCoordinate machineLocation,
            MACHINE_STATUS initialStatus = MACHINE_STATUS.AVAILABLE)
        {
            //Machine properties
            Logger.WriteDebug(LogHelpers.GenerateLogString(DateTime.UtcNow, subModelIdShort, LOGG_EVENT_TYPE.MACHINE_CREATED));
            Status = initialStatus;
            SupportedMaterial = supportedMaterial;
            SupportedSizes = supportedSizes ?? new List<SIZE>();
            SupportedQualities = supportedQualities ?? new List<QUALITY>();
            Workload = new List<Workload>();
            MachineLocation = machineLocation;

            Work = DoWork();

            //BaSyx properties
            IdShort = subModelIdShort;
            Identification = new Identifier(Guid.NewGuid().ToString(), KeyType.Custom);
            SubmodelElements = new ElementContainer<ISubmodelElement>()
            {
                new Property<MATERIAL>()
                {
                    IdShort = "MachineSupportedMaterial",
                    Set = (prop, val) => SupportedMaterial = val,
                    Get = prop => { return SupportedMaterial; }
                },
                new Property<MACHINE_STATUS>()
                {
                    IdShort = "MachineStatus",
                    Set = (prop, val) => Status = val,
                    Get = prop => { return Status; }
                },
                new Property<string>()
                {
                    IdShort = "MachineSupportedSizes",
                    Set = (prop, val) => SupportedSizes = val.Split(",").Select(x=>(SIZE) int.Parse(x.Trim())).ToList(),
                    Get = prop => { return string.Join(",",SupportedSizes); }
                },
                new Property<string>()
                {
                    IdShort = "MachineSupportedQualities",
                    Set = (prop, val) => SupportedQualities = val.Split(",").Select(x=>(QUALITY) int.Parse(x.Trim())).ToList(),
                    Get = prop => { return string.Join(",",SupportedQualities); }
                },
                new Property<string>()
                {
                    IdShort = "MachineLocation",
                    Set = (prop, val) => MachineLocation = JsonConvert.DeserializeObject<GeoCoordinate>(val),
                    Get = prop => { return JsonConvert.SerializeObject(MachineLocation); }
                },

                //Set Machine Status
                new Operation()
                {
                    IdShort = "SetStatus",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Sets the machine status")
                    },
                    InputVariables = new OperationVariableSet()
                    {
                        new Property<MACHINE_STATUS>()
                        {
                            IdShort = "NewStatus",
                            Description = new LangStringSet()
                            {
                                new LangString("EN", "New Status (number between 0 and 3)")
                            }
                        },
                    },
                    OutputVariables = new OperationVariableSet()
                    {
                        new Property<string>()
                        {
                            IdShort = "NewMachineStatus"
                        },
                    },
                    OnMethodCalled = (op, inArgs, outArgs) =>
                    {
                        MACHINE_STATUS newStatus = inArgs.Get("NewStatus") != null
                                                            ? inArgs.Get("NewStatus").Cast<IProperty>().ToObject<MACHINE_STATUS>()
                                                            : Status;
                        Status = newStatus;
                        Logger.WriteDebug(LogHelpers.GenerateLogString(DateTime.UtcNow, subModelIdShort, LOGG_EVENT_TYPE.MACHINE_STATUS_CHANGED, newStatus.ToString()));
                        outArgs.Add(new Property<string>() { IdShort = "NewMachineStatus", Value = newStatus.ToString() });
                        return new OperationResult(true);
                    }
                },

                //Get Workload
                new Operation()
                {
                    IdShort = "GetWorkLoad",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Gets the current machine workload")
                    },
                    OutputVariables = new OperationVariableSet()
                    {
                        new Property<string>()
                        {
                            IdShort = "MachineWorkload"
                        },
                    },
                    OnMethodCalled = (op, inArgs, outArgs) =>
                    {
                        var serializedWorkload = JsonConvert.SerializeObject(Workload);
                        outArgs.Add(new Property<string>() { IdShort = "MachineWorkload", Value = serializedWorkload });
                        return new OperationResult(true);
                    }
                },

                //Add Workload
                new Operation()
                {
                    IdShort = "AddWorkLoad",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Adds WorkLoad to this machine")
                    },
                    InputVariables = new OperationVariableSet()
                    {
                        new Property<List<Workload>>()
                        {
                            IdShort = "NewWorkLoad",
                            Description = new LangStringSet()
                            {
                                new LangString("EN", "List of Workload to add")
                            }
                        },
                    },
                    OutputVariables = new OperationVariableSet()
                    {
                        new Property<string>()
                        {
                            IdShort = "OperationResult"
                        },
                    },
                    OnMethodCalled = (op, inArgs, outArgs) => AddWorkloadImplementation(op, inArgs, outArgs)
                },

                //Remove Workload
                new Operation()
                {
                    IdShort = "RemoveWorkLoad",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Removes WorkLoad from this machine")
                    },
                    InputVariables = new OperationVariableSet()
                    {
                        new Property<List<string>>()
                        {
                            IdShort = "OrderIdsToRemove",
                            Description = new LangStringSet()
                            {
                                new LangString("EN", "List of Workload to add")
                            }
                        },
                    },
                    OutputVariables = new OperationVariableSet()
                    {
                        new Property<string>()
                        {
                            IdShort = "OperationResult"
                        },
                    },
                    OnMethodCalled = (op, inArgs, outArgs) => RemoveWorkloadImplementation(op, inArgs, outArgs)
                },

                new Operation()
                {
                    IdShort = "RemoveAllWorkLoad",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Removes all WorkLoad from this machine")
                    },
                    OutputVariables = new OperationVariableSet()
                    {
                        new Property<string>()
                        {
                            IdShort = "OperationResult"
                        },
                    },
                    OnMethodCalled = (op, inArgs, outArgs) => RemoveAllWorkloadImplementation(op, inArgs, outArgs)
                },

                //Remove and return not started workload
                new Operation()
                {
                    IdShort = "RemoveAndReturnNotStartedWorkLoad",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Removes not started WorkLoad from this machine")
                    },
                    OutputVariables = new OperationVariableSet()
                    {
                        new Property<string>()
                        {
                            IdShort = "OperationResult"
                        },
                    },
                    OnMethodCalled = (op, inArgs, outArgs) => RemoveAndReturnNotStartedWorkloadImplementation(op, inArgs, outArgs)
                },

                //Break machine
                new Operation()
                {
                    IdShort = "BreakMachine",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Break the machin for a period of time")
                    },
                    InputVariables = new OperationVariableSet()
                    {
                        new Property<int>()
                        {
                            IdShort = "BreakTime",
                            Description = new LangStringSet()
                            {
                                new LangString("EN", "Time to be break")
                            }
                        },
                    },
                    OutputVariables = new OperationVariableSet()
                    {
                        new Property<string>()
                        {
                            IdShort = "OperationResult"
                        },
                    },
                    OnMethodCalled = (op, inArgs, outArgs) => BreakMachineImplementation(op, inArgs, outArgs)
                },

                new Operation()
                {
                    IdShort = "GetEstimatedCompletionDate",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Returns the estimated completion date")
                    },
                    OutputVariables = new OperationVariableSet()
                    {
                        new Property<string>()
                        {
                            IdShort = "OperationResult"
                        },
                    },
                    OnMethodCalled = (op, inArgs, outArgs) =>{
                        outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = (Workload.Any(wl=>wl.EstimatedCompletionDate > DateTime.UtcNow)
                                            ? Workload.Max(x => x.EstimatedCompletionDate).ToString("yyyy-MM-dd HH:mm:ss")
                                            : "") });
                        return new OperationResult(true);
                    }
                },

                //Change 
                new Operation()
                {
                    IdShort = "TestOperationEndpoint",
                    Description = new LangStringSet()
                    {
                        new LangString("EN", "Empty Test Operation Endpoint")
                    },
                },
            };
        }

        #region IMPLEMENTATIONS
        private OperationResult AddWorkloadImplementation(IOperation op,
            IOperationVariableSet inArgs,
            IOperationVariableSet outArgs)
        {
            List<Workload> newWorkLoad = inArgs.Get("NewWorkLoad") != null
                                        ? inArgs.Get("NewWorkLoad").Cast<IProperty>().ToObject<List<Workload>>()
                                        : new List<Workload>();

            if (Status == MACHINE_STATUS.OFFLINE || Status == MACHINE_STATUS.NEEDS_MAINTENANCE)
            {
                outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = "Machine Not Available" });
                return new OperationResult(false);
            }

            if (!newWorkLoad.Any())
            {
                outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = "Invalid Empty Workload" });
                return new OperationResult(false);
            }

            if (newWorkLoad.Any(x => x.Material != SupportedMaterial))
            {
                outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = "Material Not Supported" });
                return new OperationResult(false);
            }

            var result = DateTime.MaxValue.ToString("yyyy-MM-dd HH:mm:ss");
            var currentUtcTime = DateTime.UtcNow;
            foreach (var work in newWorkLoad)
            {
                var estimatedStartDate = Workload.Any(wl => wl.EstimatedCompletionDate > DateTime.UtcNow)
                                            ? Workload.Max(x => x.EstimatedCompletionDate)
                                            : DateTime.UtcNow.AddSeconds(5);
                work.EstimatedCompletionDate = Helpers.EstimateCompletionDate(
                    (int)work.Material,
                    (int)work.Size,
                    (int)work.Quality,
                    estimatedStartDate);
                Workload.Add(work);
                Logger.WriteDebug(LogHelpers.GenerateLogString(currentUtcTime, IdShort, LOGG_EVENT_TYPE.NEW_WORK_ADDED, "", $"{work.OrderId}-{work.ItemId}"));
                result = work.EstimatedCompletionDate.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" New work for machine {IdShort}. Estimated completion date: {result}");
            }
            StartNextWorkIfNoneInProgress(currentUtcTime); //If there's no current work, start a new one.
            outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = result });
            return new OperationResult(true);
        }

        private OperationResult RemoveWorkloadImplementation(IOperation op,
            IOperationVariableSet inArgs,
            IOperationVariableSet outArgs)
        {
            List<string> orderIdsToRemove = inArgs.Get("OrderIdsToRemove") != null
                                        ? inArgs.Get("OrderIdsToRemove").Cast<IProperty>().ToObject<List<string>>()
                                        : new List<string>();

            if (!orderIdsToRemove.Any())
            {
                outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = "Invalid Empty OrderIds List" });
                return new OperationResult(false);
            }
            //TODO: whenever a workload is removed, recalculate the estimatedCompletionTime for the other works
            var result = Workload.RemoveAll(w => orderIdsToRemove.Contains(w.OrderId));
            outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = $"{result} workloads removed" });
            return new OperationResult(true);
        }

        private OperationResult RemoveAllWorkloadImplementation(IOperation op,
            IOperationVariableSet inArgs,
            IOperationVariableSet outArgs)
        {
            var removedWorkLoad = Workload.ToList();
            int removedWorkloadCount = Workload.RemoveAll(wl => removedWorkLoad.Any(wtr => wtr.OrderId == wl.OrderId && wtr.ItemId == wl.ItemId));
            if (removedWorkloadCount > 0)
            {
                foreach (var workload in removedWorkLoad)
                {
                    Logger.WriteDebug(LogHelpers.GenerateLogString(DateTime.UtcNow, IdShort, LOGG_EVENT_TYPE.WORK_REMOVED, $"{workload.OrderId}-{workload.ItemId}"));
                    if (CurrentWork != null && CurrentWork.OrderId == workload.OrderId && CurrentWork.ItemId == workload.ItemId)
                        CurrentWork = null;
                }
            }
            outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = $"{removedWorkloadCount} workloads removed" });
            return new OperationResult(true);
        }

        private Task<OperationResult> RemoveAndReturnNotStartedWorkloadImplementation(IOperation op, IOperationVariableSet inArgs, IOperationVariableSet outArgs)
        {
            var removed = Workload.Where(wl => wl.EstimatedCompletionDate > DateTime.UtcNow);
            if (removed.Any())
            {
                removed = removed.Skip(1); //The first work is currently being done.
                Workload.RemoveRange(1, Workload.Count - 1); //Remove all but the first workload
            }
            outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = JsonConvert.SerializeObject(removed.ToList()) });
            return new OperationResult(true);
        }

        private Task<OperationResult> BreakMachineImplementation(IOperation op, IOperationVariableSet inArgs, IOperationVariableSet outArgs)
        {
            Status = MACHINE_STATUS.OFFLINE;
            Logger.WriteDebug(LogHelpers.GenerateLogString(DateTime.UtcNow, IdShort, LOGG_EVENT_TYPE.MACHINE_STATUS_CHANGED, Status.ToString()));
            int timeUnits = inArgs.Get("BreakTime") != null
                                       ? inArgs.Get("BreakTime").Cast<IProperty>().ToObject<int>()
                                       : 0;
            int milliseconds = timeUnits * DELAY_BETWEEN_SIMULATION_TIME_UNITS_IN_MILLISECONDS;
            MaintenanceEnd = DateTime.UtcNow.AddMilliseconds(milliseconds);
            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Breaking machine {IdShort} for {timeUnits} time units (until {MaintenanceEnd:yyyy-MM-dd HH:mm:ss})");
            outArgs.Add(new Property<string>() { IdShort = "OperationResult", Value = MaintenanceEnd.ToString("yyyy-MM-dd HH:mm:ss") });
            return new OperationResult(true);
        }

        private async Task DoWork()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        var currentUtcTime = DateTime.UtcNow;
                        if (currentUtcTime >= MaintenanceEnd && (Status == MACHINE_STATUS.NEEDS_MAINTENANCE || Status == MACHINE_STATUS.OFFLINE))
                        {
                            Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Machine {IdShort} is available again");
                            Status = MACHINE_STATUS.AVAILABLE;
                            Logger.WriteDebug(LogHelpers.GenerateLogString(currentUtcTime, IdShort, LOGG_EVENT_TYPE.MACHINE_STATUS_CHANGED, Status.ToString()));
                        }
                        if (Status == MACHINE_STATUS.WORKING || Status == MACHINE_STATUS.AVAILABLE)
                        {
                            var workloadToRemove = Workload.Where(wl => wl.EstimatedCompletionDate <= currentUtcTime).ToList();
                            int completedWork = Workload.RemoveAll(wl => workloadToRemove.Any(wtr => wtr.OrderId == wl.OrderId && wtr.ItemId == wl.ItemId));
                            if (completedWork > 0)
                            {
                                Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" {completedWork} work(s) completed on machine {IdShort}");
                                foreach (var workload in workloadToRemove)
                                {
                                    Logger.WriteDebug(LogHelpers.GenerateLogString(currentUtcTime, IdShort, LOGG_EVENT_TYPE.WORK_FINISHED, $"{workload.OrderId}-{workload.ItemId}"));
                                    if (CurrentWork != null && CurrentWork.OrderId == workload.OrderId && CurrentWork.ItemId == workload.ItemId)
                                        CurrentWork = null;
                                }
                            }
                            StartNextWorkIfNoneInProgress(currentUtcTime);
                        }
                        else if (Status == MACHINE_STATUS.OFFLINE && Workload.Any()) //The machine is offline, the workload cannot be done
                        {
                            foreach (var work in Workload)
                            {
                                work.EstimatedCompletionDate = work.EstimatedCompletionDate.AddMilliseconds(BASYX_LOOP_MACHINE_DO_WORK_IN_MILLISECONDS);
                            }
                        }
                    }
                    catch (Exception ex) 
                    {
                        Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + $" Exception in DoWork for machine {IdShort}: {ex}");
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(BASYX_LOOP_MACHINE_DO_WORK_IN_MILLISECONDS));
                }
            }
            catch (Exception) { }
        }

        private void StartNextWorkIfNoneInProgress(DateTime? currentUtcTime = null)
        {
            if (!currentUtcTime.HasValue) currentUtcTime = DateTime.UtcNow;
            if (CurrentWork == null && Workload.Any()) //If the machine has finished the last work order and there's something pending, start working on it!
            {
                CurrentWork = Workload.First();
                Logger.WriteDebug(LogHelpers.GenerateLogString(currentUtcTime.Value, IdShort, LOGG_EVENT_TYPE.NEW_WORK_STARTED, $"{CurrentWork.OrderId}-{CurrentWork.ItemId}"));
            }
        }
        #endregion IMPLEMENTATIONS
    }
}
