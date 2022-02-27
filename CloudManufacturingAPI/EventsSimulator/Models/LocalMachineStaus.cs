namespace EventsSimulator.Models
{
    public class LocalMachineStaus
    {
        public int BrokenUntilTimeUnit { get; set; } //If the machine gets broken for 2 time units, this property should display current time unit + 2
        public string Url { get; set; }
    }
}
