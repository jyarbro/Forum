﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Forum3.Models.ViewModels.Smileys;

using DataModels = Forum3.Models.DataModels;

namespace Forum3.Services {
	public class SmileyService {
		DataModels.ApplicationDbContext DbContext { get; }

		public SmileyService(
			DataModels.ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public async Task<IndexPage> IndexPage() {
			var smileysQuery = from smiley in DbContext.Smileys
							   orderby smiley.SortOrder
							   select smiley;

			var smileys = await smileysQuery.ToListAsync();

			var viewModel = new IndexPage();

			foreach (var smiley in smileys) {
				var sortColumn = smiley.SortOrder / 100;
				var sortRow = smiley.SortOrder % 100;

				viewModel.Smileys.Add(new Smiley {
					Id = smiley.Id,
					Code = smiley.Code,
					Path = smiley.Path,
					Column = sortColumn,
					Row = sortRow
				});
			}

			return viewModel;
		}
	}
}