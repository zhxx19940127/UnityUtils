public static class Alignement
{
    // 将value向上对齐到align的倍数（align必须是2的幂）
    // 示例：Align(5,4)=8, Align(17,16)=32
    public static int Align(int value, int align)
    {
        return (((value) + ((align) - 1)) & (~((align) - 1)));
    }
    // 检查value是否已按align对齐
    // 示例：IsAligned(8,4)=true, IsAligned(7,4)=false
    public static bool IsAligned(int value, int align)
    {
        return (((value) & ~((align) - 1)) == 0);
    }
    // 检查value是否是2的幂
    // 示例：IsPowerOfTwo(8)=true, IsPowerOfTwo(7)=false
    public static bool IsPowerOfTwo(int value)
    {
        return ((value & (value - 1)) == 0);
    }
}