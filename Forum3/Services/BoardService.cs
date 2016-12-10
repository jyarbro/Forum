﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Forum3.Data;
using Forum3.Enums;
using Forum3.Helpers;

namespace Forum3.Services {
	public class BoardService {
		ApplicationDbContext DbContext { get; }
		IHttpContextAccessor HttpContextAccessor { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		UserService UserService { get; }

		public BoardService(
			ApplicationDbContext dbContext,
			IHttpContextAccessor httpContextAccessor,
			UserManager<DataModels.ApplicationUser> userManager,
			UserService userService
		) {
			DbContext = dbContext;
			HttpContextAccessor = httpContextAccessor;
			UserManager = userManager;
			UserService = userService;
		}

		public async Task<ViewModels.Boards.Index> GetBoardIndex() {
			var boards = await GetBoardTree();
			var onlineUsers = UserService.GetOnlineUsers();
			
			var viewModel = new ViewModels.Boards.Index {
				Birthdays = UserService.GetBirthdays().ToArray(),
				Boards = boards,
				OnlineUsers = onlineUsers
			};

			return viewModel;
		}

		public async Task Create(ViewModels.Boards.Create input, ModelStateDictionary modelState) {
			if (DbContext.Boards.Any(b => b.Name == input.Name))
				modelState.AddModelError(nameof(input.Name), "A board with that name already exists");

			DataModels.Board parentRecord = null;

			if (!string.IsNullOrEmpty(input.Parent)) {
				parentRecord = DbContext.Boards.FirstOrDefault(b => b.Name == input.Parent);

				if (parentRecord == null)
					modelState.AddModelError(nameof(input.Parent), "No parent was found with this name.");
			}

			if (!modelState.IsValid)
				return;

			var boardRecord = new DataModels.Board {
				Name = input.Name,
				InviteOnly = input.InviteOnly,
				VettedOnly = input.VettedOnly
			};

			if (parentRecord != null)
				boardRecord.ParentId = parentRecord.Id;

			if (modelState.IsValid) {
				await DbContext.Boards.AddAsync(boardRecord);
				await DbContext.SaveChangesAsync();
			}
		}

		async Task<List<ViewModels.Boards.IndexBoard>> GetBoardTree(int? targetBoard = null) {
			List<DataModels.Board> boardRecordList = null;
			List<DataModels.Message> lastMessages = null;
			List<ViewModels.Boards.IndexUser> lastMessagesBy = null;
			List<DataModels.ViewLog> boardViewLogs = null;

			var currentUser = await UserManager.GetUserAsync(HttpContextAccessor.HttpContext.User);
			var isAuthenticated = HttpContextAccessor.HttpContext.User.Identity.IsAuthenticated;
			var isAdmin = isAuthenticated && HttpContextAccessor.HttpContext.User.IsInRole("Admin");
			var isVetted = isAuthenticated && HttpContextAccessor.HttpContext.User.IsInRole("Vetted");

			if (isAuthenticated)
				boardRecordList = DbContext.Boards.ToList();
			else
				boardRecordList = DbContext.Boards.Where(b => !b.InviteOnly).ToList();

			if (DbContext != null) {
				var lastMessageIds = boardRecordList.Select(r => r.LastMessageId).ToList();
				lastMessages = DbContext.Messages.Where(r => lastMessageIds.Contains(r.Id)).ToList();

				var lastMessagesByIds = lastMessages.Select(m => m.LastReplyById);

				lastMessagesBy = DbContext.Users.Where(r => lastMessagesByIds.Contains(r.Id)).Select(r => new ViewModels.Boards.IndexUser {
					Id = r.Id,
					Name = r.DisplayName
				}).ToList();

				if (isAuthenticated)
					boardViewLogs = DbContext.ViewLogs.Where(r => r.UserId == currentUser.Id && r.TargetType == EViewLogTargetType.Board).ToList();
			}

			var boards = new List<ViewModels.Boards.IndexBoard>();

			foreach (var board in boardRecordList.Where(r => r.ParentId == null).OrderBy(r => r.DisplayOrder).ToList()) {
				if (!board.VettedOnly || isVetted) {
					boards.Add(LoadBoard(targetBoard, board, boardRecordList, lastMessages, lastMessagesBy, boardViewLogs));
				}
			}

			return boards;
		}

		ViewModels.Boards.IndexBoard LoadBoard(int? targetBoard, DataModels.Board board, List<DataModels.Board> boards, List<DataModels.Message> lastMessages = null, List<ViewModels.Boards.IndexUser> lastMessagesBy = null, List<DataModels.ViewLog> boardViewLogs = null) {
			var isAuthenticated = HttpContextAccessor.HttpContext.User.Identity.IsAuthenticated;
			var isAdmin = isAuthenticated && HttpContextAccessor.HttpContext.User.IsInRole("Admin");
			var isVetted = isAuthenticated && HttpContextAccessor.HttpContext.User.IsInRole("Vetted");

			var indexBoard = new ViewModels.Boards.IndexBoard {
				Id = board.Id,
				Name = board.Name,
				DisplayOrder = board.DisplayOrder,
				Parent = board.ParentId,
				VettedOnly = board.VettedOnly,
				InviteOnly = board.InviteOnly,
				Unread = false,
				Selected = targetBoard != null && targetBoard == board.Id,
				Children = new List<ViewModels.Boards.IndexBoard>()
			};

			if (lastMessages != null && lastMessagesBy != null) {
				var lastMessage = lastMessages.FirstOrDefault(r => r.Id == board.LastMessageId);

				if (lastMessage != null) {
					indexBoard.LastMessage = new ViewModels.Messages.MessagePreview {
						Id = lastMessage.Id,
						ShortPreview = lastMessage.ShortPreview,
						LastReplyByName = lastMessagesBy.First(r => r.Id == lastMessage.LastReplyById).Name,
						LastReplyId = lastMessage.LastReplyId,
						LastReplyPosted = lastMessage.LastReplyPosted.ToPassedTimeString()
					};

					if (boardViewLogs != null) {
						var viewLog = boardViewLogs.FirstOrDefault(r => r.TargetId == board.Id);

						if (viewLog == null || lastMessage.LastReplyPosted > viewLog.LogTime)
							indexBoard.Unread = true;
					}
				}
			}

			var boardChildren = boards.Where(r => r.ParentId == board.Id).OrderBy(r => r.DisplayOrder).ToList();

			foreach (var childRecord in boardChildren)
				if (!childRecord.VettedOnly || isVetted)
					indexBoard.Children.Add(LoadBoard(targetBoard, childRecord, boards, lastMessages, lastMessagesBy, boardViewLogs));

			return indexBoard;
		}
	}
}