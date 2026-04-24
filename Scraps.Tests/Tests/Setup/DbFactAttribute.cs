using Xunit;

namespace Scraps.Tests.Setup
{
    /// <summary>
    /// DB-тесты должны падать при проблемах окружения, а не пропускаться.
    /// Атрибут оставлен для явной маркировки тестов.
    /// </summary>
    public sealed class DbFactAttribute : FactAttribute
    {
    }

    /// <summary>
    /// STA-вариант для DB-тестов с тем же поведением (без Skip).
    /// </summary>
    public sealed class DbStaFactAttribute : StaFactAttribute
    {
    }
}
