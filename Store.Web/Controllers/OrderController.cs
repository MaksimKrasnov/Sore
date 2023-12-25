using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Store.Contractors;
using Store.Memory;
using Store.Messages;
using Store.Web.App;
using Store.Web.Contractors;
using Store.Web.Models;
using System.Reflection;
using System.Text.RegularExpressions;


namespace Store.Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly IEnumerable<IDeliveryService> _deliveryService;
        private readonly IEnumerable<IPaymentService> _paymentService;
        private readonly IEnumerable<IWebContractorService> _webContractorService;
        public OrderController(OrderService orderService,
                               IEnumerable<IDeliveryService> deliveryService,
                               IEnumerable<IPaymentService> paymentService,
                               IEnumerable<IWebContractorService> webContractorService)
        {
            _orderService = orderService;
            _deliveryService = deliveryService;
            _paymentService = paymentService;
            _webContractorService = webContractorService;

        }
        public IActionResult Index()
        {
            if (_orderService.TryGetModel(out OrderModel model))
                return View(model);
            return View("Empty");
        }
        [HttpPost]

        public IActionResult AddItem(int bookId, int count = 1)
        {
            _orderService.AddBook(bookId, count);

            return RedirectToAction("Index", "Book", new { id = bookId });
        }
        [HttpPost]
        public IActionResult UpdateItem(int bookId, int count)
        {
            var model = _orderService.UpdateBook(bookId, count);

            return View("Index", model);
        }

        [HttpPost]

        public IActionResult RemoveItem(int bookId)
        {
            var model = _orderService.RemoveBook(bookId);
            return View("Index", model);

        }




        [HttpPost]
        public IActionResult SendConfirmation(int id, string cellPhone)
        {
            var model = _orderService.SendConfirmation(cellPhone);
            return View("Confirmation", model);
        }

        

        [HttpPost]
        public IActionResult ConfirmCellPhone(string cellPhone, int confirmationCode)
        {
            var model = _orderService.ConfirmCellPhone(cellPhone, confirmationCode);
            if (model.Errors.Count > 0)
                return View("Confirmation", model);
            var deliveryMethods = _deliveryService.ToDictionary(service => service.Name,
                                                               service => service.Title);
            return View("DeliveryMethod", deliveryMethods);
        }


        [HttpPost]
        public IActionResult StartDelivery(string serviceName)
        {
            var deliveryService = _deliveryService.Single(service => service.Name == serviceName);
            var order = _orderService.GetOrder();
            var form = deliveryService.FirstForm(order);
            var webContractorService = _webContractorService.SingleOrDefault(service => service.Name == serviceName);
            if (webContractorService == null)
                return View("DeliveryStep", form);
            var returnUri = GetReturnUri(nameof(NextDelivery));
            var redirectUri = webContractorService.StartSession(form.Parameters, returnUri);
            return Redirect(redirectUri.ToString());
        }
        private Uri GetReturnUri(string action)
        {
            var builder = new UriBuilder(Request.Scheme, Request.Host.Host)
            {
                Path = Url.Action(action),
                Query = null,
            };
            if (Request.Host.Port != null)
                builder.Port = Request.Host.Port.Value;
            return builder.Uri;
        }
        [HttpPost]
        public IActionResult NextDelivery(string serviceName, int step, Dictionary<string, string> values)
        {
            var deliveryService = _deliveryService.Single(service => service.Name == serviceName);
            var form = deliveryService.NextForm(step, values);
            if (!form.isFinal)
                return View("DeliveryStep", form);
            var delivery = deliveryService.GetDelivery(form);
            _orderService.SetDelivery(delivery);
            var paymentMethods = _paymentService.ToDictionary(service => service.Name,
                                                              service => service.Title);
            return View("PaymentMethod", paymentMethods);
        }
        [HttpPost]
        public IActionResult StartPayment(string serviceName)
        {
            var paymentService = _paymentService.Single(service => service.Name == serviceName);
            var order = _orderService.GetOrder();
            var form = paymentService.FirstForm(order);
            var webContractorService = _webContractorService.SingleOrDefault(service => service.Name == serviceName);
            if (webContractorService == null)
                return View("PaymentStep", form);
            var returnUri = GetReturnUri(nameof(NextPayment));
            var redirectUri = webContractorService.StartSession(form.Parameters, returnUri);
            return Redirect(redirectUri.ToString());
        }

        [HttpPost]
        public IActionResult NextPayment(string serviceName, int step, Dictionary<string, string> values)
        {
            var paymentService = _paymentService.Single(service => service.Name == serviceName);
            var form = paymentService.NextForm(step, values);
            if (!form.isFinal)
                return View("PaymentStep", form);
            var payment = paymentService.GetPayment(form);
            var model = _orderService.SetPayment(payment);
            return View("Finish", model);
        }
    }
}