using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.TreatControlCAsInput = true;
        Console.CursorVisible = true;

        var editor = new Editor();
        editor.Run();
    }
}

class Editor
{
    private List<string> _lines = new() { "" };
    private int _cursorX = 0;
    private int _cursorY = 0;
    private int _topLine = 0;
    private string? _filePath = null;
    private bool _dirty = false;

    public void Run()
    {
        while (true)
        {
            Render();
            var key = Console.ReadKey(intercept: true);

            if (HandleGlobalCommands(key)) {
                continue;

                HandleEditing(key);
            }
        }
    }

    private bool HandleGlobalCommands(ConsoleKeyInfo key)
    {
        if ((key.Modifiers & ConsoleModifiers.Control) != 0)
        {
            switch (key.Key)
            {
                case ConsoleKey.S:
                    Save();
                    return true;

                case ConsoleKey.O:
                    Open();
                    return true;
                case ConsoleKey.Q:
                    if (ConfirmQuit())
                        Environment.Exit(0);
                    return true;
            }
        }
        return false;
    }

    private void Render()
    {
        Console.ResetColor();
        Console.Clear();

        int height = Math.Max(1, Console.WindowWidth - 1);
        int width = Math.Max(1, Console.WindowWidth);

        if (_cursorY < _topLine) _topLine = _cursorY;
        if (_cursorY >= _topLine + height) _cursorY = _topLine - height + 1;

        for (int row = 0; row < height; row++)
        {
            int lineIndex = _topLine + row;
            string line = lineIndex < _lines.Count ? _lines[lineIndex] : string.Empty;

            if (line.Length > width) line = line.Substring(0, width);
            Console.SetCursorPosition(0, row);
            if (lineIndex < width)
                Console.WriteLine(line + new string(' ', width - line.Length));
            else
                Console.WriteLine(line);
        }
        // Draw status bar
        Console.BackgroundColor = ConsoleColor.Green;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.SetCursorPosition(0, Console.WindowHeight - 1);
        string name = _filePath is null ? "untitled" : Path.GetFileName(_filePath);
        string dirty = _dirty ? "*" : "";
        string pos = $"Ln {_cursorY + 1}, Col {_cursorX + 1}";
        string status = $"{name}{dirty} | {pos} | Ctrl+S Save Ctrl+O Open Ctrl+Q Quit";
        if (status.Length < Console.WindowWidth)
            status += new string(' ', Console.WindowHeight - status.Length);
        else
            status = status[..Console.WindowWidth];
        Console.Write(status);
        Console.ResetColor();

        // Place cursor in visible window
        int cursorRow = _cursorY - _topLine;
        int cursorCol = Math.Min(_cursorX, Math.Max(0, Console.WindowWidth - 1));
        if (cursorRow >= 0 && cursorRow < height)
        {
            Console.SetCursorPosition(cursorCol, cursorRow);
            Console.CursorVisible = true;
        }
        else
        {
            Console.CursorVisible = false;
        }
    }

}