using Azure.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TicketApplication.Data;
using TicketApplication.Models;
using TicketApplication.Models.ViewModel;

namespace TicketApplication.Hubs
{
    public class ChatHub : Hub
    {
        public readonly static List<UserViewModel> _Connections = new List<UserViewModel>();
        private readonly ApplicationDbContext _context;
        private static readonly Dictionary<string, List<string>> _userRooms = new Dictionary<string, List<string>>();

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Leave(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            if (_userRooms.ContainsKey(Context.ConnectionId))
            {
                _userRooms[Context.ConnectionId].Remove(roomName);
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User.Identity.Name;

            if (!_Connections.Any(u => u.UserName == userName))
            {
                var newUser = new UserViewModel
                {
                    UserName = userName,
                    ConnectionId = Context.ConnectionId
                };

                _Connections.Add(newUser);

                await Clients.All.SendAsync("UserConnected", userName);
            }

            if (!_userRooms.ContainsKey(Context.ConnectionId))
            {
                _userRooms[Context.ConnectionId] = new List<string>();
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinRoom(string roomName, string userName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            if (_userRooms.ContainsKey(Context.ConnectionId))
            {
                _userRooms[Context.ConnectionId].Add(roomName);
            }

            await Clients.Group(roomName).SendAsync("ReceiveNewUserNotification", userName);
        }

        public async Task SendMessageToRoom(string roomName, string messageContent)
        {
            var userName = Context.User.Identity.Name;
            var message = new MessageViewModel
            {
                Room = roomName,
                FromUserName = userName,
                Content = messageContent,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group(roomName).SendAsync("ReceiveMessage", message)
                 .ConfigureAwait(false);
        }

        public async Task NewMessage(string roomName, NewMessageViewModel message)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == message.FromUserName);

            if(user == null)
            {
                return;
            }

            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Name == roomName);

            if (room == null)
            {
                return;
            }

            var msg = new Message()
            {
                Content = Regex.Replace(message.Content, @"<.*?>", string.Empty),
                FromUserId = user.Id,
                ToRoomId = room.Id,
                Timestamp = DateTime.Now
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            var createdMessage = new MessageViewModel
            {
                Id = msg.Id,
                Content = msg.Content,
                Timestamp = msg.Timestamp,
                FromUserName = user.Email,
                FromFullName = user.Name,
                Room = room.Name,
            };

            await Clients.Group(roomName).SendAsync("ReceiveMessage", createdMessage)
                 .ConfigureAwait(false);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var user = _Connections.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);

            if (user != null)
            {
                _Connections.Remove(user);

                await Clients.All.SendAsync("UserDisconnected", user.UserName);
            }

            if (_userRooms.ContainsKey(Context.ConnectionId))
            {
                foreach (var room in _userRooms[Context.ConnectionId])
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
                }

                _userRooms.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
