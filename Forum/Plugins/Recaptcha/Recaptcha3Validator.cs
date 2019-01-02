﻿using Forum.Errors;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Forum.Plugins.Recaptcha {
	public class Recaptcha3Validator : IRecaptcha3Validator {
		HttpClient HttpClient { get; }
		Recaptcha3Options Options { get; }

		public Recaptcha3Validator(
			IOptions<Recaptcha3Options> optionsAccessor
		) {
			HttpClient = new HttpClient(new HttpClientHandler()) {
				Timeout = TimeSpan.FromSeconds(3)
			};

			Options = optionsAccessor.Value;
		}

		public async Task Validate(string recaptchaResponse, string ipAddress) {
			var siteAddress = "https://www.google.com/recaptcha/api/siteverify";

			var request = new HttpRequestMessage(HttpMethod.Post, siteAddress) {
				Content = new FormUrlEncodedContent(new Dictionary<string, string> {
					["secret"] = Options.Recaptcha3SecretKey,
					["response"] = recaptchaResponse,
					["remoteip"] = ipAddress
				})
			};

			var requestResponse = await HttpClient.SendAsync(request);
			requestResponse.EnsureSuccessStatusCode();

			var responseText = await requestResponse.Content.ReadAsStringAsync();

			var validatorResponse = JsonConvert.DeserializeObject<Recaptcha3ValidatorResponse>(responseText);

			if (validatorResponse.Score < 0.5) {
				throw new HttpBadRequestError("You appear to be a bot. Contact me if this is wrong.");
			}

			if (!validatorResponse.Success) {
				var exceptionMessage = $"There was a problem validating the recaptcha. Error code(s) were:\n{string.Join("\n", validatorResponse.ErrorCodes)}\n";
				throw new HttpBadRequestError(validatorResponse.ErrorCodes.FirstOrDefault());
			}
		}
	}
}