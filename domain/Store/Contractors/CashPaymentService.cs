
namespace Store.Contractors
{
    public class CashPaymentService : IPaymentService
    {
        public string UniqueCode => "Cash";

        public string Title => "Оплата наличными";

        public Form CreateForm(Order order)
        {
            return new Form(UniqueCode, order.Id, 1, false, new Field[0]);
        }

        public OrderPayment GetPayment(Form form)
        {
            if(form.UniqueCode!=UniqueCode || !form.isFinal)
            {
                throw new InvalidOperationException("Invalid payment form");
            }
            return new OrderPayment(UniqueCode, "Оплата наличными", new Dictionary<string, string>());
        }

        public Form MoveNextForm(int orderId, int step, IReadOnlyDictionary<string, string> data)
        {
            if (step != 1)
            {
                throw new InvalidOperationException("Invalid cash step");
            }
            return new Form(UniqueCode,orderId,2,true,new Field[0]);
        }
    }
}