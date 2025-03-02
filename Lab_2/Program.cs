using System;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string inputZ = GetObjectiveFunction();
        List<string> constraints = GetConstraints();
        int variableCount = GetVariableCount();

        object[,] table = CreateSimplexTable(inputZ, constraints, variableCount);

        PrintConstraintsTable(table, constraints.Count, variableCount);

        Console.WriteLine("\nПочаткова симплекс-таблиця:");
        PrintTable(table);

        FindOptimalSolution(table);
    }

    // Функція для отримання функції Z від користувача
    static string GetObjectiveFunction()
    {
        Console.WriteLine("Введіть функцію Z у форматі ( x(і)... -> max):");
        return Console.ReadLine();
    }

    // Функція для отримання обмежень
    static List<string> GetConstraints()
    {
        Console.WriteLine("Введіть обмеження:");
        List<string> constraints = new List<string>();
        string line;
        while (!string.IsNullOrWhiteSpace(line = Console.ReadLine()))
        {
            constraints.Add(line);
        }
        return constraints;
    }

    // Функція для отримання кількості змінних
    static int GetVariableCount()
    {
        Console.WriteLine("Введіть кількість змінних:");
        return int.Parse(Console.ReadLine());
    }

    // Функція для створення симплекс-таблиці
    static object[,] CreateSimplexTable(string inputZ, List<string> constraints, int variableCount)
    {
        double[] Z = ParseFunction(inputZ, variableCount);
        double[,] table1 = new double[constraints.Count + 1, variableCount + 1];
        string[] rowLabels = new string[constraints.Count + 1];
        rowLabels[constraints.Count] = "Z=";

        for (int i = 0; i < constraints.Count; i++)
        {
            double[] constraintRow = ParseConstraint(constraints[i], variableCount);
            for (int j = 0; j < constraintRow.Length; j++)
            {
                table1[i, j] = constraintRow[j];
            }

            if (constraints[i].Contains(">="))
            {
                for (int j = 0; j < table1.GetLength(1); j++)
                {
                    table1[i, j] *= -1;
                }
            }

            rowLabels[i] = "y" + (i + 1) + "=";
        }

        for (int j = 0; j < variableCount; j++)
        {
            table1[constraints.Count, j] = Z[j];
        }

        NormalizeTableValues(table1);

        object[,] table = new object[table1.GetLength(0) + 1, table1.GetLength(1) + 1];
        table[0, 0] = "";
        for (int j = 0; j < variableCount; j++)
        {
            table[0, j + 1] = "-x" + (j + 1);
        }
        table[0, variableCount + 1] = "1";

        for (int i = 0; i < table1.GetLength(0); i++)
        {
            table[i + 1, 0] = rowLabels[i];
            for (int j = 0; j < table1.GetLength(1); j++)
            {
                table[i + 1, j + 1] = table1[i, j];
            }
        }

        return table;
    }

    // Функція для нормалізації значень таблиці (позбуваємося -0.00)
    static void NormalizeTableValues(double[,] table)
    {
        for (int i = 0; i < table.GetLength(0); i++)
        {
            for (int j = 0; j < table.GetLength(1); j++)
            {
                if (Math.Abs(table[i, j]) < 1e-10)
                {
                    table[i, j] = 0.00;
                }
            }
        }
    }

    // Функція для виводу системи обмежень
    static void PrintConstraintsTable(object[,] table, int constraintsCount, int variableCount)
    {
        Console.WriteLine("x[j] >= 0, j=1," + variableCount);
        Console.WriteLine("Перепишемо систему обмежень:");

        for (int i = 0; i < constraintsCount; i++)
        {
            bool firstElement = true;
            for (int j = 0; j < variableCount; j++)
            {
                double value = Convert.ToDouble(table[i + 1, j + 1]);

                if (firstElement)
                {
                    Console.Write($"{(Math.Abs(value) < 1e-10 ? "+0,00" : $"{value:F2}")} * X[{j + 1}] ");
                    firstElement = false;
                }
                else
                {
                    if (Math.Abs(value) < 1e-10)
                    {
                        Console.Write("+0,00 * X[{0}] ", j + 1);
                    }
                    else
                    {
                        Console.Write((value >= 0 ? "+" : "") + $"{value:F2} * X[{j + 1}] ");
                    }
                }
            }

            double freeTerm = Convert.ToDouble(table[i + 1, variableCount + 1]);
            Console.WriteLine($"{(freeTerm >= 0 ? "+" : "")}{freeTerm:F2} >= 0");
        }
    }

    // Функція для пошуку оптимального розв’язку
    static void FindOptimalSolution(object[,] table)
    {
        Console.WriteLine("\n--------Пошук опорного розв'язку--------");
        while (HasNegativeInLastColumn(table))
        {
            int pivotRow = FindPivotRow(table);
            int pivotColumn = FindPivotColumn(table, pivotRow);

            if (pivotRow == -1 || pivotColumn == -1)
            {
                Console.WriteLine("Неможливо знайти розв’язувальний елемент.");
                return;
            }

            Console.WriteLine($"\nРозв'язувальний стовпчик: {table[0, pivotColumn]}");
            Console.WriteLine($"Розв'язувальний рядочок: {table[pivotRow, 0].ToString().Replace("=", "").Trim()}");
            PerformJordanElimination(table, pivotRow, pivotColumn);
            PrintTable(table);
        }

        Console.WriteLine("Знайдено опорний розв'язок:");
        PrintSolution(table);

        object[,] newTable = new object[table.GetLength(0), table.GetLength(1)];
        CopyTable(table, newTable);

        Console.WriteLine("\n--------Пошук оптимального розв'язку--------\n");
        PrintTable(newTable);

        bool isUnbounded = FindAndPrintPivotRow(newTable);

        if (isUnbounded)
        {
            return;
        }

        Console.WriteLine("Знайдено оптимальний розв'язок:");
        PrintSolution(newTable);

        double lastElement = GetLastElementInLastRow(newTable);
        Console.WriteLine($"Max (Z) = {lastElement}");
    }

    static double[] ParseFunction(string input, int variableCount)
    {
        double[] coefficients = new double[variableCount];
        input = input.Split("->")[0].Replace(" ", "");

        for (int i = 1; i <= variableCount; i++)
        {
            var parts = input.Split(new string[] { $"x{i}" }, StringSplitOptions.None);
            string term = parts.Length > 1 ? parts[0] : "0";
            input = parts.Length > 1 ? parts[1] : input;
            coefficients[i - 1] = string.IsNullOrEmpty(term) || term == "+" ? 1 : term == "-" ? -1 : double.Parse(term);
        }
        return coefficients.Select(x => -x).ToArray(); 
    }

    static double[] ParseConstraint(string constraint, int variableCount)
    {
        double[] coefficients = new double[variableCount + 1];
        constraint = constraint.Replace(" ", "").Replace("<=", " ").Replace(">=", " ");
        string[] parts = constraint.Split(' ');

        for (int i = 1; i <= variableCount; i++)
        {
            var subParts = parts[0].Split(new string[] { $"x{i}" }, StringSplitOptions.None);
            string term = subParts.Length > 1 ? subParts[0] : "0";
            parts[0] = subParts.Length > 1 ? subParts[1] : parts[0];
            coefficients[i - 1] = string.IsNullOrEmpty(term) || term == "+" ? 1 : term == "-" ? -1 : double.Parse(term);
        }

        coefficients[variableCount] = double.Parse(parts[1]);
        if (constraint.Contains(">="))
        {
            coefficients = coefficients.Select(x => -x).ToArray();
        }
        return coefficients;
    }

    static double GetLastElementInLastRow(object[,] table)
    {
        int lastRow = table.GetLength(0) - 1;
        int lastCol = table.GetLength(1) - 1;

        if (table[lastRow, lastCol] is double)
        {
            return (double)table[lastRow, lastCol];
        }
        return Convert.ToDouble(table[lastRow, lastCol]); 
    }


    static bool FindAndPrintPivotRow(object[,] table)
    {
        while (true)
        {
            int pivotColumn = FindPivotColumnInZ(table);

            if (pivotColumn == -1)
            {
                Console.WriteLine("У рядку Z немає від’ємних елементів.");
                return false; 
            }

            Console.WriteLine($"Розв'язувальний стовпчик: {table[0, pivotColumn]}");

            int rowCount = table.GetLength(0) - 1;
            double[] ratios = new double[rowCount];
            for (int i = 1; i < rowCount; i++)
            {
                double denominator = Convert.ToDouble(table[i, pivotColumn]);
                if (denominator <= 0)
                {
                    ratios[i] = double.MaxValue;
                }
                else
                {
                    double numerator = Convert.ToDouble(table[i, table.GetLength(1) - 1]);
                    ratios[i] = numerator / denominator;
                }
            }

            double minRatio = double.MaxValue;
            int pivotRow = -1;
            for (int i = 1; i < rowCount; i++)
            {
                if (ratios[i] >= 0 && ratios[i] < minRatio)
                {
                    minRatio = ratios[i];
                    pivotRow = i;
                }
            }

            if (pivotRow == -1)
            {
                Console.WriteLine("Не знайдено розв'язувальний рядок. Цільова функція Z не обмежена зверху.");
                return true; 
            }

            Console.WriteLine($"Розв'язувальний рядочок: {table[pivotRow, 0].ToString().Replace("=", "").Trim()}");

            PerformJordanEliminationNewTable(table, pivotRow, pivotColumn);
            PrintTable(table);
        }
    }

    static int FindPivotColumnInZ(object[,] table)
    {
        int pivotColumn = -1;
        int rowZ = table.GetLength(0) - 1;  
        int lastColumn = table.GetLength(1) - 2; 

        for (int j = 1; j <= lastColumn; j++) 
        {
            if (Convert.ToDouble(table[rowZ, j]) < 0)
            {
                pivotColumn = j;
                break;  
            }
        }

        return pivotColumn;
    }

    static void PerformJordanEliminationNewTable(object[,] table, int pivotRow, int pivotColumn)
    {
        int rowCount = table.GetLength(0);
        int colCount = table.GetLength(1);
        double pivotElement = Convert.ToDouble(table[pivotRow, pivotColumn]);
        Console.WriteLine($"Розв'язувальний елемент:  {Convert.ToDouble(pivotElement):F2}");

        table[pivotRow, pivotColumn] = 1.0;

        for (int i = 1; i < rowCount; i++)
        {
            for (int j = 1; j < colCount; j++)
            {
                if (i != pivotRow && j != pivotColumn)
                {
                    table[i, j] = (Convert.ToDouble(table[i, j]) * pivotElement - Convert.ToDouble(table[i, pivotColumn]) * Convert.ToDouble(table[pivotRow, j])) / pivotElement;
                }
            }
        }

        for (int i = 1; i < rowCount; i++)
        {
            if (i != pivotRow)
            {
                table[i, pivotColumn] = -Convert.ToDouble(table[i, pivotColumn]);
            }
        }

        for (int i = 1; i < rowCount; i++)
        {
            if (i != pivotRow)
            {
                table[i, pivotColumn] = Convert.ToDouble(table[i, pivotColumn]) / pivotElement;
            }
        }

        for (int j = 1; j < colCount; j++)
        {
            table[pivotRow, j] = Convert.ToDouble(table[pivotRow, j]) / pivotElement;
        }

        double[] pivotColumnValues = new double[rowCount];
        for (int i = 1; i < rowCount; i++)
        {
            pivotColumnValues[i] = Convert.ToDouble(table[i, pivotColumn]);
        }

        SwapNames(table, pivotRow, pivotColumn);
    }

    static void CopyTable(object[,] source, object[,] destination)
    {
        for (int i = 0; i < source.GetLength(0); i++)
        {
            for (int j = 0; j < source.GetLength(1); j++)
            {
                destination[i, j] = source[i, j];
            }
        }
    }

    static void PrintSolution(object[,] table)
    {
        int variableCount = table.GetLength(1) - 2;
        double[] solution = new double[variableCount];

        for (int i = 1; i < table.GetLength(0) - 1; i++)
        {
            string rowName = table[i, 0].ToString();
            if (rowName.StartsWith("x") && rowName.Contains("="))
            {
                rowName = rowName.Replace("=", "").Trim();

                int index = int.Parse(rowName.Substring(1)) - 1;
                solution[index] = Convert.ToDouble(table[i, table.GetLength(1) - 1]);
            }
        }

        Console.Write("X(");
        for (int i = 0; i < solution.Length; i++)
        {
            Console.Write(solution[i]);
            if (i < solution.Length - 1) Console.Write("; ");
        }
        Console.WriteLine(")");
    }

    static bool HasNegativeInLastColumn(object[,] table)
    {
        for (int i = 1; i < table.GetLength(0) - 1; i++)
        {
            if (Convert.ToDouble(table[i, table.GetLength(1) - 1]) < 0)
                return true;
        }
        return false;
    }

    static int FindPivotRow(object[,] table)
    {
        int rowCount = table.GetLength(0) - 1;
        int pivotRow = -1;
        for (int i = 1; i < rowCount; i++)
        {
            if (Convert.ToDouble(table[i, table.GetLength(1) - 1]) < 0)
            {
                pivotRow = i;
                break;
            }
        }
        return pivotRow;
    }

    static int FindPivotColumn(object[,] table, int pivotRow)
    {
        for (int j = 1; j < table.GetLength(1) - 1; j++)
        {
            if (Convert.ToDouble(table[pivotRow, j]) < 0)
                return j;
        }
        return -1;
    }

    static void PerformJordanElimination(object[,] table, int pivotRow, int pivotColumn)
    {
        int rowCount = table.GetLength(0);
        int colCount = table.GetLength(1);
        double pivotElement = Convert.ToDouble(table[pivotRow, pivotColumn]);
        Console.WriteLine($"Розв'язувальний елемент: {Convert.ToDouble(pivotElement):F2}");

        table[pivotRow, pivotColumn] = 1.0;

        for (int i = 1; i < rowCount; i++)
        {
            if (i != pivotRow)
            {
                table[i, pivotColumn] = -Convert.ToDouble(table[i, pivotColumn]);
            }
        }

        for (int i = 1; i < rowCount; i++)
        {
            if (i != pivotRow)
            {
                table[i, pivotColumn] = Convert.ToDouble(table[i, pivotColumn]) / pivotElement;
            }
        }

        for (int j = 1; j < colCount; j++)
        {
            table[pivotRow, j] = Convert.ToDouble(table[pivotRow, j]) / pivotElement;
        }

        double[] pivotColumnValues = new double[rowCount];
        for (int i = 1; i < rowCount; i++)
        {
            pivotColumnValues[i] = Convert.ToDouble(table[i, pivotColumn]);
        }

        for (int i = 1; i < rowCount; i++)
        {
            if (i == pivotRow) continue;
            double factor = pivotColumnValues[i];
            for (int j = 1; j < colCount; j++)
            {
                if (j == pivotColumn)
                    table[i, j] = pivotColumnValues[i];
                else
                    table[i, j] = Convert.ToDouble(table[i, j]) - factor * Convert.ToDouble(table[pivotRow, j]);
            }
        }

        SwapNames(table, pivotRow, pivotColumn);
    }

    static void SwapNames(object[,] table, int pivotRow, int pivotColumn)
    {
        string columnName = table[0, pivotColumn].ToString();
        string rowName = table[pivotRow, 0].ToString();

        string cleanedColumnName = columnName.StartsWith("-") ? columnName.Substring(1) : columnName;
        string cleanedRowName = rowName.StartsWith("-") ? rowName.Substring(1) : rowName;

        bool rowHasEqual = cleanedRowName.Contains("=");

        if (rowHasEqual)
        {
            cleanedRowName = cleanedRowName.Replace("=", "").Trim(); 
        }

        table[0, pivotColumn] = "-" + cleanedRowName; 
        table[pivotRow, 0] = cleanedColumnName + (rowHasEqual ? "=" : "");
    }

    static void PrintTable(object[,] table)
    {
        int rows = table.GetLength(0);
        int cols = table.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (table[i, j] is double)
                {
                    double value = Convert.ToDouble(table[i, j]);
                    string formattedValue = value == -0.00 ? "0.00" : value.ToString("F2");
                    Console.Write($"{formattedValue}\t");
                }
                else
                {
                    Console.Write($"{table[i, j]}\t");
                }
            }
            Console.WriteLine();
        }
    }



}
