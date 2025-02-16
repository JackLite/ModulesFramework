namespace ModulesFramework.Utils
{
    internal static class MFMath
    {
        public static int CountBits(ulong x)
        {
            x -= (x >> 1) & 0x5555555555555555;
            x = (x & 0x3333333333333333) + ((x >> 2) & 0x3333333333333333);
            return (int)((((x + (x >> 4)) & 0x0F0F0F0F0F0F0F0F) * 0x0101010101010101) >> 56);
        }
    }
}