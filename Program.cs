using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.FileIO;
using System.Xml.Serialization;

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

            if (HandleGlobalCommands(key))
            {
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

    private void HandEditing(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                MoveLeft();
                break;
            case ConsoleKey.RightArrow:
                MoveRight();
                break;
            case ConsoleKey.UpArrow:
                MoveUp();
                break;
            case ConsoleKey.DownArrow:
                MoveDown();
                break;
            case ConsoleKey.Home:
                _cursorX = 0;
                break;
            case ConsoleKey.End:
                _cursorX = CurrentLine().Length;
                break;
            case ConsoleKey.Enter:
                InsertNewLine();
                break;
            case ConsoleKey.Backspace:
                Delete();
                break;
            case ConsoleKey.Delete:
                Delete();
                break;
            case ConsoleKey.Tab:
                InsertText("    ");
                break;
            default:
                if (key.KeyChar >= ' ' && !char.IsControl(key.KeyChar))
                {
                    InsertText(key.KeyChar.ToString());
                }
                break;
        }
    }
    private string CurrentLine() => _lines[_cursorY];

    private void SetCurrentLine(string line)
    {
        _lines[_cursorY] = value;
        _dirty = true;
    }

    private void MoveLeft()
    {
        if (_cursorX > 0) _cursorX--;
        else if (_cursorY > 0)
        {
            _cursorY--;
            _cursorX = CurrentLine().Length;
        }
    }

    private void MoveRight()
    {
        var line = CurrentLine();
        if (_cursorX < line.Length) _cursorX++;
        else if (_cursorX < _lines.Count - 1)
        {
            _cursorY++;
            _cursorX = 0;
        }
    }

    private void MoveUp()
    {
        if (_cursorY > 0)
        {
            _cursorY--;
            _cursorX = Math.Min(_cursorX, CurrentLine().Length);
        }
    }

    private void MoveDown()
    {
        if (_cursorY < _lines.Count - 1)
        {
            _cursorY++;
            _cursorX = Math.Min(_cursorX, CurrentLine().Length);
        }
    }

    private void InsertText(string text)
    {
        var line = CurrentLine();
        line = line.Insert(_cursorX, text);
        SetCurrentLine(line);
        _cursorX += text.Length;
    }

    private void InsertNewLine()
    {
        var line = CurrentLine();
        string left = line.Substring(0, _cursorX);
        string right = line.Substring(_cursorX);

        _lines[_cursorY] = left;
        _lines.Insert(_cursorY + 1, right);
        _cursorY++;
        _cursorX = 0;
        _dirty = true;
    }

    private void Backspace()
    {
        if (_cursorX > 0)
        {
            var line = CurrentLine();
            line = line.Remove(_cursorX - 1, 1);
            SetCurrentLine(line);
            _cursorX--;
        }
        else if (_cursorY > 0)
        {
            //merge with previoous line
            int prevLen = _lines[_cursorY - 1].Length;
            _lines[_cursorY - 1] += CurrentLine();
            _lines.RemoveAt(_cursorY);
            _cursorY--;
            _cursorX = prevLen;
            _dirty = true;
        }

    }

    private void Delete()
    {
        var line = CurrentLine();
        if (_cursorX < line.Length)
        {
            line = line.Remove(_cursorX, 1);
            SetCurrentLine(line);
        }
        else if (_cursorY < _lines.Count - 1)
        {
            //merges with the next line
            _lines[_cursorY] += _lines[_cursorY + 1];
            _lines.RemoveAt(_cursorY + 1);
            _dirty = true;
        }
    }

    private bool ConfirmQuit()
    {
        if (!_dirty) return false;
        var respone = Prompt("Unsaved changes.\nQuit without saving? (y/N): ");
        return respone.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            var path = Prompt("Save as: ").Trim();
            if (string.IsNullOrEmpty(path)) return;
            _filePath = path;
        }

        try
        {
            File.WriteAllLines(_filePath!, _lines, Encoding.UTF8);
            _dirty = false;
            ShowMessage($"Saved to {_filePath}");
        }
        catch (Exception ex)
        {
            ShowMessage($"Save Failed: {ex.Message}");
        }
    }

    private void Open()
    {
        var path = Prompt("Open file: ").Trim;
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var lines = File.Exists(path)
                ? File.ReadAllLines(path, Encoding.UTF8).ToList()
                : new List<string> { "" };

            if (lines.Count == 0) lines.Add("");

            _lines = lines;
            _dirty = false;
            _cursorX = 0;
            _cursorY = 0;
            _topLine = 0;
            _filePath = path;
            ShowMessage($"Opened {path}");
        }
        catch (Exception ex)
        {
            ShowMessage($"Open failed: {ex.Message}");
        }
    }

    private string Prompt(string message)
    {
        //render once to keep things clean
        Render();

        Console.SetCursorPosition(0, Console.WindowHeight - 1);
        Console.BackgroundColor = ConsoleColor.DarkCyan;
        Console.ForegroundColor = ConsoleColor.White;
        string padded = message;
        if (padded.Length < Console.WindowWidth)
            padded += new string(' ', Console.WindowWidth - padded.Length);
        else
            padded = padded[..Console.WindowWidth];
        Console.Write(padded);
        Console.ResetColor();

        Console.SetCursorPosition(message.Length, Console.WindowHeight - 1);
        Console.CursorVisible = true;

        Console.ForegroundColor = ConsoleColor.White;
        var input = ReadLineInline(Console.WindowHeight - message.Length);
        Console.ResetColor();
        return input ?? "";
    }
}