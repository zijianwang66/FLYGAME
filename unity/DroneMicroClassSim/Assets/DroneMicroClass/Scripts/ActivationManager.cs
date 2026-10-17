using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DroneMicroClass
{
    public static class ActivationManager
    {
        private const string ActivationMachineKey = "DroneMicroClass.Activation.MachineCode";
        private const string ActivationCodeKey = "DroneMicroClass.Activation.Code";
        private const string ActivationSecret = "DroneMicroClass-Offline-Activation-2026-Qinglei";

        public static string MachineCode => FormatCode(HashText(GetMachineFingerprint()), 20);

        public static bool IsActivated()
        {
            string savedMachine = NormalizeCode(PlayerPrefs.GetString(ActivationMachineKey, string.Empty));
            string savedCode = NormalizeCode(PlayerPrefs.GetString(ActivationCodeKey, string.Empty));
            string currentMachine = NormalizeCode(MachineCode);

            return !string.IsNullOrEmpty(savedMachine)
                && savedMachine == currentMachine
                && savedCode == NormalizeCode(GenerateActivationCode(currentMachine));
        }

        public static bool TryActivate(string activationCode, out string message)
        {
            string normalizedInput = NormalizeCode(activationCode);
            if (string.IsNullOrEmpty(normalizedInput))
            {
                message = "请输入激活码。";
                return false;
            }

            string machineCode = MachineCode;
            string expected = NormalizeCode(GenerateActivationCode(machineCode));
            if (!FixedTimeEquals(normalizedInput, expected))
            {
                message = "激活码不正确，请核对机器码后重新生成。";
                return false;
            }

            PlayerPrefs.SetString(ActivationMachineKey, NormalizeCode(machineCode));
            PlayerPrefs.SetString(ActivationCodeKey, expected);
            PlayerPrefs.Save();

            message = "激活成功，正在进入系统...";
            return true;
        }

        public static string GenerateActivationCode(string machineCode)
        {
            string normalizedMachineCode = NormalizeCode(machineCode);
            return FormatCode(HashText(ActivationSecret + "|" + normalizedMachineCode), 24);
        }

        public static string NormalizeCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToUpperInvariant(c));
                }
            }

            return builder.ToString();
        }

        private static string GetMachineFingerprint()
        {
            string uniqueId = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrWhiteSpace(uniqueId) || string.Equals(uniqueId, SystemInfo.unsupportedIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                uniqueId = SystemInfo.deviceName + "|" + SystemInfo.processorType + "|" + SystemInfo.graphicsDeviceName;
            }

            return uniqueId + "|" + SystemInfo.operatingSystem + "|" + SystemInfo.systemMemorySize;
        }

        private static string HashText(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("X2"));
                }

                return builder.ToString();
            }
        }

        private static string FormatCode(string value, int length)
        {
            string normalized = NormalizeCode(value);
            if (normalized.Length > length)
            {
                normalized = normalized.Substring(0, length);
            }

            StringBuilder builder = new StringBuilder(normalized.Length + normalized.Length / 4);
            for (int i = 0; i < normalized.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                {
                    builder.Append('-');
                }

                builder.Append(normalized[i]);
            }

            return builder.ToString();
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }
}
