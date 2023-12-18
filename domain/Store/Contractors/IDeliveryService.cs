using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Contractors
{
    public interface IDeliveryService
    {
         string UniqueCode { get; }
         string Title { get; }
         Form CreateForm(Order order);
         Form MoveNextForm(int orderId, int step, IReadOnlyDictionary<string, string> data);
        OrderDelivery GetDelivery(Form form);
    }
}
