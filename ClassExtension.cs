using System;
using System.Runtime.CompilerServices;
using static Assets.Scripts.Objects.Slot;

namespace EntropyFix
{
	public static class ClassExtension<TClass, TExtension>
		where TClass : class
		where TExtension : class
	{

		private static readonly ConditionalWeakTable<TClass, TExtension> Extensions =
			new ConditionalWeakTable<TClass, TExtension>();

		public static bool HasExtension(TClass instance)
		{
			return Extensions.TryGetValue(instance, out var value);
		}

		public static void ClearExtension(TClass instance)
		{
			Extensions.Remove(instance);
		}

		public static TExtension GetExtension(TClass instance)
		{

			if (Extensions.TryGetValue(instance, out var value))
				return value;
			return null;
		}

		public static TExtension GetOrCreateExtension(TClass instance, Func<TClass, TExtension> factory)
		{
			if (Extensions.TryGetValue(instance, out var value))
				return value;
			value = factory(instance);
			Extensions.Add(instance, value);
			return value;
		}

		public static void SetExtension(TClass instance, TExtension value)
		{
			if (!Extensions.TryGetValue(instance, out var extension))
				Extensions.Add(instance, extension);
			else
			{
				Extensions.Remove(instance);
				Extensions.Add(instance, value);
			}
		}
	}

	public static class ClassExtensionExtensions
	{
		public static void SetExtension<TClass, TExtension>(this TClass instance, TExtension extension) 
			where TClass : class
			where TExtension : class
			=> ClassExtension<TClass, TExtension>.SetExtension(instance, extension);

		public static TExtension GetOrCreateExtension<TClass, TExtension>(this TClass instance, Func<TClass, TExtension> factory)
			where TClass : class
			where TExtension : class
			=> ClassExtension<TClass, TExtension>.GetOrCreateExtension(instance, factory);

		public static TExtension GetExtension<TClass, TExtension>(this TClass instance)
			where TClass : class
			where TExtension : class
			=> ClassExtension<TClass, TExtension>.GetExtension(instance);

		public static void ClearExtension<TClass, TExtension>(this TClass instance)
			where TClass : class
			where TExtension : class
			=> ClassExtension<TClass, TExtension>.ClearExtension(instance);

		public static bool HasExtension<TClass, TExtension>(this TClass instance)
			where TClass : class
			where TExtension : class
			=> ClassExtension<TClass, TExtension>.HasExtension(instance);
	}
}
