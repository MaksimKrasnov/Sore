using Microsoft.AspNetCore.Mvc;
using Store.Web.App;

namespace Store.Web.Controllers
{
    public class BookController : Controller
    {
        private readonly BookService _bookService;
        public BookController(BookService bookRepository)
        {
            _bookService = bookRepository;
        }
        public IActionResult Index(int id)
        {
            var model = _bookService.GetById(id);
            return View(model);
        }
    }
}
