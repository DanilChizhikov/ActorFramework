using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DTech.ActorFramework
{
	internal static class ActorWorldTracker
	{
		private static readonly object _lock = new();
		private static readonly List<WeakReference<ActorWorld>> _worlds = new();

		[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
		public static void Register(ActorWorld world)
		{
			lock (_lock)
			{
				_worlds.Add(new WeakReference<ActorWorld>(world));
			}
		}

		[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
		public static void Unregister(ActorWorld world)
		{
			lock (_lock)
			{
				for (int i = _worlds.Count - 1; i >= 0; i--)
				{
					if (!_worlds[i].TryGetTarget(out ActorWorld tracked) || ReferenceEquals(tracked, world))
					{
						_worlds.RemoveAt(i);
					}
				}
			}
		}

		public static void GetWorlds(List<ActorWorld> results)
		{
			_ = results ?? throw new ArgumentNullException(nameof(results));

			results.Clear();
			lock (_lock)
			{
				for (int i = 0; i < _worlds.Count; i++)
				{
					if (!_worlds[i].TryGetTarget(out ActorWorld world) || world.IsDisposed)
					{
						_worlds.RemoveAt(i);
						i--;
						continue;
					}

					results.Add(world);
				}
			}
		}

		public static bool TryGet(int debugId, out ActorWorld world)
		{
			lock (_lock)
			{
				for (int i = 0; i < _worlds.Count; i++)
				{
					if (_worlds[i].TryGetTarget(out world) && !world.IsDisposed && world.DebugId == debugId)
					{
						return true;
					}
				}
			}

			world = null;

			return false;
		}
	}
}