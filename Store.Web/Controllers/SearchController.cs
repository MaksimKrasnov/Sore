using Microsoft.AspNetCore.Mvc;

namespace Store.Web.Controllers
{
    public class SearchController: Controller
    {
        private readonly BookService _bookService;
        public SearchController(BookService bookService)
        {
            this._bookService = bookService;
        }
        public ActionResult Index(string query) {
        var books = _bookService.GetAllByQuery(query);
            return View(books);
        }
    }
}
