using System;
using DTech.ActorFramework.Diagnostics;

namespace DTech.ActorFramework.Editor
{
	internal readonly struct ActorSearch
	{
		public string Text { get; }
		public bool HasId { get; }
		public int Id { get; }

		public ActorSearch(string text)
		{
			Text = text?.Trim();
			HasId = int.TryParse(Text?.TrimStart('#'), out int id);
			Id = id;
		}

		public bool Matches(ActorSummary actor, DebugSession session)
		{
			if (string.IsNullOrEmpty(Text))
			{
				return true;
			}

			if (HasId)
			{
				return actor.Id == Id;
			}

			int[] typeIds = actor.FeatureTypeIds;
			for (int i = 0; i < typeIds.Length; i++)
			{
				if (session.GetFeatureTypeName(typeIds[i]).IndexOf(Text, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}
	}
}