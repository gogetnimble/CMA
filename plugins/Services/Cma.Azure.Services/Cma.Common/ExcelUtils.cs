using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using Cma.Common.Exceptions;
using Cma.Common.Extensions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Cma.Common;

/// <summary>
/// This class defines functions that help manipulate Excel worksheets data using
/// Open XML Library: https://learn.microsoft.com/en-us/office/open-xml/open-xml-sdk
/// </summary>
public class ExcelUtils
{ 
    /// <summary>
    ///  Convert column letter to index of the column, like AA -> 27
    /// </summary>
    /// <param name="columnLetter"></param>
    /// <returns></returns>
    public static int ColumnLetterToIndex(string columnLetter)
    {
        var sum = 0;
        foreach (var c in columnLetter.ToUpper())
        {
            sum *= 26;
            sum += (c - 'A' + 1);
        }
        return sum;
    }

    /// <summary>
    /// Extract column name from cell reference (e.g., "AA15" -> "AA")
    /// </summary>
    /// <param name="cellReference"></param>
    /// <returns></returns>
    public static string GetColumnName(string cellReference)
    {
        return new string(cellReference.Where(char.IsLetter).ToArray());
    }
    
    /// <summary>
    /// Extract column name and row index from cell reference such as"AB", 52 from AB52
    /// </summary>
    /// <param name="cellRef"></param>
    /// <returns></returns>
    public static (string, uint) ParseCellRef(string cellRef)
    {
        // Pattern: (Column)(Row)
        // Parse the cell reference to get row and column
        string columnName = new string(cellRef.Where(char.IsLetter).ToArray());
        uint rowIndex = uint.Parse(new string(cellRef.Where(char.IsDigit).ToArray()));

        return (columnName, rowIndex);
    }
    
    /// <summary>
    /// Build cell reference like "B2", from cell index like 2,2
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    public static string GetCellReference( int col, int row)
    {
        return $"{ColumnIndexToLetter(col)}{row}";
    }

    /// <summary>
    /// Convert column index to letter of the column such as 2 to B, 27 -> AA 
    /// </summary>
    /// <param name="colIndex"></param>
    /// <returns></returns>
    public static string ColumnIndexToLetter(int colIndex)
    {
        var column = string.Empty;
        while (colIndex > 0)
        {
            var remainder = (colIndex - 1) % 26;
            column = (char)(remainder + 'A') + column;
            colIndex = (colIndex - remainder - 1) / 26;
        }
        return column;
    }
    
    /// <summary>
    /// Return worksheet part
    /// </summary>
    /// <param name="document"></param>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    /// <exception cref="InternalServerErrorException"></exception>
    public static WorksheetPart GetWorksheetPart(SpreadsheetDocument document, string sheetName)
    {
        //check input
        if (document?.WorkbookPart?.Workbook?.Sheets == null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(),
                $"Workbook or Sheets '{sheetName}' is null.");
        }
        
        //retrieve sheet by name
        var sheet = document.WorkbookPart.Workbook.Sheets.Elements<Sheet>()
            .FirstOrDefault(s => s.Name == sheetName);

