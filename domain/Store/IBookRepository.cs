using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store
{
    public interface IBookRepository
    {
        Book[] GetAllByIds(IEnumerable<int> bookIds);
        Book[] GetAllByIsbn(string isbn);
        Book[]GetAllByTitleOrAuthor(string titleOtAuthor);
        Book GetById(int id);
    }
}
