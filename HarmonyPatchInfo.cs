using System;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using AccessTools = HarmonyLib.AccessTools;

namespace EntropyFix
{
	public class HarmonyPatchInfo
	{
		public PatchCategory Category { get; }
		public MethodBase OriginalMethod { get; }
		public Type DeclaringType { get; }

		public static HarmonyPatchInfo Create(Type declaringType)
		{
			var methods = HarmonyMethodExtensions.GetFromType(declaringType);
			if (methods == null || methods.Count <= 0)
				return null;
			return new HarmonyPatchInfo(declaringType, methods);
		}
		private HarmonyPatchInfo(Type declaringType, List<HarmonyMethod> methods)
		{
			if(methods == null)
				throw new ArgumentNullException(nameof(methods));
			HarmonyMethodExtensions.GetMergedFromType(declaringType);
			DeclaringType = declaringType;
			var method = HarmonyMethod.Merge(methods);
			Category = declaringType.GetCategory();
			OriginalMethod = GetOriginalMethod(declaringType, method);
			if(OriginalMethod == null)
				throw new ApplicationException("OriginalMethod is null!");
		}

		public bool Patch(Harmony harmony)
		{
			if (harmony == null)
				throw new ArgumentNullException(nameof(harmony));
			var result = !Harmony.GetPatchInfo(OriginalMethod)?.Owners?.Any(harmony.Id.Equals) ?? true;
			
			if (result)
			{
				var processor = new PatchClassProcessor(harmony, DeclaringType);
				processor.Patch();
			}
			return result;
		}

		public bool Unpatch(Harmony harmony)
		{
			if(harmony == null)
				throw new ArgumentNullException(nameof(harmony));
			var result = Harmony.GetPatchInfo(OriginalMethod)?.Owners?.Any(harmony.Id.Equals) ?? false;
			harmony.Unpatch(OriginalMethod, HarmonyPatchType.All);
			return result;
		}

		private static MethodBase GetOriginalMethod(Type declaringType, HarmonyMethod attr)
		{
			MethodBase result = null;
			try
			{
				switch (attr.methodType.GetValueOrDefault())
				{
					default:
					case MethodType.Normal:
						if(attr.methodName != null)
							result = AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes);
						break;
					case MethodType.Getter:
						if(attr.methodName != null)
							result = AccessTools.DeclaredProperty(attr.declaringType, attr.methodName).GetGetMethod(true);
						break;
					case MethodType.Setter:
						if(attr.methodName != null)
							result = AccessTools.DeclaredProperty(attr.declaringType, attr.methodName).GetSetMethod(true);
						break;
					case MethodType.Constructor:
						result = AccessTools.DeclaredConstructor(attr.declaringType, attr.argumentTypes);
						break;
					case MethodType.StaticConstructor:
						result = AccessTools.GetDeclaredConstructors(attr.declaringType).FirstOrDefault(c => c.IsStatic);
						break;
					case MethodType.Enumerator:
						if (attr.methodName != null)
							result = AccessTools.EnumeratorMoveNext(AccessTools.DeclaredMethod(attr.declaringType, attr.methodName, attr.argumentTypes));
						break;
				}
			}
			catch (AmbiguousMatchException ex)
			{
				throw new ApplicationException($"Ambiguous match for HarmonyMethod[{attr.ToString()}]", ex.InnerException ?? ex);
			}
			if(result != null)
				return result;
			throw new ApplicationException($"Unknown method for HarmonyMethod[{attr.ToString()}] declared in {declaringType}");
		}
	}
}
