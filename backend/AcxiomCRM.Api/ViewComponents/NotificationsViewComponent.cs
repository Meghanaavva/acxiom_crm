using AcxiomCRM.Api.Dtos;
using AcxiomCRM.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.ViewComponents;

// Bell icon in the header: overdue and today's follow-ups
public class NotificationsViewComponent : ViewComponent
{
    private readonly IFollowUpService _followUps;

    public NotificationsViewComponent(IFollowUpService followUps)
    {
        _followUps = followUps;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var overdue = await _followUps.GetListAsync(new FollowUpListQuery { View = "overdue", PageSize = 5 });
        var today = await _followUps.GetListAsync(new FollowUpListQuery { View = "today", PageSize = 5 });
        return View(new NotificationsViewModel
        {
            Overdue = overdue.Data,
            Today = today.Data,
            Count = overdue.Pagination.Total + today.Pagination.Total,
        });
    }
}

public class NotificationsViewModel
{
    public List<FollowUpDto> Overdue { get; set; } = new();
    public List<FollowUpDto> Today { get; set; } = new();
    public int Count { get; set; }
}
