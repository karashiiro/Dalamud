using Lumina.Excel;

namespace Dalamud.Game.Text;

public interface IExcelManager
{
    public ExcelSheet<T>? GetExcelSheet<T>() where T : ExcelRow;

    public ExcelSheet<T>? GetExcelSheet<T>(ClientLanguage language) where T : ExcelRow;
}
