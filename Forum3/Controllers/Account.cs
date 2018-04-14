﻿using Forum3.Annotations;
using Forum3.Contexts;
using Forum3.Exceptions;
using Forum3.Extensions;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Account : ForumController {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		AccountRepository AccountRepository { get; }
		SettingsRepository SettingsRepository { get; }

		UserManager<DataModels.ApplicationUser> UserManager { get; }
		ILogger Logger { get; }

		public Account(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			SettingsRepository settingsRepository,
			UserManager<DataModels.ApplicationUser> userManager,
			ILogger<Account> logger
		) {
			DbContext = dbContext;
			UserContext = userContext;

			AccountRepository = accountRepository;
			SettingsRepository = settingsRepository;

			UserManager = userManager;
			Logger = logger;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = new ViewModels.Account.IndexPage();

			var users = await DbContext.Users.OrderBy(u => u.DisplayName).ToListAsync();

			foreach (var user in users) {
				var indexItem = new ViewModels.Account.IndexItem {
					User = user,
					Registered = user.Registered.ToPassedTimeString(),
					LastOnline = user.LastOnline.ToPassedTimeString()
				};

				if (UserContext.IsAdmin || user.Id == UserContext.ApplicationUser.Id)
					indexItem.CanManage = true;

				viewModel.IndexItems.Add(indexItem);
			}

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Details(string id) {
			var userRecord = id is null ? UserContext.ApplicationUser : await UserManager.FindByIdAsync(id);

			if (userRecord is null)
				userRecord = UserContext.ApplicationUser;

			AccountRepository.CanEdit(userRecord.Id);

			var viewModel = new ViewModels.Account.DetailsPage {
				AvatarPath = userRecord.AvatarPath,
				Id = userRecord.Id,
				DisplayName = userRecord.DisplayName,
				Email = userRecord.Email,
				EmailConfirmed = userRecord.EmailConfirmed,
				BirthdayDays = AccountRepository.DayPickList(userRecord.Birthday.Day),
				BirthdayMonths = AccountRepository.MonthPickList(userRecord.Birthday.Month),
				BirthdayYears = AccountRepository.YearPickList(userRecord.Birthday.Year),
				BirthdayDay = userRecord.Birthday.Day.ToString(),
				BirthdayMonth = userRecord.Birthday.Month.ToString(),
				BirthdayYear = userRecord.Birthday.Year.ToString(),
				Settings = await SettingsRepository.GetUserSettingsList(userRecord.Id)
			};

			ModelState.Clear();

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Details(InputModels.UpdateAccountInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.UpdateAccount(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToReferrer();
				}
			}

			var userRecord = await DbContext.Users.FindAsync(input.Id);

			if (userRecord is null) {
				var message = $"No record found with the display name '{input.DisplayName}'";
				Logger.LogWarning(message);
				throw new ApplicationException("You hackin' bro?");
			}

			AccountRepository.CanEdit(userRecord.Id);

			var viewModel = new ViewModels.Account.DetailsPage {
				DisplayName = input.DisplayName,
				Email = input.Email,
				AvatarPath = userRecord.AvatarPath,
				Id = userRecord.Id,
				EmailConfirmed = userRecord.EmailConfirmed,
				BirthdayDays = AccountRepository.DayPickList(input.BirthdayDay),
				BirthdayMonths = AccountRepository.MonthPickList(input.BirthdayMonth),
				BirthdayYears = AccountRepository.YearPickList(input.BirthdayYear),
				BirthdayDay = input.BirthdayDay.ToString(),
				BirthdayMonth = input.BirthdayMonth.ToString(),
				BirthdayYear = input.BirthdayYear.ToString(),
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateAvatar(InputModels.UpdateAvatarInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.UpdateAvatar(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToReferrer();
				}
			}

			var userRecord = input.Id is null ? UserContext.ApplicationUser : await UserManager.FindByIdAsync(input.Id);

			if (userRecord is null)
				userRecord = UserContext.ApplicationUser;

			AccountRepository.CanEdit(userRecord.Id);

			var viewModel = new ViewModels.Account.DetailsPage {
				AvatarPath = userRecord.AvatarPath,
				Id = userRecord.Id,
				DisplayName = userRecord.DisplayName,
				Email = userRecord.Email,
				EmailConfirmed = userRecord.EmailConfirmed,
				BirthdayDays = AccountRepository.DayPickList(userRecord.Birthday.Day),
				BirthdayMonths = AccountRepository.MonthPickList(userRecord.Birthday.Month),
				BirthdayYears = AccountRepository.YearPickList(userRecord.Birthday.Year),
				BirthdayDay = userRecord.Birthday.Day.ToString(),
				BirthdayMonth = userRecord.Birthday.Month.ToString(),
				BirthdayYear = userRecord.Birthday.Year.ToString(),
			};

			return View(nameof(Details), viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendVerificationEmail() {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.SendVerificationEmail();
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToReferrer();
				}
			}

			return RedirectToAction(nameof(Profile.Details), nameof(Profile));
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ConfirmEmail(InputModels.ConfirmEmailInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ConfirmEmail(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToAction(nameof(Boards.Index), nameof(Boards));
				}
			}

			return View();
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Login() {
			if (AccountRepository.IsAuthenticated)
				return RedirectToAction(nameof(Boards.Index), nameof(Boards));

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.LoginPage();
			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> Login(InputModels.LoginInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.Login(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return RedirectToAction(nameof(Boards.Index), nameof(Boards));
					else
						return RedirectFromService();
				}
			}

			if (AccountRepository.IsAuthenticated)
				return RedirectToAction(nameof(Boards.Index), nameof(Boards));

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.LoginPage {
				Email = input.Email,
				RememberMe = input.RememberMe
			};

			return View(viewModel);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult AccessDenied() {
			return View();
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Lockout() {
			await AccountRepository.SignOut();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout() {
			await AccountRepository.SignOut();
			return RedirectToAction(nameof(Boards.Index), nameof(Boards));
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Register() {
			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.RegisterPage {
				BirthdayDays = AccountRepository.DayPickList(),
				BirthdayMonths = AccountRepository.MonthPickList(),
				BirthdayYears = AccountRepository.YearPickList()
			};

			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> Register(InputModels.RegisterInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.Register(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToAction(nameof(Login));
				}
			}

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.RegisterPage {
				BirthdayDays = AccountRepository.DayPickList(),
				BirthdayDay = input.BirthdayDay.ToString(),
				BirthdayMonths = AccountRepository.MonthPickList(),
				BirthdayMonth = input.BirthdayMonth.ToString(),
				BirthdayYears = AccountRepository.YearPickList(),
				BirthdayYear = input.BirthdayYear.ToString(),
				DisplayName = input.DisplayName,
				Email = input.Email,
				ConfirmEmail = input.ConfirmEmail,
				Password = input.Password,
				ConfirmPassword = input.ConfirmPassword,
			};

			return View(viewModel);
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ForgotPassword() {
			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ForgotPasswordPage();

			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> ForgotPassword(InputModels.ForgotPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ForgotPassword(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToAction(nameof(Login));
				}
			}

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ForgotPasswordPage {
				Email = input.Email
			};

			return View(viewModel);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPasswordConfirmation() => View();

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ResetPassword(string code) {
			code.ThrowIfNull(nameof(code));

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ResetPasswordPage {
				Code = code
			};

			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> ResetPassword(InputModels.ResetPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ResetPassword(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToAction(nameof(Login));
				}
			}

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ResetPasswordPage {
				Code = input.Code
			};

			return View(viewModel);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ResetPasswordConfirmation() => View();

		[HttpGet]
		public IActionResult Delete(string userId) => View("Delete", userId);

		[HttpGet]
		public async Task<IActionResult> ConfirmDelete(string userId) {
			if (UserContext.ApplicationUser.Id != userId && !UserContext.IsAdmin)
				throw new HttpForbiddenException();

			var deletedAccount = DbContext.Users.FirstOrDefault(item => item.DisplayName == "Deleted Account");

			if (deletedAccount is null) {
				deletedAccount = new DataModels.ApplicationUser {
					DisplayName = "Deleted Account",
					UserName = Guid.NewGuid().ToString(),
					Email = Guid.NewGuid().ToString(),
					AvatarPath = string.Empty,
					Birthday = new DateTime(2000, 1, 1),
					Registered = new DateTime(2000, 1, 1),
				};

				DbContext.Users.Add(deletedAccount);
				DbContext.SaveChanges();
			}

			foreach (var item in DbContext.MessageThoughts.Where(item => item.UserId == userId).ToList())
				DbContext.Remove(item);

			DbContext.SaveChanges();

			foreach (var item in DbContext.Messages.Where(item => item.PostedById == userId).ToList()) {
				item.PostedById = deletedAccount.Id;
				item.EditedById = deletedAccount.Id;
				item.OriginalBody = string.Empty;
				item.DisplayBody = "This account has been deleted.";
				item.LongPreview = string.Empty;
				item.ShortPreview = string.Empty;
				item.Cards = string.Empty;
			}

			DbContext.SaveChanges();

			var account = DbContext.Users.Find(userId);
			await UserManager.DeleteAsync(account);

			DbContext.SaveChanges();

			return RedirectToAction(nameof(Boards.Index), nameof(Boards));
		}
	}
}