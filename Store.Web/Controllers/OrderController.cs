using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Store.Contractors;
using Store.Memory;
using Store.Messages;
using Store.Web.Contractors;
using Store.Web.Models;
using System.Text.RegularExpressions;


namespace Store.Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly IBookRepository _bookRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly INotificationService _notificationService;
        private readonly IEnumerable<IDeliveryService> _deliveryServices;
        private readonly IEnumerable<IPaymentService> _paymentServices;
        private readonly IEnumerable<IWebContractorService> _webContractorServices;
        public OrderController(IBookRepository bookRepository,
                               IOrderRepository orderRepository,
                               INotificationService notificationService,
                               IEnumerable<IDeliveryService> deliveryServices,
                               IEnumerable<IPaymentService> paymentServices,
                               IEnumerable<IWebContractorService> webContractorServices)   
        {
            _bookRepository = bookRepository;
            _orderRepository= orderRepository;
            _notificationService= notificationService;
            _deliveryServices = deliveryServices;
            _paymentServices=paymentServices;
            _webContractorServices = webContractorServices;

        }
        public IActionResult Index()
        {
            if(HttpContext.Session.TryGetCart(out Cart cart)) {
                var order = _orderRepository.GetById(cart.OrderId);
                OrderModel model= Map(order);
                return View(model);
            }
            return View("Empty");
        }
        [HttpPost]

        public IActionResult AddItem(int bookId, int count=1) {
            (Order order, Cart cart) = GetOrCreateOrderAndCart();
            var book = _bookRepository.GetById(bookId);
            order.AddOrUpdateItem(book, count);

            SaVeOrderAndCart(order, cart);
            return RedirectToAction("Index", "Book", new {id= bookId });
        }
        [HttpPost]
        public IActionResult UpdateItem(int bookId, int count)
        {
            (Order order, Cart cart) = GetOrCreateOrderAndCart();

            order.GetItem(bookId).Count=count;

            SaVeOrderAndCart(order, cart);
            return RedirectToAction("Index", "Order");
        }

        [HttpPost]

        public IActionResult RemoveItem(int bookId)
        {
            (Order order, Cart cart) = GetOrCreateOrderAndCart();
            order.RemoveItem(bookId);

            SaVeOrderAndCart(order, cart);

            return RedirectToAction("Index", "Order");

        }
        private OrderModel Map(Order order)
        {
            var bookIds = order.Items.Select(item => item.BookId);
            var books = _bookRepository.GetAllByIds(bookIds);
            var itemModels = from item in order.Items
                             join book in books on item.BookId equals book.Id
                             
                             select new OrderItemModel
                             {
                                 BookId = book.Id,
                                 Title = book.Title,
                                 Author = book.Author,
                                 Price = item.Price,
                                 Count = item.Count
                             };
            return new OrderModel
            {
                Id = order.Id,
                Items = itemModels.ToArray(),
                TotalCount = order.TotalCount,
                TotalPrice = order.TotalPrice
            };
        }
        private void SaVeOrderAndCart(Order order, Cart cart)
        {
            _orderRepository.Update(order);
            cart.TotalCount = order.TotalCount;
            cart.TotalPrice = order.TotalPrice;
            HttpContext.Session.Set(cart);
        }

        private (Order order, Cart cart) GetOrCreateOrderAndCart()

        {
            Order order;
            if (HttpContext.Session.TryGetCart(out Cart cart))
            {
                order = _orderRepository.GetById(cart.OrderId);
            }
            else
            {
                order = _orderRepository.Create();
                cart = new Cart(order.Id);
            }
            return (order, cart);
        }
        [HttpPost]
        public IActionResult SendConfirmationCode(int id, string cellPhone)
        {
            var order = _orderRepository.GetById(id);
            var model = Map(order);

            if (!IsValidCellPhone(cellPhone))
            {
                model.Errors["cellPhone"] = "Номер телефона не соответствует формату +79876543210";
                return View("Index", model);
            }

            int code = 1111; // random.Next(1000, 10000)
            HttpContext.Session.SetInt32(cellPhone, code);
            _notificationService.SendConfirmationCode(cellPhone, code);

            return View("Confirmation",
                        new ConfirmationModel
                        {
                            OrderId = id,
                            CellPhone = cellPhone
                        });
        }

        private bool IsValidCellPhone(string cellPhone)
        {
            if (cellPhone == null)
                return false;

            cellPhone = cellPhone.Replace(" ", "")
                                 .Replace("-", "");

            return Regex.IsMatch(cellPhone, @"^\+?\d{11}$");
        }

        [HttpPost]
        public IActionResult Confirmate(int id, string cellPhone, int code)
        {
            int? storedCode = HttpContext.Session.GetInt32(cellPhone);
            if (storedCode == null)
            {
                return View("Confirmation",
                            new ConfirmationModel
                            {
                                OrderId = id,
                                CellPhone = cellPhone,
                                Errors = new Dictionary<string, string>
                                {
                                    { "code", "Пустой код, повторите отправку" }
                                },
                            }); ;
            }

            if (storedCode != code)
            {
                return View("Confirmation",
                            new ConfirmationModel
                            {
                                OrderId = id,
                                CellPhone = cellPhone,
                                Errors = new Dictionary<string, string>
                                {
                                    { "code", "Отличается от отправленного" }
                                },
                            });
            }
            var order = _orderRepository.GetById(id);
            order.CellPhone = cellPhone;
            _orderRepository.Update(order);
            HttpContext.Session.Remove(cellPhone);

            var model = new DeliveryModel
            {
                OrderId = id,
                Methods = _deliveryServices.ToDictionary(service => service.UniqueCode,
                                                      service => service.Title)
            };

            
            return View("DeliveryMethod",model);

        }
		[HttpPost]
		public IActionResult StartDelivery(int id, string uniqueCode)
		{
			var deliveryService = _deliveryServices.Single(service => service.UniqueCode == uniqueCode);
			var order = _orderRepository.GetById(id);

			var form = deliveryService.CreateForm(order);

			return View("DeliveryStep", form);
		}
        [HttpPost]
        public IActionResult NextDelivery(int id, string uniqueCode,int step, Dictionary<string, string> values)
        {
			var deliveryService = _deliveryServices.Single(service => service.UniqueCode == uniqueCode);
            var form = deliveryService.MoveNextForm(id, step, values);
            if (form.isFinal)
            {
                var order = _orderRepository.GetById(id);
                order.Delivery = deliveryService.GetDelivery(form);
                _orderRepository.Update(order);
                var model = new DeliveryModel
                {
                    OrderId = id,
                    Methods = _paymentServices.ToDictionary(service => service.UniqueCode,
                                                            service => service.Title)
                };
                return View("PaymentMethod",model);
            }
            return View("DeliveryStep", form);
		}
        [HttpPost]
        public IActionResult StartPayment(int id, string uniqueCode)
        {
            var paymentService = _paymentServices.Single(service => service.UniqueCode == uniqueCode);
            var order = _orderRepository.GetById(id);

            var form = paymentService.CreateForm(order);

            var webContractorService = _webContractorServices.SingleOrDefault(service => service.UniqueCode == uniqueCode);
            if (webContractorService != null)
                return Redirect(webContractorService.GetUri);

            return View("PaymentStep", form);
        }

        [HttpPost]
        public IActionResult NextPayment(int id, string uniqueCode, int step, Dictionary<string, string> values)
        {
            var paymentService = _paymentServices.Single(service => service.UniqueCode == uniqueCode);

            var form = paymentService.MoveNextForm(id, step, values);

            if (form.isFinal)
            {
                var order = _orderRepository.GetById(id);
                order.Payment = paymentService.GetPayment(form);
                _orderRepository.Update(order);

                return View("Finish");
            }

            return View("PaymentStep", form);
        }

        public IActionResult Finish()
        {
            HttpContext.Session.RemoveCart();

            return View();
        }
    }
}
