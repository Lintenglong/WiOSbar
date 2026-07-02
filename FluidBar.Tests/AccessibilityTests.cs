using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluidBar.Accessibility;

namespace FluidBar.Tests;

/// <summary>
/// 无障碍功能测试
/// </summary>
[TestClass]
public class AccessibilityTests
{
    [TestMethod]
    public void GetFontScale_ShouldReturnValidScale()
    {
        // Act
        var scale = AccessibilityManager.GetFontScale();

        // Assert
        Assert.IsTrue(scale > 0, "字体缩放比例应大于 0");
        Assert.IsTrue(scale <= 5, "字体缩放比例应在合理范围内");
    }

    [TestMethod]
    public void AnnounceEvent_ShouldNotThrow()
    {
        // Act & Assert - 不应抛出异常
        try
        {
            AccessibilityManager.AnnounceEvent("测试通知");
            Assert.IsTrue(true);
        }
        catch
        {
            Assert.Fail("AnnounceEvent 不应抛出异常");
        }
    }
}
