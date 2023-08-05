using System;
using System.Runtime.CompilerServices;

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
}
