﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace SiliconStudio.Presentation.Extensions
{
    public static class NativeHelper
    {
        #region Methods

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Unicode)]
        public static extern int SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessage", CharSet = CharSet.Unicode)]
        public static extern int PostMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(uint threadId, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int mCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hmonitor,   [In, Out] MONITORINFO monitorInfo);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        #endregion Methods

        #region Structures

        // ReSharper disable InconsistentNaming

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class MONITORINFO
        {
            public int cbSize = sizeof(int) * 10;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X, Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        #endregion Structures

        #region Constants

        public const int GWL_EXSTYLE = unchecked((int)0xFFFFFFEC);
        public const int GWL_HINSTANCE = unchecked((int)0xFFFFFFFA);
        public const int GWL_ID = unchecked((int)0xFFFFFFF4);
        public const int GWL_STYLE = unchecked((int)0xFFFFFFF0);
        public const int GWL_USERDATA = unchecked((int)0xFFFFFFEB);
        public const int GWL_WNDPROC = unchecked((int)0xFFFFFFFC);

        public const int MONITOR_DEFAULTTONULL = unchecked(0x00000000);
        public const int MONITOR_DEFAULTTOPRIMARY = unchecked(0x00000001);
        public const int MONITOR_DEFAULTTONEAREST = unchecked(0x00000002);

        // Window Styles - http://msdn.microsoft.com/en-us/library/windows/desktop/ms632600%28v=vs.85%29.aspx
        public const int WS_BORDER = unchecked(0x00800000);
        public const int WS_CAPTION = unchecked(0x00C00000);
        public const int WS_CHILD = unchecked(0x40000000);
        public const int WS_CHILDWINDOW = unchecked(0x40000000);
        public const int WS_CLIPCHILDREN = unchecked(0x02000000);
        public const int WS_CLIPSIBLINGS = unchecked(0x04000000);
        public const int WS_DISABLED = unchecked(0x08000000);
        public const int WS_DLGFRAME = unchecked(0x00400000);
        public const int WS_GROUP = unchecked(0x00020000);
        public const int WS_HSCROLL = unchecked(0x00100000);
        public const int WS_ICONIC = unchecked(0x20000000);
        public const int WS_MAXIMIZE = unchecked(0x01000000);
        public const int WS_MAXIMIZEBOX = unchecked(0x00010000);
        public const int WS_MINIMIZE = unchecked(0x20000000);
        public const int WS_MINIMIZEBOX = unchecked(0x00020000);
        public const int WS_OVERLAPPED = unchecked(0x00000000);
        public const int WS_OVERLAPPEDWINDOW = unchecked(WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_POPUPWINDOW = unchecked(WS_POPUP | WS_BORDER | WS_SYSMENU);
        public const int WS_SIZEBOX = unchecked(0x00040000);
        public const int WS_SYSMENU = unchecked(0x00080000);
        public const int WS_TABSTOP = unchecked(0x00010000);
        public const int WS_THICKFRAME = unchecked(0x00040000);
        public const int WS_TILED = unchecked(0x00000000);
        public const int WS_TILEDWINDOW = unchecked(WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
        public const int WS_VISIBLE = unchecked(0x10000000);
        public const int WS_VSCROLL = unchecked(0x00200000);

        public const int SW_HIDE = unchecked(0x00000000);

        // Windows messages - http://www.pinvoke.net/default.aspx/Constants/WM.html
        public const int WM_ACTIVATE = 0x0006;
        public const int WM_ACTIVATEAPP = 0x001C;
        public const int WM_AFXFIRST = 0x0360;
        public const int WM_AFXLAST = 0x037F;
        public const int WM_APP = 0x8000;
        public const int WM_ASKCBFORMATNAME = 0x030C;
        public const int WM_CANCELJOURNAL = 0x004B;
        public const int WM_CANCELMODE = 0x001F;
        public const int WM_CAPTURECHANGED = 0x0215;
        public const int WM_CHANGECBCHAIN = 0x030D;
        public const int WM_CHANGEUISTATE = 0x0127;
        public const int WM_CHAR = 0x0102;
        public const int WM_CHARTOITEM = 0x002F;
        public const int WM_CHILDACTIVATE = 0x0022;
        public const int WM_CLEAR = 0x0303;
        public const int WM_CLOSE = 0x0010;
        public const int WM_COMMAND = 0x0111;
        public const int WM_COMPACTING = 0x0041;
        public const int WM_COMPAREITEM = 0x0039;
        public const int WM_CONTEXTMENU = 0x007B;
        public const int WM_COPY = 0x0301;
        public const int WM_COPYDATA = 0x004A;
        public const int WM_CREATE = 0x0001;
        public const int WM_CTLCOLORBTN = 0x0135;
        public const int WM_CTLCOLORDLG = 0x0136;
        public const int WM_CTLCOLOREDIT = 0x0133;
        public const int WM_CTLCOLORLISTBOX = 0x0134;
        public const int WM_CTLCOLORMSGBOX = 0x0132;
        public const int WM_CTLCOLORSCROLLBAR = 0x0137;
        public const int WM_CTLCOLORSTATIC = 0x0138;
        public const int WM_CUT = 0x0300;
        public const int WM_DEADCHAR = 0x0103;
        public const int WM_DELETEITEM = 0x002D;
        public const int WM_DESTROY = 0x0002;
        public const int WM_DESTROYCLIPBOARD = 0x0307;
        public const int WM_DEVICECHANGE = 0x0219;
        public const int WM_DEVMODECHANGE = 0x001B;
        public const int WM_DISPLAYCHANGE = 0x007E;
        public const int WM_DRAWCLIPBOARD = 0x0308;
        public const int WM_DRAWITEM = 0x002B;
        public const int WM_DROPFILES = 0x0233;
        public const int WM_ENABLE = 0x000A;
        public const int WM_ENDSESSION = 0x0016;
        public const int WM_ENTERIDLE = 0x0121;
        public const int WM_ENTERMENULOOP = 0x0211;
        public const int WM_ENTERSIZEMOVE = 0x0231;
        public const int WM_ERASEBKGND = 0x0014;
        public const int WM_EXITMENULOOP = 0x0212;
        public const int WM_EXITSIZEMOVE = 0x0232;
        public const int WM_FONTCHANGE = 0x001D;
        public const int WM_GETDLGCODE = 0x0087;
        public const int WM_GETFONT = 0x0031;
        public const int WM_GETHOTKEY = 0x0033;
        public const int WM_GETICON = 0x007F;
        public const int WM_GETMINMAXINFO = 0x0024;
        public const int WM_GETOBJECT = 0x003D;
        public const int WM_GETTEXT = 0x000D;
        public const int WM_GETTEXTLENGTH = 0x000E;
        public const int WM_HANDHELDFIRST = 0x0358;
        public const int WM_HANDHELDLAST = 0x035F;
        public const int WM_HELP = 0x0053;
        public const int WM_HOTKEY = 0x0312;
        public const int WM_HSCROLL = 0x0114;
        public const int WM_HSCROLLCLIPBOARD = 0x030E;
        public const int WM_ICONERASEBKGND = 0x0027;
        public const int WM_IME_CHAR = 0x0286;
        public const int WM_IME_COMPOSITION = 0x010F;
        public const int WM_IME_COMPOSITIONFULL = 0x0284;
        public const int WM_IME_CONTROL = 0x0283;
        public const int WM_IME_ENDCOMPOSITION = 0x010E;
        public const int WM_IME_KEYDOWN = 0x0290;
        public const int WM_IME_KEYLAST = 0x010F;
        public const int WM_IME_KEYUP = 0x0291;
        public const int WM_IME_NOTIFY = 0x0282;
        public const int WM_IME_REQUEST = 0x0288;
        public const int WM_IME_SELECT = 0x0285;
        public const int WM_IME_SETCONTEXT = 0x0281;
        public const int WM_IME_STARTCOMPOSITION = 0x010D;
        public const int WM_INITDIALOG = 0x0110;
        public const int WM_INITMENU = 0x0116;
        public const int WM_INITMENUPOPUP = 0x0117;
        public const int WM_INPUTLANGCHANGE = 0x0051;
        public const int WM_INPUTLANGCHANGEREQUEST = 0x0050;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYFIRST = 0x0100;
        public const int WM_KEYLAST = 0x0108;
        public const int WM_KEYUP = 0x0101;
        public const int WM_KILLFOCUS = 0x0008;
        public const int WM_LBUTTONDBLCLK = 0x0203;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_MBUTTONDBLCLK = 0x0209;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_MDIACTIVATE = 0x0222;
        public const int WM_MDICASCADE = 0x0227;
        public const int WM_MDICREATE = 0x0220;
        public const int WM_MDIDESTROY = 0x0221;
        public const int WM_MDIGETACTIVE = 0x0229;
        public const int WM_MDIICONARRANGE = 0x0228;
        public const int WM_MDIMAXIMIZE = 0x0225;
        public const int WM_MDINEXT = 0x0224;
        public const int WM_MDIREFRESHMENU = 0x0234;
        public const int WM_MDIRESTORE = 0x0223;
        public const int WM_MDISETMENU = 0x0230;
        public const int WM_MDITILE = 0x0226;
        public const int WM_MEASUREITEM = 0x002C;
        public const int WM_MENUCHAR = 0x0120;
        public const int WM_MENUCOMMAND = 0x0126;
        public const int WM_MENUDRAG = 0x0123;
        public const int WM_MENUGETOBJECT = 0x0124;
        public const int WM_MENURBUTTONUP = 0x0122;
        public const int WM_MENUSELECT = 0x011F;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int WM_MOUSEFIRST = 0x0200;
        public const int WM_MOUSEHOVER = 0x02A1;
        public const int WM_MOUSELAST = 0x020D;
        public const int WM_MOUSELEAVE = 0x02A3;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_MOUSEHWHEEL = 0x020E;
        public const int WM_MOVE = 0x0003;
        public const int WM_MOVING = 0x0216;
        public const int WM_NCACTIVATE = 0x0086;
        public const int WM_NCCALCSIZE = 0x0083;
        public const int WM_NCCREATE = 0x0081;
        public const int WM_NCDESTROY = 0x0082;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_NCLBUTTONDBLCLK = 0x00A3;
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int WM_NCLBUTTONUP = 0x00A2;
        public const int WM_NCMBUTTONDBLCLK = 0x00A9;
        public const int WM_NCMBUTTONDOWN = 0x00A7;
        public const int WM_NCMBUTTONUP = 0x00A8;
        public const int WM_NCMOUSEHOVER = 0x02A0;
        public const int WM_NCMOUSELEAVE = 0x02A2;
        public const int WM_NCMOUSEMOVE = 0x00A0;
        public const int WM_NCPAINT = 0x0085;
        public const int WM_NCRBUTTONDBLCLK = 0x00A6;
        public const int WM_NCRBUTTONDOWN = 0x00A4;
        public const int WM_NCRBUTTONUP = 0x00A5;
        public const int WM_NCXBUTTONDBLCLK = 0x00AD;
        public const int WM_NCXBUTTONDOWN = 0x00AB;
        public const int WM_NCXBUTTONUP = 0x00AC;
        public const int WM_NCUAHDRAWCAPTION = 0x00AE;
        public const int WM_NCUAHDRAWFRAME = 0x00AF;
        public const int WM_NEXTDLGCTL = 0x0028;
        public const int WM_NEXTMENU = 0x0213;
        public const int WM_NOTIFY = 0x004E;
        public const int WM_NOTIFYFORMAT = 0x0055;
        public const int WM_NULL = 0x0000;
        public const int WM_PAINT = 0x000F;
        public const int WM_PAINTCLIPBOARD = 0x0309;
        public const int WM_PAINTICON = 0x0026;
        public const int WM_PALETTECHANGED = 0x0311;
        public const int WM_PALETTEISCHANGING = 0x0310;
        public const int WM_PARENTNOTIFY = 0x0210;
        public const int WM_PASTE = 0x0302;
        public const int WM_PENWINFIRST = 0x0380;
        public const int WM_PENWINLAST = 0x038F;
        public const int WM_POWER = 0x0048;
        public const int WM_POWERBROADCAST = 0x0218;
        public const int WM_PRINT = 0x0317;
        public const int WM_PRINTCLIENT = 0x0318;
        public const int WM_QUERYDRAGICON = 0x0037;
        public const int WM_QUERYENDSESSION = 0x0011;
        public const int WM_QUERYNEWPALETTE = 0x030F;
        public const int WM_QUERYOPEN = 0x0013;
        public const int WM_QUEUESYNC = 0x0023;
        public const int WM_QUIT = 0x0012;
        public const int WM_RBUTTONDBLCLK = 0x0206;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_RENDERALLFORMATS = 0x0306;
        public const int WM_RENDERFORMAT = 0x0305;
        public const int WM_SETCURSOR = 0x0020;
        public const int WM_SETFOCUS = 0x0007;
        public const int WM_SETFONT = 0x0030;
        public const int WM_SETHOTKEY = 0x0032;
        public const int WM_SETICON = 0x0080;
        public const int WM_SETREDRAW = 0x000B;
        public const int WM_SETTEXT = 0x000C;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int WM_SHOWWINDOW = 0x0018;
        public const int WM_SIZE = 0x0005;
        public const int WM_SIZECLIPBOARD = 0x030B;
        public const int WM_SIZING = 0x0214;
        public const int WM_SPOOLERSTATUS = 0x002A;
        public const int WM_STYLECHANGED = 0x007D;
        public const int WM_STYLECHANGING = 0x007C;
        public const int WM_SYNCPAINT = 0x0088;
        public const int WM_SYSCHAR = 0x0106;
        public const int WM_SYSCOLORCHANGE = 0x0015;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int WM_SYSDEADCHAR = 0x0107;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_TCARD = 0x0052;
        public const int WM_TIMECHANGE = 0x001E;
        public const int WM_TIMER = 0x0113;
        public const int WM_UNDO = 0x0304;
        public const int WM_UNINITMENUPOPUP = 0x0125;
        public const int WM_USER = 0x0400;
        public const int WM_USERCHANGED = 0x0054;
        public const int WM_VKEYTOITEM = 0x002E;
        public const int WM_VSCROLL = 0x0115;
        public const int WM_VSCROLLCLIPBOARD = 0x030A;
        public const int WM_WINDOWPOSCHANGED = 0x0047;
        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_WININICHANGE = 0x001A;
        public const int WM_XBUTTONDBLCLK = 0x020D;
        public const int WM_XBUTTONDOWN = 0x020B;
        public const int WM_XBUTTONUP = 0x020C;
        // ReSharper restore InconsistentNaming

        #endregion Constants

        public static bool SetCursorPos(Point pt)
        {
            return SetCursorPos((int)pt.X, (int)pt.Y);
        }
    }
}
#endif