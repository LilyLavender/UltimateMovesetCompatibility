using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomCharInfo.server.Data;
using CustomCharInfo.server.Models;
using CustomCharInfo.server.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace CustomCharInfo.server.Controllers
{

    [Route("api/logs")]
    [ApiController]
    public class ActionLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ActionLogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetActionLogDto>>> GetActionLogs(
            string? userId = null,
            int page = 1,
            int pageSize = 2147483647)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be greater than 0.");
        
            var query = _context.ActionLogs
                .Include(a => a.User)
                .Include(a => a.ItemType)
                .Include(a => a.AcceptanceState)
                .OrderByDescending(a => a.CreatedAt)
                .AsQueryable();
        
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found");
        
                var modderId = user.ModderId;
                var extraModderItemIds = new List<int>();
        
                if (modderId == null)
                {
                    extraModderItemIds = await _context.ActionLogs
                        .Where(log => log.UserId == userId && log.ItemTypeId == 2)
                        .Select(log => log.ItemId)
                        .Distinct()
                        .ToListAsync();
                }
        
                if (modderId != null)
                {
                    // Get movesetIds user is a modder for
                    var userMovesetIds = await _context.MovesetModders
                        .Where(mm => mm.ModderId == modderId)
                        .Select(mm => mm.MovesetId)
                        .ToListAsync();
                
                    // Get seriesIds from movesets
                    var seriesIdsFromMovesets = await _context.Movesets
                        .Where(m => userMovesetIds.Contains(m.MovesetId))
                        .Select(m => m.SeriesId)
                        .Distinct()
                        .ToListAsync();
                
                    query = query.Where(a =>
                        (a.ItemTypeId == 2 && a.ItemId == modderId) ||
                        (a.ItemTypeId == 1 && userMovesetIds.Contains(a.ItemId)) ||
                        (a.ItemTypeId == 3 && seriesIdsFromMovesets.Contains(a.ItemId))
                    );
                }
                else
                {
                    query = query.Where(a =>
                        a.ItemTypeId == 2 && extraModderItemIds.Contains(a.ItemId)
                    );
                }
            }
        
            var logsRaw = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        
            var modderIds = logsRaw.Where(l => l.ItemTypeId == 2).Select(l => l.ItemId).Distinct().ToList();
            var movesetIds = logsRaw.Where(l => l.ItemTypeId == 1).Select(l => l.ItemId).Distinct().ToList();
            var seriesIds = logsRaw.Where(l => l.ItemTypeId == 3).Select(l => l.ItemId).Distinct().ToList();
        
            var modders = await _context.Modders
                .Where(m => modderIds.Contains(m.ModderId))
                .Select(m => new
                {
                    m.ModderId,
                    UserName = m.User.UserName ?? m.Name
                })
                .ToDictionaryAsync(m => m.ModderId, m => m);
        
            var movesets = await _context.Movesets
                .Where(m => movesetIds.Contains(m.MovesetId))
                .Select(m => new { m.MovesetId, m.ModdedCharName })
                .ToDictionaryAsync(m => m.MovesetId, m => m);
        
            var series = await _context.Series
                .Where(s => seriesIds.Contains(s.SeriesId))
                .Select(s => new { s.SeriesId, s.SeriesName })
                .ToDictionaryAsync(s => s.SeriesId, s => s);
        
            var logs = logsRaw.Select(a => new GetActionLogDto
            {
                ActionLogId = a.ActionLogId,
                User = new UserSummaryDto
                {
                    Id = a.User.Id,
                    UserName = a.User.UserName,
                    Email = a.User.Email
                },
                ItemType = new ItemTypeDto
                {
                    ItemTypeId = a.ItemType.ItemTypeId,
                    ItemTypeName = a.ItemType.ItemTypeName
                },
                Item = a.ItemTypeId switch
                {
                    1 when movesets.TryGetValue(a.ItemId, out var moveset) => new
                    {
                        MovesetId = moveset.MovesetId,
                        ModdedCharName = moveset.ModdedCharName
                    },
                    2 when modders.TryGetValue(a.ItemId, out var modder) => new
                    {
                        ModderId = modder.ModderId,
                        Name = modder.UserName
                    },
                    3 when series.TryGetValue(a.ItemId, out var s) => new
                    {
                        SeriesId = s.SeriesId,
                        SeriesName = s.SeriesName
                    },
                    _ => null
                },
                AcceptanceState = new AcceptanceStateDto
                {
                    AcceptanceStateId = a.AcceptanceState.AcceptanceStateId,
                    AcceptanceStateName = a.AcceptanceState.AcceptanceStateName
                },
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            }).ToList();
        
            return Ok(logs);
        }

        [HttpGet("{itemTypeId}-{itemId}")]
        public async Task<ActionResult<IEnumerable<GetActionLogDto>>> GetActionLogsByItem(
            int itemTypeId,
            int itemId)
        {
            var logsRaw = await _context.ActionLogs
                .Include(a => a.User)
                .Include(a => a.ItemType)
                .Include(a => a.AcceptanceState)
                .Where(a => a.ItemTypeId == itemTypeId && a.ItemId == itemId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            if (!logsRaw.Any())
                return Ok(Array.Empty<GetActionLogDto>());

            var modderIds = logsRaw
                .Where(l => l.ItemTypeId == 2)
                .Select(l => l.ItemId)
                .Distinct()
                .ToList();

            var movesetIds = logsRaw
                .Where(l => l.ItemTypeId == 1)
                .Select(l => l.ItemId)
                .Distinct()
                .ToList();

            var seriesIds = logsRaw
                .Where(l => l.ItemTypeId == 3)
                .Select(l => l.ItemId)
                .Distinct()
                .ToList();

            var modders = await _context.Modders
                .Where(m => modderIds.Contains(m.ModderId))
                .Select(m => new
                {
                    m.ModderId,
                    UserName = m.User.UserName ?? m.Name
                })
                .ToDictionaryAsync(m => m.ModderId);

            var movesets = await _context.Movesets
                .Where(m => movesetIds.Contains(m.MovesetId))
                .Select(m => new
                {
                    m.MovesetId,
                    m.ModdedCharName
                })
                .ToDictionaryAsync(m => m.MovesetId);

            var series = await _context.Series
                .Where(s => seriesIds.Contains(s.SeriesId))
                .Select(s => new
                {
                    s.SeriesId,
                    s.SeriesName
                })
                .ToDictionaryAsync(s => s.SeriesId);

            var logs = logsRaw.Select(a => new GetActionLogDto
            {
                ActionLogId = a.ActionLogId,
                User = new UserSummaryDto
                {
                    Id = a.User.Id,
                    UserName = a.User.UserName,
                    Email = a.User.Email
                },
                ItemType = new ItemTypeDto
                {
                    ItemTypeId = a.ItemType.ItemTypeId,
                    ItemTypeName = a.ItemType.ItemTypeName
                },
                Item = a.ItemTypeId switch
                {
                    1 when movesets.TryGetValue(a.ItemId, out var moveset) => new
                    {
                        MovesetId = moveset.MovesetId,
                        ModdedCharName = moveset.ModdedCharName
                    },
                    2 when modders.TryGetValue(a.ItemId, out var modder) => new
                    {
                        ModderId = modder.ModderId,
                        Name = modder.UserName
                    },
                    3 when series.TryGetValue(a.ItemId, out var s) => new
                    {
                        SeriesId = s.SeriesId,
                        SeriesName = s.SeriesName
                    },
                    _ => null
                },
                AcceptanceState = new AcceptanceStateDto
                {
                    AcceptanceStateId = a.AcceptanceState.AcceptanceStateId,
                    AcceptanceStateName = a.AcceptanceState.AcceptanceStateName
                },
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            }).ToList();

            return Ok(logs);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ActionLogDto>> CreateActionLog(ActionLogDto dto)
        {
            // Make sure user is admin
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.UserTypeId != 3)
                return Forbid();

            // Send ActionLog
            var log = new ActionLog
            {
                UserId = dto.UserId,
                ItemTypeId = dto.ItemTypeId,
                ItemId = dto.ItemId,
                AcceptanceStateId = dto.AcceptanceStateId,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActionLogs.Add(log);

            // Associate user to modder IF NOT ALREADY
            var originalSubmitterId = await _context.ActionLogs
                    .Where(a => a.ItemTypeId == 2 && a.ItemId == dto.ItemId)
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => a.UserId)
                    .FirstOrDefaultAsync();
            var originalUser = await _context.Users.FindAsync(originalSubmitterId);
            if (
                dto.ItemTypeId == 2 // Editing modder
                && dto.AcceptanceStateId == 5 // Accepting
                && !string.IsNullOrEmpty(originalSubmitterId) // If original submitter exists
                && originalUser?.UserTypeId == 1 // Original user isn't already a modder
            ) {
                var modder = await _context.Modders.FindAsync(dto.ItemId);

                if (originalUser != null && modder != null)
                {
                    if (originalUser.ModderId == null)
                    {
                        originalUser.ModderId = dto.ItemId;
                    }

                    if (originalUser.UserTypeId != 2)
                    {
                        originalUser.UserTypeId = 2;
                    }

                    _context.Users.Update(originalUser);

                    if (string.IsNullOrEmpty(modder.UserId))
                    {
                        modder.UserId = originalSubmitterId;
                        _context.Modders.Update(modder);
                    }
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetActionLog), new { id = log.ActionLogId }, log);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GetActionLogDto>> GetActionLog(int id)
        {
            var actionLog = await _context.ActionLogs
                .Include(a => a.User)
                .Include(a => a.ItemType)
                .Include(a => a.AcceptanceState)
                .SingleOrDefaultAsync(a => a.ActionLogId == id);

            if (actionLog == null)
                return NotFound();

            object? itemDetails = null;

            if (actionLog.ItemTypeId == 1) // Moveset
            {
                var moveset = await _context.Movesets.FindAsync(actionLog.ItemId);
                if (moveset != null)
                {
                    itemDetails = new
                    {
                        MovesetId = moveset.MovesetId,
                        ModdedCharName = moveset.ModdedCharName
                    };
                }
            }
            else if (actionLog.ItemTypeId == 2) // Modder
            {
                var modder = await _context.Modders
                    .Include(m => m.User)
                    .SingleOrDefaultAsync(m => m.ModderId == actionLog.ItemId);
            
                if (modder != null)
                {
                    itemDetails = new
                    {
                        ModderId = modder.ModderId,
                        Name = modder.User?.UserName ?? modder.Name
                    };
                }
            }
            else if (actionLog.ItemTypeId == 3) // Series
            {
                var series = await _context.Series.FindAsync(actionLog.ItemId);
                if (series != null)
                {
                    itemDetails = new
                    {
                        SeriesId = series.SeriesId,
                        SeriesName = series.SeriesName
                    };
                }
            }

            var logDto = new GetActionLogDto
            {
                ActionLogId = actionLog.ActionLogId,
                User = new UserSummaryDto
                {
                    Id = actionLog.User.Id,
                    UserName = actionLog.User.UserName,
                    Email = actionLog.User.Email
                },
                ItemType = new ItemTypeDto
                {
                    ItemTypeId = actionLog.ItemType.ItemTypeId,
                    ItemTypeName = actionLog.ItemType.ItemTypeName
                },
                Item = itemDetails,
                AcceptanceState = new AcceptanceStateDto
                {
                    AcceptanceStateId = actionLog.AcceptanceState.AcceptanceStateId,
                    AcceptanceStateName = actionLog.AcceptanceState.AcceptanceStateName
                },
                Notes = actionLog.Notes,
                CreatedAt = actionLog.CreatedAt
            };

            return Ok(logDto);
        }
    }
}