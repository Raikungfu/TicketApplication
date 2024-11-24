using System.Linq;
using System.Threading.Tasks;
using TicketApplication.Models;
using Microsoft.AspNetCore.Mvc;
using TicketApplication.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using TicketApplication.Hubs;
using TicketApplication.Models.ViewModel;

namespace TicketAdminChat.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Chat/Admins
        public async Task<IActionResult> Admins()
        {
            var admins = await _context.Users
                .Where(u => u.Role == "Admin")
                .ToListAsync();

            return View(admins);
        }

        public async Task<ActionResult> StartChat(string adminId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => (r.UserId == userId && r.AdminId == adminId) ||
                                           (r.UserId == adminId && r.AdminId == userId));

            if (room == null)
            {
                room = new Room
                {
                    Name = GenerateRoomName(userId, adminId),
                    UserId = userId,
                    AdminId = adminId
                };

                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ChatRoom", new { roomId = room.Id });
        }

        private string GenerateRoomName(string adminName, string userName)
        {
            var sortedUsernames = new List<string> { adminName, userName };
            sortedUsernames.Sort();
            return $"private_{sortedUsernames[0]}_{sortedUsernames[1]}";
        }

        // GET: Chat/ChatRoom/{roomId}
        public async Task<ActionResult> ChatRoom(string roomId)
        {
            var room = await _context.Rooms.Include(r => r.User).Include(r => r.Admin).FirstOrDefaultAsync(r => r.Id == roomId);

            if (room != null)
            {
                room.Messages = await _context.Messages
                    .Where(m => m.ToRoomId == roomId)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(20)
                    .Include(m => m.FromUser)
                    .Reverse()
                    .ToListAsync();

                return View(room);
            }

            return NotFound();
        }


        [HttpPost]
        public async Task<ActionResult> SendMessage(string roomId, string roomName, string messageContent)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userName = User.Identity?.Name ?? "Unknown User";

            var message = new Message
            {
                Content = messageContent,
                FromUserId = userId,
                Timestamp = DateTime.Now
            };

            var room = await _context.Rooms.FindAsync(roomId);

            if (room == null)
            {
                return Json(new { success = false, message = "Phòng không tồn tại!" });
            }

            try
            {
                room.Messages.Add(message);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Tin nhắn đã được gửi thành công!",
                        messageRes = new MessageViewModel
                        {
                            Id = message.Id,
                            Content = message.Content,
                            Timestamp = message.Timestamp,
                            FromUserName = userName,
                            Room = roomName
                        }
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Không có tin nhắn nào được gửi!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi tin nhắn!", error = ex.Message });
            }
        }

    }
}
