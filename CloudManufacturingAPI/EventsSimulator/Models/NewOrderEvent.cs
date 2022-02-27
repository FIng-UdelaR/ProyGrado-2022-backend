using CloudManufacturingSharedLibrary.Models;
using System.Collections.Generic;
using static CloudManufacturingSharedLibrary.Constants;

namespace EventsSimulator.Models
{
    public class NewOrderEvent : CMfgSystemEvent
    {
        public List<OrderItem> Items { get; set; }
        public NewOrderEvent() { }
        public NewOrderEvent(List<OrderItem> items)
        {
            Type = EVENT_TYPE.NEW_ORDER;
            Items = items;
        }
    }
}
