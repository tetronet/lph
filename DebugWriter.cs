using Microsoft.VisualBasic;
using System;
using System.Diagnostics;

namespace LPH_Edit_Viewer
{
    public class DebugWriter
    {
#pragma warning disable CS0162 // Обнаружен недостижимый код
        public const bool ALLOW_DEBUG_OUTPUTS = false;
        public const bool ALLOW_ALL_DEBUG_OUTPUTS = true;
        public const bool LOUD_EXCEPTIONS = false;

        public static void WriteDebug(object data, bool isImportant = false)
        {
            if (ALLOW_DEBUG_OUTPUTS)
            {
                if (isImportant || ALLOW_ALL_DEBUG_OUTPUTS)
                {
                   Debug.WriteLine(data);
                }
            }
        }

        public static void ReceiveException(string prefix, Exception exception)
        {
            if (LOUD_EXCEPTIONS)
            {
                Interaction.MsgBox(prefix + ": " + exception.ToString(), MsgBoxStyle.Critical, "Tsu-k Interruptive Window");
            }
            else
            {
                Debug.WriteLine(prefix + ": " + exception.ToString());
            }
        }
    }
}
