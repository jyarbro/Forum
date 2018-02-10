﻿using Forum3.Models.ViewModels.Topics.Items;
using System;
using System.Collections.Generic;

namespace Forum3.Models.ViewModels.Topics.Pages {
	public class TopicIndexPage {
		public int BoardId { get; set; }
		public string BoardName { get; set; }
		public long After { get; set; }
		public List<MessagePreview> Topics { get; set; }
	}
}