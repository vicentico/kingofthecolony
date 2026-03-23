using KingOfTheColonyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KingOfTheColonyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class RankingController : ControllerBase
{
    private readonly RankingService _ranking;

    public RankingController(RankingService ranking) => _ranking = ranking;

    [HttpGet]
    public async Task<IActionResult> GetRanking([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var ranking = await _ranking.GetRankingAsync(page, pageSize);
        return Ok(ranking);
    }
}
