using System;
using System.Collections.Generic;

namespace DTech.ActorFramework
{
	internal sealed class ActivationState
	{
		public event Action OnActivated;
		public event Action OnDeactivated;

		private readonly HashSet<object> _activators = new(ReferenceComparer.Instance);
		private readonly HashSet<object> _deactivators = new(ReferenceComparer.Instance);
		
		public bool IsActive { get; private set; }
		public bool IsArmed { get; private set; }
		public int DeactivatorCount => _deactivators.Count;
		public int ActivatorCount => _activators.Count;
		
		private bool _isEvaluating;
		private bool _isEvaluationPending;

		public void Arm()
		{
			if (IsArmed)
			{
				return;
			}

			IsArmed = true;
			Evaluate();
		}

		public void Reset()
		{
			IsArmed = false;
			_isEvaluationPending = false;
			_deactivators.Clear();
			_activators.Clear();

			if (IsActive)
			{
				IsActive = false;
				OnDeactivated?.Invoke();
			}

			_isEvaluating = false;
		}
		
		public void AddActivator(object token)
		{
			if (_activators.Add(token) && _activators.Count == 1)
			{
				Evaluate();
			}
		}

		public bool RemoveActivator(object token)
		{
			if (!_activators.Remove(token))
			{
				return false;
			}

			if (_activators.Count == 0)
			{
				Evaluate();
			}

			return true;
		}

		public bool ContainsActivator(object token)
		{
			return _activators.Contains(token);
		}

		public int GetActivatorsNonAlloc(object[] results) => CopyTo(_activators, results);

		public void AddDeactivator(object token)
		{
			if (_deactivators.Add(token) && _deactivators.Count == 1)
			{
				Evaluate();
			}
		}

		public bool RemoveDeactivator(object token)
		{
			if (!_deactivators.Remove(token))
			{
				return false;
			}

			if (_deactivators.Count == 0)
			{
				Evaluate();
			}

			return true;
		}

		public bool ContainsDeactivator(object token)
		{
			return _deactivators.Contains(token);
		}

		public int GetDeactivatorsNonAlloc(object[] results) => CopyTo(_deactivators, results);

		private static int CopyTo(HashSet<object> source, object[] results)
		{
			int writtenCount = 0;
			foreach (object token in source)
			{
				if (writtenCount == results.Length)
				{
					break;
				}

				results[writtenCount] = token;
				writtenCount++;
			}

			return writtenCount;
		}
		
		private void Evaluate()
		{
			if (!IsArmed)
			{
				return;
			}

			if (_isEvaluating)
			{
				_isEvaluationPending = true;

				return;
			}

			_isEvaluating = true;
			try
			{
				do
				{
					_isEvaluationPending = false;

					bool nextState = _deactivators.Count == 0 && _activators.Count > 0;
					if (nextState != IsActive)
					{
						IsActive = nextState;
						if (IsActive)
						{
							OnActivated?.Invoke();
						}
						else
						{
							OnDeactivated?.Invoke();
						}
					}
				}
				while (_isEvaluationPending);
			}
			finally
			{
				_isEvaluationPending = false;
				_isEvaluating = false;
			}
		}
	}
}