namespace Forma.Infrastructure.Daos;

/// <summary>A website as it is stored in the database.</summary>
public class WebsiteDao
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Domain { get; set; }
    public string Description { get; set; } = string.Empty;
    public WebsiteDaoStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum WebsiteDaoStatus
{
    Draft,
    Live,
}
