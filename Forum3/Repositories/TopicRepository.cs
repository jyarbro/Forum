﻿using Forum3.Contexts;
using Forum3.Enums;
using Forum3.Exceptions;
using Forum3.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ItemModels = Models.ViewModels.Topics.Items;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels;

	public class TopicRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		MessageRepository MessageRepository { get; }
		SettingsRepository SettingsRepository { get; }
		SmileyRepository SmileyRepository { get; }
		UserRepository UserRepository { get; }
		IUrlHelper UrlHelper { get; }

		public TopicRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			MessageRepository messageRepository,
			SettingsRepository settingsRepository,
			SmileyRepository smileyRepository,
			UserRepository userRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			MessageRepository = messageRepository;
			SettingsRepository = settingsRepository;
			SmileyRepository = smileyRepository;
			UserRepository = userRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public List<ItemModels.Message> GetMessages(List<int> messageIds) {
			var messageQuery = from message in DbContext.Messages
							   join postedBy in UserRepository.All on message.PostedById equals postedBy.Id
							   join reply in DbContext.Messages on message.ReplyId equals reply.Id into Replies
							   from reply in Replies.DefaultIfEmpty()
							   join replyPostedBy in UserRepository.All on reply.PostedById equals replyPostedBy.Id into RepliesBy
							   from replyPostedBy in RepliesBy.DefaultIfEmpty()
							   where messageIds.Contains(message.Id)
							   orderby message.Id
							   select new ItemModels.Message {
								   Id = message.Id,
								   ParentId = message.ParentId,
								   ReplyId = message.ReplyId,
								   ReplyBody = reply == null ? string.Empty : reply.DisplayBody,
								   ReplyPreview = reply == null ? string.Empty : reply.ShortPreview,
								   ReplyPostedBy = replyPostedBy == null ? string.Empty : replyPostedBy.DisplayName,
								   Body = message.DisplayBody,
								   Cards = message.Cards,
								   OriginalBody = message.OriginalBody,
								   PostedByName = postedBy.DisplayName,
								   PostedById = message.PostedById,
								   PostedByAvatarPath = postedBy.AvatarPath,
								   TimePostedDT = message.TimePosted,
								   TimeEditedDT = message.TimeEdited,
								   RecordTime = message.TimeEdited,
								   Processed = message.Processed
							   };

			var messages = messageQuery.ToList();

			foreach (var message in messages) {
				message.TimePosted = message.TimePostedDT.ToPassedTimeString();
				message.TimeEdited = message.TimeEditedDT.ToPassedTimeString();

				message.CanEdit = UserContext.IsAdmin || (UserContext.IsAuthenticated && UserContext.ApplicationUser.Id == message.PostedById);
				message.CanDelete = UserContext.IsAdmin || (UserContext.IsAuthenticated && UserContext.ApplicationUser.Id == message.PostedById);
				message.CanReply = UserContext.IsAuthenticated;
				message.CanThought = UserContext.IsAuthenticated;

				message.ReplyForm = new ItemModels.ReplyForm {
					Id = message.Id,
				};

				var thoughtQuery = from mt in DbContext.MessageThoughts
								   join s in SmileyRepository.All on mt.SmileyId equals s.Id
								   join u in UserRepository.All on mt.UserId equals u.Id
								   where mt.MessageId == message.Id
								   select new ItemModels.MessageThought {
									   Path = s.Path,
									   Thought = s.Thought.Replace("{user}", u.DisplayName)
								   };

				message.Thoughts = thoughtQuery.ToList();
			}

			return messages;
		}

		public ServiceModels.ServiceResponse GetLatest(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Messages.Find(messageId);

			if (record is null)
				throw new HttpNotFoundException($@"No record was found with the id '{messageId}'");

			if (record.ParentId > 0)
				record = DbContext.Messages.Find(record.ParentId);

			if (!UserContext.IsAuthenticated) {
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Topics.Display), nameof(Controllers.Topics), new { id = record.LastReplyId });
				return serviceResponse;
			}

			var historyTimeLimit = SettingsRepository.HistoryTimeLimit();
			var viewLogs = DbContext.ViewLogs.Where(r => r.UserId == UserContext.ApplicationUser.Id && r.LogTime >= historyTimeLimit).ToList();
			var latestViewTime = historyTimeLimit;

			foreach (var viewLog in viewLogs) {
				switch (viewLog.TargetType) {
					case EViewLogTargetType.All:
						if (viewLog.LogTime >= latestViewTime)
							latestViewTime = viewLog.LogTime;
						break;

					case EViewLogTargetType.Message:
						if (viewLog.TargetId == record.Id && viewLog.LogTime >= latestViewTime)
							latestViewTime = viewLog.LogTime;
						break;
				}
			}

			var messageIdQuery = from message in DbContext.Messages
								 where message.Id == record.Id || message.ParentId == record.Id
								 where message.TimePosted >= latestViewTime
								 select message.Id;

			var latestMessageId = messageIdQuery.FirstOrDefault();

			if (latestMessageId == 0)
				latestMessageId = record.LastReplyId;

			if (latestMessageId == 0)
				latestMessageId = record.Id;

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Topics.Display), nameof(Controllers.Topics), new { id = latestMessageId });

			return serviceResponse;
		}

		public List<ItemModels.MessagePreview> GetPreviews(int boardId, long after, int unread) {
			var participation = new List<DataModels.Participant>();
			var viewLogs = new List<DataModels.ViewLog>();
			var historyTimeLimit = SettingsRepository.HistoryTimeLimit();

			if (UserContext.IsAuthenticated) {
				participation = DbContext.Participants.Where(r => r.UserId == UserContext.ApplicationUser.Id).ToList();
				viewLogs = DbContext.ViewLogs.Where(r => r.LogTime >= historyTimeLimit && r.UserId == UserContext.ApplicationUser.Id).ToList();
			}

			var messageIds = GetIndexIds(boardId, after, unread, historyTimeLimit, participation, viewLogs);

			var messageRecordQuery = from message in DbContext.Messages
									 where message.ParentId == 0 && messageIds.Contains(message.Id)
									 join reply in DbContext.Messages on message.LastReplyId equals reply.Id into replies
									 from reply in replies.DefaultIfEmpty()
									 join replyPostedBy in UserRepository.All on message.LastReplyById equals replyPostedBy.Id
									 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
									 from pin in pins.DefaultIfEmpty()
									 let pinned = pin != null && pin.UserId == UserContext.ApplicationUser.Id
									 orderby (pinned ? pin.Id : 0) descending, message.LastReplyPosted descending
									 select new ItemModels.MessagePreview {
										 Id = message.Id,
										 ShortPreview = message.ShortPreview,
										 LastReplyId = message.LastReplyId == 0 ? message.Id : message.LastReplyId,
										 LastReplyById = message.LastReplyById,
										 LastReplyByName = replyPostedBy.DisplayName,
										 LastReplyPostedDT = message.LastReplyPosted,
										 LastReplyPreview = reply.ShortPreview,
										 Views = message.ViewCount,
										 Replies = message.ReplyCount,
										 Pinned = pinned
									 };

			var messages = messageRecordQuery.ToList();

			var take = SettingsRepository.MessagesPerPage();

			foreach (var message in messages) {
				message.Pages = Convert.ToInt32(Math.Ceiling(1.0 * message.Replies / take));
				message.LastReplyPosted = message.LastReplyPostedDT.ToPassedTimeString();

				if (message.LastReplyPostedDT > historyTimeLimit)
					message.Unread = GetUnreadLevel(message.Id, message.LastReplyPostedDT, participation, viewLogs);
			}

			return messages;
		}

		public List<int> GetIndexIds(int boardId, long after, int unreadFilter, DateTime historyTimeLimit, List<DataModels.Participant> participation, List<DataModels.ViewLog> viewLogs) {
			var take = SettingsRepository.TopicsPerPage();

			var messageQuery = from message in DbContext.Messages
							   where message.ParentId == 0
							   select new {
								   message.Id,
								   message.ParentId,
								   message.LastReplyPosted
							   };

			if (boardId > 0) {
				messageQuery = from message in DbContext.Messages
							   join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
							   where message.ParentId == 0
							   where messageBoard.BoardId == boardId
							   select new {
								   message.Id,
								   message.ParentId,
								   message.LastReplyPosted
							   };
			}

			var afterTarget = new DateTime(after);

			if (unreadFilter > 0) {
				var timeFilter = historyTimeLimit > afterTarget ? historyTimeLimit : afterTarget;
				messageQuery = messageQuery.Where(m => m.LastReplyPosted > timeFilter);
			}
			else if (afterTarget != default(DateTime))
				messageQuery = messageQuery.Where(m => m.LastReplyPosted < afterTarget);

			var sortedMessageQuery = from message in messageQuery
									 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
									 from pin in pins.DefaultIfEmpty()
									 let pinned = pin != null && pin.UserId == UserContext.ApplicationUser.Id
									 orderby message.LastReplyPosted descending
									 orderby (pinned ? pin.Id : 0) descending
									 select new {
										 message.Id,
										 message.LastReplyPosted
									 };

			var messageBoardsQuery = from message in sortedMessageQuery
									 join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId into boards
									 from messageBoard in boards.DefaultIfEmpty()
									 select new {
										 MessageId = message.Id,
										 BoardId = messageBoard == null ? -1 : messageBoard.BoardId
									 };

			var forbiddenBoardIdsQuery = from role in DbContext.Roles
										 join board in DbContext.BoardRoles on role.Id equals board.RoleId
										 where !UserContext.Roles.Contains(role.Id)
										 select board.BoardId;

			var forbiddenBoardIds = forbiddenBoardIdsQuery.ToList();

			var messageIds = new List<int>();
			var attempts = 0;

			foreach (var message in sortedMessageQuery) {
				if (IsAccessDenied(message.Id, forbiddenBoardIds)) {
					if (attempts++ > 100)
						break;

					continue;
				}

				var unreadLevel = unreadFilter == 0 ? 0 : GetUnreadLevel(message.Id, message.LastReplyPosted, participation, viewLogs);

				if (unreadLevel < unreadFilter) {
					if (attempts++ > 100)
						break;

					continue;
				}

				messageIds.Add(message.Id);

				if (messageIds.Count == take)
					break;
			}

			return messageIds;
		}

		public bool IsAccessDenied(int messageId, List<int> forbiddenBoardIds) {
			if (UserContext.IsAdmin)
				return false;

			var messageBoards = DbContext.MessageBoards.Where(mb => mb.MessageId == messageId).Select(mb => mb.BoardId);

			return messageBoards.Any() && messageBoards.Intersect(forbiddenBoardIds).Any();
		}

		public int GetUnreadLevel(int messageId, DateTime lastReplyTime, List<DataModels.Participant> participation, List<DataModels.ViewLog> viewLogs) {
			var unread = 1;

			if (UserContext.IsAuthenticated) {
				foreach (var viewLog in viewLogs) {
					switch (viewLog.TargetType) {
						case EViewLogTargetType.All:
							if (viewLog.LogTime >= lastReplyTime)
								unread = 0;
							break;

						case EViewLogTargetType.Message:
							if (viewLog.TargetId == messageId && viewLog.LogTime >= lastReplyTime)
								unread = 0;
							break;
					}

					if (unread == 0)
						break;
				}
			}

			if (unread == 1 && participation.Any(r => r.MessageId == messageId))
				unread = 2;

			return unread;
		}

		public ServiceModels.ServiceResponse Pin(int messageId) {
			var record = DbContext.Messages.Find(messageId);

			if (record is null)
				throw new HttpNotFoundException($@"No record was found with the id '{messageId}'");

			if (record.ParentId > 0)
				messageId = record.ParentId;

			var existingRecord = DbContext.Pins.FirstOrDefault(p => p.MessageId == messageId && p.UserId == UserContext.ApplicationUser.Id);

			if (existingRecord is null) {
				var pinRecord = new DataModels.Pin {
					MessageId = messageId,
					Time = DateTime.Now,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.Pins.Add(pinRecord);
			}
			else
				DbContext.Pins.Remove(existingRecord);

			DbContext.SaveChanges();

			return new ServiceModels.ServiceResponse();
		}

		public ServiceModels.ServiceResponse Toggle(InputModels.ToggleBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var messageRecord = DbContext.Messages.Find(input.MessageId);

			if (messageRecord is null)
				throw new HttpNotFoundException($@"No message was found with the id '{input.MessageId}'");

			var messageId = input.MessageId;

			if (messageRecord.ParentId > 0)
				messageId = messageRecord.ParentId;

			if (!DbContext.Boards.Any(r => r.Id == input.BoardId))
				serviceResponse.Error(string.Empty, $@"No board was found with the id '{input.BoardId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var boardId = input.BoardId;

			var existingRecord = DbContext.MessageBoards.FirstOrDefault(p => p.MessageId == messageId && p.BoardId == boardId);

			if (existingRecord is null) {
				var messageBoardRecord = new DataModels.MessageBoard {
					MessageId = messageId,
					BoardId = boardId,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.MessageBoards.Add(messageBoardRecord);
			}
			else
				DbContext.MessageBoards.Remove(existingRecord);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		public void MarkRead(int topicId, DateTime latestMessageTime) {
			var viewLogs = DbContext.ViewLogs.Where(v =>
				v.UserId == UserContext.ApplicationUser.Id
				&& (v.TargetType == EViewLogTargetType.All || (v.TargetType == EViewLogTargetType.Message && v.TargetId == topicId))
			).ToList();

			DateTime latestTime;

			if (viewLogs.Any()) {
				var latestViewLogTime = viewLogs.Max(r => r.LogTime);
				latestTime = latestViewLogTime > latestMessageTime ? latestViewLogTime : latestMessageTime;
			}
			else
				latestTime = latestMessageTime;

			var historyTimeLimit = SettingsRepository.HistoryTimeLimit();

			var existingLogs = viewLogs.Where(r => r.TargetType == EViewLogTargetType.Message);

			foreach (var viewLog in existingLogs)
				DbContext.ViewLogs.Remove(viewLog);

			DbContext.ViewLogs.Add(new DataModels.ViewLog {
				LogTime = latestTime,
				TargetId = topicId,
				TargetType = EViewLogTargetType.Message,
				UserId = UserContext.ApplicationUser.Id
			});

			//try {
			DbContext.SaveChanges();
			// TODO - uncomment if this problem occurs again.
			// see - https://docs.microsoft.com/en-us/ef/core/saving/concurrency
			// The user probably refreshed several times in a row.
			//catch (DbUpdateConcurrencyException) { }
		}
	}
}