        if (sheet?.Id == null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(),
                $"Sheet or Sheet.Id '{sheetName}' is null.");
        }
        
        //retrieve worksheet part by IDs
        return (WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id!);
    }


    /// <summary>
    /// Set recalculation flag in Excel workbook so that Excel recalculate all
    /// formulas when the workbook is opened
    /// </summary>
    /// <param name="document"></param>
    /// <exception cref="InternalServerErrorException"></exception>
    public static void SetRecalculationFlag(SpreadsheetDocument document)
    {
        if (document?.WorkbookPart?.Workbook == null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(),
                "SetRecalculationFlag: WorkbookPart or Workbook is null.");
        }

        // Ensure CalculationProperties exists
        var calcProps = document.WorkbookPart.Workbook.CalculationProperties
                        ?? new CalculationProperties();

        // Force Excel to perform a full calculation on next open
        calcProps.FullCalculationOnLoad = true;
        calcProps.ForceFullCalculation = true;
        calcProps.CalculationMode = CalculateModeValues.Auto;
        
        document.WorkbookPart.Workbook.CalculationProperties = calcProps;
    }
    
    /// <summary>
    /// Set value of a cell
    /// </summary>
    /// <param name="worksheetPart"></param>
    /// <param name="cellReference"></param>
    /// <param name="value"></param>
    /// <param name="dataType"></param>
    public static void SetCellValue(WorksheetPart worksheetPart, string cellReference, string value, CellValues dataType)
    {
        if (worksheetPart?.Worksheet == null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(),
                "SetCellValue: WorksheetPart is null.");
        }
        
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

        if (sheetData == null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(),
                "SetCellValue: Sheet data of worksheet is null.");
        }
        
        // Parse the cell reference to get row and column
        var (columnName, rowIndex) = ParseCellRef(cellReference);
        
        Row row = GetRow(sheetData, rowIndex);
        Cell cell = GetCell(row, columnName);
        
        cell.CellValue = new CellValue(value);
        cell.DataType = new EnumValue<CellValues>(dataType);
    }
    
    /// <summary>
    /// Set a formula of a cell
    /// </summary>
    /// <param name="worksheetPart"></param>
    /// <param name="cellReference"></param>
    /// <param name="cellFormula"></param>
    public static void SetCellFormula(WorksheetPart worksheetPart, string cellReference, string cellFormula)
    {
        if (worksheetPart?.Worksheet == null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(),
                "SetCellFormula: WorksheetPart is null.");
        }
    
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
    
        if (sheetData == null)
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(),
                "SetCellFormula: Sheet data of worksheet is null.");
        }
    
        // Parse the cell reference to get row and column
        var (columnName, rowIndex) = ParseCellRef(cellReference);
    
        Row row = GetRow(sheetData, rowIndex);
        Cell cell = GetCell(row, columnName);

        // Clear DataType for formula cells
        cell.DataType = null;
    
        // Set the formula
        cell.CellFormula = new CellFormula(cellFormula);
    
        // Clear any existing value to force recalculation
        cell.CellValue = null;
    }
    
    
    /// <summary>
    /// Return row from row index. insert row if it does not exist
    /// </summary>
    /// <param name="sheetData"></param>
    /// <param name="rowIndex"></param>
    /// <returns></returns>
    public static Row GetRow(SheetData sheetData, uint rowIndex)
    {
        var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex != null && r.RowIndex.Value == rowIndex);
        
        if (row == null)
        {
            row = new Row() { RowIndex = rowIndex };
            
            // Insert the row in the correct position
            var refRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex != null &&
                                                                       r.RowIndex.Value == rowIndex);
            if (refRow != null)
            {
                sheetData.InsertBefore(row, refRow);
            }
            else
            {
                sheetData.Append(row);
            }
        }
        
        return row;
    }
    
    /// <summary>
    /// Insert rows
    /// </summary>
    /// <param name="sheetData"></param>
    /// <param name="startRowIndex"></param>
    /// <param name="count"></param>
    public static void InsertRows(SheetData sheetData, uint startRowIndex, uint count)
    {
        for (uint i = 0; i < count; i++)
        {
            var newRow = new Row() { RowIndex = startRowIndex + i };
            
            var refRow = sheetData.Elements<Row>()
                .FirstOrDefault(r =>r.RowIndex !=null && r.RowIndex > startRowIndex + i);
            
            if (refRow != null)
            {
                sheetData.InsertBefore(newRow, refRow);
            }
            else
            {
                sheetData.Append(newRow);
            }
        }
    }
    
    /// <summary>
    /// Extract "A", 5, "BC", 50 from Excel area reference such as A5:BC50
    /// </summary>
    /// <param name="range"> Excel are such as A5:Z20</param>
    /// <returns></returns>
    public static (string, int, string, int) ParseAreaReference(string range)
    {
        // Regex pattern to extract Excel range components
        // Pattern: (Column)(Row):(Column)(Row)
        var rangeRegex = new Regex(
            @"^([A-Z]+)(\d+):([A-Z]+)(\d+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        var match = rangeRegex.Match(range);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid Excel range format: {range}");
        }

        return  (match.Groups[1].Value.ToUpper(), int.Parse(match.Groups[2].Value),
							 match.Groups[3].Value.ToUpper(),  int.Parse(match.Groups[4].Value));
    }

    
    /// <summary>
    /// Extend table definition by number of rows
    /// </summary>
    /// <param name="t">table to be extended</param>
    /// <param name="numRows"></param>
    /// <exception cref="InternalServerErrorException"></exception>
    public static void ExtendTableRef( Table t, int numRows)
    {
        //Reference property contains the first and the last cells of the table such as A1:C100
        if (t.Reference != null)
        {
            //parse the original reference
            var (tableStartCol, tableStartRow, tableEndColumn, tableEndRow) =
                ParseAreaReference(t.Reference!);
            
            t.Reference = $"{tableStartCol}{tableStartRow}:{tableEndColumn}{tableEndRow+numRows}";
        }
        else
        {
            throw new InternalServerErrorException(ErrorCode.ArgumentNull.ToSnakeCase(),
                "ExtendTableRef table reference is null.");
        }
    }
    
    /// <summary>
    /// Return cell from row and column name, insert cell when it does not exist
    /// </summary>
    /// <param name="row"></param>
    /// <param name="columnName"></param>
    /// <returns></returns>
    public static Cell GetCell(Row row, string columnName)
    {
        var cellReference = columnName + row.RowIndex;
        var cell = row.Elements<Cell>()
            .FirstOrDefault(c => string.Equals(c.CellReference?.Value, cellReference,
                                StringComparison.Ordinal));
        if (cell == null)
        {
            cell = new Cell() { CellReference = cellReference };
        
            // Insert cell in correct position by comparing column indices
            var refCell = row.Elements<Cell>()
                .FirstOrDefault(c => c.CellReference?.Value != null && ColumnLetterToIndex(GetColumnName(c.CellReference.Value)) 
                                     > ColumnLetterToIndex(columnName));
        
            if (refCell != null)
            {
                row.InsertBefore(cell, refCell);
            }
            else
            {
                row.Append(cell);
            }
        }
    
        return cell;
    }
    
    /// <summary>
    /// Retrieve cell text value from a reference
    /// </summary>
    /// <param name="document"></param>
    /// <param name="sheetName"></param>
    /// <param name="cellReference"></param>
    /// <returns></returns>
    public static string? GetCellTextValue(
        SpreadsheetDocument document,
        string sheetName,
        string cellReference)
    {
        if (document?.WorkbookPart == null)
        {
            return null;
        }

        var worksheetPart = GetWorksheetPart(document, sheetName);

        if (worksheetPart?.Worksheet == null)
        {
            return null;
        }

        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        if (sheetData == null)
        {
            return null;
        }

        var (columnName, rowIndex) = ExcelUtils.ParseCellRef(cellReference);

        var row = GetRow(sheetData, rowIndex);
        var cell = GetCell(row, columnName);
        if (cell == null)
        {
            return null;
        }

        // Inline string
        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.Text?.Text;
        }

        // Shared string
        if (cell.DataType?.Value == CellValues.SharedString)
        {
            if (int.TryParse(cell.CellValue?.InnerText, out int sstIndex))
            {
                var sst = document.WorkbookPart.SharedStringTablePart?.SharedStringTable;
                var item = sst?.Elements<SharedStringItem>().ElementAtOrDefault(sstIndex);

                if (item?.Text != null)
                    return item.Text.Text;

                if (item != null)
                    return string.Concat(item.Descendants<Text>().Select(t => t.Text));
            }
            return null;
        }

        // Boolean
        if (cell.DataType?.Value == CellValues.Boolean)
            return cell.CellValue?.InnerText == "1" ? "TRUE" : "FALSE";

        // Numeric / raw / formula cached
        return cell.CellValue?.InnerText;
    }
}