using StardewModdingAPI;

namespace BetterCrafting
{
    internal static class ReflectionExtension
    {
        public static TValue GetFieldValue<TValue>(this IReflectionHelper reflection, object obj, string name, bool required = true)
        {
            return reflection.GetField<TValue>(obj, name, required).GetValue();
        }
    }
}