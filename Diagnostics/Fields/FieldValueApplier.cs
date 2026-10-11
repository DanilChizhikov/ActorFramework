using System;

namespace DTech.ActorFramework.Diagnostics
{
	internal static class FieldValueApplier
	{
		public static bool TryApply(SetFieldRequest request)
		{
			_ = request ?? throw new ArgumentNullException(nameof(request));

			int[] path = request.Path;
			if (path == null || path.Length == 0 || request.Value == null)
			{
				return false;
			}

			if (!ActorWorldTracker.TryGet(request.WorldId, out ActorWorld world) ||
				!world.TryGetActor(request.ActorId, out Actor actor) || !actor.IsAlive ||
				!world.Features.TryGetPool(request.FeatureTypeId, out IFeaturePool pool) || !pool.Has(actor.Id))
			{
				return false;
			}

			object feature = pool.GetBoxed(actor.Id);
			var slots = new FieldSlot[path.Length];
			object container = feature;
			for (int i = 0; i < path.Length; i++)
			{
				if (container == null || !FieldSlot.TryCreate(container, path[i], out FieldSlot slot) || slot.IsReadOnly)
				{
					return false;
				}

				slots[i] = slot;
				container = slot.GetValue();
			}

			FieldSlot leaf = slots[path.Length - 1];
			Type targetType = leaf.DeclaredType.IsValueType || container == null ? leaf.DeclaredType : container.GetType();
			object decode = FieldValueCodec.Decode(request.Value, targetType);
			leaf.SetValue(decode);

			for (int i = path.Length - 2; i >= 0; i--)
			{
				object child = slots[i + 1].Container;
				if (!child.GetType().IsValueType)
				{
					break;
				}

				slots[i].SetValue(child);
			}

			pool.SetBoxed(actor.Id, feature);

			return true;
		}
	}
}