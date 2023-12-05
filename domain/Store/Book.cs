using System.Text.RegularExpressions;

namespace Store;

public class Book
{
    public int Id { get; }
    public string Isbn { get; }
    public string Author { get; }
    public string? Title { get; }

    public Book (int id,string icbn,string author, string title)
    {
        Title= title;
        Id = id;
        Isbn= icbn;
        Author= author;
    }
    internal static bool IsIsbn(string? data)
    {
        if (data is null)
        {
            return false;
        }
        data = data.Replace("-","")
                   .Replace(" ","")
                   .ToUpper();
        return Regex.IsMatch(data, "ISBN\\d{10}(\\d{3})?$");
    }
}
