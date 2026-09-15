// DevTrove.Crypto 是门面包，仅用于传递对 DevTrove.Crypto.Core 的依赖。
// 该 Program.cs 仅确保元包在 net10.0 目标下能编译，不暴露任何 API 类型。
namespace DevTrove.Crypto;

internal static class Program
{
    private static int Main() => 0;
}
