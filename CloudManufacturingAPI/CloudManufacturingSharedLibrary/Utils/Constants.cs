namespace CloudManufacturingSharedLibrary
{
    public class Constants
    {
        #region STRING CONSTANTS
        public const string OUTPUT_START = "DECLARE @outputResult TABLE(outputId INT)";
        public const string OUTPUT_START_STRING = "DECLARE @outputResult TABLE(outputId NVARCHAR(MAX))";
        public const string OUTPUT_END = "SELECT outputId FROM @outputResult";
        public const string COLLATE = "COLLATE Latin1_General_CI_AI";
        #endregion STRING CONSTANTS

        #region INT CONSTANTS
        public const int MAX_MACHINES_TAKEN_INTO_ACCOUNT_SCHEDULER = 100;
        #endregion INT CONSTANTS

        #region ENUMS
        public enum SQLType
        {
            BIGINT,//-2^63 (-9,223,372,036,854,775,808) to 2^63-1 (9,223,372,036,854,775,807)	8 Bytes
            INT, //-2^31 (-2,147,483,648) to 2^31-1 (2,147,483,647)	4 Bytes
            SMALLINT, //-2^15 (-32,768) to 2^15-1 (32,767)	2 Bytes
            TINYINT, //0 to 255	1 Byte
            FLOAT, //- 1.79E+308 to -2.23E-308, 0 and 2.23E-308 to 1.79E+308	Depends on the value of n
            REAL, //- 3.40E + 38 to -1.18E - 38, 0 and 1.18E - 38 to 3.40E + 38	4 Bytes

            BIT,

            NCHAR,
            VARCHAR, //Uses less disk space
            NVARCHAR, //Allows more type of characters
            TEXT,

            VARBINARY,
            DATETIME2,
        }

        public enum SQLIndexType
        {
            CLUSTERED, //Only one per table. Stored in the table or view
            NONCLUSTERED //Stored separatedly with a pointer to the data
        }

        public enum MACHINE_STATUS
        {
            OFFLINE,
            AVAILABLE,
            WORKING,
            NEEDS_MAINTENANCE
        }

        public enum MATERIAL
        {
            PLA_FILAMENT,
            ABS_FILAMENT,
            PETG_FILAMENT,
            POLYAMIDE_POWDER,
            ALUMINA_POWDER,
            RESIN
        }

        public enum SIZE
        {
            SMALL,
            MEDIUM,
            LARGE,
            EXTRA_LARGE
        }

        public enum QUALITY
        {
            LOW,
            HIGH
        }

        public enum PRIORITY
        {
            LOW,
            REGULAR,
            HIGH
        }

        public enum SCHEDULING_METHOD
        {
            FIFO,
            LESS_WORKLOAD
        }
        #endregion ENUMS

        #region SIMULATIONS
        //Simulation's time units
        public const int TOTAL_SIMULATION_TIME = 120; //Fictional time units
        public const int DELAY_BETWEEN_SIMULATION_TIME_UNITS_IN_MILLISECONDS = 2000;
        public const int RUN_MONITORING_TIME_UNIT = 5; //Run the monitoring every 5 time units
        public const int BASYX_LOOP_CHECK_NEW_MACHINES_IN_MILLISECONDS = 1000;
        public const int BASYX_LOOP_MACHINE_DO_WORK_IN_MILLISECONDS = 1000;
        public const int BASYX_LOOP_SHOULD_STOP_APPLICATION_IN_MILLISECONDS = 1000;
        public const int DELAY_BETWEEN_CONSECUTIVE_SIMULATIONS_IN_SECONDS = 300;

        //Simulation file helpers
        public const string SHOULD_APPLICATION_STOP_FILE_PATH = "D:\\GitHub\\ProyGrado-2021\\CloudManufacturing\\AssetAdministrationShell\\bin\\Debug\\netcoreapp3.1\\stopapplication.txt";
        public const string END_ALL_SIMULATIONS_TEXT = "END";
        public const string END_CURRENT_SIMULATIONS_TEXT = "NEXT";

        //Events Threshold
        public const int CREATE_MACHINE_THRESHOLD = 10;
        public const int CREATE_MACHINE_THRESHOLD_HIGH = 30; //At the beginning of the simulation
        public const int BREAK_MACHINE_THRESHOLD = 15;
        public const int BREAK_MACHINE_THRESHOLD_HIGH = 25; //At the end of the simulation
        public const int NEW_ORDER_THRESHOLD = 100;
        public const int NOTHING_THRESHOLD = 75;

        public const int SIMULATION_BATCH_SIZE = 40;

        //Simulation events
        public enum EVENT_TYPE
        {
            BREAK_MACHINE,
            CREATE_MACHINE,
            NEW_ORDER,
            NOTHING
        }

        public const string CMFG_API_BASE_URL = "http://localhost:10000/";
        #endregion SIMULATIONS
    }
}
