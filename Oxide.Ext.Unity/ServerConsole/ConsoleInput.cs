﻿using System;

using UnityEngine;

namespace Oxide.Ext.Unity.ServerConsole
{
    public class ConsoleInput
    {
        public string InputString = string.Empty;
        private float _nextUpdate;
        private Action<string> _onInputText;
        public string[] StatusText = {string.Empty, string.Empty, string.Empty};

        public int LineWidth
        {
            get { return Console.BufferWidth; }
        }

        public bool Valid
        {
            get { return Console.BufferWidth > 0; }
        }

        public void ClearLine(int numLines)
        {
            Console.CursorLeft = 0;
            Console.Write(new string(' ', LineWidth*numLines));
            Console.CursorTop = Console.CursorTop - numLines;
            Console.CursorLeft = 0;
        }

        public void RedrawInputLine()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorTop = Console.CursorTop + 1;
            for (var i = 0; i < StatusText.Length; i++)
            {
                Console.CursorLeft = 0;
                Console.Write(StatusText[i].PadRight(LineWidth));
            }
            Console.CursorTop = Console.CursorTop - (StatusText.Length + 1);
            Console.CursorLeft = 0;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            ClearLine(1);
            if (InputString.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            Console.Write(InputString.Length >= LineWidth - 2 ? InputString.Substring(InputString.Length - (LineWidth - 2)) : InputString);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void Update()
        {
            if (!Valid) return;
            if (_nextUpdate < Time.realtimeSinceStartup)
            {
                RedrawInputLine();
                _nextUpdate = Time.realtimeSinceStartup + 0.5f;
            }
            try
            {
                if (!Console.KeyAvailable)
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }
            var consoleKeyInfo = Console.ReadKey();
            switch (consoleKeyInfo.Key)
            {
                case ConsoleKey.Enter:
                    ClearLine(StatusText.Length);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Concat("> ", InputString));
                    var str = InputString;
                    InputString = string.Empty;
                    if (_onInputText != null) _onInputText(str);
                    RedrawInputLine();
                    return;
                case ConsoleKey.Backspace:
                    if (InputString.Length < 1) return;
                    InputString = InputString.Substring(0, InputString.Length - 1);
                    RedrawInputLine();
                    return;
                case ConsoleKey.Escape:
                    InputString = string.Empty;
                    RedrawInputLine();
                    return;
            }
            if (consoleKeyInfo.KeyChar == 0) return;
            InputString = string.Concat(InputString, consoleKeyInfo.KeyChar);
            RedrawInputLine();
        }

        public event Action<string> OnInputText
        {
            add { _onInputText += value; }
            remove { _onInputText -= value; }
        }
    }
}