namespace Store;

public class Book
{
    public int Id { get; set; }
    public string? Title { get;}

    public Book (int id,string title)
    {
        Title= title;
        Id = id;
    }

}
