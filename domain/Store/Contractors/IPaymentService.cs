using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Contractors
{
    public interface IPaymentService
    {
         string Name { get; }
         string Title { get; }
         Form FirstForm(Order order);
         Form NextForm( int step, IReadOnlyDictionary<string, string> data);
         OrderPayment GetPayment(Form form);

    }
}
