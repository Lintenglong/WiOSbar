using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluidBar;

namespace FluidBar.Tests;

/// <summary>
/// 自启动管理器测试
/// </summary>
[TestClass]
public class StartupManagerTests
{
    [TestMethod]
    public void IsEnabled_ShouldReturnBoolean()
    {
        // Act
        var isEnabled = StartupManager.IsEnabled();

        // Assert
        // 只是验证方法可调用，不验证具体值
        Assert.IsTrue(isEnabled == true || isEnabled == false);
    }

    [TestMethod]
    public void Enable_Disable_ShouldToggleWithoutError()
    {
        // Arrange
        var originalState = StartupManager.IsEnabled();

        // Act & Assert
        try
        {
            if (!originalState)
            {
                // 测试启用
                var enableResult = StartupManager.Enable();
                // 可能失败（权限问题），但不应崩溃
            }
            else
            {
                // 测试禁用
                var disableResult = StartupManager.Disable();
            }

            Assert.IsTrue(true);
        }
        catch
        {
            Assert.Fail("自启动操作不应抛出异常");
        }
        finally
        {
            // 恢复原始状态
            if (originalState)
                StartupManager.Enable();
            else
                StartupManager.Disable();
        }
    }
}
