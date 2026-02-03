namespace landing_page_isis.core.Models;

public class Lead
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Intent { get; set; }
    public LeadStatusEnum LeadStatus { get; set; } = LeadStatusEnum.Pending;
}
