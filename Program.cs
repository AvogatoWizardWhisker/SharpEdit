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
    private List<string> _lines = new { "" };
    private int _cursorX = 0;
    private int _cursorY = 0;
    private int _topLine = 0;
    private string? _filePath = null;
    private bool _dirty = false;

    public void Run()
    {
        while(true)
        {
            Render();
            var key = Console.ReadKey(intercept: true);

            if (HandleGlobalCommands(key)) {
                continue;

                HandleEditing(key);
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