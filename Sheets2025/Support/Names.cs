using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sheets
{
    internal static class Names
    {
        public static string NoPlotLayer { get; } = "NoPlotSupportLayer";
        public static string ExtentName { get; } = $"!_VP_Ex_";
        public static string LayoutCreateAppName { get; } = "LayoutApp";
        public static string ViewportLayerName { get; } = "!_Layouts";
        public static string BlockReferenceNumber { get; } = "Number";
        public static string BorderLayer { get; } = "!_SheetsBorder";
        public static string HeaderLayer { get; } = "!_SheetsHeader";
        public static string BackgroundSheetsLayer { get; } = "!_SheetsBackground";
        public static string SheetsLayer { get; } = "!_Sheets";
    }
}
