namespace Forma.Application.Entities.Websites;

public class Website
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Domain { get; set; }
    public string Description { get; set; } = string.Empty;
    public WebsiteStatus Status { get; set; } = WebsiteStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum WebsiteStatus
{
    Draft,
    Live,
}
