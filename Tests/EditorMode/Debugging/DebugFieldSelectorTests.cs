using System;
using DTech.ActorFramework.Diagnostics;
using NUnit.Framework;

namespace DTech.ActorFramework.Tests.EditorMode
{
	[TestFixture]
	internal sealed class DebugFieldSelectorTests
	{
		[Test]
		public void GetFields_PublicField_IsVisible()
		{
			Assert.That(HasField(nameof(VisibilityProbe.Visible)), Is.True);
		}

		[Test]
		public void GetFields_PublicFieldWithHide_IsHidden()
		{
			Assert.That(HasField(nameof(VisibilityProbe.HiddenPublic)), Is.False);
		}

		[Test]
		public void GetFields_PrivateFieldWithShow_IsVisible()
		{
			Assert.That(HasField("_shownPrivate"), Is.True);
		}

		[Test]
		public void GetFields_PrivateFieldWithoutShow_IsHidden()
		{
			Assert.That(HasField("_hiddenPrivate"), Is.False);
		}

		[Test]
		public void GetFields_StaticField_IsHidden()
		{
			Assert.That(HasField(nameof(VisibilityProbe.StaticValue)), Is.False);
		}

		[Test]
		public void GetFields_AutoPropertyBackingFieldWithShow_IsVisibleWithPropertyLabel()
		{
			DebugField[] fields = DebugFieldSelector.GetFields(typeof(VisibilityProbe));
			DebugField field = Array.Find(fields, candidate => candidate.Label == "Auto Property");

			Assert.That(field, Is.Not.Null);
			Assert.That(field.Info.Name, Does.Contain(nameof(VisibilityProbe.AutoProperty)));
		}

		[Test]
		public void GetFields_ReadOnlyField_IsVisibleAndReadOnly()
		{
			DebugField[] fields = DebugFieldSelector.GetFields(typeof(VisibilityProbe));
			DebugField field = fields[DebugTestUtility.IndexOfField(typeof(VisibilityProbe),
				nameof(VisibilityProbe.ReadOnlyValue))];

			Assert.That(field.IsReadOnly, Is.True);
		}

		[Test]
		public void GetFields_SameType_ReturnsCachedArray()
		{
			Assert.That(DebugFieldSelector.GetFields(typeof(Health)),
				Is.SameAs(DebugFieldSelector.GetFields(typeof(Health))));
		}

		[TestCase("_shownPrivate", "Shown Private")]
		[TestCase("m_Value", "Value")]
		[TestCase("<AutoProperty>k__BackingField", "Auto Property")]
		[TestCase("maxHP", "Max HP")]
		[TestCase("Current", "Current")]
		public void NicifyName_ProducesInspectorLabel(string name, string expected)
		{
			Assert.That(DebugFieldSelector.NicifyName(name), Is.EqualTo(expected));
		}

		private static bool HasField(string fieldName)
		{
			DebugField[] fields = DebugFieldSelector.GetFields(typeof(VisibilityProbe));

			return Array.Exists(fields, field => field.Info.Name == fieldName);
		}
	}
}
